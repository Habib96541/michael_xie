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

        /// <summary>Ship and inverted sections need a visible upper boundary.</summary>
        public void SetCeilingVisible(bool visible)
        {
            if (ceilingObject != null) ceilingObject.SetActive(visible);
        }

        static void AddWall(List<Vector3> verts, List<Color> colors, List<int> tris,
                            float x0, float x1, float surface, float depth, bool floor)
        {
            float dir = floor ? -1f : 1f;
            float far = surface + dir * depth;
            Color deep = new Color(0.011f, 0.044f, 0.17f);
            Color near = new Color(0.045f, 0.29f, 0.72f);
            if (floor)
                MeshTools.AddQuadGradient(verts, colors, tris, x0, far, x1, surface, deep, near);
            else
                MeshTools.AddQuadGradient(verts, colors, tris, x0, surface, x1, far, near, deep);

            float a = Mathf.Min(surface + dir * 0.20f, far);
            float b = Mathf.Max(surface + dir * 0.20f, far);
            for (float x = x0; x < x1 - 0.25f; x += 4f)
            {
                float end = Mathf.Min(x + 3.86f, x1);
                MeshTools.AddQuad(verts, colors, tris, x + 0.09f, a, end, b, new Color(0.014f, 0.08f, 0.29f, 0.58f));
                MeshTools.AddQuadGradient(verts, colors, tris, x + 0.15f, a, end - 0.04f, b - 0.06f,
                    new Color(0.05f, 0.21f, 0.64f, 0.12f), new Color(0.16f, 0.46f, 0.95f, 0.20f));
            }

            float lip = surface + dir * 0.045f;
            MeshTools.AddQuad(verts, colors, tris, x0, Mathf.Min(surface, lip), x1, Mathf.Max(surface, lip),
                new Color(0.77f, 0.96f, 1f));
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
            MeshTools.AddQuad(verts, colors, tris, x, y, x + 1f, y + 1f, new Color(0.59f, 0.82f, 1f));
            MeshTools.AddQuad(verts, colors, tris, x + 0.05f, y + 0.05f, x + 0.95f, y + 0.95f,
                new Color(0.015f, 0.045f, 0.13f));
            MeshTools.AddQuadGradient(verts, colors, tris, x + 0.14f, y + 0.14f, x + 0.86f, y + 0.86f,
                new Color(0.035f, 0.15f, 0.38f), new Color(0.09f, 0.39f, 0.78f));
            MeshTools.AddQuad(verts, colors, tris, x + 0.17f, y + 0.81f, x + 0.83f, y + 0.86f,
                new Color(0.35f, 0.63f, 0.94f));
            MeshTools.AddQuad(verts, colors, tris, x + 0.33f, y + 0.33f, x + 0.67f, y + 0.67f,
                new Color(0.025f, 0.10f, 0.28f));
        }

        static void AddSpike(List<Vector3> verts, List<Color> colors, List<int> tris, float x, float y, bool ceiling)
        {
            float baseY = y + (ceiling ? 1f : 0f);
            float tipY = y + (ceiling ? 0.05f : 0.95f);
            Color edge = new Color(0.84f, 0.96f, 1f);
            Color glow = new Color(0.28f, 0.69f, 1f, 0.52f);

            MeshTools.AddTriangle(verts, colors, tris, x + 0.03f, baseY, x + 0.97f, baseY, x + 0.5f, tipY,
                new Color(0.009f, 0.023f, 0.07f));
            MeshTools.AddLine(verts, colors, tris, x + 0.04f, baseY, x + 0.5f, tipY, 0.13f, glow);
            MeshTools.AddLine(verts, colors, tris, x + 0.96f, baseY, x + 0.5f, tipY, 0.13f, glow);
            MeshTools.AddLine(verts, colors, tris, x + 0.04f, baseY, x + 0.5f, tipY, 0.047f, edge);
            MeshTools.AddLine(verts, colors, tris, x + 0.96f, baseY, x + 0.5f, tipY, 0.047f, edge);
            MeshTools.AddLine(verts, colors, tris, x + 0.04f, baseY, x + 0.96f, baseY, 0.045f, edge);
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

                float height = level.Ceiling;
                float centerY = portal.Kind == PortalKind.GravityNormal ? height - 1.7f
                    : portal.Kind == PortalKind.CubePortal ? height * 0.5f : 1.7f;

                MeshTools.AddCircle(verts, colors, tris, 0f, 0f, 0.63f, 32, Palette.Alpha(color, 0.20f));
                MeshTools.AddRing(verts, colors, tris, 0f, 0f, 0.47f, 0.64f, 32, new Color(0.025f, 0.035f, 0.10f));
                MeshTools.AddRing(verts, colors, tris, 0f, 0f, 0.50f, 0.59f, 32, color);
                MeshTools.AddRing(verts, colors, tris, 0f, 0f, 0.51f, 0.535f, 32, Color.Lerp(color, Color.white, 0.65f));
                for (int k = 0; k < verts.Count; k++)
                {
                    Vector3 v = verts[k];
                    v.y = v.y * 2.40f + centerY;
                    verts[k] = v;
                }

                // The faint rail indicates that the trigger spans every flight lane.
                MeshTools.AddQuad(verts, colors, tris, -0.05f, 0f, 0.05f, height, Palette.Alpha(color, 0.23f));
                for (float y = 0.35f; y < height; y += 0.52f)
                    MeshTools.AddQuad(verts, colors, tris, -0.10f, y, 0.10f, y + 0.09f, Palette.Alpha(color, 0.45f));

                GameObject go = MeshTools.CreateRenderer("Portal", root, 3);
                go.transform.position = new Vector3(portal.X + 0.5f, portal.Y, 3f);
                go.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);

                Spinner spinner = new Spinner();
                spinner.Transform = go.transform;
                spinner.Pulse = 0.018f;
                spinner.Phase = portal.X * 0.5f;
                spinners.Add(spinner);
            }
        }

        /// <summary>Multiplier coins with a colored rim and a diamond inset.</summary>
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

                MeshTools.AddCircle(verts, colors, tris, 0f, 0f, 0.31f * scale, 24, new Color(0.03f, 0.06f, 0.14f));
                MeshTools.AddRing(verts, colors, tris, 0f, 0f, 0.235f * scale, 0.30f * scale, 24, color);
                MeshTools.AddRing(verts, colors, tris, 0f, 0f, 0.278f * scale, 0.29f * scale, 24,
                    Color.Lerp(color, Color.white, 0.58f));

                float h = 0.16f * scale;
                float w = 0.12f * scale;
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
