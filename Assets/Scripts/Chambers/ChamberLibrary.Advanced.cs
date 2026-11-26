using System.Collections.Generic;
using GravityLab.Puzzle;
using UnityEngine;

namespace GravityLab.Chambers
{
    public static partial class ChamberLibrary
    {
        static void AddAdvanced(List<ChamberDef> list)
        {
            // 11 - Put the cube on the stopped belt, stand on the switch plate: the belt carries it through the slot.
            list.Add(new ChamberDef("Belt Drive", 32f, new[]
            {
                "Conveyor belts move objects. This one is switched off, which is the most relaxing thing about it.",
                "The slot in the glass is cube-sized. Test-subject-sized slots are on back order."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 4f), exit = Opening.Door(Side.North, 4f);
                b.Room(0f, 0f, 16f, 16f, 6f, entry, exit);
                b.GlassZ(10f, 0f, 7.2f, 0f, 6f);
                b.GlassZ(10f, 8.8f, 16f, 0f, 6f);
                b.GlassZ(10f, 7.2f, 8.8f, 1.32f, 6f);
                PressurePlate power = b.Plate(P(3f, 0f, 13f), PlateKind.Any);
                b.Conveyor(P(4f, 0f, 8f), P(13.4f, 0f, 8f), 1.4f, 2.5f, false, power);
                PressurePlate target = b.Plate(P(14.6f, 0f, 8f));
                b.Cube(P(2f, 0f, 4f));
                b.Entry(entry);
                b.Exit(exit, target);
            }));

            // 12 - Cube A powers the fan; ride the updraft holding cube B and place it on the ledge plate.
            list.Add(new ChamberDef("Updraft", 42f, new[]
            {
                "Updrafts lift light objects. You count as a light object. We checked.",
                "This fan requires a weighted plate. You know where to find weights."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 7f), exit = Opening.Door(Side.North, 7f, 4f);
                b.Room(0f, 0f, 14f, 16f, 9f, entry, exit);
                b.Block(0f, 10f, 14f, 16f, 0f, 4f);
                PressurePlate fanPlate = b.Plate(P(3f, 0f, 4f));
                b.Fan(P(7f, 0f, 7f), 5.5f, fanPlate);
                PressurePlate ledge = b.Plate(P(7f, 4f, 13f));
                b.Cube(P(11f, 0f, 3f));
                b.Cube(P(12f, 0f, 5.5f), PropKind.Companion);
                b.Entry(entry);
                b.Exit(exit, ledge);
            }));

