using System.Collections.Generic;
using GravityLab.Puzzle;
using UnityEngine;

namespace GravityLab.Chambers
{
    /// <summary>
    /// The 20 handcrafted test chambers, in difficulty order. Each mechanic is introduced alone, then combined.
    /// Coordinates are metres; the comment above each chamber states its intended solution.
    /// </summary>
    public static partial class ChamberLibrary
    {
        static List<ChamberDef> _all;

        public static int Count { get { return All.Count; } }

        public static ChamberDef Get(int index) { return All[Mathf.Clamp(index, 0, All.Count - 1)]; }

        static List<ChamberDef> All
        {
            get
            {
                if (_all == null)
                {
                    _all = new List<ChamberDef>();
                    AddBasics(_all);
                    AddAdvanced(_all);
                }
                return _all;
            }
        }

        static Vector3 P(float x, float y, float z) { return new Vector3(x, y, z); }

        static void AddBasics(List<ChamberDef> list)
        {
            // 01 - Walk, jump the hurdle, crouch under the overhang, jump the goo gap.
            list.Add(new ChamberDef("Orientation", 22f, new[]
            {
                "Good morning, test subject. Welcome to the Gravity Laboratory.",
                "Move with [W A S D]. Look with the mouse. Try not to touch anything expensive."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 5f), exit = Opening.Door(Side.North, 5f);
                b.Shell(0f, 0f, 10f, 24f, -3f, 5f, entry, exit);
                b.Floor(0f, 0f, 10f, 17f, 0f);
                b.Floor(0f, 19.5f, 10f, 24f, 0f);
                b.Goo(0f, 17f, 10f, 19.5f, -2f);
                b.Block(0f, 6f, 10f, 7f, 0f, 0.8f);
                b.Block(0f, 12f, 10f, 14f, 1.2f, 5f);
                b.Entry(entry);
                b.Exit(exit);
                b.Say(P(5f, 1f, 4.5f), P(10f, 3f, 2f), "Obstacle ahead. Press [SPACE] to jump.");
                b.Say(P(5f, 1f, 10.5f), P(10f, 3f, 2f), "Low clearance. Hold [CTRL] or [C] to crouch.");
                b.Say(P(5f, 1f, 15.5f), P(10f, 3f, 2f), "That green liquid is not a hot tub. Hold [SHIFT] to sprint, then jump.");
            }));

            // 02 - Grab the cube off the ledge (LMB), place it on the plate.
            list.Add(new ChamberDef("Hold Still", 22f, new[]
            {
                "This is a gravity manipulation device. Point it at a cube and press [LEFT MOUSE] to hold it.",
                "The exit opens while the plate is pressed. You are heavy enough, but you also need to leave."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 6f), exit = Opening.Door(Side.North, 6f);
                b.Room(0f, 0f, 12f, 14f, 6f, entry, exit);
                b.Block(1f, 9f, 5f, 12f, 0f, 1.5f);
                b.Cube(P(3f, 1.5f, 10.5f));
                PressurePlate plate = b.Plate(P(8f, 0f, 6f), PlateKind.Any);
                b.Entry(entry);
                b.Exit(exit, plate);
                b.Say(P(6f, 1f, 8f), P(12f, 3f, 2f), "Scroll the [MOUSE WHEEL] to pull a held object closer or push it away.");
            }));

