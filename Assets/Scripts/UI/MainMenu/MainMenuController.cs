using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var startButton = root.Q<Button>("Start");
        var exitButton = root.Q<Button>("Exit");

        startButton.clicked += OnStartPressed;
        exitButton.clicked += OnExitPressed;
    }

    private void OnStartPressed()
    {
        MainMenuUIManager.Instance.ShowInputMenu();
    }

    private void OnExitPressed()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
