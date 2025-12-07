using System.Collections;
using Gameplay.Level_Data;
using UI;
using UnityEngine;

public class GameplayManager : Singleton<GameplayManager>
{
    public LevelData LevelData;
    public GameplayUIManager UIManager;
    public double vibeLevel = 0.5;
    public float musicDelayCompensationMs = 0f;

    private int totalBars;
    private int currentBar;

    private ChordProgression chordProgression;
    private NotePitchValidationStrategy notePitchValidationStrategy;
    private NoteTimingValidationStrategy noteTimingValidationStrategy;

    private void Start()
    {
        LoadLevelData();
        totalBars = chordProgression.GetTotalBars();
        Debug.Log($"Total Bars: {totalBars}");
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
        // yield return new WaitForSeconds(musicDelayCompensationMs);
        // StartBackingMusic();

        // Wait until we've advanced through all bars. This loop yields each frame so it doesn't block.
        while (currentBar < totalBars)
        {
            yield return null;
        }

        TimingDetectionManager.Instance.PauseClock();
    }

    public void OnBarEvent(int barIndex)
    {
        Debug.Log("OnBarEvent");
        Debug.Log("barIndex: " + barIndex);
        if (barIndex < 0) return; // Skip count-off bar

        var chord = chordProgression.GetChordAtBar(barIndex);
        Debug.Log("Current chord: " + (chord != null ? chord.ToString() : "(null)"));

        currentBar = barIndex;
        UIManager.UpdateChordSheetUI(currentBar);
    }

    public void StartBackingMusic()
    {
        FMODUnity.RuntimeManager.PlayOneShot(LevelData.backingMusicEvent);
    }
}
