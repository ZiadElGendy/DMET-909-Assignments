using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class GameplayUIManager : Singleton<GameplayUIManager>
    {
        public UIDocument chordSheetDocument;
        private ChordProgression chordProgression;

        // cache of the 8 chord slot elements (Row-major order)
        private VisualElement[] chordSlots = new VisualElement[8];

        // flattened per-bar display names for the whole progression (size == total bars)
        private string[] perBarNames;

        private void Start()
        {
            chordProgression = GameplayManager.Instance.LevelData.chordProgression;
            InitializeChordSheetUI();
        }

        public void UpdateChordSheetUI(int currentBar)
        {
            // highlight current chord based on currentBar and update visible names if needed
            if (chordSlots == null || chordSlots.Length == 0 || perBarNames == null || perBarNames.Length == 0)
                return;

            int totalBars = Mathf.Max(1, perBarNames.Length);

            // Determine which 8-bar window should be visible. Window only advances every 4 bars so rows stay stable.
            int windowSize = chordSlots.Length; // 8

            // Compute the 4-bar group index that contains currentBar, then start window at that group's first bar
            int groupSize = 4;
            int groupIndex = currentBar / groupSize; // integer division
            int windowStart = groupIndex * groupSize;

            // Clamp so we don't run past the end
            windowStart = Mathf.Clamp(windowStart, 0, Mathf.Max(0, totalBars - windowSize));

            // Populate the visible window and set highlight
            for (int i = 0; i < chordSlots.Length; i++)
            {
                int globalBar = windowStart + i;
                if (totalBars > 0) globalBar = globalBar % totalBars;
                string display = perBarNames[globalBar];

                SetChordSlotText(chordSlots[i], display);

                // highlight current
                if (globalBar == currentBar % totalBars)
                {
                    chordSlots[i].AddToClassList("current-bar");
                }
                else
                {
                    chordSlots[i].RemoveFromClassList("current-bar");
                }
            }
        }

        private void InitializeChordSheetUI()
        {
            if (chordSheetDocument == null)
            {
                Debug.LogWarning("GameplayUIManager: chordSheetDocument is not assigned.");
                return;
            }

            var rootVE = chordSheetDocument.rootVisualElement;
            if (rootVE == null)
            {
                Debug.LogWarning("GameplayUIManager: rootVisualElement is null.");
                return;
            }

            // Navigate the expected hierarchy: Wrapper / Root / Row / Chord
            // Assumptions: Wrapper contains a single Root element, Root contains two Row elements,
            // and each Row contains four child elements representing chord slots.
            var wrapper = rootVE.Q<VisualElement>("Wrapper");
            if (wrapper == null)
            {
                // fallback: use root directly
                wrapper = rootVE;
            }

            var root = wrapper.Q<VisualElement>("Root");
            if (root == null)
            {
                // if Root not found, assume wrapper's first child is the root container
                root = wrapper.ElementAt(0) as VisualElement ?? wrapper;
            }

            // collect rows (take first two VisualElement children that have children)
            var rows = new System.Collections.Generic.List<VisualElement>();
            foreach (var child in root.Children())
            {
                if (child is VisualElement ve && ve.childCount > 0)
                {
                    rows.Add(ve);
                    if (rows.Count >= 2) break;
                }
            }

            if (rows.Count < 2)
            {
                Debug.LogWarning("GameplayUIManager: Expected at least 2 rows in the chord sheet UI.");
            }

            // For each row, take up to 4 chord slots
            int slotIndex = 0;
            for (int r = 0; r < Mathf.Min(2, rows.Count); r++)
            {
                var row = rows[r];
                int taken = 0;
                foreach (var child in row.Children())
                {
                    if (child is VisualElement slot && taken < 4 && slotIndex < chordSlots.Length)
                    {
                        chordSlots[slotIndex] = slot;
                        // ensure there's a Label inside to hold the text
                        var label = slot.Q<Label>();
                        if (label == null)
                        {
                            label = new Label("...");
                            slot.Add(label);
                        }

                        slotIndex++;
                        taken++;
                    }
                }
            }

            // If we still have empty slots (less than 8 found), create placeholders at the end of root
            while (slotIndex < chordSlots.Length)
            {
                var placeholder = new VisualElement();
                var label = new Label("...");
                placeholder.Add(label);
                root.Add(placeholder);
                chordSlots[slotIndex] = placeholder;
                slotIndex++;
            }

            // Build the per-bar names from chord progression
            BuildPerBarNames();

            // Initial populate first 8 bars
            UpdateChordSheetUI(0);
        }

        private void BuildPerBarNames()
        {
            if (chordProgression == null || chordProgression.chordSections == null || chordProgression.chordSections.Count == 0)
            {
                perBarNames = new string[0];
                return;
            }

            int totalBars = chordProgression.GetTotalBars();
            var names = new System.Collections.Generic.List<string>(totalBars);

            // Build one loop of names
            foreach (var section in chordProgression.chordSections)
            {
                string baseName = section.chordName ?? "";
                for (int b = 0; b < section.durationInBars; b++)
                {
                    if (b == 0)
                        names.Add(baseName);
                    else
                        names.Add("//");
                }
            }

            // Repeat for looping if numLoops > 1 (ChordProgression handles real looped playback separately)
            if (chordProgression.numLoops > 1)
            {
                var full = new System.Collections.Generic.List<string>(totalBars * chordProgression.numLoops);
                for (int loop = 0; loop < chordProgression.numLoops; loop++)
                {
                    full.AddRange(names);
                }
                perBarNames = full.ToArray();
            }
            else
            {
                perBarNames = names.ToArray();
            }
        }

        private void SetChordSlotText(VisualElement slot, string text)
        {
            if (slot == null) return;
            var label = slot.Q<Label>();
            if (label == null)
            {
                // add a label if missing
                label = new Label(text);
                slot.Add(label);
            }
            else
            {
                label.text = text;
            }
        }
    }
}