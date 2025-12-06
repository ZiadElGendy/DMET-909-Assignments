using UnityEngine;

public abstract class NoteValidationStrategy : ScriptableObject
{
    public abstract bool IsValidPitch(float frequency);
    public abstract bool IsValidTiming(double timeSinceLastBeat, double timeToNextBeat);
    public abstract string GetFeedbackMessage(bool pitchValid, bool timingValid);
}
