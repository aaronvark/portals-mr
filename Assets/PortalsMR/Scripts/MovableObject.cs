using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(BoxCollider))]
public class MovableObject : XRBaseInteractable
{
	public class StoredData
	{
		public Vector3 offset;
		public Vector3 forward;
		public Vector3 bX, bY, bZ;
	}

    public int uniqueID = -1;
    public bool isPrimary = false;
	public AudioClip debugSound;

	public Action onMoved;

	private string guid = null;
	private OVRSpatialAnchor myAnchor;

	private static SpatialAnchorBehaviour anchorManager;
	private static SpatialAnchorBehaviour AnchorManager
	{
		get
		{
			if (anchorManager == null )
			{
				anchorManager = FindObjectOfType<SpatialAnchorBehaviour>();
			}
			return anchorManager;
		}
	}

	bool started = false;
	bool moving = false;
	bool anchorReturned = false;

	StoredData myData = null;
	SpatialAnchorBehaviour.AnchorData[] anchorData = null;
	int randomFrameOffset;

    [ContextMenu("GenerateMovableIDs")]
    void GenerateIDs()
    {
        MovableObject[] objects = FindObjectsOfType<MovableObject>();
        int highest = 0;
        for (int i = 0; i < objects.Length; ++i)
        {
            {
                if (highest < objects[i].uniqueID)
                    highest = objects[i].uniqueID;
            }
        }
        foreach( MovableObject mo in objects )
        {
            if (mo.uniqueID == -1)
            {
                mo.uniqueID = ++highest;
            }
        }
    }

	protected override void Awake()
	{
		base.Awake();

		// TODO: Load Spatial Anchor GUID
		string key = $"movable-{SceneManager.GetActiveScene().name}-{uniqueID}";
		if ( PlayerPrefs.HasKey(key))
		{
			guid = PlayerPrefs.GetString(key);
		}
	}

	// Start is called before the first frame update
	IEnumerator Start()
    {
		while( !AnchorManager.ReadyForQueries )
		{
			yield return null;
		}

		yield return new WaitForSeconds(.5f);

		// TODO: Get our anchor (new or existing) based on loaded GUID
		anchorReturned = false;
		AnchorManager.RequestAnchor(transform.position, onAnchorReturned, guid);
		while (!anchorReturned) yield return null;

		LoadTransform();
		started = true;

		AnchorManager.onAnchorMoved += AnchorMoved;

		randomFrameOffset = UnityEngine.Random.Range(0, 120);
	}

	private void onAnchorReturned(OVRSpatialAnchor obj)
	{
		myAnchor = obj;
		if (started) DoStore();

		if (obj != null)
		{
			string key = $"movable-{SceneManager.GetActiveScene().name}-{uniqueID}";
			guid = myAnchor.Uuid.ToString();
			PlayerPrefs.SetString(key, guid);
		}

		anchorReturned = true;
	}

	public void Moving( bool moving )
	{
		this.moving = moving;
	}

	private void AnchorMoved()
	{
		// The environment has moved, so remake your anchor
		// (we assume the object has been repositioned)
		Store();
	}

	protected override void OnEnable()
    {
        base.OnEnable();
        if (started) LoadTransform();
	}

	private void LateUpdate()
	{
		if ( AnchorManager.anchorActive && !moving && anchorReturned )
		{
			RecalcOffset();
		}
		else if ( moving )
		{
			onMoved?.Invoke();
		}
	}

	private void LoadTransform()
	{
		if (uniqueID != -1)
		{
			bool found = false;

			// check if we have stored information or not...
#if USE_EXISTING_DATA
			if (SavePlayerPrefs.useExistingData)
			{
				if (SavePlayerPrefs.positions.ContainsKey("moveable-" + uniqueID))
				{
					PositionData pd = SavePlayerPrefs.positions["moveable-" + uniqueID];

					transform.localPosition = pd.pos;
					transform.localScale = pd.scale;
					
					found = true;
				}
			}
#endif

			if (!found)
			{
				// check if there is a stored localPosition
				if (PlayerPrefs.HasKey("moveable-" + uniqueID))
				{
					Debug.LogError("LOADING PLAYERPREF DATA");

					if ( myData == null )
					{
						myData = new StoredData();
					}

					myData.offset.x = PlayerPrefs.GetFloat("moveable-" + uniqueID + "-localPos-x");
					myData.offset.y = PlayerPrefs.GetFloat("moveable-" + uniqueID + "-localPos-y");
					myData.offset.z = PlayerPrefs.GetFloat("moveable-" + uniqueID + "-localPos-z");

					myData.forward.x = PlayerPrefs.GetFloat("moveable-" + uniqueID + "-localForward-x");
					myData.forward.y = PlayerPrefs.GetFloat("moveable-" + uniqueID + "-localForward-y");
					myData.forward.z = PlayerPrefs.GetFloat("moveable-" + uniqueID + "-localForward-z");

					if (myAnchor != null)
					{
						transform.position = myAnchor.transform.TransformPoint(myData.offset);
						transform.forward = myAnchor.transform.TransformDirection(myData.forward);

						// unparent from the whole if we have a localized position
						transform.parent = null;
						
						onMoved?.Invoke();
					}
				}
				else
				{
					Debug.LogError("NOTHING FOUND TO LOAD");
				}
				/*
				else if ( myAnchor != null )
				{
					myData = new StoredData();

					transform.position = myAnchor.transform.position;
					transform.forward = myAnchor.transform.forward;
				}
				*/
			}
		}
	}

	void RecalcOffset()
	{
		if (myData == null || myAnchor == null) return;//|| anchorData == null) return;

		transform.position = myAnchor.transform.TransformPoint(myData.offset);
		transform.forward = myAnchor.transform.TransformDirection(myData.forward);

		// unparent if we have a relative anchor set
		transform.parent = null;
		

		onMoved?.Invoke();
	}

	protected override void OnActivated(ActivateEventArgs args)
    {
        ObjectMover.setActiveObject(this);

		if ( debugSound )
		{
			AudioSource.PlayClipAtPoint(debugSound, transform.position);
		}

		base.OnActivated(args);
    }

	public void Store()
	{
		// TODO: Move our anchor
		if (myAnchor != null)
		{
			AnchorManager.RequestDeleteAnchor(myAnchor);
		}

		anchorReturned = false;
		AnchorManager.RequestAnchor(transform.position, onAnchorReturned, null);
	}

	void DoStore()
	{
		if (uniqueID != -1 && isPrimary && myAnchor != null)
		{
			PlayerPrefs.SetInt("moveable-" + uniqueID, uniqueID);

			Vector3 localPos = myAnchor.transform.InverseTransformPoint(transform.position);
			Vector3 localForward = myAnchor.transform.InverseTransformDirection(transform.forward);
			PlayerPrefs.SetFloat("moveable-" + uniqueID + "-localPos-x", localPos.x);
			PlayerPrefs.SetFloat("moveable-" + uniqueID + "-localPos-y", localPos.y);
			PlayerPrefs.SetFloat("moveable-" + uniqueID + "-localPos-z", localPos.z);
			PlayerPrefs.SetFloat("moveable-" + uniqueID + "-localForward-x", localForward.x);
			PlayerPrefs.SetFloat("moveable-" + uniqueID + "-localForward-y", localForward.y);
			PlayerPrefs.SetFloat("moveable-" + uniqueID + "-localForward-z", localForward.z);

			Debug.Log("STORED!");

			if (myData == null) myData = new StoredData();
			myData.offset = localPos;
			myData.forward = localForward;
		}
		else
		{
			Debug.Log("NOT STORED!");
		}
	}
}
