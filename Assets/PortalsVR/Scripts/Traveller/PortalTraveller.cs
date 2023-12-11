using UnityEngine;
using UnityEngine.Events;

namespace PortalsVR
{

	public delegate void WorldEvent(string world);
	public class PortalTraveller : MonoBehaviour
    {
        public event WorldEvent onWorldChanged;

        #region Fields
        [SerializeField] private Transform target;
        // Set this to true for objects that need to travel between worlds
        public bool migrateUponTeleport = false;
        // Set this to false for non-player travellers
        public bool deformPortal = true;
        public bool isPlayer = true;
        #endregion

        #region Properties
        public Transform Target => target;

        public Vector3 PreviousOffsetFromPortal { get; set; }
        public bool InPortal { get; set; }
        #endregion
        
        //[HideInInspector]
        public string activeWorld = "World 1";

        public Eye[] eyes;

        private void Awake()
        {
            if (target == null) target = transform;
        }

        public void Start()
        {
            onWorldChanged?.Invoke(activeWorld);
		}

        #region Methods
        public virtual void Teleport(Portal fromPortal, Portal toPortal, Vector3 pos, Quaternion rot)
        {
            if ( migrateUponTeleport )
                fromPortal.parentWorld.Migrate(gameObject, toPortal.parentWorld, true);

            transform.position += pos - target.position;
            Physics.SyncTransforms();
            
			activeWorld = toPortal.parentWorld.name;
			onWorldChanged?.Invoke(activeWorld);
			foreach ( Eye eye in eyes )
            {
                eye.activeWorld = activeWorld;
			}
        }
        #endregion
    }
}