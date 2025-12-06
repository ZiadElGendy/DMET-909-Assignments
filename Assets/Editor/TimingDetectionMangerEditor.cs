using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TimingDetectionManager))]
public class TimingDetectionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TimingDetectionManager manager = (TimingDetectionManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Clock Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Start", GUILayout.Height(30)))
        {
            manager.StartClock();
        }

        if (GUILayout.Button("Pause", GUILayout.Height(30)))
        {
            manager.PauseClock();
        }

        if (GUILayout.Button("Restart", GUILayout.Height(30)))
        {
            manager.RestartClock();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);

        // Current beat display
        int currentBeat = manager.GetCurrentBeat();
        EditorGUILayout.LabelField($"Current Beat: {currentBeat + 1}", EditorStyles.largeLabel);

        // Beat progress bar
        float progress = manager.GetBeatProgress();
        Rect progressRect = GUILayoutUtility.GetRect(18, 30);
        EditorGUI.ProgressBar(progressRect, progress, $"Beat Progress: {progress:P0}");

        // On beat indicator
        bool isOnBeat = manager.IsOnBeat();
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = isOnBeat ? Color.green : Color.red;

        GUIStyle indicatorStyle = new GUIStyle(GUI.skin.box);
        indicatorStyle.alignment = TextAnchor.MiddleCenter;
        indicatorStyle.fontSize = 14;
        indicatorStyle.fontStyle = FontStyle.Bold;

        GUILayout.Box(isOnBeat ? "IN TIMING WINDOW" : "Outside Window", indicatorStyle, GUILayout.Height(40));
        GUI.backgroundColor = originalColor;

        // Visual beat indicator (4 boxes representing beats)
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Beat Visualization:");
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < 4; i++)
        {
            GUI.backgroundColor = (currentBeat == i) ? Color.cyan : Color.gray;
            GUILayout.Box((i + 1).ToString(), GUILayout.Height(50), GUILayout.ExpandWidth(true));
        }

        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndHorizontal();

        // Force repaint while playing
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}