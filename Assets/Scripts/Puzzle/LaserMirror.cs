using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>Static reflective panel; only its front face (local +Z) reflects lasers.</summary>
    public sealed class LaserMirror : MonoBehaviour
    {
        public Vector3 Normal { get { return transform.forward; } }
    }
}
