using UnityEngine;

namespace Gameplay.Note_Validation_Strategies
{
    [CreateAssetMenu(fileName = "EveryHalfNoteTimingValidationStrategy", menuName = "Gameplay/Timing Validation Strategy/Every Half Note", order = 0)]
    public class EveryHalfNoteTimingValidationStrategy : NoteTimingValidationStrategy
    {
        public override int IsValidTiming(bool noteWasPlayed, int currentBeat)
        {
            bool isHalfNote = currentBeat % 2 == 0;
            if (isHalfNote)
            {
                // Half note required
                return noteWasPlayed ? 1 : -1; // Success or miss
            }

            return 0; // Neutral (doesn't matter if played or not)

        }
    }
}