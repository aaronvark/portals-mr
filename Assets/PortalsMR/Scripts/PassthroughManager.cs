using PortalsVR;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PassthroughManager : MonoBehaviour
{
    public List<WorldOverlaySettings> worldOverlaySettings = new List<WorldOverlaySettings>();

    [System.Serializable]
    public struct WorldOverlaySettings
    {
        public string name;
        public float ulOpacity, olOpacity;
        public Color ulEdgeColor, olEdgeColor;
        public bool ulEdgeOn, olEdgeOn;
    }

    public OVRPassthroughLayer overlay, underlay;
    public PortalTraveller relativeTraveller;

    string currentWorld = "";

    private void Awake()
    {
        if (relativeTraveller)
        {
            relativeTraveller.onWorldChanged += ActiveWorldChanged;
		}
    }

    // Update is called once per frame
    public void ActiveWorldChanged( string newWorld )
    {
        WorldOverlaySettings settings = FindSettings(newWorld);
        if ( string.IsNullOrEmpty(currentWorld))
        {
			// immediately set the new values
			HardSet(settings);
		}
        else
        {
            // get the new values, and animate to them over a second (based on current status)
            StartCoroutine(ApplySettings(settings));
        }

        currentWorld = newWorld;
	}

    WorldOverlaySettings FindSettings(string world)
    {
        for(int i = 0; i < worldOverlaySettings.Count; ++i )
        {
            if (worldOverlaySettings[i].name == world)
            {
                return worldOverlaySettings[i];
			}
        }

        Debug.LogError($"NO WORLD SETTINGS FOUND FOR: {world}");
        return worldOverlaySettings[0];
	}

    IEnumerator ApplySettings(WorldOverlaySettings newSettings)
    {
        float t = 0;

		float ulOpacity, olOpacity;
	    Color ulEdgeColor, olEdgeColor;

        ulOpacity = underlay.textureOpacity;
        ulEdgeColor = underlay.edgeColor;
        if (ulOpacity == 0 && newSettings.ulOpacity > 0)
        {
            ulOpacity = 0;
        }
		if (!underlay.edgeRenderingEnabled && newSettings.ulEdgeOn)
		{
			ulEdgeColor = newSettings.ulEdgeColor;
			ulEdgeColor.a = 0;
			underlay.edgeRenderingEnabled = true;
		}
        
		olOpacity = overlay.textureOpacity;
        olEdgeColor = overlay.edgeColor;
        if (olOpacity == 0 && newSettings.olOpacity > 0)
        {
            olOpacity = 0;
        }
        if (!overlay.edgeRenderingEnabled && newSettings.olEdgeOn)
        {
            olEdgeColor = newSettings.olEdgeColor;
            olEdgeColor.a = 0;
            overlay.edgeRenderingEnabled = true;
		}

		newSettings.olEdgeColor.a = newSettings.olEdgeOn ? newSettings.olEdgeColor.a : 0;
		newSettings.ulEdgeColor.a = newSettings.ulEdgeOn ? newSettings.ulEdgeColor.a : 0;
        
		while ( t < 1f )
        {
            underlay.textureOpacity = Mathf.Lerp(ulOpacity, newSettings.ulOpacity, t);
			overlay.textureOpacity = Mathf.Lerp(olOpacity, newSettings.olOpacity, t);

            underlay.edgeColor = Color.Lerp(ulEdgeColor, newSettings.ulEdgeColor, t);
			overlay.edgeColor = Color.Lerp(olEdgeColor, newSettings.olEdgeColor, t);

            t += 0.01f;// Time.deltaTime;
            yield return null;
        }

        // Apply one final time, and enable/disable accordingly
        HardSet(newSettings);

		yield return null;
    }

    void HardSet(WorldOverlaySettings settings)
    {
		underlay.textureOpacity = settings.ulOpacity;
		underlay.edgeColor = settings.ulEdgeColor;
		underlay.edgeRenderingEnabled = settings.ulEdgeOn;
        
		overlay.textureOpacity = settings.olOpacity;
		overlay.edgeColor = settings.olEdgeColor;
		overlay.edgeRenderingEnabled = settings.olEdgeOn;
	}
}
