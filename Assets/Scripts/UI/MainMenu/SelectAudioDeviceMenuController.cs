using UnityEngine;
using UnityEngine.SceneManagement;

public class InputDeviceMenuController : MonoBehaviour
{
    // Index of the selected recording device/driver
    int recordDriverIndex = 0;
    public PlayerSettings settings;


    void OnGUI()
    {
        var width = 400f;
        var height = 600f;
        var rect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        var previousLabelStyle = GUI.skin.label.fontSize;
        var previousButtonStyle = GUI.skin.button.fontSize;

        GUI.skin.label.fontSize = 20;
        GUI.skin.button.fontSize = 20;

        GUILayout.BeginArea(rect, GUI.skin.window);

        GUILayout.Label("Select Microphone:");

        if (FMODUnity.RuntimeManager.CoreSystem.hasHandle())
        {
            int numDrivers = 0;
            int numConnected = 0;
            FMODUnity.RuntimeManager.CoreSystem.getRecordNumDrivers(out numDrivers, out numConnected);

            for (int i = 0; i < numConnected; i++)
            {
                string name;
                System.Guid guid;
                int systemRate;
                FMOD.SPEAKERMODE speakerMode;
                int speakerModeChannels;
                FMOD.DRIVER_STATE driverState;

                FMODUnity.RuntimeManager.CoreSystem.getRecordDriverInfo(
                    i,
                    out name,
                    64,
                    out guid,
                    out systemRate,
                    out speakerMode,
                    out speakerModeChannels,
                    out driverState
                );

                if (GUILayout.Button(name, GUILayout.Height(50)))
                    SelectDevice(i);
            }
        }

        GUILayout.EndArea();

        GUI.skin.label.fontSize = previousLabelStyle;
        GUI.skin.button.fontSize = previousButtonStyle;
    }

    public void SelectDevice(int index)
    {
        recordDriverIndex = index;
        settings.selectedAudioDeviceIndex = index;
        SceneManager.LoadScene("WorldMap");
    }
}
