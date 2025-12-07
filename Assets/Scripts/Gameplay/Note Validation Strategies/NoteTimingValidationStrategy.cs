using UnityEngine;

public abstract class NoteTimingValidationStrategy : ScriptableObject
{
    public abstract int IsValidTiming(bool noteWasPlayed, int currentBeat);
}