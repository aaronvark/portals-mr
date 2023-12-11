using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnchorDebug : MonoBehaviour
{
    public Material red, green;
    private Renderer mRend;

    public TextMesh vPos, aPos;

    private void Awake()
    {
		mRend = GetComponent<Renderer>();
    }

    public void SetData( Vector3 v, Vector3 a )
    {
        vPos.text = $"vPos: {v.x.ToString("0.00")},{v.y.ToString("0.00")},{v.z.ToString("0.00")}";
		aPos.text = $"aPos: {a.x.ToString("0.00")},{a.y.ToString("0.00")},{a.z.ToString("0.00")}";
	}

    public void SetAPos( Vector3 a )
    {
		aPos.text = $"aPos: {a.x.ToString("0.00")},{a.y.ToString("0.00")},{a.z.ToString("0.00")}";
	}

    public void Red()
    {
        mRend.sharedMaterial = red;
		// vPos.GetComponent<MeshRenderer>().enabled = false;
		// aPos.GetComponent<MeshRenderer>().enabled = false;
	}

	public void Green()
	{
		mRend.sharedMaterial = green;
        // vPos.GetComponent<MeshRenderer>().enabled = true;
		// aPos.GetComponent<MeshRenderer>().enabled = true;
	}
}
