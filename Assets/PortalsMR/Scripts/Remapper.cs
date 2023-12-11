using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Remapper : MonoBehaviour
{
    public RemapSet setA, setB;
    public Transform offsetMarker;

    Vector3? newPos = null;
    Vector3? newRot = null;
    
	Vector3? debugAx = null;
	Vector3? debugAy = null;
	Vector3? debugAz = null;

	Vector3? debugBx = null;
	Vector3? debugBy = null;
	Vector3? debugBz = null;

	Vector3? debugAc = null;
	Vector3? debugBc = null;

	// Start is called before the first frame update
	IEnumerator Start()
    {
		Remap();
		yield return null;
    }

	void Remap()
	{
		List<Vector3> vecA = setA.setObjects.Select(transform => transform.position).ToList();
		List<Vector3> vecB = setB.setObjects.Select(transform => transform.position).ToList();

		Vector3 aC = MathUtils.CalculateCentroid(vecA);
		Vector3 bC = MathUtils.CalculateCentroid(vecB);

		Vector3 aC1 = (vecA[1] - aC).normalized;
		Vector3 aC2 = (vecA[2] - aC).normalized;
		Vector3 bC1 = (vecB[1] - bC).normalized;
		Vector3 bC2 = (vecB[2] - bC).normalized;

		Vector3 aX = aC1;
		Vector3 aY = Vector3.Cross(aC1, aC2).normalized;
		Vector3 aZ = Vector3.Cross(aY, aX).normalized;

		Vector3 bX = bC1;
		Vector3 bY = Vector3.Cross(bC1, bC2).normalized;
		Vector3 bZ = Vector3.Cross(bY, bX).normalized;

		debugAc = aC;
		debugBc = bC;

		newPos = MathUtils.BasisTransformVector((offsetMarker.position - bC), bX, bY, bZ, aX, aY, aZ) + aC;
		newRot = MathUtils.BasisTransformVector(offsetMarker.forward, bX, bY, bZ, aX, aY, aZ);
		
		debugAx = aX;
		debugAy = aY;
		debugAz = aZ;
		debugBx = bX;
		debugBy = bY;
		debugBz = bZ;
	}

	private void Update()
    {
        // if ( Time.frameCount % 120 == 0 )
        {
			Remap();
		}
    }

    private void OnDrawGizmos()
    {
        if (newPos != null)
        {
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(newPos.Value, new Vector3(0.1f, 1f, 0.1f));
			if (newRot != null)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(newPos.Value, newPos.Value + newRot.Value);
			}
		}

        if ( debugAc != null && debugBc != null)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(debugAc.Value, new Vector3(0.1f, 0.1f, 0.1f));
			Gizmos.DrawWireCube(debugBc.Value, new Vector3(0.1f, 0.1f, 0.1f));

			Gizmos.color = Color.red;
            Gizmos.DrawLine(debugAc.Value, debugAc.Value + debugAx.Value);
			Gizmos.DrawLine(debugBc.Value, debugBc.Value + debugBx.Value);

			Gizmos.color = Color.green;
			Gizmos.DrawLine(debugAc.Value, debugAc.Value + debugAy.Value);
			Gizmos.DrawLine(debugBc.Value, debugBc.Value + debugBy.Value);

			Gizmos.color = Color.blue;
			Gizmos.DrawLine(debugAc.Value, debugAc.Value + debugAz.Value);
			Gizmos.DrawLine(debugBc.Value, debugBc.Value + debugBz.Value);
		}
    }
}
