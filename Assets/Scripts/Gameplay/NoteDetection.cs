using System;
using System.Collections.Generic;
using UnityEngine;

public class NoteDetection : Singleton<NoteDetection>
{
    public AudioSource audioSource;

    string _selectedDevice;
    string[] _devices;
    float[] _spectrumData;

    static readonly string[] NoteNames12 =
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };

    void Start()
    {
        _devices = Microphone.devices;
        _selectedDevice = _devices.Length > 0 ? _devices[0] : null;

        _spectrumData = new float[2048];

    }

    void Update()
    {
        if (_selectedDevice == null)
            return;

        audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);

        float peakFreq = GetPeakFrequency();
        string closestNote = GetClosestNoteName(peakFreq);

        Debug.Log($"Freq: {peakFreq:F2} Hz   Note: {closestNote}");
    }

    float GetPeakFrequency()
    {
        int index = 0;
        float maxVal = 0f;

        for (int i = 0; i < _spectrumData.Length; i++)
        {
            if (_spectrumData[i] > maxVal)
            {
                maxVal = _spectrumData[i];
                index = i;
            }
        }

        float freqN = index;
        float sampleRate = AudioSettings.outputSampleRate;
        float freq = freqN * sampleRate / (_spectrumData.Length * 2);

        return freq;
    }

    string MidiToName(int midi)
    {
        int note = midi % 12;
        int octave = (midi / 12) - 1;

        return NoteNames12[note] + octave;
    }


    string GetClosestNoteName(float frequency)
    {
        if (frequency <= 0f)
            return "";

        float midi = 69f + 12f * Mathf.Log(frequency / 440f, 2f);
        int midiRounded = Mathf.RoundToInt(midi);

        return MidiToName(midiRounded);
    }


    void OnGUI()
    {
        if (_devices == null)
            return;

        GUILayout.BeginVertical("box");

        GUILayout.Label("Audio Inputs:");
        foreach (var d in _devices)
        {
            if (GUILayout.Button(d))
                SelectDevice(d);
        }

        GUILayout.EndVertical();
    }

    void SelectDevice(string device)
    {
        _selectedDevice = device;

        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.clip = Microphone.Start(device, true, 1, 44100);
        while (Microphone.GetPosition(device) <= 0) { }
        audioSource.Play();
    }
}
