using UnityEngine;

public class DontDestroyOnLoad : Singleton<DontDestroyOnLoad>
{
    private void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("DontDestroyOnLoad");

        foreach (GameObject obj in objs)
        {
            DontDestroyOnLoad(obj);
        }

        objs = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject obj in objs)
        {
            DontDestroyOnLoad(obj);
        }
    }
}