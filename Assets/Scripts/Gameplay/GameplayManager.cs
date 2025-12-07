using System;
using System.Collections;
using Gameplay.Level_Data;
using Melanchall.DryWetMidi.MusicTheory;
using UI;
using UnityEngine;

public class GameplayManager : Singleton<GameplayManager>
{
    public LevelData LevelData;
    public GameplayUIManager UIManager;
    public float vibeLevel = 0.5f;
    public float musicDelayCompensationMs = 0f;

    private int totalBars;
    private int currentBar = -1;
    private int currentBeat = 0;

    private ChordProgression chordProgression;
    private NotePitchValidationStrategy notePitchValidationStrategy;
    private NoteTimingValidationStrategy noteTimingValidationStrategy;

    public bool inTimingWindow = false;
    public int pitchValid = 0;
    public int timingValid = 0;
    public int fulfilledNoteInTimingWindow = 0;

    // Track the last processed note to avoid duplicate processing
    private double lastProcessedNoteDspTime = -1.0;
    private bool notePlayedInBar = false; // Flag to track if a note was played in the current bar

    private void Start()
    {
        LoadLevelData();
        totalBars = chordProgression.GetTotalBars();
        Debug.Log($"Total Bars: {totalBars}");
        Debug.Log($"Progression Bars: {chordProgression.GetProgressionBars()}");

        currentBar = 0;
        TimingDetectionManager.Instance.SetBPM(chordProgression.bpm);
        TimingDetectionManager.Instance.SetBackingMusic(LevelData.backingMusicEvent);

        UIManager = GameplayUIManager.Instance;
        StartGame();
    }

    public void StartGame()
    {
        StartCoroutine(ManageGameFlow());
    }

    private void LoadLevelData()
    {
        if (LevelData == null)
        {
            Debug.LogError("GameplayManager: LevelData is not assigned in the inspector.");
            return;
        }

        chordProgression = LevelData.chordProgression;
        notePitchValidationStrategy = LevelData.notePitchValidationStrategy;
        noteTimingValidationStrategy = LevelData.noteTimingValidationStrategy;
    }

    private IEnumerator ManageGameFlow()
    {
        yield return new WaitForSeconds(1f);
        TimingDetectionManager.Instance.StartClock();

        // Wait until we've advanced through all bars
        while (currentBar < totalBars)
        {
            yield return null;
        }

        TimingDetectionManager.Instance.PauseClock();
    }

    private void Update()
    {
        // Debug.Log($"Frame Check - In Window: {inTimingWindow}, " +
        //           $"Last DSP: {NoteDetectionManager.Instance.lastNoteDspTime:F3}, " +
        //           $"Last Processed: {lastProcessedNoteDspTime:F3}, " +
        //           $"Detected MIDI: {NoteDetectionManager.Instance.detectedMidi}");


    }

    public void OnNotePlayDetected(int midiNote)
    {
        Debug.Log($"⏱ OnNotePlayDetected called - inTimingWindow: {inTimingWindow}, currentBar: {currentBar}, fulfilledNoteInTimingWindow: {fulfilledNoteInTimingWindow} ");
        notePlayedInBar = true;

        if (inTimingWindow && fulfilledNoteInTimingWindow != 1)
        {
            int pitch = midiNote;
            Chord chord = chordProgression.GetChordAtBar(currentBar);

            pitchValid = notePitchValidationStrategy.IsValidPitch(pitch, chord);
            timingValid = noteTimingValidationStrategy.IsValidTiming(true, currentBeat);

            Debug.Log($"Note Detected - Pitch: {NoteDetectionManager.Instance.detectedNote} (MIDI: {pitch}), " +
                      $"Pitch Valid: {pitchValid}, Timing Valid: {timingValid}");

            CheckNoteFulfillment();
        }
        else if (!inTimingWindow)
        {
            Debug.Log($"❌ Note detected OUTSIDE timing window - Bar {currentBar}");
        }
        else if (fulfilledNoteInTimingWindow == 1)
        {
            Debug.Log("Note detected but already fulfilled - ignored");
        }
    }

