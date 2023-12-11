using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(XRRayInteractor))]
public class LineController : MonoBehaviour
{
    public InputActionReference actionReference;
    private InputAction action;

    LineRenderer lRend;
    XRRayInteractor interactor;

    // Start is called before the first frame update
    void Awake()
    {
        lRend = GetComponent<LineRenderer>();
        interactor = GetComponent<XRRayInteractor>();

        action = actionReference.action;

        action.started += Action_started;
        action.canceled += Action_canceled;
    }

    private void Update()
    {
        if ( lRend.enabled )
        {
            // check if we're hitting a floor
            lRend.startColor = interactor.hasHover ? Color.green : Color.red;
		}
    }

    private void Action_started(InputAction.CallbackContext obj)
	{
        lRend.enabled = true;
	}

	private void Action_canceled(InputAction.CallbackContext obj)
    {
        lRend.enabled = false;
    }
}
