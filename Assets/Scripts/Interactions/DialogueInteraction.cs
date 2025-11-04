using UnityEngine;

public class DialogueInteraction : MonoBehaviour
{
    public bool isPlayerInInteractionZone = false;
    public bool isMenuOpen = false;
    public GameObject menuUI;
    public PlayerController _playerController;
    void Update()
    {
        if (isPlayerInInteractionZone)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                isMenuOpen = !isMenuOpen;
                menuUI.SetActive(isMenuOpen);
                _playerController.SetCanMove(!isMenuOpen);

            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInInteractionZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInInteractionZone = false;
    }
}
