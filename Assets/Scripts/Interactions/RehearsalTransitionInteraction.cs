using UnityEngine;
using UnityEngine.SceneManagement;

public class RehearsalTransitionInteraction : MonoBehaviour
{
    public bool isPlayerInInteractionZone = false;
    void Update()
    {
        if (isPlayerInInteractionZone)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                SceneManager.LoadScene("Rehearsal");
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
