using System;
using System.Collections.Generic;
using GravityLab.Core;
using GravityLab.Puzzle;
using GravityLab.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GravityLab.Chambers
{
    /// <summary>
    /// Modular room kit used by the chamber scripts: shells (floor/walls/ceiling with openings), floors, blocks, goo pits,
    /// glass, catwalks, entry/exit elevators, and every puzzle element, wired to their signal sources automatically.
    /// World units are metres; +Y is up, the room interior spans x0..x1 / z0..z1.
    /// </summary>
    public sealed class ChamberBuilder
    {
        const float WallThickness = 0.3f;
        const float BoothWidth = 3f;
        const float BoothDepth = 3.2f;
        const float BoothHeight = 3.4f;

        readonly Chamber _chamber;
        readonly Transform _root;
        readonly ChamberDef _def;
        readonly Action<string> _narrate;
        float _x0, _z0, _x1, _z1, _bottom, _ceiling = 6f;

        /// <summary>Draw floor indicator wires from signal sources to the elements they power.</summary>
        public bool AutoWires { get; set; }
        public float CeilingY { get { return _ceiling; } }

        public ChamberBuilder(Chamber chamber, ChamberDef def, Action<string> narrate)
        {
            _chamber = chamber;
            _root = chamber.transform;
            _def = def;
            _narrate = narrate;
            AutoWires = true;
            _chamber.KillY = -40f;
        }

        // ---- primitives ------------------------------------------------------------------------

        GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Quaternion localRot, Material mat, bool collider = true, bool shadows = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            if (!collider) UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent != null ? parent : _root, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        GameObject Box(Vector3 center, Vector3 size, Material mat, bool collider = true, bool shadows = true, Transform parent = null)
        {
            return Prim(PrimitiveType.Cube, parent, center, size, Quaternion.identity, mat, collider, shadows);
        }

        Transform Group(string name, Vector3 position, Quaternion rotation)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(_root, false);
            t.SetPositionAndRotation(position, rotation);
            return t;
        }

        // ---- structure -------------------------------------------------------------------------

        /// <summary>Flat room with its floor at y = 0.</summary>
        public void Room(float x0, float z0, float x1, float z1, float height, params Opening[] openings)
        {
            Shell(x0, z0, x1, z1, 0f, height, openings);
        }

        /// <summary>Four walls, ceiling and a bottom slab at <paramref name="bottom"/> (use a negative bottom for pit rooms).</summary>
        public void Shell(float x0, float z0, float x1, float z1, float bottom, float top, params Opening[] openings)
        {
            _x0 = x0; _z0 = z0; _x1 = x1; _z1 = z1; _bottom = bottom; _ceiling = top;
            _chamber.Center = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            _chamber.Size = new Vector3(x1 - x0, top, z1 - z0);
            _chamber.KillY = bottom - 5f;
            const float t = WallThickness;

            Wall(true, z1 + t * 0.5f, x0 - t, x1 + t, bottom, top, t, GapsFor(Side.North, openings));
            Wall(true, z0 - t * 0.5f, x0 - t, x1 + t, bottom, top, t, GapsFor(Side.South, openings));
            Wall(false, x1 + t * 0.5f, z0, z1, bottom, top, t, GapsFor(Side.East, openings));
            Wall(false, x0 - t * 0.5f, z0, z1, bottom, top, t, GapsFor(Side.West, openings));

            Vector3 mid = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            Vector3 span = new Vector3(x1 - x0 + 2f * t, t, z1 - z0 + 2f * t);
            Box(mid + Vector3.up * (top + t * 0.5f), span, MatLib.Ceiling, true, false);
            Box(mid + Vector3.up * (bottom - t * 0.5f), span, MatLib.Floor);

            // Ceiling light panels and a glowing trim line just under the ceiling.
            bool rowsAlongX = (x1 - x0) <= (z1 - z0);
            float length = rowsAlongX ? z1 - z0 : x1 - x0;
            int rows = Mathf.Max(1, Mathf.RoundToInt(length / 4f));
            for (int i = 0; i < rows; i++)
            {
                float c = (rowsAlongX ? z0 : x0) + (i + 0.5f) * length / rows;
                Vector3 p = rowsAlongX ? new Vector3(mid.x, top - 0.02f, c) : new Vector3(c, top - 0.02f, mid.z);
                Vector3 s = rowsAlongX ? new Vector3(x1 - x0 - 2f, 0.04f, 0.45f) : new Vector3(0.45f, 0.04f, z1 - z0 - 2f);
                Box(p, s, MatLib.LightPanel, false, false);
            }
            float trimY = top - 0.4f;
            Box(new Vector3(mid.x, trimY, z1 - 0.01f), new Vector3(x1 - x0, 0.05f, 0.03f), MatLib.AccentBlue, false, false);
            Box(new Vector3(mid.x, trimY, z0 + 0.01f), new Vector3(x1 - x0, 0.05f, 0.03f), MatLib.AccentBlue, false, false);
            Box(new Vector3(x1 - 0.01f, trimY, mid.z), new Vector3(0.03f, 0.05f, z1 - z0), MatLib.AccentBlue, false, false);
            Box(new Vector3(x0 + 0.01f, trimY, mid.z), new Vector3(0.03f, 0.05f, z1 - z0), MatLib.AccentBlue, false, false);
        }

        Gap[] GapsFor(Side side, Opening[] openings)
        {
            List<Gap> gaps = new List<Gap>();
            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i].Side == side) gaps.Add(new Gap(openings[i].Along, openings[i].Width, openings[i].Bottom, openings[i].Height));
            }
            return gaps.ToArray();
        }

        /// <summary>Wall running along X at a fixed Z, with optional gaps.</summary>
        public void WallX(float z, float x0, float x1, float y0, float y1, params Gap[] gaps) { Wall(true, z, x0, x1, y0, y1, WallThickness, gaps); }

        /// <summary>Wall running along Z at a fixed X, with optional gaps.</summary>
        public void WallZ(float x, float z0, float z1, float y0, float y1, params Gap[] gaps) { Wall(false, x, z0, z1, y0, y1, WallThickness, gaps); }

        void Wall(bool alongX, float fixedCoord, float a0, float a1, float y0, float y1, float thickness, Gap[] gaps)
        {
            Gap[] sorted = (Gap[])gaps.Clone();
            Array.Sort(sorted, (p, q) => p.Center.CompareTo(q.Center));
            float cursor = a0;
            for (int i = 0; i < sorted.Length; i++)
            {
                Gap g = sorted[i];
                float g0 = g.Center - g.Width * 0.5f, g1 = g.Center + g.Width * 0.5f;
                if (g0 > cursor + 0.01f) WallPiece(alongX, fixedCoord, cursor, g0, y0, y1, thickness);
                if (g.Bottom > y0 + 0.01f) WallPiece(alongX, fixedCoord, g0, g1, y0, g.Bottom, thickness);
                float top = g.Bottom + g.Height;
                if (top < y1 - 0.01f) WallPiece(alongX, fixedCoord, g0, g1, top, y1, thickness);
                cursor = g1;
            }
            if (a1 > cursor + 0.01f) WallPiece(alongX, fixedCoord, cursor, a1, y0, y1, thickness);
        }

        void WallPiece(bool alongX, float fixedCoord, float a0, float a1, float y0, float y1, float thickness)
        {
            Vector3 c = alongX ? new Vector3((a0 + a1) * 0.5f, (y0 + y1) * 0.5f, fixedCoord) : new Vector3(fixedCoord, (y0 + y1) * 0.5f, (a0 + a1) * 0.5f);
            Vector3 s = alongX ? new Vector3(a1 - a0, y1 - y0, thickness) : new Vector3(thickness, y1 - y0, a1 - a0);
            Box(c, s, MatLib.Panel);
        }

        /// <summary>Floor slab whose top is at <paramref name="y"/>. Raised slabs get glowing edge lines.</summary>
        public void Floor(float x0, float z0, float x1, float z1, float y)
        {
            Box(new Vector3((x0 + x1) * 0.5f, y - 0.15f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.3f, z1 - z0), MatLib.Floor);
            if (y > _bottom + 0.5f) EdgeLines(x0, z0, x1, z1, y, MatLib.AccentOrange);
        }

        /// <summary>Solid lab block (ledges, steps, overhangs) from <paramref name="y0"/> to <paramref name="y1"/>.</summary>
        public void Block(float x0, float z0, float x1, float z1, float y0, float y1)
        {
            Box(new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), MatLib.Panel);
            if (y1 > 0.5f && y1 < _ceiling - 0.5f) EdgeLines(x0, z0, x1, z1, y1, MatLib.AccentBlue);
        }

        void EdgeLines(float x0, float z0, float x1, float z1, float y, Material mat)
        {
            const float w = 0.05f;
            Box(new Vector3((x0 + x1) * 0.5f, y + 0.005f, z0 + w), new Vector3(x1 - x0, 0.012f, w), mat, false, false);
            Box(new Vector3((x0 + x1) * 0.5f, y + 0.005f, z1 - w), new Vector3(x1 - x0, 0.012f, w), mat, false, false);
            Box(new Vector3(x0 + w, y + 0.005f, (z0 + z1) * 0.5f), new Vector3(w, 0.012f, z1 - z0), mat, false, false);
            Box(new Vector3(x1 - w, y + 0.005f, (z0 + z1) * 0.5f), new Vector3(w, 0.012f, z1 - z0), mat, false, false);
        }

        /// <summary>Toxic goo surface at <paramref name="y"/>; touching it kills the player and respawns props.</summary>
        public void Goo(float x0, float z0, float x1, float z1, float y)
        {
            Box(new Vector3((x0 + x1) * 0.5f, y - 0.03f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.06f, z1 - z0), MatLib.Goo, false, false);
            _chamber.KillY = Mathf.Max(_chamber.KillY, y - 0.1f);
        }

        public void GlassX(float z, float x0, float x1, float y0, float y1) { Glass(true, z, x0, x1, y0, y1); }
        public void GlassZ(float x, float z0, float z1, float y0, float y1) { Glass(false, x, z0, z1, y0, y1); }

        void Glass(bool alongX, float fixedCoord, float a0, float a1, float y0, float y1)
        {
            Vector3 c = alongX ? new Vector3((a0 + a1) * 0.5f, (y0 + y1) * 0.5f, fixedCoord) : new Vector3(fixedCoord, (y0 + y1) * 0.5f, (a0 + a1) * 0.5f);
            Vector3 s = alongX ? new Vector3(a1 - a0, y1 - y0, 0.05f) : new Vector3(0.05f, y1 - y0, a1 - a0);
            GameObject pane = Box(c, s, MatLib.Glass, true, false);
            pane.layer = Layers.Glass;
            pane.GetComponent<Renderer>().receiveShadows = false;

            // Frame: mullions every <= 4 m plus top and bottom rails.
            int segments = Mathf.Max(1, Mathf.CeilToInt((a1 - a0) / 4f - 0.01f));
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)segments);
                Vector3 mp = alongX ? new Vector3(a, c.y, fixedCoord) : new Vector3(fixedCoord, c.y, a);
                Box(mp, new Vector3(0.1f, y1 - y0, 0.1f), MatLib.Metal);
            }
            Vector3 railSize = alongX ? new Vector3(a1 - a0, 0.1f, 0.12f) : new Vector3(0.12f, 0.1f, a1 - a0);
            Box(new Vector3(c.x, y1 - 0.05f, c.z), railSize, MatLib.Metal);
            Box(new Vector3(c.x, y0 + 0.05f, c.z), railSize, MatLib.Metal);
        }

        /// <summary>Metal grate walkway with railings along its two long sides and support legs down to y = 0.</summary>
        public void Catwalk(float x0, float z0, float x1, float z1, float y)
        {
            bool alongX = (x1 - x0) >= (z1 - z0);
            Box(new Vector3((x0 + x1) * 0.5f, y - 0.05f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.1f, z1 - z0), MatLib.MetalLight);
            for (int side = 0; side < 2; side++)
            {
                float edge = alongX ? (side == 0 ? z0 + 0.04f : z1 - 0.04f) : (side == 0 ? x0 + 0.04f : x1 - 0.04f);
                float a0 = alongX ? x0 : z0, a1 = alongX ? x1 : z1;
                Vector3 railCenter = alongX ? new Vector3((x0 + x1) * 0.5f, y + 1f, edge) : new Vector3(edge, y + 1f, (z0 + z1) * 0.5f);
                Box(railCenter, alongX ? new Vector3(x1 - x0, 0.06f, 0.06f) : new Vector3(0.06f, 0.06f, z1 - z0), MatLib.Metal);
                Box(railCenter - Vector3.up * 0.5f, alongX ? new Vector3(x1 - x0, 0.04f, 0.04f) : new Vector3(0.04f, 0.04f, z1 - z0), MatLib.Metal, false);
                int posts = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / 1.5f) + 1);
                for (int i = 0; i < posts; i++)
                {
                    float a = Mathf.Lerp(a0 + 0.04f, a1 - 0.04f, i / (float)(posts - 1));
                    Vector3 p = alongX ? new Vector3(a, y + 0.5f, edge) : new Vector3(edge, y + 0.5f, a);
                    Box(p, new Vector3(0.05f, 1f, 0.05f), MatLib.Metal, false);
                }
            }
            float[] xs = { x0 + 0.15f, x1 - 0.15f };
            float[] zs = { z0 + 0.15f, z1 - 0.15f };
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++) Box(new Vector3(xs[i], (y - 0.1f) * 0.5f, zs[j]), new Vector3(0.12f, y - 0.1f, 0.12f), MatLib.Metal);
            }
        }

        // ---- elevators -------------------------------------------------------------------------

        /// <summary>Arrival elevator behind <paramref name="o"/>; the player starts inside it.</summary>
        public void Entry(Opening o)
        {
            Vector3 outward;
            Vector3 wallPoint = WallPoint(o, out outward);
            Door door = Booth(o, false, null);
            door.ForceClosed = true;
            _chamber.EntryDoor = door;
            _chamber.SpawnPosition = wallPoint + outward * (WallThickness + BoothDepth * 0.5f) + Vector3.up * 0.05f;
            _chamber.SpawnYaw = Quaternion.LookRotation(-outward).eulerAngles.y;
            Sign(o, wallPoint, outward);
        }

        /// <summary>Exit elevator behind <paramref name="o"/>; its door opens while all <paramref name="inputs"/> are powered.</summary>
        public void Exit(Opening o, params SignalSource[] inputs)
        {
            Booth(o, true, inputs);
        }

        Vector3 WallPoint(Opening o, out Vector3 outward)
        {
            switch (o.Side)
            {
                case Side.North: outward = Vector3.forward; return new Vector3(o.Along, o.Bottom, _z1);
                case Side.South: outward = Vector3.back; return new Vector3(o.Along, o.Bottom, _z0);
                case Side.East: outward = Vector3.right; return new Vector3(_x1, o.Bottom, o.Along);
                default: outward = Vector3.left; return new Vector3(_x0, o.Bottom, o.Along);
            }
        }

        Door Booth(Opening o, bool isExit, SignalSource[] inputs)
        {
            Vector3 outward;
            Vector3 wallPoint = WallPoint(o, out outward);
            Quaternion rot = Quaternion.LookRotation(outward);
            Transform booth = Group(isExit ? "ExitElevator" : "EntryElevator", wallPoint, rot);
            const float t = WallThickness, w = BoothWidth, d = BoothDepth, h = BoothHeight;
            Material accent = isExit ? MatLib.AccentOrange : MatLib.AccentBlue;

            Box(new Vector3(0f, -0.15f, (t + d) * 0.5f), new Vector3(w + 2f * t, 0.3f, t + d), MatLib.Floor, true, true, booth);
            Box(new Vector3(0f, h + 0.15f, (t + d + t) * 0.5f), new Vector3(w + 2f * t, 0.3f, t + d + t), MatLib.Ceiling, true, false, booth);
            Box(new Vector3(0f, h * 0.5f, t + d + t * 0.5f), new Vector3(w + 2f * t, h, t), MatLib.Panel, true, true, booth);
            Box(new Vector3(w * 0.5f + t * 0.5f, h * 0.5f, t + d * 0.5f), new Vector3(t, h, d), MatLib.Panel, true, true, booth);
            Box(new Vector3(-w * 0.5f - t * 0.5f, h * 0.5f, t + d * 0.5f), new Vector3(t, h, d), MatLib.Panel, true, true, booth);

            Box(new Vector3(0f, h - 0.03f, t + d * 0.5f), new Vector3(w - 0.6f, 0.04f, d - 0.6f), MatLib.LightPanel, false, false, booth);
            Box(new Vector3(0f, 0.006f, t + d * 0.5f), new Vector3(w - 0.5f, 0.012f, d - 0.5f), MatLib.Metal, false, false, booth);
            Box(new Vector3(0f, 0.012f, t + d * 0.5f), new Vector3(w - 0.8f, 0.012f, d - 0.8f), accent, false, false, booth);
            Box(new Vector3(0f, 0.018f, t + d * 0.5f), new Vector3(w - 0.95f, 0.012f, d - 0.95f), MatLib.Metal, false, false, booth);
            for (int i = -1; i <= 1; i += 2)
            {
                Box(new Vector3(i * (w * 0.5f - 0.02f), h * 0.5f, t + d * 0.5f), new Vector3(0.03f, h - 0.6f, 0.06f), accent, false, false, booth);
            }
            Box(new Vector3(0f, h * 0.5f, t + d - 0.02f), new Vector3(0.08f, h - 0.6f, 0.03f), accent, false, false, booth);

            Door door = BuildDoor(wallPoint + outward * (t * 0.5f), rot, o.Width, o.Height, inputs, isExit);

            if (isExit)
            {
                Transform zone = new GameObject("ExitZone").transform;
                zone.SetParent(booth, false);
                zone.localPosition = new Vector3(0f, 0f, t + d * 0.5f);
                ExitElevator exit = zone.gameObject.AddComponent<ExitElevator>();
                exit.Init(door, new Vector3(w * 0.5f - 0.3f, 1.2f, d * 0.5f - 0.4f));
                _chamber.Exit = exit;
            }
            return door;
        }

        void Sign(Opening o, Vector3 wallPoint, Vector3 outward)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, outward);
            Vector3 pos = wallPoint - tangent * (o.Width * 0.5f + 1.5f) + Vector3.up * 2.1f - outward * 0.01f;

            GameObject go = new GameObject("Sign", typeof(RectTransform));
            go.transform.SetParent(_root, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(220f, 150f);
            rt.SetPositionAndRotation(pos, Quaternion.LookRotation(outward));
            rt.localScale = Vector3.one * 0.01f;

            Image bg = new GameObject("Bg", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            Stretch(bg.rectTransform);
            bg.color = new Color(0.12f, 0.15f, 0.2f, 0.92f);

            Text number = SignText(go.transform, (_chamber.Index + 1).ToString("00"), Fonts.Title, 84, new Vector2(0f, 18f), Palette.BlueGlow);
            number.alignment = TextAnchor.MiddleCenter;
            Text label = SignText(go.transform, _def.Name.ToUpperInvariant(), Fonts.Bold, 17, new Vector2(0f, -48f), Color.white);
            label.alignment = TextAnchor.MiddleCenter;
        }

        static Text SignText(Transform parent, string value, Font font, int size, Vector2 offset, Color color)
        {
            Text t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.rectTransform.sizeDelta = new Vector2(210f, 100f);
            t.rectTransform.anchoredPosition = offset;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.text = value;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---- doors -----------------------------------------------------------------------------

        /// <summary>Sliding door inside the room (in a gap of an interior wall).</summary>
        public Door InnerDoor(Vector3 bottomCenter, bool alongX, params SignalSource[] inputs)
        {
            return BuildDoor(bottomCenter, alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f), 2.4f, 3f, inputs, false);
        }

        Door BuildDoor(Vector3 bottomCenter, Quaternion rot, float width, float height, SignalSource[] inputs, bool exitStyle)
        {
            Transform root = Group("Door", bottomCenter, rot);
            Box(new Vector3(width * 0.5f + 0.08f, (height + 0.16f) * 0.5f, 0f), new Vector3(0.16f, height + 0.16f, 0.42f), MatLib.Metal, true, true, root);
            Box(new Vector3(-width * 0.5f - 0.08f, (height + 0.16f) * 0.5f, 0f), new Vector3(0.16f, height + 0.16f, 0.42f), MatLib.Metal, true, true, root);
            Box(new Vector3(0f, height + 0.08f, 0f), new Vector3(width + 0.32f, 0.16f, 0.42f), MatLib.Metal, true, true, root);
            Renderer lamp = Box(new Vector3(0f, height + 0.08f, 0f), new Vector3(width * 0.6f, 0.05f, 0.44f), MatLib.Signal, false, false, root).GetComponent<Renderer>();
            Renderer lampL = Box(new Vector3(width * 0.5f + 0.08f, height * 0.5f, 0f), new Vector3(0.04f, height * 0.7f, 0.44f), MatLib.Signal, false, false, root).GetComponent<Renderer>();
            Renderer lampR = Box(new Vector3(-width * 0.5f - 0.08f, height * 0.5f, 0f), new Vector3(0.04f, height * 0.7f, 0.44f), MatLib.Signal, false, false, root).GetComponent<Renderer>();

            Rigidbody left = DoorPanel(root, new Vector3(-width * 0.25f, height * 0.5f, 0f), new Vector3(width * 0.5f, height, 0.12f), exitStyle);
            Rigidbody right = DoorPanel(root, new Vector3(width * 0.25f, height * 0.5f, 0f), new Vector3(width * 0.5f, height, 0.12f), exitStyle);

            Door door = root.gameObject.AddComponent<Door>();
            door.Init(left, right, width * 0.5f * 0.98f, new[] { lamp, lampL, lampR });
            door.Inputs = inputs;
            WireAll(inputs, bottomCenter);
            return door;
        }

        Rigidbody DoorPanel(Transform root, Vector3 localPos, Vector3 size, bool exitStyle)
        {
            GameObject panel = Box(localPos, size, MatLib.MetalLight, true, true, root);
            Rigidbody rb = panel.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            float innerEdge = localPos.x < 0f ? 0.5f - 0.04f : -0.5f + 0.04f;
            Box(new Vector3(innerEdge, 0f, 0f), new Vector3(0.04f, 0.8f, 1.08f), exitStyle ? MatLib.AccentOrange : MatLib.AccentBlue, false, false, panel.transform);
            return rb;
        }

        // ---- props -----------------------------------------------------------------------------

        /// <summary>Cube resting on <paramref name="floorPoint"/>. With a dispenser, a ceiling chute marks where it respawns.</summary>
        public PhysicsProp Cube(Vector3 floorPoint, PropKind kind = PropKind.Standard, float yaw = 0f, bool dispenser = true)
        {
            float size = kind == PropKind.Heavy ? 1f : 0.9f;
            Vector3 pos = floorPoint + Vector3.up * (size * 0.5f + 0.02f);
            GameObject go = Prim(PrimitiveType.Cube, null, pos, Vector3.one * size, Quaternion.Euler(0f, yaw, 0f), MatLib.Cube(kind));
            go.name = kind + "Cube";

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = kind == PropKind.Heavy ? 60f : 20f;
            rb.useGravity = false;
            rb.drag = 0.05f;
            rb.angularDrag = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.maxAngularVelocity = 20f;

            Renderer lens = null;
            if (kind == PropKind.Reflector)
            {
                Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0f, 0.5f), new Vector3(0.72f, 0.015f, 0.72f), Quaternion.Euler(90f, 0f, 0f), MatLib.Metal, false, false);
                lens = Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0f, 0.505f), new Vector3(0.5f, 0.012f, 0.5f), Quaternion.Euler(90f, 0f, 0f), MatLib.Lens, false, false).GetComponent<Renderer>();
            }

            PhysicsProp prop = go.AddComponent<PhysicsProp>();
            prop.Init(kind, lens);
            _chamber.Props.Add(prop);
            if (dispenser) Dispenser(pos);
            return prop;
        }

        void Dispenser(Vector3 spawn)
        {
            Vector3 top = new Vector3(spawn.x, _ceiling, spawn.z);
            Prim(PrimitiveType.Cylinder, null, top - Vector3.up * 0.2f, new Vector3(1.5f, 0.2f, 1.5f), Quaternion.identity, MatLib.Metal, false, false);
            Prim(PrimitiveType.Cylinder, null, top - Vector3.up * 0.42f, new Vector3(1.25f, 0.02f, 1.25f), Quaternion.identity, MatLib.AccentBlue, false, false);
            float tubeBottom = spawn.y + 2.4f;
            float tubeLength = _ceiling - 0.4f - tubeBottom;
            if (tubeLength > 0.4f)
            {
                Prim(PrimitiveType.Cylinder, null, new Vector3(spawn.x, tubeBottom + tubeLength * 0.5f, spawn.z), new Vector3(1.15f, tubeLength * 0.5f, 1.15f), Quaternion.identity, MatLib.Glass, false, false);
                Prim(PrimitiveType.Cylinder, null, new Vector3(spawn.x, tubeBottom, spawn.z), new Vector3(1.3f, 0.06f, 1.3f), Quaternion.identity, MatLib.Metal, false, false);
            }
        }

        // ---- signal sources --------------------------------------------------------------------

        /// <summary>Floor plate. Standard plates need 20 kg (one cube), heavy plates 50 kg (a heavy cube).</summary>
        public PressurePlate Plate(Vector3 floorPoint, PlateKind kind = PlateKind.PropsOnly, bool heavy = false)
        {
            return BuildPlate(floorPoint, Quaternion.identity, kind, heavy);
        }

        /// <summary>Plate mounted upside-down on the ceiling: only props with reversed gravity can press it.</summary>
        public PressurePlate CeilingPlate(float x, float z, bool heavy = false)
        {
            return BuildPlate(new Vector3(x, _ceiling, z), Quaternion.Euler(180f, 0f, 0f), PlateKind.PropsOnly, heavy);
        }

        PressurePlate BuildPlate(Vector3 position, Quaternion rot, PlateKind kind, bool heavy)
        {
            Transform root = Group(heavy ? "HeavyPlate" : "Plate", position, rot);
            BoxCollider col = root.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.05f, 0f);
            col.size = new Vector3(1.7f, 0.1f, 1.7f);

            float baseSize = heavy ? 2.1f : 1.8f;
            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.05f, 0f), new Vector3(baseSize, 0.05f, baseSize), Quaternion.identity, MatLib.Metal, false);
            Renderer ring = Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.1f, 0f), new Vector3(baseSize - 0.25f, 0.012f, baseSize - 0.25f), Quaternion.identity, MatLib.Signal, false, false).GetComponent<Renderer>();
            Material padMat = kind == PlateKind.Any ? MatLib.Plastic : MatLib.MetalLight;
            Transform pad = Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.13f, 0f), new Vector3(1.3f, 0.03f, 1.3f), Quaternion.identity, padMat, false).transform;
            if (kind == PlateKind.PropsOnly)
            {
                // Square inset hints "place a cube here".
                Prim(PrimitiveType.Cube, pad, new Vector3(0f, 1.05f, 0f), new Vector3(0.62f, 0.3f, 0.62f), Quaternion.identity, MatLib.Metal, false, false);
            }
            if (heavy)
            {
                for (int i = 0; i < 4; i++)
                {
                    Quaternion r = Quaternion.Euler(0f, 45f + i * 90f, 0f);
                    Prim(PrimitiveType.Cube, root, r * new Vector3(0f, 0.105f, baseSize * 0.42f), new Vector3(0.35f, 0.02f, 0.12f), r, MatLib.AccentOrange, false, false);
                }
            }

            PressurePlate plate = root.gameObject.AddComponent<PressurePlate>();
            plate.Init(kind, heavy ? 50f : 20f, pad, ring);
            return plate;
        }

        /// <summary>Laser emitter mounted on a surface at <paramref name="wallPoint"/>, firing along <paramref name="direction"/>.</summary>
        public LaserEmitter Emitter(Vector3 wallPoint, Vector3 direction)
        {
            Quaternion rot = Quaternion.LookRotation(direction);
            Transform root = Group("LaserEmitter", wallPoint, rot);
            Box(new Vector3(0f, 0f, 0.25f), new Vector3(0.6f, 0.6f, 0.5f), MatLib.Metal, true, true, root);
            Box(new Vector3(0f, 0f, 0.5f), new Vector3(0.36f, 0.36f, 0.04f), MatLib.LaserHousing, false, false, root);
            Box(new Vector3(0f, 0.31f, 0.25f), new Vector3(0.62f, 0.03f, 0.3f), MatLib.AccentOrange, false, false, root);

            GameObject origin = new GameObject("Beam");
            origin.transform.SetParent(root, false);
            origin.transform.localPosition = new Vector3(0f, 0f, 0.56f);
            LineRenderer core = Line(origin.transform, MatLib.LaserCore, 0.035f);
            LineRenderer glow = Line(origin.transform, MatLib.LaserGlow, 0.16f);
            LaserEmitter emitter = origin.AddComponent<LaserEmitter>();
            emitter.Init(direction, core, glow);
            return emitter;
        }

        static LineRenderer Line(Transform parent, Material mat, float width)
        {
            GameObject go = new GameObject("Line");
            go.transform.SetParent(parent, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.positionCount = 0;
            return lr;
        }

        /// <summary>Laser receiver on a surface, facing <paramref name="facing"/>. Latching receivers stay on once hit.</summary>
        public LaserReceiver Receiver(Vector3 wallPoint, Vector3 facing, bool latching = false)
        {
            Quaternion rot = Quaternion.LookRotation(facing);
            Transform root = Group("LaserReceiver", wallPoint, rot);
            GameObject housing = Box(new Vector3(0f, 0f, 0.25f), new Vector3(0.8f, 0.8f, 0.5f), MatLib.Metal, true, true, root);
            Renderer core = Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0f, 0.5f), Vector3.one * 0.42f, Quaternion.identity, MatLib.Signal, false, false).GetComponent<Renderer>();
            Transform ring = new GameObject("Ring").transform;
            ring.SetParent(root, false);
            ring.localPosition = new Vector3(0f, 0f, 0.52f);
            for (int i = 0; i < 4; i++)
            {
                Quaternion r = Quaternion.Euler(0f, 0f, i * 90f);
                Prim(PrimitiveType.Cube, ring, r * new Vector3(0f, 0.3f, 0f), new Vector3(0.18f, 0.05f, 0.05f), r, MatLib.AccentOrange, false, false);
            }
            if (latching) Box(new Vector3(0f, -0.43f, 0.3f), new Vector3(0.5f, 0.06f, 0.3f), MatLib.AccentOrange, false, false, root);

            LaserReceiver receiver = housing.AddComponent<LaserReceiver>();
            Vector3 anchor = wallPoint + facing * 0.4f;
            anchor.y = 0f;
            receiver.Init(core, ring, latching, anchor);
            return receiver;
        }

        /// <summary>Static mirror standing on <paramref name="floorPoint"/>, reflective side facing <paramref name="normal"/>.</summary>
        public void Mirror(Vector3 floorPoint, Vector3 normal)
        {
            normal.y = 0f;
            normal.Normalize();
            Quaternion rot = Quaternion.LookRotation(normal);
            Transform root = Group("Mirror", floorPoint, rot);
            Box(new Vector3(0f, 0.15f, -0.05f), new Vector3(0.7f, 0.3f, 0.5f), MatLib.Metal, true, true, root);
            Box(new Vector3(0f, 0.95f, -0.08f), new Vector3(1.75f, 1.45f, 0.05f), MatLib.Metal, true, true, root);
            GameObject panel = Box(new Vector3(0f, 0.95f, 0f), new Vector3(1.6f, 1.3f, 0.08f), MatLib.Mirror, true, true, root);
            panel.AddComponent<LaserMirror>();
            Box(new Vector3(0f, 1.62f, -0.03f), new Vector3(1.6f, 0.04f, 0.12f), MatLib.AccentBlue, false, false, root);
        }

        // ---- moving / zone elements ------------------------------------------------------------

        /// <summary>Conveyor from <paramref name="a"/> to <paramref name="b"/> (floor points, or ceiling points when <paramref name="ceiling"/>).</summary>
        public ConveyorBelt Conveyor(Vector3 a, Vector3 b, float width, float speed, bool ceiling, params SignalSource[] inputs)
        {
            Vector3 up = ceiling ? Vector3.down : Vector3.up;
            Vector3 dir = (b - a).normalized;
            float length = Vector3.Distance(a, b);
            Transform root = Group("Conveyor", (a + b) * 0.5f + up * 0.3f, Quaternion.LookRotation(dir, up));

            Box(new Vector3(0f, -0.15f, 0f), new Vector3(width + 0.24f, 0.3f, length + 0.2f), MatLib.Metal, true, true, root);
            GameObject belt = Prim(PrimitiveType.Quad, root, new Vector3(0f, 0.004f, 0f), new Vector3(width, length, 1f), Quaternion.Euler(90f, 0f, 0f), MatLib.Conveyor, false, false);
            belt.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(new Vector3(width * 0.5f + 0.07f, 0.01f, 0f), new Vector3(0.05f, 0.04f, length), MatLib.AccentBlue, false, false, root);
            Box(new Vector3(-width * 0.5f - 0.07f, 0.01f, 0f), new Vector3(0.05f, 0.04f, length), MatLib.AccentBlue, false, false, root);

            ConveyorBelt conveyor = root.gameObject.AddComponent<ConveyorBelt>();
            conveyor.Init(speed, length, width, belt.GetComponent<Renderer>());
            conveyor.Inputs = inputs;
            if (!ceiling) WireAll(inputs, a);
            return conveyor;
        }

        /// <summary>Floor fan with an updraft column <paramref name="height"/> metres tall.</summary>
        public Fan Fan(Vector3 floorPoint, float height, params SignalSource[] inputs)
        {
            Transform root = Group("Fan", floorPoint, Quaternion.identity);
            const float s = 2.9f;
            Box(new Vector3(0f, 0.15f, s * 0.5f - 0.1f), new Vector3(s, 0.3f, 0.2f), MatLib.Metal, true, true, root);
            Box(new Vector3(0f, 0.15f, -s * 0.5f + 0.1f), new Vector3(s, 0.3f, 0.2f), MatLib.Metal, true, true, root);
            Box(new Vector3(s * 0.5f - 0.1f, 0.15f, 0f), new Vector3(0.2f, 0.3f, s - 0.4f), MatLib.Metal, true, true, root);
            Box(new Vector3(-s * 0.5f + 0.1f, 0.15f, 0f), new Vector3(0.2f, 0.3f, s - 0.4f), MatLib.Metal, true, true, root);
            BoxCollider grate = root.gameObject.AddComponent<BoxCollider>();
            grate.center = new Vector3(0f, 0.26f, 0f);
            grate.size = new Vector3(s - 0.4f, 0.08f, s - 0.4f);
            for (int i = -3; i <= 3; i++) Box(new Vector3(i * 0.36f, 0.27f, 0f), new Vector3(0.05f, 0.05f, s - 0.4f), MatLib.MetalLight, false, true, root);
            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.04f, 0f), new Vector3(2.3f, 0.02f, 2.3f), Quaternion.identity, MatLib.AccentBlue, false, false);

            Transform rotor = new GameObject("Rotor").transform;
            rotor.SetParent(root, false);
            rotor.localPosition = new Vector3(0f, 0.15f, 0f);
            for (int i = 0; i < 3; i++)
            {
                Quaternion r = Quaternion.Euler(0f, i * 120f, 0f);
                Prim(PrimitiveType.Cube, rotor, r * new Vector3(0f, 0f, 0.55f), new Vector3(0.3f, 0.03f, 1.05f), r * Quaternion.Euler(0f, 0f, 20f), MatLib.Plastic, false, false);
            }

            GameObject psGo = new GameObject("Updraft");
            psGo.transform.SetParent(root, false);
            psGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            psGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startSpeed = 6.5f;
            main.startLifetime = height / 6.5f;
            main.startSize = 0.07f;
            main.startColor = new Color(0.55f, 0.85f, 1f, 0.55f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.2f, 2.2f, 0.05f);
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            psGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = MatLib.Particle;
            ps.Play();

            Fan fan = root.gameObject.AddComponent<Fan>();
            fan.Init(height, rotor, ps);
            fan.Inputs = inputs;
            WireAll(inputs, floorPoint);
            return fan;
        }

        /// <summary>Moving platform between two top-centre points.</summary>
        public MovingPlatform Platform(Vector3 a, Vector3 b, Vector3 size, float speed, params SignalSource[] inputs)
        {
            Vector3 center = a - Vector3.up * (size.y * 0.5f);
            GameObject go = Prim(PrimitiveType.Cube, null, center, size, Quaternion.identity, MatLib.Plastic);
            go.name = "MovingPlatform";
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            Renderer[] strips = new Renderer[4];
            Vector3 inv = new Vector3(1f / size.x, 1f / size.y, 1f / size.z);
            strips[0] = Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.5f, 0.5f - 0.06f * inv.z), new Vector3(1f, 0.02f * inv.y, 0.06f * inv.z), Quaternion.identity, MatLib.Signal, false, false).GetComponent<Renderer>();
            strips[1] = Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.5f, -0.5f + 0.06f * inv.z), new Vector3(1f, 0.02f * inv.y, 0.06f * inv.z), Quaternion.identity, MatLib.Signal, false, false).GetComponent<Renderer>();
            strips[2] = Prim(PrimitiveType.Cube, go.transform, new Vector3(0.5f - 0.06f * inv.x, 0.5f, 0f), new Vector3(0.06f * inv.x, 0.02f * inv.y, 1f), Quaternion.identity, MatLib.Signal, false, false).GetComponent<Renderer>();
            strips[3] = Prim(PrimitiveType.Cube, go.transform, new Vector3(-0.5f + 0.06f * inv.x, 0.5f, 0f), new Vector3(0.06f * inv.x, 0.02f * inv.y, 1f), Quaternion.identity, MatLib.Signal, false, false).GetComponent<Renderer>();
            Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, -0.9f, 0f), new Vector3(0.6f, 0.8f, 0.6f), Quaternion.identity, MatLib.Metal, false);

            MovingPlatform platform = go.AddComponent<MovingPlatform>();
            platform.Init(center, b - Vector3.up * (size.y * 0.5f), speed, 1.2f, strips);
            platform.Inputs = inputs;
            WireAll(inputs, new Vector3(a.x, 0f, a.z));
            return platform;
        }

        /// <summary>Energy field curtain standing on <paramref name="bottomCenter"/>. With <paramref name="invert"/> it switches OFF while powered.</summary>
        public EnergyField Field(Vector3 bottomCenter, bool alongX, float width, float height, bool invert, params SignalSource[] inputs)
        {
            Transform root = Group("EnergyField", bottomCenter, alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f));
            for (int i = -1; i <= 1; i += 2)
            {
                Box(new Vector3(i * (width * 0.5f + 0.08f), height * 0.5f, 0f), new Vector3(0.16f, height, 0.32f), MatLib.Metal, true, true, root);
                Box(new Vector3(i * (width * 0.5f + 0.005f), height * 0.5f, 0f), new Vector3(0.02f, height - 0.1f, 0.12f), MatLib.AccentBlue, false, false, root);
            }
            Renderer curtain = Box(new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, 0.02f), MatLib.Field, false, false, root).GetComponent<Renderer>();
            curtain.receiveShadows = false;

            EnergyField field = root.gameObject.AddComponent<EnergyField>();
            field.Init(new Vector3(width * 0.5f, height * 0.5f, 0.25f), curtain, invert);
            field.Inputs = inputs;
            WireAll(inputs, bottomCenter);
            return field;
        }

        /// <summary>Narrator line played when the player first enters the box.</summary>
        public void Say(Vector3 center, Vector3 size, string line)
        {
            Transform t = Group("NarratorZone", center, Quaternion.identity);
            t.gameObject.AddComponent<NarratorZone>().Init(size * 0.5f, line, _narrate);
        }

        // ---- wires -----------------------------------------------------------------------------

        void WireAll(SignalSource[] inputs, Vector3 target)
        {
            if (!AutoWires || inputs == null) return;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null) Wire(inputs[i], target);
            }
        }

        void Wire(SignalSource source, Vector3 target)
        {
            float y = target.y + 0.006f;
            Vector3 a = source.WireAnchor;
            Vector3 p0 = new Vector3(a.x, y, a.z);
            Vector3 p1 = new Vector3(target.x, y, a.z);
            Vector3 p2 = new Vector3(target.x, y, target.z);

            Transform group = Group("Wire", Vector3.zero, Quaternion.identity);
            List<Renderer> segments = new List<Renderer>();
            WireSegment(group, p0, p1, segments);
            WireSegment(group, p1, p2, segments);
            group.gameObject.AddComponent<SignalWire>().Init(source, segments.ToArray());
        }

        void WireSegment(Transform group, Vector3 a, Vector3 b, List<Renderer> into)
        {
            float length = Vector3.Distance(a, b);
            if (length < 0.05f) return;
            bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
            Vector3 size = alongX ? new Vector3(length + 0.08f, 0.012f, 0.08f) : new Vector3(0.08f, 0.012f, length + 0.08f);
            into.Add(Box((a + b) * 0.5f, size, MatLib.Signal, false, false, group).GetComponent<Renderer>());
        }
    }
}
