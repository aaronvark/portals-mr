using System.Collections.Generic;
using UnityEngine;

namespace PortalsVR
{
    [RequireComponent(typeof(Camera))]
    public class Eye : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Camera.StereoscopicEye eye;
        #endregion

        #region Properties
        public Camera Camera { get; private set; }
        public List<Portal> Portals { get; set; } = new List<Portal>();
        public string activeWorld = "World 1";
        #endregion

        #region Methods
        private void Awake()
        {
            Camera = GetComponent<Camera>();
        }

        private void OnPreCull()
        {
			World.worlds[activeWorld].SetVisible(false);

			for (int i = 0; i < Portals.Count; i++)
            {
                if (Portals[i].parentWorld == World.worlds[activeWorld]) //|| Portals[i].parentWorld == Portals[i].linkedPortal.parentWorld )
                {
                    Portals[i].Render(eye);
                }
            }

			// TODO: Update active world
			World.worlds[activeWorld].SetVisible(true);
		}

        private void OnPostRender()
        {
			// World.worlds[activeWorld].SetVisible(false);
		}
        #endregion
    }
}