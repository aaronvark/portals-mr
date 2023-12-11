// Based on: https://github.com/andrewzimmer906/PocketPortalVR
//
// Adapted by Aaron Oostdijk, LogicLab @ HKU Games, HKU Univeristy of the Arts, Utrecht

using UnityEngine;

namespace PortalsVR
{
    public class CircleMeshDeformer : MeshDeformer
    {
        public Vector3 scaleAxis = new Vector3(0, 1, 0);
        const float halfSquared = 0.5f * 0.5f;
        private bool direction;

        public override void AddDeformingForce(Vector3 point, float force, bool direction)
        {
            this.direction = direction;
            AddDeformingForce(point, force);
        }

        protected override void AddForceToVertex(int i, Vector3 point, float force)
        {
            // Any edge lock is all edge lock
            if (lockXEdges || lockYEdges || lockZEdges )
            {
				float squaredEdgeDistance = originalVertices[i].x * originalVertices[i].x + originalVertices[i].z * originalVertices[i].z;
                if (Mathf.Abs(squaredEdgeDistance - halfSquared) < 0.01f) return;
			}

            Vector3 pointToVertex = originalVertices[i] - point;
            pointToVertex = new Vector3(
                pointToVertex.x * transform.localScale.x,
                pointToVertex.y * transform.localScale.y,
                pointToVertex.z * transform.localScale.z);

            float attenuatedForce = Mathf.Max(Mathf.Abs(force) - pointToVertex.magnitude, 0.1f);
            Vector3 transformForce = scaleAxis * (this.direction ? -1 : 1) * attenuatedForce / transform.localScale.y;

            displacedVertices[i] = originalVertices[i] + (transformForce);
        }
    }
}