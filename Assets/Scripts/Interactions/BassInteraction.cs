using UnityEngine;

public class BassInteraction : MonoBehaviour
{
    public bool isPlayerInInteractionZone = false;
    public bool isMenuOpen = false;
    public GameObject menuUI;
    public PlayerController _playerController;

    public Camera oldCamera;
    public Camera newCamera;
    void Update()
    {
        if (isPlayerInInteractionZone)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                isMenuOpen = true;
                menuUI.SetActive(isMenuOpen);
                _playerController.SetCanMove(!isMenuOpen);

                oldCamera.enabled = false;
                newCamera.enabled = true;
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
