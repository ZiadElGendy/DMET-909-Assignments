using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public Transform InteractionPoint;
    public GameObject Icon;
    public List<MMF_Player> IdleFeedbacks;
    public List<MMF_Player> ActiveFeedbacks;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        IdleFeedbacks.ForEach(feedback => feedback?.StopFeedbacks());
        ActiveFeedbacks.ForEach(feedback => feedback?.PlayFeedbacks());
        Icon.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        IdleFeedbacks.ForEach(feedback => feedback?.PlayFeedbacks());
        ActiveFeedbacks.ForEach(feedback => feedback?.StopFeedbacks());
        Icon.SetActive(false);
    }
}

