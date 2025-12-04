using UnityEngine;
using System.Runtime.InteropServices;

public class NoteDetection : Singleton<NoteDetection>
{
    public int sampleRate = 44100;
    public int windowSize = 1024;
    public float yinThreshold = 0.15f;

    public float detectedFrequency;
    public int detectedMidi;
    public string detectedNote;

    float[] circularBuffer;
    int head;

    FMOD.Sound sound;
    FMOD.System coreSystem;
    int recordDriverIndex = 0;
    bool isRecording = false;

    uint lastRecordPos = 0;
    float[] readBuffer;

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
            samplesToRead = (int)(soundLength - lastRecordPos + recordPos);
        }

        if (samplesToRead > 0)
        {
            ProcessNewSamples(lastRecordPos, samplesToRead, soundLength);
            lastRecordPos = recordPos;
        }

        if (head >= windowSize)
        {
            float[] window = new float[windowSize];
            int start = head - windowSize;
            if (start < 0) start += circularBuffer.Length;

            for (int i = 0; i < windowSize; i++)
                window[i] = circularBuffer[(start + i) % circularBuffer.Length];

            float f = YIN(window, sampleRate, yinThreshold);
            if (f > 0f)
            {
                detectedFrequency = f;
                detectedMidi = FrequencyToMIDI(f);
                detectedNote = MidiToName(detectedMidi);
            }
        }
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
        float m = 69f + 12f * Mathf.Log(f / 440f, 2f);
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
            cmnd[tau] = diff[tau] * tau / running;
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
        return sr / betterTau;
    }
}