using UnityEngine;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a chord that lasts for one or more bars
/// </summary>
[System.Serializable]
public class ChordSection
{
    [Tooltip("The chord to play (e.g., C Major, Am, G7)")]
    public string chordName = "C";

    [Tooltip("One or more scales to use over this chord if needed for the level (e.g., C Major, A Minor). Leave empty for no explicit scales.")]
    public List<string> scaleNames = new List<string>() { "C Major" };

    [Tooltip("Number of bars this chord lasts")]
    [Min(1)]
    public int durationInBars = 1;

    // Cache the parsed chord and scales
    private Chord _cachedChord;
    private List<Scale> _cachedScales;

    /// <summary>
    /// Gets the DryWetMIDI Chord object
    /// </summary>
    public Chord GetChord()
    {
        if (_cachedChord == null)
        {
            _cachedChord = ParseChord(chordName);
        }
        return _cachedChord;
    }

    /// <summary>
    /// Gets the parsed DryWetMIDI Scale objects specified for this chord.
    /// Returns an empty list when no scales are specified.
    /// </summary>
    public List<Scale> GetScales()
    {
        if (_cachedScales == null)
        {
            _cachedScales = new List<Scale>();

            if (scaleNames != null && scaleNames.Count > 0)
            {
                foreach (var s in scaleNames)
                {
                    if (string.IsNullOrWhiteSpace(s))
                        continue;

                    var parsed = ParseScale(s);
                    if (parsed != null)
                        _cachedScales.Add(parsed);
                }
            }
        }

        return _cachedScales;
    }

    /// <summary>
    /// Parses a chord name string into a DryWetMIDI Chord
    /// Supports formats like: RootNote ChordCharacteristic, RootNote ChordCharacteristic BassNote
    /// </summary>
    private Chord ParseChord(string name)
    {
        try
        {
            // Parse the chord name using DryWetMIDI's parser
            return Chord.Parse(name);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse chord '{name}': {e.Message}");
            // Return C Major as fallback
            return Chord.GetByTriad(NoteName.C, ChordQuality.Major);
        }
    }

    /// <summary>
    /// Parses a scale name string into a DryWetMIDI Scale
    /// Returns null if parsing fails.
    /// </summary>
    private Scale ParseScale(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        try
        {
            return Scale.Parse(name);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse scale '{name}': {e.Message}");
            return null;
        }
    }

    private Scale GetPentatonicScale(Chord chord)
    {
        var intervals = chord.GetIntervalsFromRootNote();

        if (intervals.Contains(Interval.FromHalfSteps(4))) // Contains major third
        {
            return new Scale(ScaleIntervals.MajorPentatonic, chord.RootNoteName);
        }
        else
        {
            return new Scale(ScaleIntervals.MinorPentatonic, chord.RootNoteName);
        }
    }

    /// <summary>
    /// Gets all note names in this chord
    /// </summary>
    public NoteName[] GetChordNoteNames()
    {
        return GetChord().NotesNames.ToArray();
    }

    /// <summary>
    /// Gets all note names from the configured scales for this chord.
    /// If no scales are configured, falls back to returning the chord's note names.
    /// This preserves the original API for simple use-cases.
    /// </summary>
    public NoteName[] GetScaleNoteNames()
    {
        var scales = GetScales();
        if (scales == null || scales.Count == 0)
        {
            // No explicit scales configured: return chord notes
            return GetChordNoteNames();
        }

        return scales.SelectMany(sc => sc.GetNotes()).Select(n => n.NoteName).Distinct().ToArray();
    }

    /// <summary>
    /// New: returns a list of scale -> note names mappings. If no scales are configured,
    /// the list contains a single mapping with Scale = null and NoteNames = chord notes (fallback).
    /// </summary>
    public List<ScaleNoteMapping> GetScaleNoteMappings()
    {
        var scales = GetScales();
        var mappings = new List<ScaleNoteMapping>();

        if (scales == null || scales.Count == 0)
        {
            mappings.Add(new ScaleNoteMapping
            {
                Scale = null,
                NoteNames = GetChordNoteNames(),
                DisplayName = chordName + " (Chord)"
            });

            return mappings;
        }

        foreach (var s in scales)
        {
            mappings.Add(new ScaleNoteMapping
            {
                Scale = s,
                NoteNames = s.GetNotes().Select(n => n.NoteName).Distinct().ToArray(),
                DisplayName = s.ToString()
            });
        }

        return mappings;
    }

