using UnityEngine;
using System.Runtime.InteropServices;

public class NoteDetection : Singleton<NoteDetection>
{
    // Sample rate used for recording and analysis
    public int sampleRate = 44100;

    // Number of samples in the  analysis window for the YIN pitch detection
    public int windowSize = 4096;

    // Threshold used by the YIN algorithm to decide a valid pitch
    public float yinThreshold = 0.15f;

    // The last detected fundamental frequency in Hz
    public float detectedFrequency;

    // The last detected MIDI note number corresponding to detectedFrequency
    public int detectedMidi;

    // The last detected note name corresponding to detectedMidi
    public string detectedNote;

    // The DSP timestamp (AudioSettings.dspTime) when the last valid note was detected
    public double lastNoteDspTime { get; private set; } = -1.0;

    // Circular buffer storing recent audio samples to feed into YIN
    // Necessary to use because FMOD recording buffer is constantly being overwritten, making it difficult to handle in code
    float[] circularBuffer;

    // Current write index within the circularBuffer
    int head;

    // FMOD Sound object used as the recording target buffer
    FMOD.Sound sound;

    // Reference to the FMOD core system instance (FMOD.System)
    FMOD.System coreSystem;

    // Index of the selected recording device/driver (used with FMOD record APIs)
    int recordDriverIndex = 0;

    // The position of the last processed sample in the FMOD recording buffer.
    uint lastRecordPos = 0;

    // Temporary read buffer used when copying samples out of the FMOD sound into the circular buffer
    float[] readBuffer;

    bool isRecording = false;

