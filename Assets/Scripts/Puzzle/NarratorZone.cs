using System;
using GravityLab.Player;
using UnityEngine;

namespace GravityLab.Puzzle
{
    /// <summary>Plays one narrator line the first time the player walks into its box.</summary>
    public sealed class NarratorZone : MonoBehaviour
    {
        Vector3 _half;
        string _line;
        Action<string> _narrate;

        public void Init(Vector3 halfExtents, string line, Action<string> narrate)
        {
            _half = halfExtents;
            _line = line;
            _narrate = narrate;
        }

        void Update()
        {
            PlayerController player = PlayerController.Active;
            if (player == null || !player.InputEnabled) return;
            if (!ZoneUtil.Contains(transform, Vector3.zero, _half, player.Center)) return;
            if (_narrate != null) _narrate(_line);
            enabled = false;
        }
    }
}
