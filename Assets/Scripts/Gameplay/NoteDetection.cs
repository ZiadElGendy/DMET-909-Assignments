using UnityEngine;

public class NoteDetection : Singleton<NoteDetection>
{
    public AudioSource audioSource;
    public int sampleRate = 44100;
    public int windowSize = 1024;
    public float yinThreshold = 0.15f;

    public float detectedFrequency;
    public int detectedMidi;
    public string detectedNote;

    float[] circularBuffer;
    int head;
    string selectedDevice = "";

    void Start()
    {
        audioSource.loop = true;
        audioSource.mute = true;

        circularBuffer = new float[sampleRate * 2];
    }

    public void SelectDevice(string device)
    {
        if (selectedDevice == device) return;
        selectedDevice = device;
        StartMicrophone();
    }

    void StartMicrophone()
    {
        if (Microphone.IsRecording(selectedDevice))
            Microphone.End(selectedDevice);

        audioSource.Stop();
        audioSource.clip = Microphone.Start(selectedDevice, true, 1, sampleRate);
        audioSource.Play();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        int frames = data.Length / channels;

        for (int f = 0; f < frames; f++)
        {
            float s = 0f;
            for (int c = 0; c < channels; c++)
                s += data[f * channels + c];
            s /= channels;

            circularBuffer[head] = s;
            head = (head + 1) % circularBuffer.Length;
        }

        if (frames >= windowSize / 2)
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

    void OnGUI()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("Select Microphone:");

        foreach (var d in Microphone.devices)
        {
            if (GUILayout.Button(d))
                SelectDevice(d);
        }

        GUILayout.Space(10);

        GUILayout.Label("Frequency: " + detectedFrequency.ToString("F1") + " Hz");
        GUILayout.Label("MIDI: " + detectedMidi);
        GUILayout.Label("Note: " + detectedNote);
        GUILayout.EndVertical();
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