    void Start()
    {
        circularBuffer = new float[sampleRate * 2];
        readBuffer = new float[8192];
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

        // FMOD boilerplate for recording sound from a microphone
        // Parameters for sound setup
        FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();
        exinfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        exinfo.numchannels = 1;
        exinfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
        exinfo.defaultfrequency = sampleRate;
        exinfo.length = (uint)(sampleRate * sizeof(float) * 5);

        // Create a sound object to record into
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

        // Start recording from the selected recording device into the sound object
        result = coreSystem.recordStart(recordDriverIndex, sound, true);

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"FMOD: Failed to start recording: {result}");
            sound.release();
            return;
        }

        isRecording = true;
        lastRecordPos = 0;

        Debug.Log($"FMOD: Started recording on device {recordDriverIndex}");
    }

    void StopRecording()
    {
        //FMOD cleanup
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

        // Get current recording position
        // imagine a vinyl player, the recordPos is where the needle currently is
        uint recordPos = 0;
        FMOD.RESULT result = coreSystem.getRecordPosition(recordDriverIndex, out recordPos);

        if (result != FMOD.RESULT.OK)
            return;

        uint soundLength = 0;
        // Get the total length of the sound buffer to use for the case where the needle wraps around to the beginning of the buffer
        sound.getLength(out soundLength, FMOD.TIMEUNIT.PCM);

        // Determine how many new samples have been recorded since last update
        int samplesToRead = 0;
        if (recordPos >= lastRecordPos)
        {
            // Simple case: needle has moved forward
            samplesToRead = (int)(recordPos - lastRecordPos);
        }
        else
        {
            // Wrapped case: needle has looped back to start
            samplesToRead = (int)((soundLength - lastRecordPos) + recordPos);
        }

        if (samplesToRead > 0)
        {
            ProcessNewSamples(lastRecordPos, samplesToRead, soundLength);
            lastRecordPos = recordPos;
        }

        // If we have enough samples in the circular buffer for a full YIN window, run YIN pitch detection
        if (head >= windowSize)
        {
            // Extract the latest windowSize samples from the circular buffer
            float[] window = new float[windowSize];
            int start = head - windowSize;
            if (start < 0) start += circularBuffer.Length;

            for (int i = 0; i < windowSize; i++)
                window[i] = circularBuffer[(start + i) % circularBuffer.Length];

            //
            float f = YIN(window, sampleRate, yinThreshold);
            if (f > 0f)
            {
                detectedFrequency = f;
                detectedMidi = FrequencyToMIDI(f);
                detectedNote = MidiToName(detectedMidi);
                // record the DSP timestamp of this detection so timing logic can be applied outside
                lastNoteDspTime = AudioSettings.dspTime;
            }
        }
    }

    /// <summary>
    /// Copies new samples from the FMOD sound buffer into the circular buffer
    /// </summary>
    /// <param name="startPos">The start position of unprocessed samples</param>
    /// <param name="numSamples">The number of samples to be processed </param>
    /// <param name="soundLength">The length of the FMOD sound buffer used</param>
    void ProcessNewSamples(uint startPos, int numSamples, uint soundLength)
    {
        int samplesProcessed = 0;

        while (samplesProcessed < numSamples)
        {
            // Load samples in chunks that fit into the read buffer
            // Read buffer is used to handle wrapping and partial reads before copying into circular buffer
            int samplesToReadNow = Mathf.Min(readBuffer.Length, numSamples - samplesProcessed);
            // Calculate current position in the sound buffer, wrapping around if necessary
            uint currentPos = (startPos + (uint)samplesProcessed) % soundLength;

            // Adjust samples to read if we would exceed the sound length
            if (currentPos + samplesToReadNow > soundLength)
            {
                samplesToReadNow = (int)(soundLength - currentPos);
            }

            // Lock a section of the sound buffer to read samples
            // ptr1 and len1 define the first contiguous block of samples
            // ptr2 and len2 define the second block in case of wrap-around
            System.IntPtr ptr1, ptr2;
            uint len1, len2;

            FMOD.RESULT result = sound.@lock(
                currentPos * sizeof(float),
                (uint)(samplesToReadNow * sizeof(float)),
                out ptr1, out ptr2,
                out len1, out len2
            );

            // Copy samples from the locked sound buffer into our circular buffer
            if (result == FMOD.RESULT.OK)
            {
                int samples1 = (int)(len1 / sizeof(float));

                // Copy first chunk
                if (samples1 > 0)
                {
                    Marshal.Copy(ptr1, readBuffer, 0, samples1);

                    for (int i = 0; i < samples1; i++)
                    {
                        circularBuffer[head] = readBuffer[i];
                        head = (head + 1) % circularBuffer.Length;
                    }
                }

                // Copy second chunk if it exists (wrap-around case)
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
        // MIDI note number calculation formula
        // Uses cents calculation formula (f x m^12 = 2f)
        // then converts it to midi domain(a4 = midi 69)
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

    /// <summary>
    /// Implements the YIN pitch detection algorithm on a buffer of audio samples.
    /// Returns the estimated fundamental frequency in Hz, or -1f if no pitch is found.
    /// </summary>
    /// <param name="buffer">Audio samples to be analyzed</param>
    /// <param name="sr">Sample rate in Hz</param>
    /// <param name="threshold">Decision threshold for the CMND function</param>
    /// <returns>Estimated frequency in Hz, or -1f if no reliable pitch was detected.</returns>
    float YIN(float[] buffer, int sr, float threshold)
    {
        // The idea of the YIN algorithm is to find the period of periodic waveforms by measuring similarity
        // A periodic waveform will be most similar to its self when we time-shift (lag) it by its period
        // The algorithm therefore computes a difference function over various lags (tau values) to get a period estimate,
        // then converts that to frequency.


        // Buffer length and half-size used by the algorithm (only compute up to half for tau values)
        int size = buffer.Length;
        int half = size / 2;

        // Difference function d(tau)
        // diff[tau] = sum_{i=0}^{half-1} (buffer[i] - buffer[i+tau])^2
        // This measures how similar the signal is to itself at different lags
        float[] diff = new float[half];
        for (int tau = 1; tau < half; tau++)
        {
            float sum = 0f;
            // accumulate squared differences for the given lag (tau)
            for (int i = 0; i < half; i++)
            {
                float d = buffer[i] - buffer[i + tau];
                sum += d * d;
            }
            diff[tau] = sum;
        }

        // Cumulative Mean Normalized Difference (CMND)
        // cmnd[tau] = diff[tau] * tau / sum_{j=1..tau} diff[j]
        // This normalizes the difference function to make minima detection easier.
        float[] cmnd = new float[half];
        float running = 0f;
        for (int tau = 1; tau < half; tau++)
        {
            running += diff[tau];
            // protect against division by zero — if running == 0, cmnd will remain 0
            cmnd[tau] = running > 0f ? diff[tau] * tau / running : 0f;
        }

        // Absolute threshold: find the first tau where cmnd[tau] < threshold
        // then refine by searching for the local minimum following that crossing
        int tauEstimate = -1;
        for (int tau = 2; tau < half; tau++)
        {
            if (cmnd[tau] < threshold)
            {
                // refine: find local minimum (walk forward while values keep decreasing)
                while (tau + 1 < half && cmnd[tau + 1] < cmnd[tau])
                    tau++;
                tauEstimate = tau;
                break;
            }
        }

        // If no estimate found, return -1 to indicate failure
        if (tauEstimate == -1) return -1f;

        // Parabolic interpolation to refine the tau estimate
        // betterTau = tau + (cmnd[tau-1] - cmnd[tau+1]) / (2 * (cmnd[tau-1] - 2*cmnd[tau] + cmnd[tau+1]))
        // This gives a more precise estimate of the period by fitting a parabola to the points around the minimum
        // and finding its vertex.
        float betterTau = tauEstimate;
        if (tauEstimate > 1 && tauEstimate < half - 1)
        {
            float alpha = cmnd[tauEstimate - 1];
            float beta = cmnd[tauEstimate];
            float gamma = cmnd[tauEstimate + 1];

            float denominator = 2f * (alpha - 2f * beta + gamma);
            if (Mathf.Abs(denominator) > 1e-6f) // avoid division by zero
            {
                betterTau = tauEstimate + (alpha - gamma) / denominator;
            }
        }

        // Convert lag (in samples) to frequency in Hz
        return sr / betterTau;
    }
}

