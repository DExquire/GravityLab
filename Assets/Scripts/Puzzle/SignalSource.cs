using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>Anything that can power a puzzle element: pressure plates, laser receivers.</summary>
    public abstract class SignalSource : MonoBehaviour
    {
        public abstract bool IsActive { get; }

        /// <summary>Point on the floor where an indicator wire starts.</summary>
        public virtual Vector3 WireAnchor { get { return transform.position; } }
    }

    public static class Signals
    {
        /// <summary>AND of all inputs; an element without inputs is always powered.</summary>
        public static bool All(SignalSource[] inputs)
        {
            if (inputs == null) return true;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null && !inputs[i].IsActive) return false;
            }
            return true;
        }
    }
}
