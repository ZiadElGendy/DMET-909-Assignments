using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "PlayerSettings")]
public class PlayerSettings : ScriptableObject
{
    public int selectedAudioDeviceIndex = 0;
}
