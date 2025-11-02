using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class ShadowsToggleWindow : EditorWindow
{
    private GameObject targetObject;
    private bool castShadows = true;
    private bool receiveShadows = true;

    [MenuItem("Tools/Shadows Toggle")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<ShadowsToggleWindow>("Shadows Toggle");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Select GameObject:", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target", targetObject, typeof(GameObject), true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Shadow Settings:", EditorStyles.boldLabel);
        castShadows    = EditorGUILayout.Toggle("Cast Shadows", castShadows);
        receiveShadows = EditorGUILayout.Toggle("Receive Shadows", receiveShadows);

        EditorGUILayout.Space();

        GUI.enabled = targetObject != null;
        if (GUILayout.Button("Apply to Target & Children"))
        {
            ApplyShadowSettings(targetObject.transform, castShadows, receiveShadows);
            Debug.Log($"Applied settings to {targetObject.name} and its children.");
        }
        GUI.enabled = true;
    }

    private static void ApplyShadowSettings(Transform t, bool castOn, bool receiveOn)
    {
        Renderer[] renderers = t.GetComponents<Renderer>();
        foreach (var rend in renderers)
        {
            Undo.RecordObject(rend, "Toggle shadows");
            rend.shadowCastingMode = castOn ? ShadowCastingMode.On : ShadowCastingMode.Off;
            rend.receiveShadows     = receiveOn;
            EditorUtility.SetDirty(rend);
        }

        for (int i = 0; i < t.childCount; i++)
        {
            ApplyShadowSettings(t.GetChild(i), castOn, receiveOn);
        }
    }
}