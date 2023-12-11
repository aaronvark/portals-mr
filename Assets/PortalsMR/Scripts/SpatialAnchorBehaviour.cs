using Meta.WitAi;
using OculusSampleFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static OVRSpatialAnchor;


/// <summary>
/// Serves as the core class to request anchors. Also allows the entire virtual environment to be displaced
///		but we should probably destroy and recreate anchors when this happens (or not allow this at all).
///		
/// A list of spaces is maintained, and each space list has a set of known anchor UUIDs that can be requested by objects.
///		TODO: What do we do with an anchor request that is known but not currently available?
/// </summary>
public class SpatialAnchorBehaviour : MonoBehaviour
{
	public struct AnchorData
	{
		public Guid uuid;
		public OVRSpatialAnchor instance;
		public Vector3 virtualPosition;

		public AnchorData(OVRSpatialAnchor reference, Vector3 vPos )
		{
			this.instance = reference;
			this.uuid = reference.Uuid;
			this.virtualPosition = vPos;
		}
	}

	public Action onAnchorMoved;

	public InputActionReference actionRef_DeleteCreateAnchor;
	public InputActionReference actionRef_MoveXZ;
	public InputActionReference actionRef_MoveYRot;
	public InputActionReference actionRef_RedoPositioning;
	
	// public string ANCHOR_UUID = "uuid";
	public bool anchorActive = true;
	public List<string> uuidList = new List<string>();
	public HashSet<string> spaceUuidList = new HashSet<string>();

	public Transform target;
	public GameObject debugPrefab;

	public bool ReadyForQueries
	{
		get
		{
			return !requestedNewAnchor && initialized;
		}
	}

	private OVRSpatialAnchor prevAnchor;
	private AnchorDebug prev;

	private Vector3 offsetPos = Vector3.zero;
	private Vector3 offsetForward = Vector3.zero;
	private float offsetYRot = 0;
	private GameObject root;

	private Dictionary<Guid, Vector3> anchorPositions = new Dictionary<Guid, Vector3>();
	private bool requestedNewAnchor = false;
	private bool initialized = false;

	private InputAction moveXZ, moveYRot, deleteCreateAnchor, redoPositioning;
	private List<OVRSpatialAnchor> anchors = new List<OVRSpatialAnchor>();
	private Dictionary<OVRSpatialAnchor, GameObject> debugInstances = new Dictionary<OVRSpatialAnchor, GameObject>();
	private OVRCameraRig ovrRig;

	List<UnboundAnchor> unboundAnchors;
	bool loadedUnboundAnchors = false;
	string activeSpace = "";

	void Awake()
	{
		// ANCHOR_UUID = SceneManager.GetActiveScene().path + ANCHOR_UUID;
		ovrRig = FindObjectOfType<OVRCameraRig>();
	}

	public List<AnchorData> GetClosestAnchors( Vector3 position, int count )
	{
		if (anchors == null) return null;

		List<AnchorData> retList = null;

		OVRSpatialAnchor[] closest = anchors.OrderBy(anchor => Vector3.Distance(anchor.transform.position, position)).ToArray();
		if ( closest.Length >= count )
		{
			retList = new List<AnchorData>();
			for ( int i = 0; i < count; ++i )
			{
				AnchorData data = new AnchorData(closest[i], anchorPositions[closest[i].Uuid]);
				retList.Add(data);
			}
		}

		return retList;
	}

	public bool RebuildData( ref AnchorData data )
	{
		if (anchorPositions.ContainsKey(data.uuid))
		{
			data.virtualPosition = anchorPositions[data.uuid];
		}

		for( int i = 0; i < anchors.Count; ++i )
		{
			if (anchors[i].Uuid == data.uuid )
			{
				data.instance = anchors[i];
				return true;
			}
		}

		return false;
	}

	// Start is called before the first frame update
	IEnumerator Start()
	{
		requestedNewAnchor = true;

		if (actionRef_MoveXZ && actionRef_MoveYRot && actionRef_DeleteCreateAnchor)
		{
			moveXZ = actionRef_MoveXZ.action;
			moveYRot = actionRef_MoveYRot.action;
			deleteCreateAnchor = actionRef_DeleteCreateAnchor.action;

			deleteCreateAnchor.started += DeleteCreateAnchor_started;
		}

		// TODO: Check if this delay is still relevant...
		yield return new WaitForSeconds(1f);

		// Check if the Quest has any previously saved Spaces
		//	That way, we can only load the previously stored anchors related to this Space's UUID (if any exist)
		Debug.Log("ATTEMPTING TO LOAD SPACES");

		if (PlayerPrefs.HasKey("spaces"))
		{
			Debug.Log("GOT SPACES, LOADING");
			spaceUuidList = new HashSet<string>();
			string[] spaceList = JsonHelper.FromJson<string>(PlayerPrefs.GetString("spaces"));
			foreach (string space in spaceList)
				spaceUuidList.Add(space);

			StartCoroutine(TryLoadExistingSpace());
		}
		else //(!OVRPlugin.QuerySpaces(info, out requestId))
		{
			Debug.LogError("NO EXISTING SPACES SAVED");
			requestedNewAnchor = true;
			yield return InitializeAnchor();
		}

		yield return null;
	}

