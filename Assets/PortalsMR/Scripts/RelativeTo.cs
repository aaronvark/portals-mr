#if UNITY_EDITOR
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Callbacks;
#endif

using UnityEngine;

public class RelativeTo : MonoBehaviour
{
    public MovableObject movableObject;
    public Vector3 offset = Vector3.zero, forward = Vector3.forward, up = Vector3.up;

    private bool paused = false;

    // Start is called before the first frame update
    void Start()
    {
        if (movableObject == null)
        {
            enabled = false;
            return;
        }

        movableObject.onMoved += OnMoved;
	}

    public void Pause( bool pause )
    {
        this.paused = pause;
        if (!pause) RecalcOffset();
    }

    // Update is called once per frame
    void OnMoved()
    {
        if (paused) return;

        transform.position = movableObject.transform.position + movableObject.transform.TransformDirection(offset);
        transform.LookAt(transform.position + movableObject.transform.TransformDirection(forward), movableObject.transform.TransformDirection(up));
	}

    void RecalcOffset()
    {
		if (movableObject == null) return;

		offset = movableObject.transform.InverseTransformDirection(transform.position - movableObject.transform.position);
		forward = movableObject.transform.InverseTransformDirection(transform.forward);
		up = movableObject.transform.InverseTransformDirection(transform.up);
	}

#if UNITY_EDITOR
	[PostProcessScene()]
	public static void OnPostprocessScene()
	{
		RelativeTo[] relatives = FindObjectsOfType<RelativeTo>();

        foreach( RelativeTo relative in relatives )
        {
            if (relative.movableObject == null) continue;

            relative.offset = relative.movableObject.transform.InverseTransformDirection(relative.transform.position - relative.movableObject.transform.position);
            relative.forward = relative.movableObject.transform.InverseTransformDirection(relative.transform.forward);
            relative.up = relative.movableObject.transform.InverseTransformDirection(relative.transform.up);
		}
	}
#endif
}