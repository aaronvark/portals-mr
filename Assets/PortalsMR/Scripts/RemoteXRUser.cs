using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteXRUser : MonoBehaviour
{
    public Transform head, lHand, rHand;
	private Transform relativeAnchor;

	class Data
	{
		public int frameCount;
		public Vector3 headPos, headF, headU;
		public Vector3 lhPos, lhU, lhF;
		public Vector3 rhPos, rhU, rhF;

		public Data(int fc) {
			frameCount = fc;
			headPos = headF = headU = lhPos = lhF = lhF = rhPos = rhF = rhU = Vector3.zero;
		}
	}

	private Data data = new Data(0);

	private void Awake()
	{
		relativeAnchor = FindObjectOfType<SpatialAnchorBehaviour>().target;
	}

	public void SetData(int frameCount, Vector3 headPos, Vector3 headF, Vector3 headUp, Vector3 lhPos, Vector3 lhF, Vector3 lhUp, Vector3 rhPos, Vector3 rhF, Vector3 rhU )
    {
		// only use latest data
		if (frameCount < data.frameCount)
		{
			// Debug.Log("Ignore update:" +frameCount);
			return;
		}

		data.frameCount = frameCount;
		data.headPos = headPos;
		data.headF = headF;
		data.headU = headUp;
		data.lhPos = lhPos;
		data.lhF = lhF;
		data.lhU = lhUp;
		data.rhPos = rhPos;
		data.rhF = rhF;
		data.rhU = rhU;
	}

	private void LateUpdate()
	{
		// TODO: implement some kind of smoothing / prediction etc... because wifi is unstable
		head.position = relativeAnchor.TransformPoint(data.headPos);
		head.LookAt(head.position + relativeAnchor.TransformDirection(data.headF), relativeAnchor.TransformDirection(data.headU));
		lHand.position = relativeAnchor.TransformPoint(data.lhPos);
		lHand.LookAt(lHand.position + relativeAnchor.TransformDirection(data.lhF), relativeAnchor.TransformDirection(data.lhU));
		rHand.position = relativeAnchor.TransformPoint(data.rhPos);
		rHand.LookAt(rHand.position + relativeAnchor.TransformDirection(data.rhF), relativeAnchor.TransformDirection(data.rhU));
	}
}