    /// <summary>
    /// Checks if a given note is in any configured scale for this chord.
    /// If no scales are configured, checks against the chord notes instead.
    /// Kept for compatibility.
    /// </summary>
    public bool ScaleContainsNote(NoteName note)
    {
        var scales = GetScales();
        if (scales == null || scales.Count == 0)
        {
            // No scales configured -> check chord notes
            return GetChord().NotesNames.Contains(note);
        }

        return scales.Any(s => s.GetNotes().Any(n => n.NoteName == note));
    }

    /// <summary>
    /// Overload: checks if a note is in any configured scale and returns the subset of scales that contain it.
    /// If no scales configured, returns false and an empty list (use ChordContainsNote to check chord).
    /// </summary>
    public bool ScaleContainsNote(NoteName note, out List<Scale> containingScales)
    {
        containingScales = new List<Scale>();
        var scales = GetScales();
        if (scales == null || scales.Count == 0)
            return false;

        foreach (var s in scales)
        {
            if (s.GetNotes().Any(n => n.NoteName == note))
                containingScales.Add(s);
        }

        return containingScales.Count > 0;
    }

    /// <summary>
    /// New: returns the subset of parsed Scale objects that contain the provided note.
    /// If no scales are configured, returns an empty list (use ChordContainsNote to check chord).
    /// </summary>
    public List<Scale> GetScalesContainingNote(NoteName note)
    {
        var scales = GetScales();
        if (scales == null || scales.Count == 0)
            return new List<Scale>();

        return scales.Where(s => s.GetNotes().Any(n => n.NoteName == note)).ToList();
    }

    /// <summary>
    /// New: returns the list of ScaleNoteMapping entries whose note lists include the provided note.
    /// If no scales are configured and the chord contains the note, returns a single mapping with Scale = null (chord fallback).
    /// </summary>
    public List<ScaleNoteMapping> GetScaleNoteMappingsContainingNote(NoteName note)
    {
        var mappings = GetScaleNoteMappings();
        return mappings.Where(m => m.NoteNames != null && m.NoteNames.Any(n => n == note)).ToList();
    }

    /// <summary>
    /// Checks if a given note is in this chord
    /// </summary>
    public bool ChordContainsNote(NoteName note)
    {
        return GetChord().NotesNames.Contains(note);
    }

    /// <summary>
    /// Gets the root note of this chord
    /// </summary>
    public NoteName GetRootNote()
    {
        return GetChord().RootNoteName;
    }

    /// <summary>
    /// Simple container for returning a Scale and its note names together.
    /// Scale will be null for the chord-fallback mapping.
    /// </summary>
    public class ScaleNoteMapping
    {
        public Scale Scale;
        public NoteName[] NoteNames;
        public string DisplayName;
    }
}

/// <summary>
/// ScriptableObject for storing a song's chord progression
/// </summary>
[CreateAssetMenu(fileName = "NewChordProgression", menuName = "Music/Chord Progression", order = 1)]
public class ChordProgression : ScriptableObject
{
    [Header("Song Information")]
    [SerializeField] public string songName = "Untitled";
    [SerializeField] public string artist = "";
    [SerializeField] public string key = "C";
    [SerializeField] public int numLoops = 1;

    [Header("Timing")]
    [SerializeField] public float bpm = 120f;
    [SerializeField] public int beatsPerBar = 4;

    [Header("Chord Progression")]
    [SerializeField] public List<ChordSection> chordSections = new List<ChordSection>();

    public int GetTotalBars()
    {
        int totalBars = 0;
        foreach (var section in chordSections)
        {
            totalBars += section.durationInBars;
        }
        return totalBars * numLoops;
    }

    public int GetProgressionBars()
    {
        int totalBars = 0;
        foreach (var section in chordSections)
        {
            totalBars += section.durationInBars;
        }
        return totalBars;
    }

    public Chord GetChordAtBar(int barIndex)
    {
        int barsCounted = 0;
        foreach (var section in chordSections)
        {
            for (int i = 0; i < section.durationInBars; i++)
            {
                if (barsCounted == barIndex % GetProgressionBars())
                {
                    return section.GetChord();
                }
                barsCounted++;
            }
        }
        return null;
    }
}