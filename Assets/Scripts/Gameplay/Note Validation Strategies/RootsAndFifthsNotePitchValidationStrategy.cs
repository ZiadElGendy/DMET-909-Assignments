using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using UnityEngine;

namespace Gameplay.Note_Validation_Strategies
{
    [CreateAssetMenu(fileName = "RootsAndFifthsNotePitchValidationStrategy", menuName = "Gameplay/Pitch Validation Strategy/Roots and Fifths", order = 0)]
    public class RootsAndFifthsNotePitchValidationStrategy : NotePitchValidationStrategy
    {
        public override int IsValidPitch(int midiNote, Chord currentChord)
        {
            var note = Note.Get((SevenBitNumber)midiNote);
            var root = currentChord.RootNoteName;

            // Get the note name without octave
            var noteName = note.NoteName;

            // Calculate the fifth (7 semitones above root)
            var rootNote = Note.Get(root, 0); // Octave doesn't matter for name comparison
            var fifthNote = rootNote.Transpose(Interval.FromHalfSteps(7));
            var fifthNoteName = fifthNote.NoteName;

            // Check if the played note matches root or fifth (ignoring octave)
            bool isRootOrFifth = (noteName == root) || (noteName == fifthNoteName);

            int result = isRootOrFifth ? 1 : -1;

            Debug.Log($"Checking note {noteName} against chord {currentChord} (Root: {root}, Fifth: {fifthNoteName}): Result = {result}");

            return result;
        }
    }
}