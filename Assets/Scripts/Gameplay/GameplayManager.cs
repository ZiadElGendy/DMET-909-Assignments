using System.Collections;
using Gameplay.Level_Data;
using UI;
using UnityEngine;

public class GameplayManager : Singleton<GameplayManager>
{
    public LevelData LevelData;
    public GameplayUIManager UIManager;
    public double vibeLevel = 0.5;

    private int totalBars;
    private int currentBar;

    private ChordProgression chordProgression;
    private NotePitchValidationStrategy notePitchValidationStrategy;
    private NoteTimingValidationStrategy noteTimingValidationStrategy;

    private void Start()
    {
        LoadLevelData();

        if (chordProgression == null)
        {
            Debug.LogError("GameplayManager: chordProgression is null. Make sure LevelData is assigned and contains a ChordProgression.");
            totalBars = 0;
        }
        else
        {
            totalBars = chordProgression.GetTotalBars();
        }

        currentBar = 0;

        // Ensure we have a UIManager reference - fall back to singleton if available
        if (UIManager == null)
            UIManager = GameplayUIManager.Instance;

        // Start the game flow coroutine instead of blocking the main thread
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
        // Start timing and show UI
        if (TimingDetectionManager.Instance != null)
            TimingDetectionManager.Instance.StartClock();
        else
            Debug.LogWarning("TimingDetectionManager instance not found.");

        if (UIManager != null && UIManager.chordSheetDocument != null)
            UIManager.chordSheetDocument.rootVisualElement.style.display = UnityEngine.UIElements.DisplayStyle.Flex;
        else
            Debug.LogWarning("GameplayManager: UIManager or its chordSheetDocument is not assigned.");

        // Wait until we've advanced through all bars. This loop yields each frame so it doesn't block.
        while (currentBar < totalBars)
        {
            yield return null;
        }

        if (TimingDetectionManager.Instance != null)
            TimingDetectionManager.Instance.PauseClock();

        if (UIManager != null && UIManager.chordSheetDocument != null)
            UIManager.chordSheetDocument.rootVisualElement.style.display = UnityEngine.UIElements.DisplayStyle.None;
    }

    public void OnBarEvent(int barIndex)
    {
        Debug.Log("OnBarEvent");
        Debug.Log("barIndex: " + barIndex);

        if (chordProgression == null)
        {
            Debug.LogError("OnBarEvent: chordProgression is null. Cannot query chord at bar.");
        }
        else
        {
            var chord = chordProgression.GetChordAtBar(barIndex);
            Debug.Log("Current chord: " + (chord != null ? chord.ToString() : "(null)"));
        }

        currentBar = barIndex;

        if (UIManager != null)
        {
            UIManager.UpdateChordSheetUI(currentBar);
        }
        else
        {
            Debug.LogWarning("OnBarEvent: UIManager is null. Cannot update chord sheet UI.");
        }
    }
}
