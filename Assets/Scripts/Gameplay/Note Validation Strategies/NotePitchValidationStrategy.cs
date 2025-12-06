using Melanchall.DryWetMidi.MusicTheory;
using UnityEngine;

public abstract class NotePitchValidationStrategy : ScriptableObject
{
    public abstract bool IsValidPitch(int midiNote, Chord currentChord);
}