	private IEnumerator TryLoadExistingSpace()
	{
		string spaceIdFound = null;
		foreach ( string spaceUuid in spaceUuidList )
		{
			Debug.Log($"CHECKING FOR {spaceUuid}");
			// Try to load a single anchor from this space, and if it can be localized, load the whole set
			if (PlayerPrefs.HasKey(spaceUuid))
			{
				Debug.Log($"LOADING {spaceUuid}");

				// Get all anchors associated with this space, and filter one out
				List<string> anchorUUIDs = new List<string>();
				anchorUUIDs.AddRange(JsonHelper.FromJson<string>(PlayerPrefs.GetString(spaceUuid)));
				List<string> testAnchor = new List<string>();
				testAnchor.Add(anchorUUIDs[0]);

				// Load it
				OVRSpatialAnchor.LoadOptions options = new OVRSpatialAnchor.LoadOptions();
				options.StorageLocation = OVRSpace.StorageLocation.Local;
				options.Uuids = testAnchor.Select(System.Guid.Parse).ToList();
				options.Timeout = 10;
				var task1 = OVRSpatialAnchor.LoadUnboundAnchorsAsync(options);
				while (!task1.IsCompleted) yield return null;

				Debug.Log($"LOADED UNBOUND ANCHORS");

				// Check if it can be localized
				UnboundAnchor[] anchors = task1.GetResult();
				if (anchors != null && anchors.Length > 0)
				{
					var task2 = anchors[0].LocalizeAsync();
					while (!task2.IsCompleted) yield return null;

					// If it can, this space is valid and we can load the entire thing
					if (task2.GetResult())
					{
						Debug.Log($"LOCALIZED UNBOUND ANCHOR");
						spaceIdFound = spaceUuid;
						break;
					}
				}
				Debug.Log($"COULD NOT LOCALIZE UNBOUND ANCHOR");
			}
		}

		// No space localized, start over
		if (spaceIdFound == null)
		{
			Debug.Log($"NO SPACE LOCALIZED, STARTING OVER");

			requestedNewAnchor = true;
			yield return InitializeAnchor();
			yield break;
		}

		Debug.Log($"SPACE {spaceIdFound} LOCALIZED, ATTEMPTING LOAD");
		activeSpace = spaceIdFound;

		if (PlayerPrefs.HasKey(activeSpace))
		{
			Debug.Log($"ATTEMPTING TO LOAD ANCHORS ASSOCIATED WITH SPACE: {activeSpace}");

			if (PlayerPrefs.HasKey(activeSpace + "offset-x"))
			{
				offsetPos.x = PlayerPrefs.GetFloat(activeSpace + "offset-x");
				offsetPos.y = PlayerPrefs.GetFloat(activeSpace + "offset-y");
				offsetPos.z = PlayerPrefs.GetFloat(activeSpace + "offset-z");

				offsetForward.x = PlayerPrefs.GetFloat(activeSpace + "offset-fx");
				offsetForward.y = PlayerPrefs.GetFloat(activeSpace + "offset-fy");
				offsetForward.z = PlayerPrefs.GetFloat(activeSpace + "offset-fz");
			}

			OVRSpatialAnchor.LoadOptions options = new OVRSpatialAnchor.LoadOptions();
			options.StorageLocation = OVRSpace.StorageLocation.Local;

			uuidList = new List<string>();
			uuidList.AddRange(JsonHelper.FromJson<string>(PlayerPrefs.GetString(activeSpace)));

			options.Uuids = uuidList.Select(System.Guid.Parse).ToList();
			options.Timeout = 10;

			if (!OVRSpatialAnchor.LoadUnboundAnchors(options, onComplete))
			{
				Debug.LogError("COULD NOT LOAD PREVIOUSLY SAVED ANCHORS");
			}
			else
			{
				while (!loadedUnboundAnchors) yield return null;

				int tries = 0;

				if (unboundAnchors != null && unboundAnchors.Count > 0)
				{
					while (unboundAnchors.Count > 0 && tries < 30)
					{
						tries++;
						// TODO: Localize the anchors... if this fails we should wipe the anchor list and start over after a certain time
						for (int i = 0; i < unboundAnchors.Count; ++i)
						{
							UnboundAnchor unboundAnchor = unboundAnchors[i];

							if (unboundAnchor.Localized)
							{
								GameObject anchorGO = new GameObject();
								anchorGO.transform.parent = root.transform;
								Vector3 localPos;

								localPos.x = PlayerPrefs.GetFloat(unboundAnchor.Uuid.ToString() + "x");
								localPos.y = PlayerPrefs.GetFloat(unboundAnchor.Uuid.ToString() + "y");
								localPos.z = PlayerPrefs.GetFloat(unboundAnchor.Uuid.ToString() + "z");

								anchorGO.transform.localPosition = localPos;
								anchorPositions.Add(unboundAnchor.Uuid, localPos);

								OVRSpatialAnchor newAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
								unboundAnchor.BindTo(newAnchor);
								newAnchor.Save();

								this.anchors.Add(newAnchor);

								if (debugPrefab != null)
								{
									GameObject g = GameObject.Instantiate(debugPrefab);
									g.transform.parent = newAnchor.transform;
									g.transform.localPosition = Vector3.zero;
									AnchorDebug ad = g.GetComponent<AnchorDebug>();
									ad.SetData(localPos, newAnchor.transform.position);
									debugInstances.Add(newAnchor, g);
								}

								unboundAnchors.RemoveAt(i--);
							}
							else
							{
								unboundAnchor.Localize();
							}
						}

						if (unboundAnchors.Count > 0)
							yield return new WaitForSeconds(.5f);
					}

					// After 15 seconds of trying to load anchors associated with a localized space
					if (tries < 30 && unboundAnchors.Count != 0)
					{
						// TODO: Deal with this case if it occurs...
						//			I am kind of assuming that if any anchor is localized, they are all localized (since it is a connected space)
						Debug.LogError("SOME ANCHORS WERE NOT FOUND");
					}

					StartCoroutine(RecalcOffset());
				}
				else
				{
					StartCoroutine(InitializeAnchor());
				}
			}
		}
		// No anchors found in localized space, start over
		else
		{
			requestedNewAnchor = true;
			yield return InitializeAnchor();
		}

		yield return null;
	}

