using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalsVR;

// TODO: flip the source and destination worlds based on which side of the mirror we're looking at...
public class MirrorPortal : MonoBehaviour
{
    public Portal source, destination;

    public PortalTraveller referenceTraveller;

    public string sourceWorld;
	public string destinationWorld;

    World sourceWorldRef = null, destWorldRef = null;

    private void Start()
    {
        SetSourceWorld(sourceWorld);
        SetDestinationWorld(destinationWorld);
	}

    private void LateUpdate()
    {
        if ( referenceTraveller )
        {
            if ( referenceTraveller.activeWorld != sourceWorld )
            {
                Debug.Log($"Following traveller to {referenceTraveller.activeWorld}");
                SetSourceWorld(referenceTraveller.activeWorld);

                if (destinationWorld == sourceWorld)
                {
                    // pick another world... somehow...
                    // HACK
                    // Should probably just leave these mirrors in a fixed world environment?
                    switch( sourceWorld )
                    {
                        case "World 1":
                            destinationWorld = "World 2";
                            break;
						case "World 2":
							destinationWorld = "World 3";
							break;
						case "World 3":
							destinationWorld = "World 1";
							break;
                        default:
                            Debug.LogError("NO VALID CASE");
                            break;
					}

                    SetDestinationWorld(destinationWorld);
                }
            }
		}
    }

    public void SetSourceWorld( string sourceWorld )
    {
        this.sourceWorld = sourceWorld;
        source.parentWorld = World.worlds[sourceWorld];

        if ( sourceWorldRef != null )   sourceWorldRef.Remove(source.gameObject, false);
		sourceWorldRef = source.parentWorld;
		sourceWorldRef.Add(source.gameObject, false);

		Debug.Log($"Source world: {sourceWorldRef.name}");
	}

	public void SetDestinationWorld(string destinationWorld)
	{
        this.destinationWorld = destinationWorld;
		destination.parentWorld = World.worlds[destinationWorld];

		if (destWorldRef != null)   destWorldRef.Remove(destination.gameObject, false);
		destWorldRef = destination.parentWorld;
		destWorldRef.Add(destination.gameObject, false);

		Debug.Log($"Dest world: {destWorldRef.name}");
	}
}
