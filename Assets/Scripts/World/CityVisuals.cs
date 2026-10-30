using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Procedural placeholder city for the maze:
    /// walls become buildings with lit windows and neon roof edges,
    /// streets get glowing curb lines, and a skyline surrounds the maze.
    /// Real art (prefabs) can replace this later through LevelBuilder.wallPrefab.
    /// </summary>
    public static class CityVisuals
    {
        // Grid directions: N (row - 1 = far side), E, S, W
        static readonly Vector2Int[] Dirs =
            { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

        static readonly Color Pink = Visuals.Hex("#FF2BD6");
        static readonly Color Blue = Visuals.Hex("#3D7BFF");

        static Texture2D windowBase, windowGlow;
        static readonly Dictionary<int, Material> facadeCache = new Dictionary<int, Material>();
        static Material roofMat, floorMat;

        /// <summary>World direction of a grid direction (grid y grows toward the camera).</summary>
        static Vector3 ToWorld(Vector2Int d) => new Vector3(d.x, 0f, -d.y);

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static System.Random RngFor(Vector2Int cell) =>
            new System.Random((cell.x * 73856093) ^ (cell.y * 19349663) ^ 0x5A17);

        // ---------- Buildings ----------

        /// <summary>
        /// Builds one building on a wall cell (or a filler cell outside the maze).
        /// Buildings with a street on their far side stay low so the camera can always see that street.
        /// </summary>
        public static void BuildBuilding(LevelBuilder level, Vector2Int cell, Transform parent, bool filler)
        {
            var rng = RngFor(cell);
            float cs = level.cellSize;

            float h;
            if (filler)
                h = cell.y >= level.Height ? Range(rng, 0.7f, 1.6f) : Range(rng, 2.5f, 5.5f);
            else if (cell.y == 0)
                h = Range(rng, 2.8f, 4.5f);
            else if (level.IsWalkable(cell + Dirs[0]))
                h = Range(rng, 0.9f, 1.3f);
            else
                h = Range(rng, 1.5f, 2.8f);
            h = Mathf.Round(h * 4f) / 4f;

            float size = cs * 0.92f;
            var root = new GameObject($"Building_{cell.x}_{cell.y}").transform;
            root.SetParent(parent, false);
            root.position = level.CellToWorld(cell);

            Visuals.Shape(PrimitiveType.Cube, "Body", root, new Vector3(0, h / 2f, 0),
                new Vector3(size, h, size), FacadeMaterial(h, rng.Next(4)));
            Visuals.Shape(PrimitiveType.Cube, "Roof", root, new Vector3(0, h + 0.03f, 0),
                new Vector3(size, 0.06f, size), RoofMaterial());

            Color rimColor = rng.NextDouble() < 0.5 ? Visuals.Cyan : Visuals.Purple;
            foreach (var d in Dirs)
            {
                bool facesStreet = level.IsWalkable(cell + d);
                if (!facesStreet && !(filler && rng.NextDouble() < 0.3)) continue;

                Vector3 w = ToWorld(d);
                Vector3 absW = new Vector3(Mathf.Abs(w.x), 0f, Mathf.Abs(w.z));
                Vector3 along = new Vector3(absW.z, 0f, absW.x);

                // Neon line along the roof edge
                Visuals.Shape(PrimitiveType.Cube, "NeonRim", root,
                    w * (size / 2f) + Vector3.up * (h + 0.07f),
                    along * size + absW * 0.04f + Vector3.up * 0.04f,
                    Visuals.GetMaterial(rimColor, rimColor, 2.2f));

                // Sometimes a vertical neon sign on the facade
                if (h > 1.4f && rng.NextDouble() < 0.25)
                {
                    Color sc = PickSignColor(rng);
                    float sh = Range(rng, 0.35f, 0.7f);
                    float sy = Range(rng, 0.5f + sh / 2f, h - 0.2f - sh / 2f);
                    float sx = Range(rng, -size * 0.3f, size * 0.3f);
                    Visuals.Shape(PrimitiveType.Cube, "NeonSign", root,
                        w * (size / 2f + 0.02f) + along * sx + Vector3.up * sy,
                        along * 0.1f + absW * 0.03f + Vector3.up * sh,
                        Visuals.GetMaterial(sc, sc, 3f));
                }
            }
        }

        static Color PickSignColor(System.Random rng)
        {
            switch (rng.Next(4))
            {
                case 0: return Pink;
                case 1: return Visuals.Purple;
                case 2: return Visuals.Cyan;
                default: return Blue;
            }
        }

        /// <summary>Fills a band of buildings around the maze so it feels like part of a bigger city.</summary>
        public static void BuildSkyline(LevelBuilder level, Transform parent, int band)
        {
            for (int y = -band; y < level.Height + band; y++)
                for (int x = -band; x < level.Width + band; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (level.InBounds(c)) continue;
                    BuildBuilding(level, c, parent, true);
                }
        }

        // ---------- Streets ----------

        /// <summary>Glowing curb lines where a street meets a building.</summary>
        public static void BuildStreetLines(LevelBuilder level, Transform parent)
        {
            float cs = level.cellSize;
            var mat = Visuals.GetMaterial(Visuals.Cyan, Visuals.Cyan, 1.1f);
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!level.IsWalkable(c)) continue;
                    foreach (var d in Dirs)
                    {
                        if (level.IsWalkable(c + d)) continue;
                        Vector3 w = ToWorld(d);
                        Vector3 absW = new Vector3(Mathf.Abs(w.x), 0f, Mathf.Abs(w.z));
                        Vector3 along = new Vector3(absW.z, 0f, absW.x);
                        Visuals.Shape(PrimitiveType.Cube, "CurbLine", parent,
                            level.CellToWorld(c) + w * (cs * 0.42f) + Vector3.up * 0.012f,
                            along * cs + absW * 0.025f + Vector3.up * 0.01f, mat);
                    }
                }
        }

        /// <summary>Dark, slightly wet-looking asphalt covering the maze and the skyline band.</summary>
        public static void BuildGround(LevelBuilder level, Transform parent, int band)
        {
            float cs = level.cellSize;
            var center = new Vector3((level.Width - 1) * cs / 2f, -0.05f, (level.Height - 1) * cs / 2f);
            var floor = Visuals.Shape(PrimitiveType.Cube, "Ground", parent, Vector3.zero,
                new Vector3((level.Width + band * 2) * cs, 0.1f, (level.Height + band * 2) * cs), FloorMaterial());
            floor.transform.position = center;
        }

        public static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Visuals.Hex("#0A1226");
            RenderSettings.fogDensity = 0.03f;
        }

        // ---------- Materials ----------

        static Material NewLit()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader);
        }

        static Material FacadeMaterial(float height, int variant)
        {
            int key = Mathf.RoundToInt(height * 4f) * 8 + variant;
            if (facadeCache.TryGetValue(key, out var m) && m != null) return m;
            EnsureWindowTextures();

            var mat = NewLit();
            mat.SetTexture("_BaseMap", windowBase);
            mat.mainTexture = windowBase;
            mat.SetColor("_BaseColor", Color.white);
            mat.color = Color.white;
            // The texture covers 1 unit wide x 2 units tall
            mat.SetTextureScale("_BaseMap", new Vector2(1f, height / 2f));
            mat.SetTextureOffset("_BaseMap", new Vector2(0f, variant * 0.37f));
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", windowGlow);
            mat.SetColor("_EmissionColor", Color.white * 1.6f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetFloat("_Smoothness", 0.35f);
            facadeCache[key] = mat;
            return mat;
        }

        static Material RoofMaterial()
        {
            if (roofMat != null) return roofMat;
            roofMat = NewLit();
            roofMat.SetColor("_BaseColor", Visuals.Hex("#0C1324"));
            roofMat.SetFloat("_Smoothness", 0.2f);
            return roofMat;
        }

        static Material FloorMaterial()
        {
            if (floorMat != null) return floorMat;
            floorMat = NewLit();
            floorMat.SetColor("_BaseColor", Visuals.Hex("#0B1222"));
            floorMat.SetFloat("_Smoothness", 0.7f);
            return floorMat;
        }

        /// <summary>Generates a small window-grid texture: 4 columns x 8 rows of windows, some lit.</summary>
        static void EnsureWindowTextures()
        {
            if (windowBase != null) return;
            const int W = 32, H = 64, Cell = 8;
            windowBase = new Texture2D(W, H, TextureFormat.RGBA32, true);
            windowGlow = new Texture2D(W, H, TextureFormat.RGBA32, true);
            var facade = new Color(0.035f, 0.045f, 0.08f);
            var darkWindow = new Color(0.06f, 0.09f, 0.16f);
            Color[] lit =
            {
                new Color(1f, 0.78f, 0.45f), new Color(1f, 0.78f, 0.45f), new Color(1f, 0.85f, 0.6f),
                new Color(0.4f, 0.9f, 1f), new Color(0.85f, 0.45f, 1f)
            };
            var rng = new System.Random(7);
            var basePx = new Color[W * H];
            var glowPx = new Color[W * H];
            for (int i = 0; i < basePx.Length; i++) { basePx[i] = facade; glowPx[i] = Color.black; }

            for (int cy = 0; cy < H / Cell; cy++)
                for (int cx = 0; cx < W / Cell; cx++)
                {
                    bool on = rng.NextDouble() < 0.4;
                    Color c = on ? lit[rng.Next(lit.Length)] : darkWindow;
                    for (int py = 2; py < 7; py++)
                        for (int px = 2; px < 6; px++)
                        {
                            int idx = (cy * Cell + py) * W + cx * Cell + px;
                            basePx[idx] = on ? c * 0.6f : c;
                            glowPx[idx] = on ? c : Color.black;
                        }
                }

            windowBase.SetPixels(basePx);
            windowGlow.SetPixels(glowPx);
            foreach (var t in new[] { windowBase, windowGlow })
            {
                t.wrapMode = TextureWrapMode.Repeat;
                t.filterMode = FilterMode.Bilinear;
                t.Apply(true);
            }
        }
    }
}
