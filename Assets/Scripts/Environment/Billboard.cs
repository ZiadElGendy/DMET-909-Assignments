using UnityEngine;

public class Billboard : MonoBehaviour
{

    private Camera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(mainCamera.transform);
        //transform.rotation = Quaternion.Euler(0f,transform.rotation.eulerAngles.y, 0f);
    }
}
