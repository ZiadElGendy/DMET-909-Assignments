using UnityEngine;
using System.Runtime.InteropServices;
using Obvious.Soap;

public class NoteDetectionManager : Singleton<NoteDetectionManager>
{
    // Sample rate used for recording and analysis
    public int sampleRate = 44100;

    // Number of samples in the analysis window for the YIN pitch detection
    public int windowSize = 4096;

    // Threshold used by the YIN algorithm to decide a valid pitch
    public float yinThreshold = 0.15f;

    // Attack threshold: detects sudden increases in amplitude (impulses/note attacks)
    [Range(0f, 1f)]
    public float attackThreshold = 0.1f;

    // Noise gate threshold: minimum RMS level required to run pitch detection
    [Range(0f, 1f)]
    public float noiseGateThreshold = 0.02f;

    // Number of samples to look back for attack detection
    public int attackLookback = 512;

    // The last detected fundamental frequency in Hz
    public float detectedFrequency;

    // The last detected MIDI note number corresponding to detectedFrequency
    public int detectedMidi;

    // The last detected note name corresponding to detectedMidi
    public string detectedNote;

    // The DSP timestamp (AudioSettings.dspTime) when the last valid note was detected
    public double lastNoteDspTime { get; private set; } = -1.0;

    // Circular buffer storing recent audio samples to feed into YIN
    float[] circularBuffer;

    // Current write index within the circularBuffer
    int head;

    // FMOD Sound object used as the recording target buffer
    FMOD.Sound sound;

    // Reference to the FMOD core system instance (FMOD.System)
    FMOD.System coreSystem;

    // Index of the selected recording device/driver
    int recordDriverIndex = 0;

    // The position of the last processed sample in the FMOD recording buffer
    uint lastRecordPos = 0;

    // Temporary read buffer used when copying samples out of the FMOD sound
    float[] readBuffer;

    bool isRecording = false;

    public bool isDebugGUIVisible = false;

    // Event raised when a note is detected (passes MIDI note number)
    public ScriptableEventInt onNotePlayedDetected;

    // Track previous RMS for attack detection
    private float previousRMS = 0f;

    public PlayerSettings settings;

    void Start()
    {
        circularBuffer = new float[sampleRate * 2];
        readBuffer = new float[8192];
        recordDriverIndex = settings.selectedAudioDeviceIndex;

        InitializeFMOD();
    }

    void InitializeFMOD()
    {
        coreSystem = FMODUnity.RuntimeManager.CoreSystem;

        if (coreSystem.hasHandle())
        {
            int numDrivers = 0;
            int numConnected = 0;
            coreSystem.getRecordNumDrivers(out numDrivers, out numConnected);
            Debug.Log($"FMOD: Found {numConnected} recording devices");
            SelectDevice(recordDriverIndex);
        }
    }

    public void SelectDevice(int deviceIndex)
    {
        if (isRecording && recordDriverIndex == deviceIndex) return;

        recordDriverIndex = deviceIndex;
        StartMicrophone();
    }

