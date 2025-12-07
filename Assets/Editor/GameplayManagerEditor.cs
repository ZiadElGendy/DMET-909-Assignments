using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameplayManager))]
public class GameplayManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameplayManager gameplayManager = (GameplayManager)target;

        if (GUILayout.Button("Start Game"))
        {
            if (Application.isPlaying)
            {
                gameplayManager.StartGame();
            }
            else
            {
                Debug.LogWarning("The game must be running to start the game from the editor.");
            }
        }
    }
}
