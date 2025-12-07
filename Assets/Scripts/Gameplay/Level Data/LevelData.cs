using UnityEngine;
using FMODUnity;

namespace Gameplay.Level_Data
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Gameplay/Level Data", order = 0)]
    public class LevelData : ScriptableObject
    {
        public string levelName;
        public NotePitchValidationStrategy notePitchValidationStrategy;
        public NoteTimingValidationStrategy noteTimingValidationStrategy;
        public ChordProgression chordProgression;
        public EventReference backingMusicEvent;
    }
}