    void StartMicrophone()
    {
        StopRecording();

        FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();
        exinfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        exinfo.numchannels = 1;
        exinfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
        exinfo.defaultfrequency = sampleRate;
        exinfo.length = (uint)(sampleRate * sizeof(float) * 5);

        FMOD.RESULT result = coreSystem.createSound(
            string.Empty,
            FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER,
            ref exinfo,
            out sound
        );

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"FMOD: Failed to create sound: {result}");
            return;
        }

        result = coreSystem.recordStart(recordDriverIndex, sound, true);

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"FMOD: Failed to start recording: {result}");
            sound.release();
            return;
        }

        isRecording = true;
        lastRecordPos = 0;
        previousRMS = 0f;

        Debug.Log($"FMOD: Started recording on device {recordDriverIndex}");
    }

    void StopRecording()
    {
        if (isRecording)
        {
            coreSystem.recordStop(recordDriverIndex);
            isRecording = false;
        }

        if (sound.hasHandle())
        {
            sound.release();
            sound.clearHandle();
        }
    }

    void Update()
    {
        if (!isRecording || !sound.hasHandle())
            return;

        uint recordPos = 0;
        FMOD.RESULT result = coreSystem.getRecordPosition(recordDriverIndex, out recordPos);

        if (result != FMOD.RESULT.OK)
            return;

        uint soundLength = 0;
        sound.getLength(out soundLength, FMOD.TIMEUNIT.PCM);

        int samplesToRead = 0;
        if (recordPos >= lastRecordPos)
        {
            samplesToRead = (int)(recordPos - lastRecordPos);
        }
        else
        {
            samplesToRead = (int)((soundLength - lastRecordPos) + recordPos);
        }

        if (samplesToRead > 0)
        {
            ProcessNewSamples(lastRecordPos, samplesToRead, soundLength);
            lastRecordPos = recordPos;
        }

        // If we have enough samples in the circular buffer for analysis
        if (head >= windowSize)
        {
            // Extract the latest windowSize samples from the circular buffer
            float[] window = new float[windowSize];
            int start = head - windowSize;
            if (start < 0) start += circularBuffer.Length;

            for (int i = 0; i < windowSize; i++)
                window[i] = circularBuffer[(start + i) % circularBuffer.Length];

            // Calculate RMS (Root Mean Square) for noise gate
            float rms = CalculateRMS(window);

            // Check if signal passes noise gate threshold
            if (rms >= noiseGateThreshold)
            {
                // Check for attack (sudden increase in amplitude)
                bool isAttack = DetectAttack(rms);

                // Only run pitch detection on attacks or if RMS is significantly high
                if (isAttack || rms > noiseGateThreshold * 2f)
                {
                    float f = YIN(window, sampleRate, yinThreshold);

                    if (f > 0f && f < 2000f) // Valid frequency range (filter out spurious detections)
                    {
                        detectedFrequency = f;
                        int newMidi = FrequencyToMIDI(f);

                        // Only update and trigger event if it's a new note or on an attack
                        if (isAttack || newMidi != detectedMidi)
                        {
                            detectedMidi = newMidi;
                            detectedNote = MidiToName(detectedMidi);
                            lastNoteDspTime = AudioSettings.dspTime;

                            // Raise event for detected note
                            onNotePlayedDetected?.Raise(detectedMidi);
                            Debug.Log($"Note Detected: {detectedNote} (MIDI: {detectedMidi}, Freq: {f:F1} Hz, RMS: {rms:F3})");
                        }
                    }
                }
            }

            // Update previous RMS for next attack detection
            previousRMS = rms;
        }
    }

    /// <summary>
    /// Calculate Root Mean Square (RMS) amplitude of the buffer
    /// </summary>
    float CalculateRMS(float[] buffer)
    {
        float sum = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            sum += buffer[i] * buffer[i];
        }
        return Mathf.Sqrt(sum / buffer.Length);
    }

    /// <summary>
    /// Detect if there's a sudden increase in amplitude (note attack/impulse)
    /// </summary>
    bool DetectAttack(float currentRMS)
    {
        // Attack detected if current RMS is significantly higher than previous RMS
        float increase = currentRMS - previousRMS;
        return increase > attackThreshold && currentRMS > noiseGateThreshold;
    }

    void ProcessNewSamples(uint startPos, int numSamples, uint soundLength)
    {
        int samplesProcessed = 0;

        while (samplesProcessed < numSamples)
        {
            int samplesToReadNow = Mathf.Min(readBuffer.Length, numSamples - samplesProcessed);
            uint currentPos = (startPos + (uint)samplesProcessed) % soundLength;

            if (currentPos + samplesToReadNow > soundLength)
            {
                samplesToReadNow = (int)(soundLength - currentPos);
            }

            System.IntPtr ptr1, ptr2;
            uint len1, len2;

            FMOD.RESULT result = sound.@lock(
                currentPos * sizeof(float),
                (uint)(samplesToReadNow * sizeof(float)),
                out ptr1, out ptr2,
                out len1, out len2
            );

            if (result == FMOD.RESULT.OK)
            {
                int samples1 = (int)(len1 / sizeof(float));

                if (samples1 > 0)
                {
                    Marshal.Copy(ptr1, readBuffer, 0, samples1);

                    for (int i = 0; i < samples1; i++)
                    {
                        circularBuffer[head] = readBuffer[i];
                        head = (head + 1) % circularBuffer.Length;
                    }
                }

                if (ptr2 != System.IntPtr.Zero && len2 > 0)
                {
                    int samples2 = (int)(len2 / sizeof(float));
                    Marshal.Copy(ptr2, readBuffer, 0, samples2);

                    for (int i = 0; i < samples2; i++)
                    {
                        circularBuffer[head] = readBuffer[i];
                        head = (head + 1) % circularBuffer.Length;
                    }
                }

                sound.unlock(ptr1, ptr2, len1, len2);
                samplesProcessed += samples1 + (int)(len2 / sizeof(float));
            }
            else
            {
                break;
            }
        }
    }

    void OnGUI()
    {
        if (!isDebugGUIVisible) return;
        GUILayout.BeginVertical("box");
        GUILayout.Label("Select Microphone:");

        if (coreSystem.hasHandle())
        {
            int numDrivers = 0;
            int numConnected = 0;
            coreSystem.getRecordNumDrivers(out numDrivers, out numConnected);

            for (int i = 0; i < numConnected; i++)
            {
                string name;
                System.Guid guid;
                int systemRate;
                FMOD.SPEAKERMODE speakerMode;
                int speakerModeChannels;
                FMOD.DRIVER_STATE driverState;

                coreSystem.getRecordDriverInfo(
                    i,
                    out name,
                    64,
                    out guid,
                    out systemRate,
                    out speakerMode,
                    out speakerModeChannels,
                    out driverState
                );

                string buttonLabel = recordDriverIndex == i && isRecording
                    ? $"● {name} (Recording)"
                    : name;

                if (GUILayout.Button(buttonLabel))
                    SelectDevice(i);
            }
        }

        GUILayout.Space(10);

        if (isRecording)
        {
            GUILayout.Label("Status: Recording");
            GUILayout.Label("Frequency: " + detectedFrequency.ToString("F1") + " Hz");
            GUILayout.Label("MIDI: " + detectedMidi);
            GUILayout.Label("Note: " + detectedNote);

            GUILayout.Space(5);
            GUILayout.Label($"Attack Threshold: {attackThreshold:F2}");
            GUILayout.Label($"Noise Gate: {noiseGateThreshold:F2}");
        }
        else
        {
            GUILayout.Label("Status: Not Recording - Select a device");
        }

        GUILayout.EndVertical();
    }

    void OnDestroy()
    {
        StopRecording();
    }

    int FrequencyToMIDI(float f)
    {
        if (f <= 0f) return -1;
        float cents = 1200f * Mathf.Log(f / 440f, 2f);
        float m = 69f + (cents / 100f);
        return Mathf.Clamp(Mathf.RoundToInt(m), 0, 127);
    }

    static readonly string[] noteNames =
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    string MidiToName(int midi)
    {
        if (midi < 0) return "";
        int note = midi % 12;
        int octave = (midi / 12) - 1;
        return noteNames[note] + octave;
    }

    float YIN(float[] buffer, int sr, float threshold)
    {
        int size = buffer.Length;
        int half = size / 2;

        float[] diff = new float[half];
        for (int tau = 1; tau < half; tau++)
        {
            float sum = 0f;
            for (int i = 0; i < half; i++)
            {
                float d = buffer[i] - buffer[i + tau];
                sum += d * d;
            }
            diff[tau] = sum;
        }

        float[] cmnd = new float[half];
        float running = 0f;
        for (int tau = 1; tau < half; tau++)
        {
            running += diff[tau];
            cmnd[tau] = running > 0f ? diff[tau] * tau / running : 0f;
        }

        int tauEstimate = -1;
        for (int tau = 2; tau < half; tau++)
        {
            if (cmnd[tau] < threshold)
            {
                while (tau + 1 < half && cmnd[tau + 1] < cmnd[tau])
                    tau++;
                tauEstimate = tau;
                break;
            }
        }

        if (tauEstimate == -1) return -1f;

        float betterTau = tauEstimate;
        if (tauEstimate > 1 && tauEstimate < half - 1)
        {
            float alpha = cmnd[tauEstimate - 1];
            float beta = cmnd[tauEstimate];
            float gamma = cmnd[tauEstimate + 1];

            float denominator = 2f * (alpha - 2f * beta + gamma);
            if (Mathf.Abs(denominator) > 1e-6f)
            {
                betterTau = tauEstimate + (alpha - gamma) / denominator;
            }
        }

        return sr / betterTau;
    }
}