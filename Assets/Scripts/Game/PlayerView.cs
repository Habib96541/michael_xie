using System.Collections.Generic;
using NeonDash.Core;
using UnityEngine;

namespace NeonDash
{
    /// <summary>Draws the cube and ship from the simulation pose.</summary>
    public sealed class PlayerView : MonoBehaviour
    {
        const float CubeRadius = 0.46f;
        static readonly Color Ink = new Color(0.018f, 0.025f, 0.07f);
        GameObject cube;
        GameObject ship;
        float trailTimer;

        void Awake()
        {
            cube = BuildCube();
            ship = BuildShip();
            ResetForRun();
        }

        GameObject BuildCube()
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            void Facet(Color color, params Vector2[] outline)
            {
                Vector2 start = outline[0];
                for (int i = 1; i < outline.Length - 1; i++)
                {
                    Vector2 edge = outline[i];
                    Vector2 next = outline[i + 1];
                    MeshTools.AddTriangle(verts, colors, tris, start.x, start.y, edge.x, edge.y, next.x, next.y, color);
                }
            }

            void Chassis(float r, float c, Color color)
            {
                Facet(color, new Vector2(-r + c, -r), new Vector2(r - c, -r),
                    new Vector2(r, -r + c), new Vector2(r, r - c), new Vector2(r - c, r),
                    new Vector2(-r + c, r), new Vector2(-r, r - c), new Vector2(-r, -r + c));
            }

            // An angled power cell echoes the NeonDash wordmark. The square base still reads at speed.
            Chassis(CubeRadius, 0.12f, Ink);
            Chassis(0.413f, 0.102f, new Color(0.29f, 0.14f, 0.57f));
            MeshTools.AddLine(verts, colors, tris, -0.37f, -0.26f, -0.37f, 0.25f, 0.046f, Palette.Cyan);
            MeshTools.AddLine(verts, colors, tris, -0.37f, 0.25f, -0.25f, 0.37f, 0.046f, Palette.Cyan);
            MeshTools.AddLine(verts, colors, tris, -0.25f, 0.37f, 0.24f, 0.37f, 0.046f, Palette.Cyan);
            MeshTools.AddLine(verts, colors, tris, 0.37f, 0.24f, 0.37f, -0.25f, 0.046f, Palette.Pink);
            MeshTools.AddLine(verts, colors, tris, 0.37f, -0.25f, 0.25f, -0.37f, 0.046f, Palette.Pink);
            MeshTools.AddLine(verts, colors, tris, 0.25f, -0.37f, -0.24f, -0.37f, 0.046f, Palette.Pink);
            Facet(Ink, new Vector2(-0.30f, -0.035f), new Vector2(0.065f, 0.31f),
                new Vector2(0.31f, 0.035f), new Vector2(-0.065f, -0.31f));
            Vector2 joint = new Vector2(0.03f, 0.00f);
            Vector2 tip = new Vector2(0.067f, 0.24f);
            Vector2 left = new Vector2(-0.23f, -0.045f);
            Vector2 right = new Vector2(0.24f, 0.035f);
            Vector2 baseTip = new Vector2(-0.068f, -0.24f);
            Facet(new Color(0.82f, 1f, 1f), tip, joint, left);
            Facet(Palette.Cyan, tip, right, joint);
            Facet(new Color(0.58f, 0.25f, 0.97f), joint, right, baseTip);
            Facet(Palette.Pink, joint, baseTip, left);
            MeshTools.AddQuad(verts, colors, tris, -0.29f, 0.11f, -0.235f, 0.165f, Color.white);
            MeshTools.AddQuad(verts, colors, tris, 0.235f, -0.165f, 0.29f, -0.11f, Palette.Pink);
            return MakeMesh("Cube", verts, colors, tris);
        }

        GameObject BuildShip()
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            MeshTools.AddTriangle(verts, colors, tris, -0.60f, -0.26f, 0.65f, -0.10f, -0.47f, 0.27f, Ink);
            MeshTools.AddTriangle(verts, colors, tris, -0.54f, -0.21f, 0.54f, -0.10f, -0.43f, 0.21f, Palette.Cyan);
            MeshTools.AddTriangle(verts, colors, tris, -0.50f, -0.19f, 0.54f, -0.10f, -0.23f, -0.34f, Ink);
            MeshTools.AddTriangle(verts, colors, tris, -0.42f, -0.18f, 0.42f, -0.12f, -0.23f, -0.28f, new Color(0.10f, 0.45f, 0.93f));
            MeshTools.AddQuad(verts, colors, tris, -0.28f, 0.04f, 0.14f, 0.41f, Ink);
            MeshTools.AddQuad(verts, colors, tris, -0.23f, 0.09f, 0.09f, 0.36f, Palette.Violet);
            MeshTools.AddQuad(verts, colors, tris, -0.16f, 0.17f, 0.02f, 0.29f, Ink);
            return MakeMesh("Ship", verts, colors, tris);
        }

        GameObject MakeMesh(string name, List<Vector3> verts, List<Color> colors, List<int> tris)
        {
            GameObject icon = MeshTools.CreateRenderer(name, transform, 1);
            icon.transform.localPosition = Vector3.zero;
            icon.GetComponent<MeshFilter>().sharedMesh = MeshTools.Build(verts, colors, tris);
            return icon;
        }

        public static Vector2 VisualCenter(PlayerSim player)
        {
            float y = player.CenterY;
            if (player.Mode == PlayerMode.Cube)
                y += player.GravitySign * (CubeRadius - player.HalfHeight);
            return new Vector2(player.X, y);
        }

        public void ResetForRun()
        {
            trailTimer = 0f;
            transform.position = new Vector3(0f, CubeRadius, 1f);
            cube.transform.localRotation = Quaternion.identity;
            cube.SetActive(true);
            ship.SetActive(false);
        }

        public void Tick(PlayerSim player, float dt, Fx fx, bool alive)
        {
            Vector2 center = VisualCenter(player);
            transform.position = new Vector3(center.x, center.y, 1f);
            bool shipMode = player.Mode == PlayerMode.Ship;
            cube.SetActive(alive && !shipMode);
            ship.SetActive(alive && shipMode);
            if (!alive) return;

            if (shipMode)
            {
                float bank = Mathf.Clamp(Mathf.Atan2(player.VY, GameConstants.BlockSpeed) * Mathf.Rad2Deg, -36f, 36f);
                ship.transform.localRotation = Quaternion.Euler(0f, 0f, bank);
            }
            else
            {
                cube.transform.localRotation = Quaternion.Euler(0f, 0f, player.Rotation);
            }

            trailTimer -= dt;
            if (trailTimer <= 0f)
            {
                trailTimer = shipMode ? 0.018f : 0.031f;
                var back = center + new Vector2(-0.43f, shipMode ? -0.10f : -player.GravitySign * 0.29f);
                fx.Trail(back, shipMode ? Palette.Gold : Palette.Cyan, shipMode ? 0.18f : 0.12f);
            }
        }

        public void OnLanded(PlayerSim player, Fx fx)
        {
            float surface = player.GravitySign > 0 ? player.Y : player.Y + player.HalfHeight * 2f;
            fx.Emit(new Vector2(player.X, surface), Palette.Alpha(Palette.Cyan, 0.72f), 6, 2.4f, 0.1f, 0.22f);
        }
    }
}
