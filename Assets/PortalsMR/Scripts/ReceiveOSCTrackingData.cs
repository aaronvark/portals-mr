using System.Collections.Generic;
using UnityEngine;
using SharpOSC;

public class ReceiveOSCTrackingData : MonoBehaviour
{
    public GameObject remoteUserPrefab;
    public Transform relativeAnchor;
    public int listenPort = 6300;
    private UDPListener listener;

    private Dictionary<string, RemoteXRUser> trackedUsers = new Dictionary<string, RemoteXRUser>();
	private Dictionary<string, float> lastReceivedTimes = new Dictionary<string, float>();

    private List<string> toRemove = new List<string>();
	private List<string> toSpawn = new List<string>();

    private string ignoreIP;
    private Quaternion relativeRotation;
    private Vector3 relativePosition;

	// Start is called before the first frame update
	void Awake()
    {
		ignoreIP = SendOSCTrackingData.GetLocalIPAddress();
		listener = new UDPListener(listenPort, OscPacketReceived);
	}

    private void OnDestroy()
    {
        listener.Close();
    }

    void OscPacketReceived ( byte[] bytes )
    {
        OscPacket packet = OscPacket.GetPacket(bytes);
        if ( packet is OscMessage )
        {
            ParseMessage(packet as OscMessage);
		}
        else
        {
            OscBundle bundle = packet as OscBundle;
            foreach( OscMessage msg in bundle.Messages )
            {
                ParseMessage(msg);
            }
        }
    }

    private void LateUpdate()
    {
        relativeRotation = relativeAnchor.rotation;
        relativePosition = relativeAnchor.position;

		if (Time.frameCount % 10 != 0) return;

		/*
		foreach ( KeyValuePair<string, float> pair in lastReceivedTimes )
        {
            // more than 10 seconds no data
            if (Time.realtimeSinceStartup - pair.Value > 10 )
            {
                toRemove.Add(pair.Key);
			}
        }
        

        foreach( string s in toRemove )
        {
            Debug.Log("Removing: " + s);
            lastReceivedTimes.Remove(s);
            GameObject.Destroy(trackedUsers[s].gameObject);
            trackedUsers.Remove(s);

		}
		toRemove.Clear();
        */

        foreach( string localIP in toSpawn )
        {
            if (!trackedUsers.ContainsKey(localIP))
            {
				Debug.Log("Adding: " + localIP);
				GameObject g = Instantiate(remoteUserPrefab);
                g.name = localIP;
                trackedUsers.Add(localIP, g.GetComponent<RemoteXRUser>());
                lastReceivedTimes.Add(localIP, Time.realtimeSinceStartup);
			}
		}
        toSpawn.Clear();
	}

    void ParseMessage( OscMessage message )
    {
        if (message.Address != "/xr-user") 
        {
            // Debug.Log("Not an XR User!");
            return;
        }

		int i = 0;
        string localIP = (string)message.Arguments[i++];

        if (localIP == ignoreIP)
        {
            // Debug.Log("Ignore own IP");
            return;
        }

		string activeWorld = (string)message.Arguments[i++];
		int frameCount = (int)message.Arguments[i++];

		Vector3 headPos, headF, headU;
        headPos.x = (float)message.Arguments[i++];
        headPos.y = (float)message.Arguments[i++];
        headPos.z = (float)message.Arguments[i++];
        headF.x = (float)message.Arguments[i++];
		headF.y = (float)message.Arguments[i++];
		headF.z = (float)message.Arguments[i++];
		headU.x = (float)message.Arguments[i++];
		headU.y = (float)message.Arguments[i++];
		headU.z = (float)message.Arguments[i++];

		Vector3 lhPos, lhF, lhU;
		lhPos.x = (float)message.Arguments[i++];
		lhPos.y = (float)message.Arguments[i++];
		lhPos.z = (float)message.Arguments[i++];
		lhF.x = (float)message.Arguments[i++];
		lhF.y = (float)message.Arguments[i++];
		lhF.z = (float)message.Arguments[i++];
		lhU.x = (float)message.Arguments[i++];
		lhU.y = (float)message.Arguments[i++];
		lhU.z = (float)message.Arguments[i++];

		Vector3 rhPos, rhF, rhU;
		rhPos.x = (float)message.Arguments[i++];
		rhPos.y = (float)message.Arguments[i++];
		rhPos.z = (float)message.Arguments[i++];
		rhF.x = (float)message.Arguments[i++];
		rhF.y = (float)message.Arguments[i++];
		rhF.z = (float)message.Arguments[i++];
		rhU.x = (float)message.Arguments[i++];
		rhU.y = (float)message.Arguments[i++];
		rhU.z = (float)message.Arguments[i++];

		// Check based on IP if an object is already tracking this user
		if (!trackedUsers.ContainsKey(localIP))
        {
			// Debug.Log("Request spawn");
			toSpawn.Add(localIP);
        }
        else
        {            
            trackedUsers[localIP].SetData(frameCount, headPos, headF, headU, lhPos, lhF, lhU, rhPos, rhF, rhU);
		}        
	}
}