    private void CheckNoteFulfillment()
    {
        Debug.Log($"pitchValid: {pitchValid}, timingValid: {timingValid}, fulfilledNoteInTimingWindow: {fulfilledNoteInTimingWindow}");

        // NEVER overwrite a locked-in success (1)
        if (fulfilledNoteInTimingWindow == 1)
        {
            Debug.Log("Already locked in as success - no changes");
            return;
        }

        // If BOTH are valid (1), lock it in as success
        if (pitchValid == 1 && timingValid == 1)
        {
            Debug.Log("✓ Valid note - LOCKED IN");
            fulfilledNoteInTimingWindow = 1; // Lock it in, won't process more notes
        }
        // If EITHER is invalid (-1), mark as failed (but can still be overridden by a correct note later)
        else if (pitchValid == -1 || timingValid == -1)
        {
            Debug.Log("✗ Invalid note - marked as failed (can still recover)");
            fulfilledNoteInTimingWindow = -1; // Can be overridden by correct note
        }
        // If neutral (0), don't change anything
        else
        {
            Debug.Log("○ Neutral note - ignored");
            // Keep whatever state we had
        }
    }

    public void OnBeatEvent(int beatIndex)
    {
        currentBeat = beatIndex;
        // Debug.Log($"--- BEAT {beatIndex} --- {timingValid}");
        // if(timingValid != 1 && fulfilledNoteInTimingWindow != 1)
        // {
        // timingValid = noteTimingValidationStrategy.IsValidTiming(false);
        // CheckNoteFulfillment();
        //
        // }
    }

    public void OnBarEvent(int barIndex)
    {
        Debug.Log($"═══ BAR {barIndex} ═══");
        if (barIndex < 0) return; // Skip count-off bar

        var chord = chordProgression.GetChordAtBar(barIndex);
        Debug.Log($"Current chord: {(chord != null ? chord.ToString() : "(null)")}");

        currentBar = barIndex;
        UIManager.UpdateChordSheetUI(currentBar);

        if (!notePlayedInBar && barIndex > 0)
        {
            OnBarSkipped(); // Penalize if no note was played in the previous bar
        }
        notePlayedInBar = false;
    }

    public void OnTimingWindowOpen()
    {
        if (currentBar < 0) return; // Skip count-off bar

        Debug.Log($">>> TIMING WINDOW OPENED - Bar {currentBar} <<<");
        inTimingWindow = true;
        fulfilledNoteInTimingWindow = 0; // Reset - looking for a valid note
        lastProcessedNoteDspTime = -1.0; // Reset to catch new notes
    }

    public void OnTimingWindowClose()
    {
        if (currentBar < 0) return; // Skip count-off bar
        Debug.Log($">>> Timing Window CLOSED - Bar {currentBar} - Fulfilled: {fulfilledNoteInTimingWindow} <<<");

        if (fulfilledNoteInTimingWindow == 1)
        {
            OnNotePlayedSuccessfully();
        }
        else if (fulfilledNoteInTimingWindow == -1)
        {
            OnNotePlayedUnuccessfully();
        }

        UIManager.UpdateVibeMeterUI(vibeLevel);

        inTimingWindow = false;
        timingValid = 0;
        pitchValid = 0;
        fulfilledNoteInTimingWindow = 0;
    }

    public void OnNotePlayedSuccessfully()
    {
        Debug.Log("★★★ Correct Note Played! ★★★");
        vibeLevel = Math.Min(vibeLevel + 0.025f, 1);
        Debug.Log($"Vibe Level: {vibeLevel:F3}");
    }

    public void OnNotePlayedUnuccessfully()
    {
        Debug.Log("✗ Incorrect Note Played");
        vibeLevel = Math.Max(vibeLevel - 0.05f, 0);
        Debug.Log($"Vibe Level: {vibeLevel:F3}");
    }

    public void OnBarSkipped()
    {
        Debug.Log("✗✗ No Notes played in Bar");
        vibeLevel = Math.Max(vibeLevel - 0.1f, 0);
        Debug.Log($"Vibe Level: {vibeLevel:F3}");
    }
}