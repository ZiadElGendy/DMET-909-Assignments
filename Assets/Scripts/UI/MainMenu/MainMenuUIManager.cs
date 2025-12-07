using UnityEngine;

public class MainMenuUIManager : Singleton<MainMenuUIManager>
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject inputMenu;

    private void Awake()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        inputMenu.SetActive(false);
    }

    public void ShowInputMenu()
    {
        mainMenu.SetActive(false);
        inputMenu.SetActive(true);
    }
}
