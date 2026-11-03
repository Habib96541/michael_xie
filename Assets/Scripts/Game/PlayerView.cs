using NeonDash.Core;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// The player cube / ship. Visuals only: every transform here is driven by the simulation,
    /// with a small amount of smoothing so landings look snapped and the ship banks into motion.
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        Transform root;
        GameObject cube;
        GameObject ship;
        MeshFilter cubeFilter;
        MeshFilter shipFilter;
        float visualRotation;
        float trailTimer;
        float landFlash;
        Material cubeMaterial;
        Material shipMaterial;

        public Vector3 Position { get { return root.position; } }

        void Awake()
        {
            root = transform;
            BuildCube();
            BuildShip();
        }

        void BuildCube()
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var colors = new System.Collections.Generic.List<Color>();
            var tris = new System.Collections.Generic.List<int>();

            MeshTools.AddQuad(verts, colors, tris, -0.46f, -0.46f, 0.46f, 0.46f, Palette.Lime);
            MeshTools.AddQuad(verts, colors, tris, -0.46f, 0.38f, 0.46f, 0.46f, Color.white);
            MeshTools.AddQuad(verts, colors, tris, -0.46f, -0.46f, 0.46f, -0.38f, Palette.Alpha(Palette.Lime, 0.55f));
            MeshTools.AddQuad(verts, colors, tris, -0.30f, -0.30f, 0.30f, 0.30f, new Color(0.05f, 0.16f, 0.08f, 1f));
            MeshTools.AddQuad(verts, colors, tris, -0.22f, -0.06f, -0.06f, 0.12f, Color.white);
            MeshTools.AddQuad(verts, colors, tris, 0.06f, -0.06f, 0.22f, 0.12f, Color.white);

            cube = MeshTools.CreateRenderer("Cube", root, 1);
            cubeFilter = cube.GetComponent<MeshFilter>();
            cubeFilter.mesh = MeshTools.Build(verts, colors, tris);
            cubeMaterial = cube.GetComponent<MeshRenderer>().material;
        }

        void BuildShip()
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var colors = new System.Collections.Generic.List<Color>();
            var tris = new System.Collections.Generic.List<int>();

            MeshTools.AddTriangle(verts, colors, tris, -0.52f, -0.12f, 0.52f, 0f, -0.52f, 0.12f, Palette.Cyan);
            MeshTools.AddTriangle(verts, colors, tris, -0.52f, -0.12f, 0.52f, 0f, -0.30f, -0.30f, Palette.Alpha(Palette.Cyan, 0.7f));
            MeshTools.AddQuad(verts, colors, tris, -0.16f, 0.02f, 0.20f, 0.26f, new Color(0.04f, 0.10f, 0.18f, 1f));
            MeshTools.AddCircle(verts, colors, tris, 0.02f, 0.14f, 0.07f, 12, Color.white);
            MeshTools.AddTriangle(verts, colors, tris, -0.52f, 0.12f, -0.32f, 0.12f, -0.42f, 0.40f, Palette.Magenta);

            ship = MeshTools.CreateRenderer("Ship", root, 1);
            shipFilter = ship.GetComponent<MeshFilter>();
            shipFilter.mesh = MeshTools.Build(verts, colors, tris);
            shipMaterial = ship.GetComponent<MeshRenderer>().material;
            ship.SetActive(false);
        }

        public void ResetForRun(Level level)
        {
            visualRotation = 0f;
            trailTimer = 0f;
            landFlash = 0f;
            root.position = new Vector3(0f, 0f, 1f);
            cube.SetActive(true);
            ship.SetActive(false);
            SetAlpha(1f);
        }

        public void SetAlpha(float alpha)
        {
            if (cubeMaterial != null) cubeMaterial.color = new Color(1f, 1f, 1f, alpha);
            if (shipMaterial != null) shipMaterial.color = new Color(1f, 1f, 1f, alpha);
        }

        public void Tick(PlayerSim player, float dt, Fx fx, bool alive)
        {
            if (player == null) return;

            landFlash = Mathf.Max(0f, landFlash - dt * 4f);
            float squash = 1f + landFlash * 0.22f;

            root.position = new Vector3(player.X, player.Y + player.HalfHeight, 1f);
            bool shipMode = player.Mode == PlayerMode.Ship;
            cube.SetActive(!shipMode);
            ship.SetActive(shipMode);

            if (!shipMode)
            {
                float target = player.Grounded ? Mathf.Round(player.Rotation / 90f) * 90f : player.Rotation;
                visualRotation = Mathf.Lerp(visualRotation, target, 1f - Mathf.Exp(-26f * dt));
                cube.transform.localRotation = Quaternion.Euler(0f, 0f, visualRotation);
                cube.transform.localScale = new Vector3(1f / squash, squash, 1f);
            }
            else
            {
                float bank = Mathf.Clamp(-player.VY * 2.4f, -30f, 30f);
                ship.transform.localRotation = Quaternion.Euler(0f, 0f, bank);
                ship.transform.localScale = Vector3.one;
            }

            if (!alive) return;

            // Trail: dust behind the cube, exhaust behind the ship.
            trailTimer -= dt;
            if (trailTimer <= 0f)
            {
                trailTimer = shipMode ? 0.015f : 0.035f;
                Vector2 back = new Vector2(player.X - 0.35f, player.Y + player.HalfHeight);
                fx.Trail(back, shipMode ? Palette.Magenta : Palette.Alpha(Palette.Lime, 0.7f), shipMode ? 0.22f : 0.14f);
            }
        }

        /// <summary>Called when the cube lands so the view can squash and puff dust.</summary>
        public void OnLanded(PlayerSim player, Fx fx)
        {
            landFlash = 1f;
            fx.Emit(new Vector2(player.X, player.Y), Palette.Alpha(Palette.Cyan, 0.55f), 5, 2.4f, 0.12f, 0.3f);
        }
    }
}
