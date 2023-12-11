using PortalsVR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
	public PortalTraveller traveller;
	public List<string> inWorlds = new List<string>();
	public List<string> outWorlds = new List<string>();

	public List<Animator> targets;

	public bool hideTargetsOnStart = true;

	private void Start()
	{
		foreach (Animator target in targets)
		{
			target.SetTrigger("Hide");
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (inWorlds.Contains(traveller.activeWorld))
		{
			ExecuteIn();
			Debug.Log($"IN! {other.name}", other.gameObject);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (outWorlds.Contains(traveller.activeWorld))
		{
			ExecuteOut();
			Debug.Log($"OUT! {other.name}", other.gameObject);
		}
	}

	protected virtual void ExecuteIn()
	{
		foreach( Animator target in targets )
		{
			target.SetTrigger("Show");
		}
	}

	protected virtual void ExecuteOut()
	{
		foreach (Animator target in targets)
		{
			target.SetTrigger("Hide");
		}
	}
}