	private void LateUpdate()
	{
		// The environment as a whole is being moved
		if (!anchorActive)
		{
			Vector2 xz = moveXZ.ReadValue<Vector2>() * Time.deltaTime;
			Vector2 yRot = moveYRot.ReadValue<Vector2>();

			Vector3 offset = Vector3.zero;
			offset.x = -xz.x;
			offset.y = yRot.y * Time.deltaTime;
			offset.z = -xz.y;

			Vector3 forward = Camera.main.transform.forward;
			forward.y = 0;
			forward.Normalize();

			Quaternion q = Quaternion.FromToRotation(Vector3.forward, forward);

			target.transform.Translate(q * offset, Space.World);
			target.transform.RotateAround(ovrRig.centerEyeAnchor.position, Vector3.up, yRot.x * Time.deltaTime * 15);

			offsetPos = target.transform.position;
			offsetForward = target.transform.forward;
			offsetForward.y = 0;
			offsetForward.Normalize();
		}
	}

	public void RequestAnchor(Vector3 position, Action<OVRSpatialAnchor> onComplete, string guid = null)
	{
		OVRSpatialAnchor anchor = FindAnchor(guid);
		if (guid == null || anchor == null)
		{
			requestedNewAnchor = true;
			StartCoroutine(CreateAnchorAt(position, onComplete));
		}
		else onComplete?.Invoke(anchor);		
	}

	public bool RequestDeleteAnchor(OVRSpatialAnchor anchor)
	{
		if ( anchors.Contains(anchor) ) {
			anchors.Remove(anchor);

			GameObject obj = debugInstances[anchor];
			debugInstances.Remove(anchor);
			obj.DestroySafely();

			anchorPositions.Remove(anchor.Uuid);
			uuidList.Remove(anchor.Uuid.ToString());
			anchor.DestroySafely();
			return true;
		}

		return false;
	}

	// Attempts to find an anchor in currently loaded anchors
	OVRSpatialAnchor FindAnchor( string strGuid )
	{
		if (strGuid == null) return null;

		Guid guid = Guid.Parse(strGuid);
		foreach( OVRSpatialAnchor anchor in anchors )
		{
			if (anchor.Uuid == guid) return anchor;
		}
		return null;
	}

