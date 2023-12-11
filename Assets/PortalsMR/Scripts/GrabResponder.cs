using PortalsVR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabResponder : MonoBehaviour
{
    private XRGrabInteractable interactable;

    int defaultLayer = 0;
	World parentWorld;
	PortalTraveller traveller;


	private void Awake()
    {
        interactable = GetComponent<XRGrabInteractable>();

        interactable.selectEntered.AddListener(OnSelected);
		interactable.selectExited.AddListener(OnDeselected);

        defaultLayer = gameObject.layer;

		parentWorld = GetComponentInParent<World>();
		traveller = FindObjectOfType<PortalTraveller>();
	}

    private void OnSelected(SelectEnterEventArgs arg0)
    {
		if (parentWorld)
		{
			parentWorld.Remove(gameObject);
			parentWorld = null;
		}

		// Make sure the object is visible
		gameObject.layer = 0; // defaultLayer;
	}

	private void OnDeselected(SelectExitEventArgs arg0)
	{
		// Parent the object to the current user's active world
		if (traveller) {
			parentWorld = World.worlds[traveller.activeWorld];
			parentWorld.Add(gameObject);
		}
	}
}