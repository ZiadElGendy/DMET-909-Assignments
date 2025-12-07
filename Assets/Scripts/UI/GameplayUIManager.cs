using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class GameplayUIManager : Singleton<GameplayUIManager>
    {
        public UIDocument chordSheetDocument;
        private ChordProgression chordProgression;
        private VisualElement[] chordSlots = new VisualElement[8];

        private void Start()
        {
            chordProgression = GameplayManager.Instance.LevelData.chordProgression;
            InitializeChordSheetUI();
        }

        public void UpdateChordSheetUI(int currentBar)
        {
            if (chordProgression == null || chordSlots[0] == null)
                return;

            // Calculate which 8-bar window to show (advances every 4 bars)
            int windowStart = (currentBar / 4) * 4;

            // Update all 8 chord slots
            for (int i = 0; i < 8; i++)
            {
                int barIndex = windowStart + i;
                var chord = chordProgression.GetChordAtBar(barIndex);
                
                // Get chord name or "//" for continuation bars
                string chordName = GetChordNameForBar(barIndex);
                
                // Update the label text
                var label = chordSlots[i].Q<Label>();
                if (label != null)
                {
                    label.text = chordName;
                }

                // Highlight current bar with light yellow background
                if (barIndex == currentBar)
                {
                    chordSlots[i].style.backgroundColor = new Color(1f, 1f, 0.7f, 1f); // Light yellow
                }
                else
                {
                    chordSlots[i].style.backgroundColor = StyleKeyword.Null; // Remove background
                }
            }
        }

        private void InitializeChordSheetUI()
        {
            if (chordSheetDocument == null)
            {
                Debug.LogError("GameplayUIManager: chordSheetDocument is not assigned.");
                return;
            }

            var root = chordSheetDocument.rootVisualElement;

            // Simply query for Chord1 through Chord8 by name
            for (int i = 0; i < 8; i++)
            {
                chordSlots[i] = root.Q<VisualElement>("Chord" + (i + 1));
                
                if (chordSlots[i] == null)
                {
                    Debug.LogWarning($"Could not find Chord{i + 1} element in UI.");
                }
            }

            // Initial update
            UpdateChordSheetUI(0);
        }

        private string GetChordNameForBar(int barIndex)
        {
            if (chordProgression == null || chordProgression.chordSections == null)
                return "";

            // Get base progression length (without loops)
            int baseTotalBars = 0;
            foreach (var section in chordProgression.chordSections)
                baseTotalBars += section.durationInBars;

            if (baseTotalBars == 0)
                return "";

            // Normalize to base progression
            int normalizedBar = ((barIndex % baseTotalBars) + baseTotalBars) % baseTotalBars;

            // Find which section and which bar within that section
            int barCount = 0;
            foreach (var section in chordProgression.chordSections)
            {
                if (normalizedBar >= barCount && normalizedBar < barCount + section.durationInBars)
                {
                    // First bar of section shows chord name, rest show "//"
                    return (normalizedBar == barCount) ? section.chordName : "//";
                }
                barCount += section.durationInBars;
            }

            return "";
        }
    }
}