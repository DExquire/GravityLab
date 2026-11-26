using System;
using GravityLab.Player;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>The exit booth: once the player steps inside through the open door, it seals and reports completion.</summary>
    public sealed class ExitElevator : MonoBehaviour
    {
        Door _door;
        Vector3 _half;
        bool _triggered;

        public event Action Reached;

        public void Init(Door door, Vector3 halfExtents)
        {
            _door = door;
            _half = halfExtents;
        }

        void Update()
        {
            if (_triggered || !_door.IsOpen) return;
            PlayerController player = PlayerController.Active;
            if (player == null || !player.InputEnabled) return;
            if (!ZoneUtil.Contains(transform, Vector3.up * _half.y, _half, player.Center)) return;

            _triggered = true;
            _door.ForceClosed = true;
            if (Reached != null) Reached();
        }
    }
}
