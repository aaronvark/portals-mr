using PortalsVR;
using SharpOSC;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class SendOSCTrackingData : MonoBehaviour
{
    public Transform relativeAnchor;
	public PortalTraveller traveller;

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    public int delayInMilliseconds = 0;
    public int port = 6200;
    public string serverIP = "10.3.4.6";

	// Might need to send more than one (at least as a test)
    static UDPSender sender = null;

	private string localIP = "";

    private void Start()
    {
		localIP = GetLocalIPAddress();
		StartCoroutine(SendData());
    }

    IEnumerator SendData()
    {
		int frameCount = 0;
		if (sender == null ) sender = new UDPSender(serverIP, port);
		while (true)
        {
            if ( delayInMilliseconds == 0 )
            {
				yield return new WaitForEndOfFrame();
			}
            else
            {
                yield return new WaitForSeconds(delayInMilliseconds * 0.001f);
            }

			Vector3 hPos = Vector3.zero, lhPos = Vector3.zero, rhPos = Vector3.zero;
			Vector3 hF = Vector3.zero, hU = Vector3.zero, lhF = Vector3.zero, lhU = Vector3.zero, rhF = Vector3.zero, rhU = Vector3.zero;
			if (head)
			{
				hPos = relativeAnchor.InverseTransformPoint(head.position);
				hF = relativeAnchor.InverseTransformDirection(head.forward);
				hU = relativeAnchor.InverseTransformDirection(head.up);
			}
			if (leftHand)
			{
				lhPos = relativeAnchor.InverseTransformPoint(leftHand.position);
				lhF = relativeAnchor.InverseTransformDirection(leftHand.forward);
				lhU = relativeAnchor.InverseTransformDirection(leftHand.up);
			}
			if (rightHand)
			{
				rhPos = relativeAnchor.InverseTransformPoint(rightHand.position);
				rhF = relativeAnchor.InverseTransformDirection(rightHand.forward);
				rhU = relativeAnchor.InverseTransformDirection(rightHand.up);
			}

			OscMessage msg = new OscMessage("/xr-user", localIP, traveller.activeWorld, frameCount,
												hPos.x, hPos.y, hPos.z, hF.x, hF.y, hF.z, hU.x, hU.y, hU.z,
												lhPos.x, lhPos.y, lhPos.z, lhF.x, lhF.y, lhF.z, lhU.x, lhU.y, lhU.z,
												rhPos.x, rhPos.y, rhPos.z, rhF.x, rhF.y, rhF.z, rhU.x, rhU.y, rhU.z
												);

			sender.Send(msg);

			frameCount++;
		}
    }

    private void OnApplicationPause(bool pause)
    {
		if ( pause )
        {
			if (sender != null)
			{
				sender.Close();
				sender = null;
			}
			StopAllCoroutines();
		}
        else
        {
			StartCoroutine(SendData());
		}
    }

    private void OnApplicationQuit()
    {
		if (sender != null)
		{
			sender.Close();
			sender = null;
		}
		StopAllCoroutines();
	}

	public static string GetLocalIPAddress()
	{
		var host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork)
			{
				return ip.ToString();
			}
		}
		throw new Exception("No network adapters with an IPv4 address in the system!");
	}
}