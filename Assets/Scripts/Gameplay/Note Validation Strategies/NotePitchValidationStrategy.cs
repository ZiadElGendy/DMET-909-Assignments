using Melanchall.DryWetMidi.MusicTheory;
using UnityEngine;

public abstract class NotePitchValidationStrategy : ScriptableObject
{
    public abstract int IsValidPitch(int midiNote, Chord currentChord);
}
