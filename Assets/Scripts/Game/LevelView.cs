using System.Collections.Generic;
using NeonDash.Core;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// Builds and animates all level geometry: the neon track, blocks, spikes, pads, orbs,
    /// portals and the finish gate. Everything is generated from meshes at runtime.
    /// </summary>
    public sealed class LevelView : MonoBehaviour
    {
        sealed class Spinner
        {
            public Transform Transform;
            public float Speed;
            public float Pulse;
            public float Phase;
            public Vector3 BaseScale = Vector3.one;
        }

        Transform root;
        MeshFilter trackFilter;
        MeshFilter ceilingFilter;
        GameObject ceilingObject;
        MeshFilter tileFilter;
        MeshFilter decorFilter;
        sealed class PickupPop
        {
            public Transform Transform;
            public float Start;
        }

        readonly List<Spinner> spinners = new List<Spinner>();
        readonly List<GameObject> gemObjects = new List<GameObject>();
        readonly List<int> gemSpinnerIds = new List<int>();
        readonly List<bool> gemTaken = new List<bool>();

        readonly List<GameObject> orbObjects = new List<GameObject>();
        readonly List<int> orbSpinnerIds = new List<int>();
        readonly List<bool> orbTaken = new List<bool>();

        readonly List<GameObject> padObjects = new List<GameObject>();
        readonly List<int> padSpinnerIds = new List<int>();
        readonly List<bool> padTaken = new List<bool>();

        readonly List<PickupPop> pops = new List<PickupPop>();
        GameObject finishGate;
        float time;

        const float PickupPopSeconds = 0.3f;

        public Level CurrentLevel { get; private set; }

        void Awake()
        {
            root = transform;
        }

        public void Build(Level level)
        {
            CurrentLevel = level;
            Clear();
            if (level == null) return;
            BuildTrack(level);
            BuildTiles(level);
            BuildPads(level);
            BuildOrbs(level);
            BuildPortals(level);
            BuildGems(level);
            BuildFinish(level);
        }

        void Clear()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
            spinners.Clear();
            gemObjects.Clear();
            gemSpinnerIds.Clear();
            gemTaken.Clear();
            orbObjects.Clear();
            orbSpinnerIds.Clear();
            orbTaken.Clear();
            padObjects.Clear();
            padSpinnerIds.Clear();
            padTaken.Clear();
            pops.Clear();
            trackFilter = null;
            ceilingFilter = null;
            ceilingObject = null;
            tileFilter = null;
            decorFilter = null;
            finishGate = null;
        }

        void Update()
        {
            time += Time.deltaTime;
            for (int i = 0; i < spinners.Count; i++)
            {
                Spinner s = spinners[i];
                if (s.Transform == null) continue;
                float pulse = 1f + s.Pulse * Mathf.Sin(time * 3.4f + s.Phase);
                s.Transform.localScale = s.BaseScale * pulse;
                if (s.Speed != 0f) s.Transform.Rotate(0f, 0f, s.Speed * Time.deltaTime);
            }

            TickPops();
        }

        /// <summary>
        /// The pickup effect, shared by gems, orbs and pads: the item flares up, spins fast and
        /// shrinks to nothing before it is switched off, so it always visibly dies instead of
        /// blinking away.
        /// </summary>
        void TickPops()
        {
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                PickupPop pop = pops[i];
                if (pop.Transform == null)
                {
                    pops.RemoveAt(i);
                    continue;
                }

                float p = (time - pop.Start) / PickupPopSeconds;
                if (p >= 1f)
                {
                    pop.Transform.gameObject.SetActive(false);
                    pops.RemoveAt(i);
                    continue;
                }

                float grow = Mathf.Lerp(1f, 1.75f, Mathf.Clamp01(p * 1.8f));
                float shrink = 1f - Mathf.SmoothStep(0.4f, 1f, p);
                pop.Transform.localScale = Vector3.one * (grow * shrink);
                pop.Transform.Rotate(0f, 0f, 640f * Time.deltaTime);
            }
        }

        // ------------------------------------------------------------------
        void BuildTrack(Level level)
        {
            float start = -14f;
            float end = level.Length + 26f;

            // Solid blocky floor under the cube.
            var floorVerts = new List<Vector3>();
            var floorColors = new List<Color>();
            var floorTris = new List<int>();
            AddWall(floorVerts, floorColors, floorTris, start, end, 0f, 4.6f, true);
            MeshRenderer floorRenderer = MeshTools.CreateMeshObject("Floor", root, 6, out trackFilter);
            trackFilter.mesh = MeshTools.Build(floorVerts, floorColors, floorTris);
            floorRenderer.name = "Floor";

            // Matching wall above the ceiling, so flipped gravity has a real surface to run on
            // instead of empty air. Kept as its own mesh so the title scene can hide it.
            var ceilingVerts = new List<Vector3>();
            var ceilingColors = new List<Color>();
            var ceilingTris = new List<int>();
            AddWall(ceilingVerts, ceilingColors, ceilingTris, start, end, level.Ceiling, 4.6f, false);
            MeshRenderer ceilingMesh = MeshTools.CreateMeshObject("Ceiling", root, 6, out ceilingFilter);
            ceilingFilter.mesh = MeshTools.Build(ceilingVerts, ceilingColors, ceilingTris);
            ceilingMesh.name = "Ceiling";
            ceilingObject = ceilingMesh.gameObject;
        }

        /// <summary>The lobby and record screens show the level open-topped; runs close it in.</summary>
        public void SetCeilingVisible(bool visible)
        {
            if (ceilingObject != null) ceilingObject.SetActive(visible);
        }

        /// <summary>
        /// Builds one masonry wall: a deep fill, three rows of offset bricks with a bevelled lip
        /// facing the playfield, and a bright neon line marking the walkable surface.
        /// </summary>
        static void AddWall(List<Vector3> verts, List<Color> colors, List<int> tris,
                            float x0, float x1, float surface, float depth, bool floor)
        {
            float dir = floor ? -1f : 1f;
            float far = surface + dir * depth;
            Color voidColor = new Color(0.008f, 0.012f, 0.030f, 1f);

            if (floor)
                MeshTools.AddQuadGradient(verts, colors, tris, x0, far, x1, surface, voidColor, Palette.Track);
            else
                MeshTools.AddQuadGradient(verts, colors, tris, x0, surface, x1, far, Palette.Track, voidColor);

            const float block = 1f;
            const float inset = 0.055f;

            for (int row = 0; row < 3; row++)
            {
                float low = surface + dir * row * block;
                float high = surface + dir * (row + 1) * block;
                float a = Mathf.Min(low, high);
                float b = Mathf.Max(low, high);

                float fade = 1f - row * 0.30f;
                var face = Color.Lerp(Palette.Track, new Color(0.13f, 0.17f, 0.30f, 1f), fade * 0.9f);
                face.a = 0.96f;
                Color bevel = Palette.Alpha(Palette.BlockEdge, 0.34f * fade);

                float offset = row % 2 == 0 ? 0f : block * 0.5f;
                for (float x = x0 - block; x < x1 + block; x += block)
                {
                    float bx0 = Mathf.Max(x + offset + inset, x0);
                    float bx1 = Mathf.Min(x + offset + block - inset, x1);
                    if (bx1 - bx0 < 0.05f) continue;

                    MeshTools.AddQuad(verts, colors, tris, bx0, a + inset, bx1, b - inset, face);

                    if (floor)
                        MeshTools.AddQuad(verts, colors, tris, bx0, b - inset - 0.10f, bx1, b - inset, bevel);
                    else
                        MeshTools.AddQuad(verts, colors, tris, bx0, a + inset, bx1, a + inset + 0.10f, bevel);
                }
            }

            // Bright play surface plus a soft bloom spilling into the playfield.
            if (floor)
            {
                MeshTools.AddQuad(verts, colors, tris, x0, surface - 0.12f, x1, surface, Palette.TrackLine);
                MeshTools.AddQuad(verts, colors, tris, x0, surface - 0.62f, x1, surface - 0.12f,
                    Palette.Alpha(Palette.Cyan, 0.09f));
            }
            else
            {
                MeshTools.AddQuad(verts, colors, tris, x0, surface, x1, surface + 0.12f, Palette.TrackLine);
                MeshTools.AddQuad(verts, colors, tris, x0, surface + 0.12f, x1, surface + 0.62f,
                    Palette.Alpha(Palette.Cyan, 0.09f));
            }
        }

        void BuildTiles(Level level)
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            for (int i = 0; i < level.Tiles.Count; i++)
            {
                Tile tile = level.Tiles[i];
                if (tile.Kind == TileKind.Block) AddBlock(verts, colors, tris, tile.X, tile.Y);
                else if (tile.Kind == TileKind.Spike) AddSpike(verts, colors, tris, tile.X, tile.Y, false);
                else AddSpike(verts, colors, tris, tile.X, tile.Y, true);
            }

            MeshRenderer renderer = MeshTools.CreateMeshObject("Tiles", root, 4, out tileFilter);
            tileFilter.mesh = MeshTools.Build(verts, colors, tris);
            renderer.name = "Tiles";
        }

        static void AddBlock(List<Vector3> verts, List<Color> colors, List<int> tris, float x, float y)
        {
            MeshTools.AddQuad(verts, colors, tris, x, y, x + 1f, y + 1f, Palette.Block);
            MeshTools.AddQuad(verts, colors, tris, x, y + 0.94f, x + 1f, y + 1f, Palette.BlockEdge);
            MeshTools.AddQuad(verts, colors, tris, x, y, x + 0.06f, y + 1f, Palette.Alpha(Palette.BlockEdge, 0.45f));
            MeshTools.AddQuad(verts, colors, tris, x + 0.94f, y, x + 1f, y + 1f, Palette.Alpha(Palette.BlockEdge, 0.45f));
            MeshTools.AddQuad(verts, colors, tris, x + 0.22f, y + 0.22f, x + 0.78f, y + 0.28f,
                Palette.Alpha(Palette.Cyan, 0.20f));
        }

        static void AddSpike(List<Vector3> verts, List<Color> colors, List<int> tris, float x, float y, bool ceiling)
        {
            if (!ceiling)
            {
                MeshTools.AddTriangle(verts, colors, tris, x, y, x + 1f, y, x + 0.5f, y + 1f, Palette.Spike);
                MeshTools.AddTriangle(verts, colors, tris, x + 0.5f, y + 0.15f, x + 0.28f, y + 0.02f, x + 0.72f, y + 0.02f,
                    Palette.Alpha(Palette.SpikeEdge, 0.22f));
                MeshTools.AddLine(verts, colors, tris, x + 0.06f, y + 0.02f, x + 0.5f, y + 0.96f, 0.07f, Palette.SpikeEdge);
                MeshTools.AddLine(verts, colors, tris, x + 0.94f, y + 0.02f, x + 0.5f, y + 0.96f, 0.07f, Palette.SpikeEdge);
            }
            else
            {
                MeshTools.AddTriangle(verts, colors, tris, x, y + 1f, x + 1f, y + 1f, x + 0.5f, y, Palette.Spike);
                MeshTools.AddLine(verts, colors, tris, x + 0.06f, y + 0.98f, x + 0.5f, y + 0.04f, 0.07f, Palette.SpikeEdge);
                MeshTools.AddLine(verts, colors, tris, x + 0.94f, y + 0.98f, x + 0.5f, y + 0.04f, 0.07f, Palette.SpikeEdge);
            }
        }

        void BuildPads(Level level)
        {
            for (int i = 0; i < level.Pads.Count; i++)
            {
                Pad pad = level.Pads[i];
                var verts = new List<Vector3>();
                var colors = new List<Color>();
                var tris = new List<int>();
                Color color = pad.Kind == PadKind.Yellow ? Palette.Gold : Palette.Pink;

                MeshTools.AddQuad(verts, colors, tris, -0.55f, 0f, 0.55f, 0.14f, color);
                MeshTools.AddQuad(verts, colors, tris, -0.55f, 0.14f, 0.55f, 0.2f, Palette.Alpha(color, 0.32f));
                MeshTools.AddTriangle(verts, colors, tris, -0.3f, 0.24f, 0.3f, 0.24f, 0f, 0.52f, Palette.Alpha(color, 0.85f));
                MeshTools.AddTriangle(verts, colors, tris, -0.18f, 0.62f, 0.18f, 0.62f, 0f, 0.82f, Palette.Alpha(color, 0.55f));

                GameObject go = MeshTools.CreateRenderer("Pad", root, 3);
                go.transform.position = new Vector3(pad.X + 0.5f, pad.Y, 3f);
                go.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);

                Spinner spinner = new Spinner();
                spinner.Transform = go.transform;
                spinner.Pulse = 0.06f;
                spinner.Phase = pad.X;
                spinner.BaseScale = Vector3.one;
                spinners.Add(spinner);

                padObjects.Add(go);
                padSpinnerIds.Add(spinners.Count - 1);
                padTaken.Add(false);
            }
        }

        void BuildOrbs(Level level)
        {
            for (int i = 0; i < level.Orbs.Count; i++)
            {
                Orb orb = level.Orbs[i];
                Color color = orb.Kind == OrbKind.Yellow ? Palette.Gold
                    : orb.Kind == OrbKind.Pink ? Palette.Pink : Palette.Violet;

                var verts = new List<Vector3>();
                var colors = new List<Color>();
                var tris = new List<int>();
                MeshTools.AddCircle(verts, colors, tris, 0f, 0f, 0.34f, 24, Palette.Alpha(color, 0.9f));
                MeshTools.AddRing(verts, colors, tris, 0f, 0f, 0.44f, 0.56f, 28, Palette.Alpha(color, 0.55f));
                MeshTools.AddCircle(verts, colors, tris, 0f, 0f, 0.16f, 18, new Color(1f, 1f, 1f, 0.85f));

                GameObject go = MeshTools.CreateRenderer("Orb", root, 3);
                go.transform.position = new Vector3(orb.X, orb.Y + 0.5f, 3f);
                go.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);

                Spinner spinner = new Spinner();
                spinner.Transform = go.transform;
                spinner.Pulse = 0.13f;
                spinner.Speed = orb.Kind == OrbKind.Blue ? -55f : 25f;
                spinner.Phase = orb.X * 0.7f;
                spinners.Add(spinner);

                orbObjects.Add(go);
                orbSpinnerIds.Add(spinners.Count - 1);
                orbTaken.Add(false);
            }
        }

        void BuildPortals(Level level)
        {
            for (int i = 0; i < level.Portals.Count; i++)
            {
                Portal portal = level.Portals[i];
                Color color = ColorFor(portal.Kind);
                var verts = new List<Vector3>();
                var colors = new List<Color>();
                var tris = new List<int>();

                float w = GameConstantsBridge.PortalWidth;
                float h = GameConstantsBridge.PortalHeight;
                float halfW = w * 0.5f;
                float thickness = 0.14f;

                // Rectangular outline.
                MeshTools.AddQuad(verts, colors, tris, -halfW, -thickness * 0.5f, halfW, thickness * 0.5f, color);
                MeshTools.AddQuad(verts, colors, tris, -halfW, h - thickness * 0.5f, halfW, h + thickness * 0.5f, color);
                MeshTools.AddQuad(verts, colors, tris, -halfW - thickness * 0.5f, 0f, -halfW + thickness * 0.5f, h, color);
                MeshTools.AddQuad(verts, colors, tris, halfW - thickness * 0.5f, 0f, halfW + thickness * 0.5f, h, color);

                // Faint fill so the full-height gate reads as a field, not just an outline.
                MeshTools.AddQuad(verts, colors, tris, -halfW, 0f, halfW, h, Palette.Alpha(color, 0.09f));

                // Horizontal rungs all the way up the gate.
                for (float y = 0.35f; y < h - 0.2f; y += 0.62f)
                    MeshTools.AddQuad(verts, colors, tris, -halfW + 0.14f, y, halfW - 0.14f, y + 0.14f,
                        Palette.Alpha(color, 0.24f));

                // Direction chevron at the middle of the gate.
                bool upward = portal.Kind == PortalKind.GravityFlip;
                bool downward = portal.Kind == PortalKind.GravityNormal;
                float cy = h * 0.5f;
                if (upward || downward)
                {
                    float dir = upward ? 1f : -1f;
                    MeshTools.AddTriangle(verts, colors, tris, -0.34f, cy - dir * 0.26f, 0.34f, cy - dir * 0.26f,
                        0f, cy + dir * 0.58f, Palette.Alpha(color, 0.95f));
                }
                else
                {
                    // Ship / cube portals: a diamond instead of an arrow.
                    MeshTools.AddTriangle(verts, colors, tris, -0.3f, cy, 0f, cy + 0.5f, 0f, cy, Palette.Alpha(color, 0.85f));
                    MeshTools.AddTriangle(verts, colors, tris, 0f, cy + 0.5f, 0.3f, cy, 0f, cy, Palette.Alpha(color, 0.85f));
                    MeshTools.AddTriangle(verts, colors, tris, -0.3f, cy, 0f, cy, 0f, cy - 0.5f, Palette.Alpha(color, 0.85f));
                    MeshTools.AddTriangle(verts, colors, tris, 0f, cy, 0.3f, cy, 0f, cy - 0.5f, Palette.Alpha(color, 0.85f));
                }

                GameObject go = MeshTools.CreateRenderer("Portal", root, 3);
                go.transform.position = new Vector3(portal.X + 0.5f, portal.Y, 3f);
                go.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);

                Spinner spinner = new Spinner();
                spinner.Transform = go.transform;
                spinner.Pulse = 0.05f;
                spinner.Phase = portal.X * 0.5f;
                spinners.Add(spinner);
            }
        }

        /// <summary>Multiplier gems: spinning diamonds, brighter and larger when they sit high.</summary>
        void BuildGems(Level level)
        {
            for (int i = 0; i < level.Gems.Count; i++)
            {
                Gem gem = level.Gems[i];
                bool high = gem.Kind == GemKind.High;
                Color color = high ? Palette.Cyan : Palette.Gold;
                float scale = high ? 1.15f : 0.95f;

                var verts = new List<Vector3>();
                var colors = new List<Color>();
                var tris = new List<int>();

                MeshTools.AddRing(verts, colors, tris, 0f, 0f, 0.26f, 0.34f, 20, Palette.Alpha(color, 0.45f));

                float h = 0.22f * scale;
                float w = 0.15f * scale;
                MeshTools.AddTriangle(verts, colors, tris, 0f, h, w, 0f, 0f, 0f, color);
                MeshTools.AddTriangle(verts, colors, tris, w, 0f, 0f, -h, 0f, 0f, color);
                MeshTools.AddTriangle(verts, colors, tris, 0f, -h, -w, 0f, 0f, 0f, color);
                MeshTools.AddTriangle(verts, colors, tris, -w, 0f, 0f, h, 0f, 0f, color);
                MeshTools.AddCircle(verts, colors, tris, 0f, 0f, 0.06f, 10, Color.white);

                GameObject go = MeshTools.CreateRenderer("Gem", root, 2);
                go.transform.position = new Vector3(gem.X, gem.Y, 2f);
                go.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);
                gemObjects.Add(go);

                Spinner spinner = new Spinner();
                spinner.Transform = go.transform;
                spinner.Pulse = 0.10f;
                spinner.Speed = high ? 70f : -55f;
                spinner.Phase = gem.X * 0.9f;
                spinners.Add(spinner);
                gemSpinnerIds.Add(spinners.Count - 1);
                gemTaken.Add(false);
            }
        }

        /// <summary>
        /// Starts the pickup animation for one item. Safe to call twice: an item that is already
        /// being collected is ignored, so a fast run cannot restart the effect mid-shrink.
        /// </summary>
        void StartPop(List<GameObject> objects, List<int> spinnerIds, List<bool> taken, int index)
        {
            if (index < 0 || index >= objects.Count || taken[index]) return;
            GameObject item = objects[index];
            if (item == null) return;

            taken[index] = true;

            // Hand the transform over from the idle bobbing spinner to the pop animation.
            int spinnerId = spinnerIds[index];
            if (spinnerId >= 0 && spinnerId < spinners.Count) spinners[spinnerId].Transform = null;

            PickupPop pop = new PickupPop();
            pop.Transform = item.transform;
            pop.Start = time;
            pops.Add(pop);
        }

        /// <summary>A multiplier gem was collected.</summary>
        public void CollectGem(int index)
        {
            StartPop(gemObjects, gemSpinnerIds, gemTaken, index);
        }

        /// <summary>An orb was used; it is single use, so it pops and disappears.</summary>
        public void ConsumeOrb(int index)
        {
            StartPop(orbObjects, orbSpinnerIds, orbTaken, index);
        }

        /// <summary>A pad fired; it is single use too.</summary>
        public void ConsumePad(int index)
        {
            StartPop(padObjects, padSpinnerIds, padTaken, index);
        }

        /// <summary>Shows every gem, orb and pad again for a fresh attempt.</summary>
        public void ResetPickups()
        {
            pops.Clear();
            ResetPickupSet(gemObjects, gemSpinnerIds, gemTaken);
            ResetPickupSet(orbObjects, orbSpinnerIds, orbTaken);
            ResetPickupSet(padObjects, padSpinnerIds, padTaken);
        }

        void ResetPickupSet(List<GameObject> objects, List<int> spinnerIds, List<bool> taken)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] == null) continue;
                objects[i].SetActive(true);
                objects[i].transform.localScale = Vector3.one;
                taken[i] = false;

                int spinnerId = spinnerIds[i];
                if (spinnerId >= 0 && spinnerId < spinners.Count)
                    spinners[spinnerId].Transform = objects[i].transform;
            }
        }

        void BuildFinish(Level level)
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            float x = level.Length;
            float top = GameConstantsBridge.CeilingY;
            MeshTools.AddQuad(verts, colors, tris, x, 0f, x + 0.18f, top, Palette.Alpha(Palette.Gold, 0.9f));
            for (float y = 0f; y < top; y += 0.7f)
                MeshTools.AddQuad(verts, colors, tris, x - 0.5f, y, x + 0.7f, y + 0.18f, Palette.Alpha(Palette.Gold, 0.55f));

            finishGate = MeshTools.CreateRenderer("Finish", root, 3);
            finishGate.transform.position = new Vector3(0f, 0f, 3f);
            finishGate.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);
        }

        static Color ColorFor(PortalKind kind)
        {
            switch (kind)
            {
                case PortalKind.GravityFlip: return Palette.Violet;
                case PortalKind.GravityNormal: return Palette.Cyan;
                case PortalKind.ShipPortal: return Palette.Magenta;
                default: return Palette.Lime;
            }
        }
    }
}
