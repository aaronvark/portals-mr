using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectMover : MonoBehaviour
{
	public delegate void ObjectInput(MovableObject obj);
	public static ObjectInput setActiveObject { get; private set; }

	enum ScaleMode
	{
		UNIFORM = 0,
		X = 1,
		Y = 2,
		Z = 3,
		MAX = 4
	}
    public InputActionReference moveXZRef, moveYRotRef, gripPressed;

    //public InputActionReference scaleRef, scaleModeRef;

	public InputActionReference deselectRef;

    private InputAction moveXZ, moveYRot, deselect, gripAction;
	private ScaleMode scaleMode = ScaleMode.X;

	public MovableObject active = null;

	private SpatialAnchorBehaviour sab;

	private void Awake()
	{
		setActiveObject = SetActiveObject;

		moveYRot = moveYRotRef.action;
		moveXZ = moveXZRef.action;
		gripAction = gripPressed.action;
		//scale = scaleRef.action;
		//scaleModeAction = scaleModeRef.action;
		//scaleModeAction.started += ScaleModeAction_started;

		deselect = deselectRef.action;
		deselect.started += Deselect_started;

		sab = FindObjectOfType<SpatialAnchorBehaviour>();
	}

	private void Start()
	{
		WorldCanvas.changeCanvasText($"");
	}

	private void ScaleModeAction_started(InputAction.CallbackContext obj)
	{
		if (active == null) return;

		scaleMode = (ScaleMode) ( (int)(scaleMode+1) % (int)ScaleMode.MAX );
		WorldCanvas.changeCanvasText($"Selected: {active.gameObject.name}\nScaleMode: {scaleMode}");
	}

	private void Deselect_started(InputAction.CallbackContext obj)
	{
		if ( active )
		{
			active.Store();
			active.Moving(false);
			active = null;
			WorldCanvas.changeCanvasText($"");
		}
	}

	// Update is called once per frame
	void Update()
    {
        if ( active != null )
        {
			Vector2 xz = moveXZ.ReadValue<Vector2>();// * Time.deltaTime * ( .1f + gripAction.ReadValue<float>() * 5f );
			Vector2 yr = moveYRot.ReadValue<Vector2>();// * Time.deltaTime * ( .1f + gripAction.ReadValue<float>() * 5f );

			float moveSpeed = Time.deltaTime * (.1f + gripAction.ReadValue<float>() * 5f );
			float rotSpeed = Time.deltaTime * ( 5 + gripAction.ReadValue<float>() * 180f );

			Vector3 offset = Vector3.zero;
			offset.x += xz.x * moveSpeed;
			offset.y += yr.y * moveSpeed;
			offset.z += xz.y * moveSpeed;

			Vector3 forward = Camera.main.transform.forward;
			forward.y = 0;
			forward.Normalize();

			//TEST: Rotate the movement over this forward direction relative to world forward
			Quaternion q = Quaternion.FromToRotation(Vector3.forward, forward);
			active.transform.Translate(q * offset, Space.World);
			active.transform.Rotate(Vector3.up, yr.x * rotSpeed );
		}
	}

    public void SetActiveObject( MovableObject obj )
    {
		if ( sab.anchorActive )
		{
			scaleMode = ScaleMode.UNIFORM;
			active = obj;
			active.Moving(true);
			WorldCanvas.changeCanvasText($"Selected: {active.gameObject.name}\nScaleMode: {scaleMode}");
		}
		else
		{
			WorldCanvas.changeCanvasText($"Selected: BLOCKED");
			StartCoroutine(EmptyCanvas());
		}
	}

	IEnumerator EmptyCanvas()
	{
		yield return new WaitForSeconds(.5f);
		WorldCanvas.changeCanvasText($"");
	}

	public void TriggerDeselect()
	{
		if ( active )
		{
			active.Store();
			active.Moving(false);
			active = null;
			scaleMode = ScaleMode.UNIFORM;
			WorldCanvas.changeCanvasText($"Selected: CANCELLED");
			StartCoroutine(EmptyCanvas());
		}
	}
}