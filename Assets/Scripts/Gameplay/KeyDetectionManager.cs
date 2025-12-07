using UnityEngine;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Common;
using System.Collections.Generic;
using System.Linq;

public class KeyDetection : MonoBehaviour
{
    private NoteDetectionManager _noteDetectionManager;

    // Current detected state
    public bool isCurrentNoteInKey { get; private set; }
    public NoteName lastCheckedNoteName { get; private set; }

    // C Major scale notes for testing
    private HashSet<NoteName> cMajorScale;

    void Start()
    {
        _noteDetectionManager = NoteDetectionManager.Instance;

        // Initialize C Major scale: C, D, E, F, G, A, B
        cMajorScale = new HashSet<NoteName>
        {
            NoteName.C,
            NoteName.D,
            NoteName.E,
            NoteName.F,
            NoteName.G,
            NoteName.A,
            NoteName.B
        };
    }

    void Update()
    {
        if (_noteDetectionManager == null || _noteDetectionManager.detectedMidi < 0)
        {
            isCurrentNoteInKey = false;
            return;
        }

        // Get the MIDI note from NoteDetection
        int midiNoteNumber = _noteDetectionManager.detectedMidi;

        // Convert to DryWetMIDI Note
        var note = Melanchall.DryWetMidi.MusicTheory.Note.Get((SevenBitNumber)midiNoteNumber);

        // Check if it's in key
        isCurrentNoteInKey = IsInKey(note, cMajorScale);
        lastCheckedNoteName = note.NoteName;
    }

    /// <summary>
    /// Checks if a MIDI note is in the given set of note names (ignoring octave)
    /// </summary>
    /// <param name="midiNote">The MIDI note to check</param>
    /// <param name="noteSet">Set of note names that define the key/scale</param>
    /// <returns>True if the note's name is in the set, false otherwise</returns>
    public bool IsInKey(Melanchall.DryWetMidi.MusicTheory.Note midiNote, HashSet<NoteName> noteSet)
    {
        // Extract just the note name (ignoring octave)
        NoteName noteName = midiNote.NoteName;

        // Check if the note name is in the provided set
        return noteSet.Contains(noteName);
    }
}