using UnityEngine;

public abstract class NoteTimingValidationStrategy : ScriptableObject
{
    public abstract bool IsValidTiming(double dspTimestamp);
}