	// This should be re-named, since the anchor is not deleted any longer
	private void DeleteCreateAnchor_started(InputAction.CallbackContext obj)
	{
		// Not allowing environment root to be moved at this time
		anchorActive = !anchorActive;

		if (anchorActive)
		{
			//PlayerPrefs.SetFloat(activeSpace + "offset-x", offsetPos.x);
			//PlayerPrefs.SetFloat(activeSpace + "offset-y", offsetPos.y);
			//PlayerPrefs.SetFloat(activeSpace + "offset-z", offsetPos.z);
			//PlayerPrefs.SetFloat(activeSpace + "offset-fx", offsetForward.x);
			//PlayerPrefs.SetFloat(activeSpace + "offset-fy", offsetForward.y);
			//PlayerPrefs.SetFloat(activeSpace + "offset-fz", offsetForward.z);

			onAnchorMoved?.Invoke();
		}
	}

	void OnEraseComplete(OVRSpatialAnchor anch, bool succeeded)
	{
		if (succeeded) { Destroy(anch); }
		else
		{
			anch.enabled = true;
		}
	}

	// TODO: Figure out if this is still called (I don't think it is)
	void onCompleteSecondary(OVRSpatialAnchor.UnboundAnchor[] anchors)
	{
		foreach (OVRSpatialAnchor.UnboundAnchor unboundAnchor in anchors)
		{
			OVRSpatialAnchor newAnchor = null;
			for ( int i = 0; i < this.anchors.Count; ++i )
			{
				if (this.anchors[i].Uuid == unboundAnchor.Uuid )
				{
					newAnchor = this.anchors[i];
				}
			}

			if (newAnchor == null) continue;

			Vector3 localPos = anchorPositions[unboundAnchor.Uuid];

			unboundAnchor.BindTo(newAnchor);
			newAnchor.Save();
		}
	}

	void onComplete(OVRSpatialAnchor.UnboundAnchor[] anchors)
	{
		if (anchors != null)
		{
			if (root == null) root = new GameObject("anchorsRoot");

			unboundAnchors = new List<UnboundAnchor>();
			unboundAnchors.AddRange(anchors);
			loadedUnboundAnchors = true;
		}
	}

	/// <summary>
	/// Performs a basis transformation after comparing both sets of anchor positions.
	/// 
	/// TODO: Refactor this, I don't think the environment should be linked to a full set of anchors anymore.
	///			Maybe just remove this?
	/// </summary>
	/// <returns></returns>
	IEnumerator RecalcOffset()
	{
		yield return new WaitForSeconds(.5f);

		// Apply offsets to each stored value in anchorPositions
		for (int i = 0; i < anchors.Count; ++i)
		{
			anchorPositions[anchors[i].Uuid] = anchors[i].transform.position;
		}

		requestedNewAnchor = false;
		initialized = true;
	}

	IEnumerator CreateAnchorAt(Vector3 pos, Action<OVRSpatialAnchor> onComplete = null)
	{
		if (root == null) root = new GameObject("anchorsRoot");

		GameObject anchorGO = new GameObject();
		anchorGO.transform.parent = root.transform;
		anchorGO.transform.localPosition = pos;
		OVRSpatialAnchor newAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
		yield return new WaitForSeconds(.25f);

		while (newAnchor != null && newAnchor.PendingCreation)
		{
			yield return null;
		}

		if (newAnchor.Created)
		{
			yield return null;

			this.anchors.Add(newAnchor);

			uuidList.Add(newAnchor.Uuid.ToString());

			PlayerPrefs.SetFloat(newAnchor.Uuid.ToString() + "x", newAnchor.transform.position.x);
			PlayerPrefs.SetFloat(newAnchor.Uuid.ToString() + "y", newAnchor.transform.position.y);
			PlayerPrefs.SetFloat(newAnchor.Uuid.ToString() + "z", newAnchor.transform.position.z);

			anchorPositions.Add(newAnchor.Uuid, newAnchor.transform.position);

			string json = JsonHelper.ToJson(uuidList.ToArray());
			PlayerPrefs.SetString(activeSpace, json);

			newAnchor.Save();

			if (debugPrefab != null)
			{
				GameObject g = GameObject.Instantiate(debugPrefab);
				g.transform.parent = newAnchor.transform;
				g.transform.localPosition = Vector3.zero;
				AnchorDebug ad = g.GetComponent<AnchorDebug>();
				ad.SetData(anchorPositions[newAnchor.Uuid], newAnchor.transform.position);
				debugInstances.Add(newAnchor, g);
			}

			onComplete?.Invoke(newAnchor);
		}
		else
		{
			Debug.LogError("COULD NOT CREATE ANCHOR");
			onComplete?.Invoke(null);
		}
		requestedNewAnchor = false;
	}

