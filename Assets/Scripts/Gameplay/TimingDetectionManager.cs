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
    [SerializeField] private EventReference backingMusicEvent;
    [SerializeField] private ScriptableEventInt onBarEvent;
    [SerializeField] private ScriptableEventInt onBeatEvent;
    [SerializeField] private ScriptableEventNoParam onTimingWindowOpen;
    [SerializeField] private ScriptableEventNoParam onTimingWindowClose;
    [SerializeField] private float musicStartOffsetMs = 0f;
    [SerializeField] private float latencyCompensationMs = 0f;

    private bool isPlaying;
    private FMOD.Studio.EventInstance musicInstance;
    private double musicStartDspTime;
    private double beatInterval;
    private int lastProcessedBeat = -1; // Track which beat we last processed
    private int currentBeat = 0;
    private int curentBar = -1; // Start at -1 to account for count off
    public bool isOnBeat;

    void Start()
    {
        beatInterval = 60.0 / bpm;
    }

    void Update()
    {
        if (!isPlaying) return;

        // Apply the offset correction
        double elapsedTime = AudioSettings.dspTime - musicStartDspTime - (musicStartOffsetMs / 1000.0);
        int totalBeatsElapsed = Mathf.FloorToInt((float)(elapsedTime / beatInterval));

        // Only process each beat once
        if (totalBeatsElapsed > lastProcessedBeat)
        {
            lastProcessedBeat = totalBeatsElapsed;
            currentBeat = totalBeatsElapsed % beatsPerMeasure;

            if (enableMetronome) RuntimeManager.PlayOneShot(metronomeEvent);

            onBeatEvent.Raise(currentBeat);

            if (currentBeat == 0)
            {
                curentBar = totalBeatsElapsed / beatsPerMeasure;
                onBarEvent.Raise(GetCurrentBar());
            }
        }
    }

    public void StartClock()
    {
        beatInterval = 60.0 / bpm;
        musicStartDspTime = AudioSettings.dspTime;
        lastProcessedBeat = -1;
        currentBeat = 0;
        curentBar = 0;
        isPlaying = true;

        // Create and start music instance (only once)
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }

        musicInstance = RuntimeManager.CreateInstance(backingMusicEvent);
        musicInstance.start();
    }

    public void PauseClock()
    {
        isPlaying = false;
        if (musicInstance.isValid())
        {
            musicInstance.setPaused(true);
        }
    }

    public void RestartClock()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }
        StartClock();
    }

    void OnDestroy()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }
    }

    public bool IsOnBeat()
    {
        if (!isPlaying) return false;

        double elapsedTime = AudioSettings.dspTime - musicStartDspTime - (musicStartOffsetMs / 1000.0);
        double exactBeatPosition = elapsedTime / beatInterval;
        double fractionalPart = exactBeatPosition - System.Math.Floor(exactBeatPosition);
        double offsetFromBeat = fractionalPart * beatInterval;

        isOnBeat = offsetFromBeat <= windowAfterBeat ||
                   offsetFromBeat >= (beatInterval - windowBeforeBeat);
        return isOnBeat;
    }

    public bool IsOnBeat(int beatNumber)
    {
        if (!IsOnBeat()) return false;

        double elapsedTime = AudioSettings.dspTime - musicStartDspTime - (musicStartOffsetMs / 1000.0);
        int currentBeatAtThisTime = Mathf.FloorToInt((float)(elapsedTime / beatInterval)) % beatsPerMeasure;

        return currentBeatAtThisTime == beatNumber;
    }

    public bool IsOnBeatFromDspTime(double dspTimestamp)
    {
        if (!isPlaying) return false;

        double adjustedTimestamp = dspTimestamp - (latencyCompensationMs / 1000.0);
        double elapsedTime = adjustedTimestamp - musicStartDspTime - (musicStartOffsetMs / 1000.0);

        if (beatInterval <= 0.0) return false;

        double exactBeatPosition = elapsedTime / beatInterval;
        double fractionalPart = exactBeatPosition - System.Math.Floor(exactBeatPosition);
        double offsetFromBeat = fractionalPart * beatInterval;

        bool withinWindow = offsetFromBeat <= windowAfterBeat ||
                            offsetFromBeat >= (beatInterval - windowBeforeBeat);

        return withinWindow;
    }


    public int GetCurrentBeat() => currentBeat;

    public int GetCurrentBar() => curentBar;

    public float GetBeatProgress()
    {
        if (!isPlaying) return 0f;

        double elapsedTime = AudioSettings.dspTime - musicStartDspTime - (musicStartOffsetMs / 1000.0);
        double exactBeatPosition = elapsedTime / beatInterval;
        return (float)(exactBeatPosition - System.Math.Floor(exactBeatPosition));
    }

    public void SetBPM(float newBPM)
    {
        bpm = newBPM;
        beatInterval = 60.0f / bpm;
    }

    public void SetBackingMusic(EventReference backingMusicEventRef)
    {
        backingMusicEvent = backingMusicEventRef;
    }
}