using UnityEngine;

namespace GravityLab.Puzzle
{
    public static class ZoneUtil
    {
        /// <summary>True when <paramref name="worldPoint"/> lies inside an oriented box given in <paramref name="frame"/>'s local space.</summary>
        public static bool Contains(Transform frame, Vector3 localCenter, Vector3 halfExtents, Vector3 worldPoint)
        {
            Vector3 p = frame.InverseTransformPoint(worldPoint) - localCenter;
            return Mathf.Abs(p.x) <= halfExtents.x && Mathf.Abs(p.y) <= halfExtents.y && Mathf.Abs(p.z) <= halfExtents.z;
        }

        /// <summary>Returns false when the same rigidbody already appeared earlier in the overlap buffer.</summary>
        public static bool IsFirstOccurrence(Collider[] hits, int index)
        {
            Rigidbody rb = hits[index].attachedRigidbody;
            for (int j = 0; j < index; j++)
            {
                if (hits[j].attachedRigidbody == rb) return false;
            }
            return true;
        }
    }
}