            // 03 - Heavy cube on the heavy plate, standard cube on the standard plate.
            list.Add(new ChamberDef("Weight Class", 35f, new[]
            {
                "Heavy plates are marked in orange. They require a heavy cube. Standard cubes are not heavy cubes.",
                "Heavy cubes are also slower to move and harder to throw. Much like you."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 7f), exit = Opening.Door(Side.North, 7f);
                b.Room(0f, 0f, 14f, 16f, 6f, entry, exit);
                PressurePlate heavy = b.Plate(P(4f, 0f, 11f), PlateKind.PropsOnly, true);
                PressurePlate standard = b.Plate(P(10f, 0f, 11f), PlateKind.Any);
                b.Cube(P(3f, 0f, 4f), PropKind.Heavy);
                b.Cube(P(11f, 0f, 4f));
                b.Entry(entry);
                b.Exit(exit, heavy, standard);
            }));

            // 04 - Throw (RMB) or lift the cube over the glass pen onto the plate inside it.
            list.Add(new ChamberDef("Lob", 25f, new[]
            {
                "While holding an object, press [RIGHT MOUSE] to launch it.",
                "Aim high. The glass is not."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 7f), exit = Opening.Door(Side.North, 7f);
                b.Room(0f, 0f, 14f, 18f, 7f, entry, exit);
                b.GlassX(9f, 5f, 9f, 0f, 3f);
                b.GlassX(13f, 5f, 9f, 0f, 3f);
                b.GlassZ(5f, 9f, 13f, 0f, 3f);
                b.GlassZ(9f, 9f, 13f, 0f, 3f);
                PressurePlate plate = b.Plate(P(7f, 0f, 11f));
                b.Cube(P(3f, 0f, 5f), PropKind.Companion);
                b.Entry(entry);
                b.Exit(exit, plate);
                b.Say(P(7f, 1f, 6f), P(14f, 3f, 3f), "This is a Companion Cube. It is exactly like a standard cube, except you will miss it.");
            }));

            // 05 - The field dissolves cubes; throw the cube over the partition, walk through the field, place it.
            list.Add(new ChamberDef("Clean Room", 32f, new[]
            {
                "Blue energy fields dissolve any object that passes through them. Test subjects are not objects. Legally.",
                "Dissolved cubes are reconstructed at their dispenser, so feel free to experiment."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 6f), exit = Opening.Door(Side.North, 6f);
                b.Room(0f, 0f, 12f, 20f, 6f, entry, exit);
                b.WallX(10f, 0f, 12f, 0f, 3.6f, new Gap(6f, 2.4f, 0f, 3f));
                b.Field(P(6f, 0f, 10f), true, 2.4f, 3f, false);
                PressurePlate plate = b.Plate(P(6f, 0f, 15f));
                b.Cube(P(3f, 0f, 5f));
                b.Entry(entry);
                b.Exit(exit, plate);
            }));

            // 06 - Carry the cube under the ceiling plate and press F: it falls up onto the plate.
            list.Add(new ChamberDef("Topsy-Turvy", 22f, new[]
            {
                "Press [F] to invert an object's personal gravity. Its feelings about down will change permanently.",
                "Note the pressure plate on the ceiling. That is not a mistake. Probably."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 6f), exit = Opening.Door(Side.North, 6f);
                b.Room(0f, 0f, 12f, 14f, 5f, entry, exit);
                PressurePlate plate = b.CeilingPlate(6f, 9f);
                b.Cube(P(3f, 0f, 5f));
                b.Entry(entry);
                b.Exit(exit, plate);
            }));

            // 07 - The gun aims through glass: flip the cube up, then pull it over the glass from the ceiling.
            list.Add(new ChamberDef("Behind Glass", 35f, new[]
            {
                "The device can target objects through glass. The objects, however, cannot pass through glass.",
                "Up is a direction too. Use it."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 7f), exit = Opening.Door(Side.East, 6f);
                b.Room(0f, 0f, 14f, 18f, 7f, entry, exit);
                b.GlassX(12f, 0f, 14f, 0f, 3f);
                b.Cube(P(7f, 0f, 15f));
                PressurePlate plate = b.Plate(P(4f, 0f, 5f));
                b.Entry(entry);
                b.Exit(exit, plate);
                b.Say(P(7f, 1f, 9f), P(14f, 3f, 3f), "Inverted objects can be flipped back with [F] again, even while you hold them.");
            }));

            // 08 - Hold the cube over the goo, freeze it (Q), jump onto it, then onto the ledge.
            list.Add(new ChamberDef("Cold Feet", 28f, new[]
            {
                "Press [Q] to place an object in stasis. It will hang in the air, motionless, for five seconds.",
                "Frozen objects are very solid. You may stand on them. We encourage it."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 6f), exit = Opening.Door(Side.North, 6f, 1.7f);
                b.Shell(0f, 0f, 12f, 16f, -3f, 7f, entry, exit);
                b.Floor(0f, 0f, 12f, 9f, 0f);
                b.Goo(0f, 9f, 12f, 12f, -2f);
                b.Block(0f, 12f, 12f, 16f, -3f, 1.7f);
                b.Cube(P(6f, 0f, 5f));
                b.Entry(entry);
                b.Exit(exit);
                b.Say(P(6f, 1f, 7.5f), P(12f, 3f, 2f), "The ledge is out of jumping range. A step would help. You have a cube.");
            }));

            // 09 - Put the reflector cube in the beam, lens facing the receiver.
            list.Add(new ChamberDef("First Light", 25f, new[]
            {
                "This is a reflector cube. Lasers that hit it are re-emitted through its lens.",
                "A held cube turns with you. Face the receiver, let go, and admire the geometry."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 7f), exit = Opening.Door(Side.North, 4f);
                b.Room(0f, 0f, 14f, 16f, 6f, entry, exit);
                b.Emitter(P(0f, 0.47f, 4f), Vector3.right);
                LaserReceiver receiver = b.Receiver(P(10f, 0.47f, 16f), Vector3.back);
                b.Cube(P(5f, 0f, 9f), PropKind.Reflector);
                b.Entry(entry);
                b.Exit(exit, receiver);
            }));

            // 10 - Two fixed mirrors carry the beam around the room; finish the path with the reflector cube at (14, 5).
            list.Add(new ChamberDef("Hall of Mirrors", 32f, new[]
            {
                "Fixed mirrors reflect lasers. They do not move, and neither will you until this beam reaches its receiver.",
                "Follow the light."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 8f), exit = Opening.Door(Side.North, 8f);
                b.Room(0f, 0f, 16f, 18f, 6f, entry, exit);
                b.Emitter(P(2f, 0.47f, 0f), Vector3.forward);
                b.Mirror(P(2f, 0f, 14f), new Vector3(1f, 0f, -1f));
                b.Mirror(P(14f, 0f, 14f), new Vector3(-1f, 0f, -1f));
                LaserReceiver receiver = b.Receiver(P(16f, 0.47f, 5f), Vector3.left);
                b.Cube(P(8f, 0f, 8f), PropKind.Reflector, 90f);
                b.Entry(entry);
                b.Exit(exit, receiver);
            }));
        }
    }
}
