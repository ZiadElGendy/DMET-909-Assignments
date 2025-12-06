using UnityEngine;
using FMODUnity;
using Obvious.Soap;

public class TimingDetectionManager : Singleton<TimingDetectionManager>
{
    [SerializeField] private float bpm = 120f;
    [SerializeField] private int beatsPerMeasure = 4;
    [SerializeField] private float windowBeforeBeat = 0.1f;
    [SerializeField] private float windowAfterBeat = 0.1f;
    [SerializeField] private bool enableMetronome = false;
    [SerializeField] private EventReference metronomeEvent;
    [SerializeField] private ScriptableEventInt onBarEvent;
    [SerializeField] private ScriptableEventInt onBeatEvent;



    // Latency compensation in milliseconds. Subtracted from the detected DSP timestamp to compensate
    // for analysis/processing latency. Set this in the Inspector to match your detection latency.
    [SerializeField] private float latencyCompensationMs = 0f;

    private bool isPlaying;
    private double nextBeatTime;
    private double lastBeatTime;
    private double beatInterval;
    private int currentBeat = 0;
    private int curentBar = 0;
    public bool isCountOff = true;
    public bool isOnBeat;

    void Start() => beatInterval = 60.0 / bpm;

    void Update()
    {
        if (isPlaying && AudioSettings.dspTime >= nextBeatTime)
        {
            lastBeatTime = nextBeatTime;
            currentBeat = (currentBeat + 1) % beatsPerMeasure;
            if (enableMetronome) RuntimeManager.PlayOneShot(metronomeEvent);
            if(isCountOff && GetCurrentBar() >= 0) isCountOff = false;
            nextBeatTime += beatInterval;
            onBeatEvent.Raise(currentBeat);
            if (currentBeat == 0)
            {
                curentBar++;
                onBarEvent.Raise(GetCurrentBar());
            }
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

    /// <summary>
    /// Checks whether a note timestamped with AudioSettings.dspTime falls within the timing window.
    /// Use this when you have an absolute DSP timestamp (for example, from audio note detection).
    /// </summary>
    /// <param name="dspTimestamp">The DSP timestamp (AudioSettings.dspTime) when the note was detected.</param>
    public bool IsOnBeatFromDspTime(double dspTimestamp)
    {
        if (!isPlaying) return false;

        // Apply latency compensation (detection timestamps are delayed by processing)
        double adjustedTimestamp = dspTimestamp - (latencyCompensationMs / 1000.0);

        // If beatInterval is invalid, prevent divide-by-zero
        if (beatInterval <= 0.0) return false;

        // Compute how many beats (possibly fractional) the adjusted timestamp is away from the lastBeatTime
        double beatsFromLast = (adjustedTimestamp - lastBeatTime) / beatInterval;

        // Find nearest beat index offset (integer number of beats from lastBeatTime)
        int nearestBeatOffset = (int)System.Math.Round(beatsFromLast);

        // Compute the absolute time of that nearest beat
        double nearestBeatTime = lastBeatTime + nearestBeatOffset * beatInterval;

        // Timing offset relative to the nearest beat (negative = early, positive = late)
        double offset = adjustedTimestamp - nearestBeatTime;

        // Check within configured window
        bool withinWindow = offset >= -windowBeforeBeat && offset <= windowAfterBeat;

        return withinWindow;
    }

    public int GetCurrentBeat() => currentBeat;

    public int GetCurrentBar() => curentBar;

    public float GetBeatProgress() => isPlaying ? (float)((AudioSettings.dspTime - (nextBeatTime - beatInterval)) / beatInterval) : 0f;
}