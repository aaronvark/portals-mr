using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class World : MonoBehaviour
{
    public static Dictionary<string, World> worlds = new Dictionary<string, World>();
    Dictionary<Renderer, int> rendererLayers;

    // Start is called before the first frame update
    void Awake()
    {
        worlds.Add(gameObject.name, this);

		rendererLayers = new Dictionary<Renderer, int>();
		
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        // TODO: Exempt specific objects?
        foreach( Renderer r in renderers )
        {
            rendererLayers.Add(r, r.gameObject.layer);
        }

		SetVisible(false);
	}

    public void SetVisible( bool visible )
    {
        foreach( KeyValuePair<Renderer,int> pair in rendererLayers )
        {
            pair.Key.gameObject.layer = visible ? pair.Value : 30;
        }
    }

	public void Migrate(GameObject g, World target, bool reparent = true)
	{
		Renderer[] renderers = g.GetComponentsInChildren<Renderer>();

		foreach (Renderer r in renderers)
		{
			if (rendererLayers.ContainsKey(r))
			{
				target.rendererLayers.Add(r, rendererLayers[r]);
				rendererLayers.Remove(r);
			}
		}

		if (reparent)
			g.transform.parent = target.transform;
	}

	public void Remove(GameObject g, bool reparent = true)
	{
		Renderer[] renderers = g.GetComponentsInChildren<Renderer>();

		foreach (Renderer r in renderers)
		{
			if (rendererLayers.ContainsKey(r))	// performance critical?
				rendererLayers.Remove(r);
		}

		if (reparent) g.transform.parent = null;
	}

	public void Add(GameObject g, bool reparent = true)
	{
		Renderer[] renderers = g.GetComponentsInChildren<Renderer>();

		foreach (Renderer r in renderers)
		{
			if (!rendererLayers.ContainsKey(r)) // performance critical?
				rendererLayers.Add(r, r.gameObject.layer);
		}

		if (reparent) g.transform.parent = transform;
	}
}
