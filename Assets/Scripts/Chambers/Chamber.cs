using System.Collections.Generic;
using GravityLab.Puzzle;
using UnityEngine;

namespace GravityLab.Chambers
{
    /// <summary>Runtime root of a built test chamber: spawn point, kill height, props, entry door and exit elevator.</summary>
    public sealed class Chamber : MonoBehaviour
    {
        public readonly List<PhysicsProp> Props = new List<PhysicsProp>();

        public int Index { get; set; }
        public Vector3 SpawnPosition { get; set; }
        public float SpawnYaw { get; set; }
        /// <summary>Feet below this height = the player fell into goo or out of the world.</summary>
        public float KillY { get; set; }
        public Door EntryDoor { get; set; }
        public ExitElevator Exit { get; set; }
        public Vector3 Center { get; set; }
        public Vector3 Size { get; set; }

        /// <summary>Opens the arrival door: the test begins.</summary>
        public void BeginTest()
        {
            if (EntryDoor != null) EntryDoor.ForceClosed = false;
        }

        void Update()
        {
            for (int i = 0; i < Props.Count; i++)
            {
                PhysicsProp p = Props[i];
                if (p.IsDissolving) continue;
                float y = p.transform.position.y;
                if (y < KillY + 0.3f || y > 200f) p.Respawn();
            }
        }
    }
}
