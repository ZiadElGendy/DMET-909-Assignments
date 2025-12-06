using UnityEngine;

namespace Gameplay.Note_Validation_Strategies
{
    [CreateAssetMenu(fileName = "EveryHalfNoteTimingValidationStrategy", menuName = "Gameplay/Timing Validation Strategy/Every Half Note", order = 0)]
    public class EveryHalfNoteTimingValidationStrategy : NoteTimingValidationStrategy
    {
        public override bool IsValidTiming(double dspTimestamp)
        {
            bool isOnBeat = TimingDetectionManager.Instance.IsOnBeatFromDspTime(dspTimestamp);
            bool isHalfNote = TimingDetectionManager.Instance.GetCurrentBeat() % 2 == 0;
            return isOnBeat && isHalfNote;
        }
    }
}