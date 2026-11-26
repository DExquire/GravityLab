# Gravity Lab

A first-person physics puzzler in the test-chamber tradition. You carry a gravity gun through
20 chambers: catch cubes, throw them, flip their gravity, freeze them in mid-air, bounce lasers off
mirrors, and ride platforms and updrafts. Every chamber has a timer, a rank (S/A/B/C) and a personal best.

The whole scene is built by code — procedural geometry, shaders, textures, UI and audio.

<!-- Add a gameplay GIF and a "Play in browser" link here once the WebGL build is published. -->

---

## What this project demonstrates

- **Physics gameplay that holds up:** spring-based object carrying, per-object gravity, kinematic
  freezing, force volumes, carrying platforms, and a raycast laser with reflections.
- **Data-driven level design through a modular kit.** 20 chambers on a difficulty curve, where
  mechanics are introduced one at a time and then combined.
- Fully procedural content: geometry, hand-written CG shaders for the built-in pipeline, textures,
  SDF UI sprites, synthesised SFX and music.
- **A finished PC product**, not a prototype: main menu, chamber select, pause, settings (volume,
  sensitivity, invert, FOV, head bob, fullscreen, resolution, quality, VSync), result and failure
  screens, saves, cursor capture.
- Compatibility work: Unity 2022.3 and Unity 6, both input backends, no TextMeshPro, no Asset Store
  dependencies.

---

## The mechanics

**Gravity gun.** The held object is carried *physically*: a critically damped spring pulls it toward a
point in front of the camera, so it collides with the world and drops out of the grip if it falls more
than 2 m behind or loses line of sight. You can aim through glass but not pull an object through it.
A held object rotates with the player, snapped to 90° from the angle it was grabbed at.

**Per-object gravity.** Every cube carries its own gravity direction, up or down. Flipped cubes stick
to the ceiling, which is what makes ceiling plates and ceiling conveyors work.

**Freeze.** A cube becomes kinematic for 5 seconds and you can stand on it. It blinks before expiring.

**Pressure plates** weigh what is on them: 20 kg for a normal plate, 50 kg for the orange heavy one.
`Any` counts the player, `PropsOnly` only responds to cubes. Plates mount on ceilings too.

**Lasers** are traced as a series of raycasts. Fixed mirrors reflect; a reflector cube re-emits through
a lens, but blocks the beam while held. Receivers are either momentary or latching. Glass and the
player do not interrupt a beam.

**Energy fields** dissolve any cube, including one in your hands — it reassembles at its dispenser.
Fields can be inverted, so a signal switches them off.

Plus floor and ceiling **conveyors**, **fans** (which lift light cubes and the player, but not heavy
cubes or a player holding one), **moving platforms** that carry both, and **doors**. Indicator wires
run along the floor from each signal source to what it drives — blue for no signal, orange for live.
An OVERSEER narrator explains mechanics through subtitles.

| Action | Key |
|---|---|
| Move / look | WASD or arrows / mouse |
| Jump / crouch / sprint | Space / Ctrl or C / Shift |
| Grab or release | LMB |
| Throw (push, with empty hands) | RMB |
| Flip object gravity | F |
| Freeze object for 5 s | Q |
| Hold distance | mouse wheel |
| Restart chamber / pause | R / Esc |

---

## Architecture notes

**Signals.** `SignalSource` (a plate, a receiver) exposes `IsActive`. A consumer — door, fan, conveyor,
platform, field — turns on when *all* of its inputs are active. That single AND rule covers every
chamber in the game.

**Zones without triggers.** Volumes are polled with `Physics.OverlapBoxNonAlloc` and a point-in-oriented-box
test instead of trigger colliders, so behaviour does not depend on how `CharacterController` happens to
report trigger events.

**Layers 8–11** (Props, Player, Held, Glass) are used directly, without TagManager entries. A held
object does not collide with the player.

