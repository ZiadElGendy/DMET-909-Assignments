using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using UnityEngine;

namespace Gameplay.Note_Validation_Strategies
{
    [CreateAssetMenu(fileName = "RootsAndFifthsNotePitchValidationStrategy", menuName = "Gameplay/Pitch Validation Strategy/Roots and Fifths", order = 0)]
    public class RootsAndFifthsNotePitchValidationStrategy : NotePitchValidationStrategy
    {
        public override bool IsValidPitch(int midiNote, Chord currentChord)
        {
            var note = Note.Get((SevenBitNumber)midiNote);
            var root = currentChord.RootNoteName;
            var scaleString = $"{root.ToString()} P5";
            var scale = Scale.TryParse(scaleString, out var parsedScale) ? parsedScale : null;
            return parsedScale.IsNoteInScale(note);
        }
    }
}