	// TODO: Figure out if an anchor needs to be spawned here at all...
	//			If MovableObjects just create and request anchors, and everything is tethered to them, we don't need this.
	//				This does require a re-write of the "moving the entire environment", so that unchanged
	//					movable objects are also transported (with their anchors re-created at the new physical location) 
	IEnumerator InitializeAnchor()
	{
		if (root == null) root = new GameObject("anchorsRoot");

		GameObject anchorGO = new GameObject();
		anchorGO.transform.parent = root.transform;
		anchorGO.transform.localPosition = Vector3.up * 0.5f;

		OVRSpatialAnchor newAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
		yield return new WaitForSeconds(.25f);

		while (newAnchor.PendingCreation)
		{
			yield return null;
		}

		if (newAnchor.Created)
		{
			// Wait 1 extra frame for the transform to update (just in case)
			yield return null;
			this.anchors.Add(newAnchor);

			uuidList = new List<string>();
			uuidList.Add(newAnchor.Uuid.ToString());

			PlayerPrefs.SetFloat(newAnchor.Uuid.ToString() + "x", newAnchor.transform.position.x);
			PlayerPrefs.SetFloat(newAnchor.Uuid.ToString() + "y", newAnchor.transform.position.y);
			PlayerPrefs.SetFloat(newAnchor.Uuid.ToString() + "z", newAnchor.transform.position.z);

			anchorPositions.Add(newAnchor.Uuid, newAnchor.transform.position);

			Guid spaceUuid;
			if (newAnchor.Space.TryGetUuid(out spaceUuid))
			{
				activeSpace = Guid.NewGuid().ToString();
				Debug.Log($"SETTING ACTIVE SPACE {activeSpace}");

				spaceUuidList.Add(activeSpace);

				string spaceJson = JsonHelper.ToJson(spaceUuidList.ToArray());
				PlayerPrefs.SetString("spaces", spaceJson);
			}
			else
			{
				Debug.LogError("COULD NOT GET SPACE UUID ON INIT ANCHOR");
			}

			string json = JsonHelper.ToJson(uuidList.ToArray());
			PlayerPrefs.SetString(activeSpace, json);

			newAnchor.Save();

			if (debugPrefab != null)
			{
				GameObject g = GameObject.Instantiate(debugPrefab);
				g.transform.parent = newAnchor.transform;
				g.transform.localPosition = Vector3.zero;
				AnchorDebug ad = g.GetComponent<AnchorDebug>();
				ad.SetData(anchorPositions[newAnchor.Uuid], newAnchor.transform.position);
				debugInstances.Add(newAnchor, g);
			}
		}
		else
		{
			Debug.LogError("COULD NOT CREATE ANCHOR");
		}

		requestedNewAnchor = false;
		initialized = true;
	}
}

public static class MathUtils
{
	public static Vector3 BasisTransformVector(Vector3 vectorToTransform, Vector3 B1X, Vector3 B1Y, Vector3 B1Z, Vector3 B2X, Vector3 B2Y, Vector3 B2Z)
	{
		// Express the vector in the B1 basis
		float x = Vector3.Dot(vectorToTransform, B1X);
		float y = Vector3.Dot(vectorToTransform, B1Y);
		float z = Vector3.Dot(vectorToTransform, B1Z);

		// Transform the vector to the B2 basis
		Vector3 transformedVector = B2X * x + B2Y * y + B2Z * z;

		return transformedVector;
	}

	public static Vector3 CalculateCentroid(List<Vector3> vectors)
	{
		int n = vectors.Count;
		Vector3 centroid = Vector3.zero;

		foreach (var vector in vectors)
		{
			centroid += vector;
		}

		centroid.x /= (float)n;
		centroid.y /= (float)n;
		centroid.z /= (float)n;

		return centroid;
	}
}

public static class JsonHelper
{
	public static T[] FromJson<T>(string json)
	{
		Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
		return wrapper.Items;
	}

	public static string ToJson<T>(T[] array)
	{
		Wrapper<T> wrapper = new Wrapper<T>();
		wrapper.Items = array;
		return JsonUtility.ToJson(wrapper);
	}

	public static string ToJson<T>(T[] array, bool prettyPrint)
	{
		Wrapper<T> wrapper = new Wrapper<T>();
		wrapper.Items = array;
		return JsonUtility.ToJson(wrapper, prettyPrint);
	}

	[Serializable]
	private class Wrapper<T>
	{
		public T[] Items;
	}
}