**The `Lab` shader** is half-lambert with a tinted shadow, soft shadows through `AutoLight`, SH ambient,
a procedural panel grid in world space, fake reflections via Fresnel (an environment gradient plus a
ceiling-lamp highlight), and glow/stasis/dissolve driven per-instance through `MaterialPropertyBlock`.
Dissolve needed its own ShadowCaster pass. `Glass` is Fresnel transparency with highlights; `Field` is
an additive animated curtain.

```
Assets/
  Editor/GravityLabSceneSetup.cs   scene auto-creation, PC player settings
  Resources/Fonts                  LilitaOne, Fredoka (OFL)
  Resources/Shaders                Lab, Glass, Unlit, Field, Particle (Built-in, CG)
  Scripts/
    Core/       Bootstrap, GameManager, GameInput, SaveSystem, DisplaySettings,
                Layers, Palette, Rank, Tweener
    Player/     PlayerController — CharacterController, smooth accel, coyote time,
                jump buffer, crouch with ceiling check, head bob, platform carry
    Gadgets/    GravityGun, GunViewModel
    Puzzle/     PhysicsProp, PressurePlate, Door, LaserEmitter/Receiver/Mirror,
                ConveyorBelt, Fan, EnergyField, MovingPlatform, ExitElevator,
                NarratorZone, SignalWire, SignalSource
    Chambers/   ChamberBuilder (modular kit), ChamberLibrary (20 chambers), Chamber
    Rendering/  MatLib, ProcTex, FxManager, CameraShake, MenuCamera
    Audio/      AudioManager, SynthClips
    UI/         UIManager, screens, widgets, SDF sprites
```

---

## Running it

1. **Unity 2022.3.30f1** or **Unity 6 (6000.0)**, Built-in render pipeline.
2. Open the `GravityLab` folder through Unity Hub. After import the editor script creates
   `Assets/Scenes/Main.unity` with one `Bootstrap` object, adds it to Build Settings and applies PC
   player settings (FullScreenWindow, 1920×1080, resizable). If the scene does not appear, run
   **GravityLab → Rebuild Main Scene**.
3. Press Play. Best at 16:9 or 16:10.

Both input backends work — Input Manager, Input System 1.7.0, and Both.

---

## Adding a chamber

A chamber is a `ChamberDef(name, parTime, introLines, build)` in `ChamberLibrary`. The build script
calls kit methods; the unit is a metre and Y is up:

```csharp
list.Add(new ChamberDef("My Test", 30f, new[] { "Narrator line." }, b =>
{
    Opening entry = Opening.Door(Side.South, 6f), exit = Opening.Door(Side.North, 6f);
    b.Room(0f, 0f, 12f, 14f, 6f, entry, exit);   // floor, walls with openings, ceiling, lamps
    PressurePlate plate = b.Plate(P(8f, 0f, 7f));
    b.Cube(P(3f, 0f, 5f));                        // cube plus its dispenser
    b.Entry(entry);                               // arrival elevator = spawn point
    b.Exit(exit, plate);                          // exit elevator, door opens from the plate
}));
```

Kit methods: `Shell/Room`, `Floor`, `Block`, `Goo`, `WallX/WallZ` with `Gap`, `GlassX/GlassZ`,
`Catwalk`, `Cube`, `Plate/CeilingPlate`, `Emitter`, `Receiver`, `Mirror`, `Conveyor`, `Fan`,
`Platform`, `Field`, `InnerDoor`, `Say`. Wires route themselves (`AutoWires`). Every chamber carries a
comment describing its intended solution.

Rank: **S** at or under par, **A** up to 1.5× par, **B** up to 2.2× par, otherwise **C**. Clearing a
chamber unlocks the next.

---

## Tech

Unity 2022.3 LTS or Unity 6 · Built-in render pipeline · Input System 1.7.0 ·
two OFL fonts, no other assets.

---

Подробный разбор на русском: [README.ru.md](README.ru.md)
