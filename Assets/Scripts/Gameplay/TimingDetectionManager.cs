using AudioHelm;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class TimingDetectionManager : Singleton<TimingDetectionManager>
{
    public bool isInTimingWindow = false;
    public bool metronomeEnabled = false;

    public EventReference metronomeEvent;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleSequencedEvents(Note note)
    {
        Debug.Log(note.note);
        switch (note.note)
        {
            case 71: //B4
                SetTimingWindow(true);
                break;
            case 72: //C4
                if(metronomeEnabled)
                {
                    PlayMetronomeClick();
                }
                break;
            case 73: //C#4
                SetTimingWindow(false);
                break;

        }
    }

    public void SetTimingWindow(bool state)
    {
        isInTimingWindow = state;
    }

    public void PlayMetronomeClick()
    {
        RuntimeManager.PlayOneShot(metronomeEvent);
    }
}