            // 13 - Ride the moving platform over the goo carrying the cube.
            list.Add(new ChamberDef("Commute", 34f, new[]
            {
                "Moving platforms. Stand on one and it will take you places. Mostly places with goo underneath."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 6f), exit = Opening.Door(Side.North, 6f);
                b.Shell(0f, 0f, 12f, 26f, -3f, 7f, entry, exit);
                b.Floor(0f, 0f, 12f, 6f, 0f);
                b.Floor(0f, 20f, 12f, 26f, 0f);
                b.Goo(0f, 6f, 12f, 20f, -2f);
                b.Platform(P(6f, 0f, 7.6f), P(6f, 0f, 18.4f), new Vector3(3f, 0.4f, 3f), 2.2f);
                PressurePlate plate = b.Plate(P(9f, 0f, 23f));
                b.Cube(P(3f, 0f, 3f));
                b.Entry(entry);
                b.Exit(exit, plate);
            }));

            // 14 - Flip the cube up under the ceiling belt; it rides over the glass onto the ceiling plate.
            list.Add(new ChamberDef("Upside Conveyance", 28f, new[]
            {
                "Conveyors can be installed on ceilings. Our architect insisted. Our architect has since been inverted."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 3f), exit = Opening.Door(Side.North, 3f);
                b.Room(0f, 0f, 14f, 16f, 5f, entry, exit);
                b.GlassZ(9f, 0f, 16f, 0f, 3.5f);
                b.Conveyor(P(3f, 5f, 8f), P(12f, 5f, 8f), 1.4f, 2.2f, true);
                PressurePlate plate = b.CeilingPlate(12.8f, 8f);
                b.Cube(P(5f, 0f, 5f));
                b.Entry(entry);
                b.Exit(exit, plate);
            }));

            // 15 - Hold the reflector cube in the high beam at (10, 3.5, 4) facing north and freeze it; the receiver latches.
            list.Add(new ChamberDef("Suspended Light", 35f, new[]
            {
                "This receiver latches. Once lit, it stays lit. Unlike your career prospects.",
                "The beam is out of reach. Stasis is not."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 7f), exit = Opening.Door(Side.North, 4f);
                b.Room(0f, 0f, 14f, 16f, 7f, entry, exit);
                b.Emitter(P(0f, 3.5f, 4f), Vector3.right);
                LaserReceiver receiver = b.Receiver(P(10f, 3.5f, 16f), Vector3.back, true);
                b.Cube(P(4f, 0f, 8f), PropKind.Reflector);
                b.Entry(entry);
                b.Exit(exit, receiver);
            }));

            // 16 - Reflector cube at (12, 5) facing south powers the fan; ride it holding the standard cube to the ledge plate.
            list.Add(new ChamberDef("Uplift", 48f, new[]
            {
                "The fan is wired to a laser receiver. The receiver is wired to your ingenuity. One of those wires is loose."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 8f), exit = Opening.Door(Side.North, 8f, 4.5f);
                b.Room(0f, 0f, 16f, 18f, 10f, entry, exit);
                b.Block(0f, 12f, 16f, 18f, 0f, 4.5f);
                b.Emitter(P(0f, 0.47f, 5f), Vector3.right);
                LaserReceiver receiver = b.Receiver(P(12f, 0.47f, 0f), Vector3.forward);
                b.Fan(P(8f, 0f, 9f), 6.5f, receiver);
                PressurePlate ledge = b.Plate(P(8f, 4.5f, 15f));
                b.Cube(P(3f, 0f, 8f), PropKind.Reflector);
                b.Cube(P(13f, 0f, 9f));
                b.Entry(entry);
                b.Exit(exit, ledge);
            }));

            // 17 - Flip the reflector cube onto the ceiling in the high beam (11, 5.55, 4) facing south: it opens the vault.
            list.Add(new ChamberDef("Ceiling Light", 45f, new[]
            {
                "The vault opens for a laser. The laser lives near the ceiling. Introductions are in order."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 4f), exit = Opening.Door(Side.North, 4f);
                b.Room(0f, 0f, 14f, 18f, 6f, entry, exit);
                b.WallX(10f, 7.85f, 14f, 0f, 6f, new Gap(11f, 2.4f, 0f, 3f));
                b.WallZ(8f, 10f, 18f, 0f, 6f);
                b.Emitter(P(0f, 5.55f, 4f), Vector3.right);
                LaserReceiver receiver = b.Receiver(P(11f, 5.55f, 0f), Vector3.forward);
                b.InnerDoor(P(11f, 0f, 10f), true, receiver);
                b.Cube(P(3f, 0f, 7f), PropKind.Reflector);
                b.Cube(P(11f, 0f, 15f));
                PressurePlate plate = b.Plate(P(4f, 0f, 13f));
                b.Entry(entry);
                b.Exit(exit, plate);
            }));

            // 18 - Fans can't lift heavy cubes (or you, holding one). Flip it to the ceiling, ride up, pull it over, flip it down.
            list.Add(new ChamberDef("Heavy Lifting", 50f, new[]
            {
                "Updrafts cannot lift heavy cubes. Nor can they lift a test subject who is carrying one.",
                "Gravity, on the other hand, can be persuaded."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 8f), exit = Opening.Door(Side.North, 4f, 4f);
                b.Room(0f, 0f, 16f, 18f, 9f, entry, exit);
                b.Block(0f, 12f, 16f, 18f, 0f, 4f);
                b.Fan(P(4f, 0f, 9f), 6f);
                b.Cube(P(11f, 0f, 6f), PropKind.Heavy);
                PressurePlate plate = b.Plate(P(11f, 4f, 15f), PlateKind.PropsOnly, true);
                b.Entry(entry);
                b.Exit(exit, plate);
            }));

            // 19 - Reflector cube at (14, 3) facing south switches the slot's field OFF; ride over, belt the cube through.
            list.Add(new ChamberDef("Assembly Line", 60f, new[]
            {
                "This field switches off while its receiver is lit. Everything in this facility is connected. Except you."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 9f), exit = Opening.Door(Side.North, 6f);
                b.Shell(0f, 0f, 18f, 20f, -3f, 7f, entry, exit);
                b.Floor(0f, 0f, 18f, 6f, 0f);
                b.Floor(0f, 12f, 18f, 20f, 0f);
                b.Goo(0f, 6f, 18f, 12f, -2f);
                b.Platform(P(4f, 0f, 7.6f), P(4f, 0f, 10.4f), new Vector3(3f, 0.4f, 3f), 1.6f);
                b.Emitter(P(0f, 0.47f, 3f), Vector3.right);
                LaserReceiver receiver = b.Receiver(P(14f, 0.47f, 0f), Vector3.forward);
                b.Cube(P(6f, 0f, 1.5f), PropKind.Reflector);

                b.GlassZ(12f, 12f, 15.2f, 0f, 7f);
                b.GlassZ(12f, 16.8f, 20f, 0f, 7f);
                b.GlassZ(12f, 15.2f, 16.8f, 1.32f, 7f);
                b.Conveyor(P(6f, 0f, 16f), P(15f, 0f, 16f), 1.4f, 2.4f, false);
                b.AutoWires = false;
                b.Field(P(12f, 0f, 16f), false, 1.5f, 1.32f, true, receiver);
                b.AutoWires = true;
                PressurePlate plate = b.Plate(P(16.4f, 0f, 16f));
                b.Cube(P(3f, 0f, 17f));
                b.Entry(entry);
                b.Exit(exit, plate);
            }));

            // 20 - Laser powers the platform; carry the heavy cube across and flip it onto the ceiling plate;
            //      pull the standard cube off the catwalk onto the floor plate.
            list.Add(new ChamberDef("Graduation", 80f, new[]
            {
                "Final test. Everything you have learned, all at once, with goo.",
                "Complete it and you will receive a certificate. The certificate is also a cube."
            }, b =>
            {
                Opening entry = Opening.Door(Side.South, 10f), exit = Opening.Door(Side.North, 8f);
                b.Shell(0f, 0f, 20f, 26f, -3f, 8f, entry, exit);
                b.Floor(0f, 0f, 20f, 8f, 0f);
                b.Floor(0f, 14f, 20f, 26f, 0f);
                b.Goo(0f, 8f, 20f, 14f, -2f);
                b.Emitter(P(0f, 0.47f, 4f), Vector3.right);
                LaserReceiver receiver = b.Receiver(P(16f, 0.47f, 0f), Vector3.forward);
                b.AutoWires = false;
                b.Platform(P(10f, 0f, 9.6f), P(10f, 0f, 12.4f), new Vector3(3f, 0.4f, 3f), 1.6f, receiver);
                b.AutoWires = true;
                b.Cube(P(4f, 0f, 2f), PropKind.Reflector);
                b.Cube(P(14f, 0f, 6.5f), PropKind.Heavy);
                PressurePlate ceiling = b.CeilingPlate(5f, 20f, true);
                b.Catwalk(14f, 20f, 20f, 24f, 3f);
                b.Cube(P(17f, 3f, 22f), PropKind.Companion);
                PressurePlate floor = b.Plate(P(10f, 0f, 19f));
                b.Entry(entry);
                b.Exit(exit, ceiling, floor);
            }));
        }
    }
}
