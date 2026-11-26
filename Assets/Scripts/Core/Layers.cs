using UnityEngine;

namespace GravityLab.Core
{
    /// <summary>
    /// Physics layer assignments. Layers 8+ need no TagManager entry to work in code,
    /// so the project ships without custom ProjectSettings.
    /// </summary>
    public static class Layers
    {
        public const int World = 0;
        public const int IgnoreRaycast = 2;
        public const int Props = 8;
        public const int Player = 9;
        public const int Held = 10;
        public const int Glass = 11;

        /// <summary>What blocks a laser: glass is transparent to it, players are ignored.</summary>
        public const int LaserMask = (1 << World) | (1 << Props) | (1 << Held);
        /// <summary>What the gravity gun can aim through: glass does not block targeting.</summary>
        public const int GunMask = (1 << World) | (1 << Props) | (1 << Held);
        /// <summary>Static geometry that breaks the hold beam's line of sight.</summary>
        public const int SightBlockMask = (1 << World) | (1 << Glass);
        /// <summary>Everything that should stop a player standing up or a held object being pushed.</summary>
        public const int SolidMask = (1 << World) | (1 << Props) | (1 << Glass);
        public const int PropMask = (1 << Props) | (1 << Held);
        public const int PlateMask = (1 << Props) | (1 << Player);

        public static void Configure()
        {
            Physics.IgnoreLayerCollision(Player, Held, true);
        }
    }
}
