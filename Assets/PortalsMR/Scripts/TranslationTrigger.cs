using PortalsVR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslationTrigger : MonoBehaviour
{
    public AnimationCurve curve;
    public Vector3 offset = Vector3.down * 2.5f;

    public PortalTraveller traveller;

    public string inWorld = "World 1";
    public string outWorld = "World 2";

    public float speed = 2;

    public List<Transform> targets = new();

    private List<Vector3> startPositions = new();
	private float currentPoint = 0;

	bool movingIn, movingOut;

    private void OnTriggerEnter(Collider other)
    {
        if (traveller.activeWorld == inWorld )
        {
            StartCoroutine(AnimateIn());
        }
    }

	private void OnTriggerExit(Collider other)
	{
		if (traveller.activeWorld == outWorld)
		{
			StartCoroutine(AnimateOut());
		}
	}

	IEnumerator AnimateIn()
    {
		while (movingOut) yield return null;

		movingIn = true;
        startPositions.Clear();
		foreach (Transform t in targets)
		{
			startPositions.Add(t.position);
			t.GetComponent<RelativeTo>()?.Pause(true);
		}

		while (currentPoint < 1f)
        {
            currentPoint += Time.deltaTime * speed;
            SetAnimationPosition(currentPoint);
            yield return null;
		}

        currentPoint = 1f;
		SetAnimationPosition(currentPoint);

		foreach (Transform t in targets)
		{
			t.GetComponent<RelativeTo>()?.Pause(false);
		}
		movingIn = false;
	}

	IEnumerator AnimateOut()
	{
		while (movingIn) yield return null;

		movingOut = true;
		foreach (Transform t in targets)
		{
			t.GetComponent<RelativeTo>()?.Pause(true);
		}

		while (currentPoint > 0f)
		{
			currentPoint -= Time.deltaTime * speed;
			SetAnimationPosition(currentPoint);
			yield return null;
		}

		currentPoint = 0f;
		SetAnimationPosition(currentPoint);

		foreach (Transform t in targets)
		{
			t.GetComponent<RelativeTo>()?.Pause(false);
		}

		movingOut = false;
	}

    void SetAnimationPosition( float t )
    {
        float value = curve.Evaluate(t);

        for( int i = 0; i < targets.Count; ++i )
        {
            targets[i].position = startPositions[i] + offset * value;
        }
    }
}