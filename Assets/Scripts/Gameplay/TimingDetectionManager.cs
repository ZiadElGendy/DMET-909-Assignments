
using UnityEngine;
using FMODUnity;

public class TimingDetectionManager : MonoBehaviour
{
    [SerializeField] private float bpm = 120f;
    [SerializeField] private int beatsPerMeasure = 4;
    [SerializeField] private float windowBeforeBeat = 0.1f;
    [SerializeField] private float windowAfterBeat = 0.1f;
    [SerializeField] private bool enableMetronome = false;
    [SerializeField] private EventReference metronomeEvent;

    private bool isPlaying;
    private double nextBeatTime;
    private double lastBeatTime;
    private double beatInterval;
    private int currentBeat = 0;
    public bool isOnBeat;

    void Start() => beatInterval = 60.0 / bpm;

    void Update()
    {
        if (isPlaying && AudioSettings.dspTime >= nextBeatTime)
        {
            lastBeatTime = nextBeatTime;
            currentBeat = (currentBeat + 1) % beatsPerMeasure;
            if (enableMetronome) RuntimeManager.PlayOneShot(metronomeEvent);
            nextBeatTime += beatInterval;
        }
    }

    public void StartClock()
    {
        beatInterval = 60.0 / bpm;
        nextBeatTime = AudioSettings.dspTime + beatInterval;
        lastBeatTime = AudioSettings.dspTime;
        currentBeat = beatsPerMeasure - 1;
        isPlaying = true;
    }

    public void PauseClock() => isPlaying = false;

    public void RestartClock() => StartClock();


    public bool IsOnBeat()
    {
        if (!isPlaying) return false;

        double currentTime = AudioSettings.dspTime;
        double timeSinceLastBeat = currentTime - lastBeatTime;
        double timeToNextBeat = nextBeatTime - currentTime;

        isOnBeat = timeSinceLastBeat <= windowAfterBeat || timeToNextBeat <= windowBeforeBeat;
        return isOnBeat;
    }

    public bool IsOnBeat(int beatNumber)
    {
        return IsOnBeat() && currentBeat == beatNumber;
    }

    public int GetCurrentBeat() => currentBeat;

    public float GetBeatProgress() => isPlaying ? (float)((AudioSettings.dspTime - (nextBeatTime - beatInterval)) / beatInterval) : 0f;
}