using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    [Header("References")]
    public List<GameObject> lightObjects;
    public Transform sunLight;
    public Transform player;
    public Transform target;

    [Header("Settings")]
    public float activationDistance = 10f;

    private float startRotationX = 60f;
    private float endRotationX = -100f;
    private bool lightsAreActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        setLightObjectsActive(false);
    }

    void Update()
    {
        // Calculate distance between player and target
        float distance = Vector3.Distance(player.position, target.position);

        // Calculate lerp factor (0 when at activation distance, 1 when at target)
        float lerpFactor = Mathf.Clamp01(1f - (distance / activationDistance));

        // Lerp the X rotation
        float newRotationX = Mathf.Lerp(startRotationX, endRotationX, lerpFactor);

        // Apply the new rotation to the sun light while preserving Y and Z rotations
        Vector3 currentRotation = sunLight.eulerAngles;
        sunLight.eulerAngles = new Vector3(newRotationX, currentRotation.y, currentRotation.z);

        // Manage light objects activation based on distance
        ManageLightActivation(distance);
    }

    private void ManageLightActivation(float distance)
    {
        // If player is within activation distance and lights are not active
        if (distance <= activationDistance/1.5 && !lightsAreActive)
        {
            setLightObjectsActive(true);
            lightsAreActive = true;
        }
        // If player leaves activation distance and lights are still active
        else if (distance > activationDistance/1.5 && lightsAreActive)
        {
            setLightObjectsActive(false);
            lightsAreActive = false;
        }
    }

    private void setLightObjectsActive(bool isActive)
    {
        foreach (var lightObject in lightObjects)
        {
            if (lightObject != null)
            {
                lightObject.SetActive(isActive);
            }
        }
    }

    // Optional: Visualize the activation distance in the editor
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position, activationDistance);
        }
    }
}