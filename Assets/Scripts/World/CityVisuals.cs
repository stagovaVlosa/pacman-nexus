using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nexus
{
    /// <summary>
    /// Procedural placeholder city for the maze:
    /// walls become buildings with lit windows, neon roof edges, neon signs and billboards
    /// (with colored lights and reflections on the wet street), streets get glowing curb lines,
    /// a skyline surrounds the maze, and the atmosphere adds fog, post effects and a reflection probe.
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
        static readonly Dictionary<int, Material> billboardCache = new Dictionary<int, Material>();
        static readonly Dictionary<Color, Material> reflectionCache = new Dictionary<Color, Material>();
        static Texture2D reflectionMask;

        // Neon lights and street reflections live outside the building roots,
        // so BuildingFader only fades the buildings themselves
        static Transform neonRoot;
        static int lightCount;
        const int MaxNeonLights = 80;

        /// <summary>Call once before building a level.</summary>
        public static void BeginCity(Transform levelRoot)
        {
            neonRoot = new GameObject("NeonLights").transform;
            neonRoot.SetParent(levelRoot, false);
            lightCount = 0;
        }

        /// <summary>World direction of a grid direction (grid y grows toward the camera).</summary>
        static Vector3 ToWorld(Vector2Int d) => new Vector3(d.x, 0f, -d.y);

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static System.Random RngFor(Vector2Int cell) =>
            new System.Random((cell.x * 73856093) ^ (cell.y * 19349663) ^ 0x5A17);

        // ---------- Buildings ----------

        /// <summary>
        /// Builds one building on a wall cell (or a filler cell outside the maze).
        /// Maze buildings are 2-4 high (BuildingFader makes the ones in front of Pac-Man see-through);
        /// buildings with a street on their far side stay at the low end of that range.
        /// </summary>
        public static void BuildBuilding(LevelBuilder level, Vector2Int cell, Transform parent, bool filler)
        {
            var rng = RngFor(cell);
            float cs = level.cellSize;

            float h;
            if (filler)
                h = cell.y >= level.Height ? Range(rng, 0.7f, 1.6f) : Range(rng, 3.5f, 6.5f);
            else if (cell.y == 0)
                h = Range(rng, 3.5f, 5f);
            else if (level.IsWalkable(cell + Dirs[0]))
                h = Range(rng, 2f, 2.8f);
            else
                h = Range(rng, 2.6f, 4f);
            h = Mathf.Round(h * 4f) / 4f;

            float size = cs * 0.92f;
            var root = new GameObject($"Building_{cell.x}_{cell.y}").transform;
            root.SetParent(parent, false);
            root.position = level.CellToWorld(cell);

            Visuals.Shape(PrimitiveType.Cube, "Body", root, new Vector3(0, h / 2f, 0),
                new Vector3(size, h, size), FacadeMaterial(h, rng.Next(4)));
            Visuals.Shape(PrimitiveType.Cube, "Roof", root, new Vector3(0, h + 0.03f, 0),
                new Vector3(size, 0.06f, size), RoofMaterial());

            // Neon roof edge in one of the three district colors
            int rim = rng.Next(3);
            Color rimColor = rim == 0 ? Pink : rim == 1 ? Visuals.Cyan : Visuals.Purple;
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

                // Signs only on facades the camera can see (not the far side)
                if (d == Dirs[0] || h < 1.8f) continue;
                Vector3 facade = root.position + w * (size / 2f);
                double roll = rng.NextDouble();
                if (roll < 0.4)
                {
                    // Vertical neon sign
                    Color sc = PickSignColor(rng);
                    float sh = Range(rng, 0.45f, 0.9f);
                    float sy = Range(rng, 0.5f + sh / 2f, h - 0.2f - sh / 2f);
                    float sx = Range(rng, -size * 0.3f, size * 0.3f);
                    Visuals.Shape(PrimitiveType.Cube, "NeonSign", root,
                        w * (size / 2f + 0.03f) + along * sx + Vector3.up * sy,
                        along * 0.1f + absW * 0.04f + Vector3.up * sh,
                        Visuals.GetMaterial(sc, sc, 3f));
                    if (!filler) AddNeonGlow(facade + along * sx, w, sc, sy, rng.NextDouble() < 0.5, 0.5f);
                }
                else if (roll < 0.62 && h >= 2.2f)
                {
                    // Billboard: dark frame with a glowing ad
                    int variant = rng.Next(BillboardColors.Length);
                    float bw = size * Range(rng, 0.6f, 0.8f);
                    float bh = Range(rng, 0.45f, 0.75f);
                    float by = Range(rng, 0.9f + bh / 2f, h - 0.25f - bh / 2f);
                    Visuals.Shape(PrimitiveType.Cube, "BillboardFrame", root,
                        w * (size / 2f + 0.015f) + Vector3.up * by,
                        along * (bw + 0.08f) + absW * 0.03f + Vector3.up * (bh + 0.08f), RoofMaterial());
                    Visuals.Shape(PrimitiveType.Cube, "Billboard", root,
                        w * (size / 2f + 0.035f) + Vector3.up * by,
                        along * bw + absW * 0.02f + Vector3.up * bh, BillboardMaterial(variant));
                    if (!filler) AddNeonGlow(facade, w, BillboardColors[variant], by, true, bw);
                }
            }
        }

        /// <summary>
        /// Colored point light in front of a sign and a soft glow on the wet street below it
        /// (a cheap fake reflection). Lights are capped so the frame rate stays good.
        /// </summary>
        static void AddNeonGlow(Vector3 facade, Vector3 outward, Color color, float height, bool withLight, float width)
        {
            if (neonRoot == null) return;
            if (withLight && lightCount < MaxNeonLights)
            {
                lightCount++;
                var light = new GameObject("NeonLight").AddComponent<Light>();
                light.transform.SetParent(neonRoot, false);
                light.transform.position = facade + outward * 0.45f + Vector3.up * Mathf.Min(height, 1.2f);
                light.type = LightType.Point;
                light.color = color;
                light.range = 2.6f;
                light.intensity = 1.6f;
                light.shadows = LightShadows.None;
            }

            if (reflectionMask == null) reflectionMask = Visuals.GradientTexture(true);
            if (!reflectionCache.TryGetValue(color, out var mat) || mat == null)
                reflectionCache[color] = mat = Visuals.Glow(color, 0.45f, 0f, reflectionMask, true);
            var glow = Visuals.Shape(PrimitiveType.Quad, "StreetReflection", neonRoot, Vector3.zero, Vector3.one, mat);
            glow.transform.position = facade + outward * 0.4f + Vector3.up * 0.02f;
            // Lying flat on the street, its local X runs along the facade
            glow.transform.rotation = Quaternion.LookRotation(Vector3.down, outward);
            glow.transform.localScale = new Vector3(width + 0.4f, 0.7f, 1f);
            glow.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
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

        /// <summary>
        /// Streets: a slightly lighter, faintly glowing road surface on every walkable cell
        /// and bright cyan curb lines where a street meets a building, so the maze reads at a glance.
        /// </summary>
        public static void BuildStreetLines(LevelBuilder level, Transform parent)
        {
            float cs = level.cellSize;
            var mat = Visuals.GetMaterial(Visuals.Cyan, Visuals.Cyan, 2.4f);

            var road = new MeshBuilder();
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (level.IsWalkable(c)) road.Box(level.CellToWorld(c) + Vector3.up * 0.004f, new Vector3(cs, 0.008f, cs), Quaternion.identity);
                }
            var roadGo = Visuals.MeshPart("Streets", parent, road.ToMesh("Streets"), StreetMaterial());
            roadGo.GetComponent<Renderer>().receiveShadows = true;

            // All curb lines go into ONE mesh (one draw call instead of ~1000 small cubes)
            var curbs = new MeshBuilder();
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
                        curbs.Box(level.CellToWorld(c) + w * (cs * 0.44f) + Vector3.up * 0.014f,
                            along * cs + absW * 0.05f + Vector3.up * 0.012f, Quaternion.identity);
                    }
                }
            Visuals.MeshPart("CurbLines", parent, curbs.ToMesh("CurbLines"), mat);
        }

        /// <summary>
        /// Realtime reflection probe over the whole city, rendered once after the level is built
        /// (box projection makes the wet street reflect the buildings around it).
        /// </summary>
        public static void AddReflectionProbe(LevelBuilder level, Transform parent, int band)
        {
            float cs = level.cellSize;
            var go = new GameObject("CityReflections");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((level.Width - 1) * cs / 2f, 2f, (level.Height - 1) * cs / 2f);
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3((level.Width + band * 2) * cs, 10f, (level.Height + band * 2) * cs);
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.hdr = true;
            probe.intensity = 1.3f;
            probe.RenderProbe();
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
            RenderSettings.fogColor = Visuals.Hex("#1A1038");   // purple haze
            RenderSettings.fogDensity = 0.045f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Visuals.Hex("#221E48");

            // Post effects on a runtime volume with a higher priority than the scene's volume
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 2.2f;
            bloom.threshold.value = 0.8f;
            bloom.scatter.value = 0.75f;
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.15f;
            color.contrast.value = 18f;
            color.saturation.value = 20f;
            color.colorFilter.value = Visuals.Hex("#E4E0FF");
            var split = profile.Add<SplitToning>(true);
            split.shadows.value = Visuals.Hex("#5B3BC4");     // purple shadows
            split.highlights.value = Visuals.Hex("#38D8FF");  // cyan highlights
            split.balance.value = -15f;
            var vignette = profile.Add<Vignette>(true);
            vignette.color.value = Visuals.Hex("#0B0420");
            vignette.intensity.value = 0.38f;
            vignette.smoothness.value = 0.5f;
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;

            var volume = new GameObject("CityPostFX").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
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
            mat.SetColor("_EmissionColor", Color.white * 1.3f);
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

        static readonly Color[] BillboardColors =
        {
            Visuals.Hex("#FF2BD6"), Visuals.Cyan, Visuals.NeonYellow, Visuals.Purple, Visuals.CorruptedRed
        };

        /// <summary>Glowing ad with a simple generated pattern (stripes, a round logo, text-like bars).</summary>
        static Material BillboardMaterial(int variant)
        {
            if (billboardCache.TryGetValue(variant, out var m) && m != null) return m;
            const int W = 64, H = 32;
            Color main = BillboardColors[variant];
            Color accent = BillboardColors[(variant + 2) % BillboardColors.Length];
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    Color c = Color.Lerp(main * 0.25f, main * 0.8f, (float)y / H);               // background gradient
                    if ((x + y * (variant % 2 == 0 ? 1 : -1) + 640) % 16 < 2) c = main * 0.95f;  // diagonal stripes
                    float dx = x - W * 0.25f, dy = y - H * 0.5f;
                    if (dx * dx + dy * dy < 81f) c = accent;                                      // round logo
                    if (dx * dx + dy * dy < 25f) c = Color.white;
                    if (x > W * 0.45f && x < W * 0.9f && (y == 20 || y == 21 || y == 14 || (y == 9 && x < W * 0.75f)))
                        c = Color.white;                                                          // "text"
                    if (x < 2 || x >= W - 2 || y < 2 || y >= H - 2) c = accent;                   // neon border
                    c.a = 1f;
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(true);

            m = NewLit();
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white * 0.4f);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", Color.white * 2.2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetFloat("_Smoothness", 0.6f);
            billboardCache[variant] = m;
            return m;
        }

        static Material streetMat;

        /// <summary>Wet road: lighter blue than the ground and rooftops, with a faint glow.</summary>
        static Material StreetMaterial()
        {
            if (streetMat != null) return streetMat;
            streetMat = Visuals.Lit(Visuals.Hex("#18264F"), 0.35f, 0.9f, Visuals.Hex("#0C1A3C"), 1f);
            return streetMat;
        }

        static Material FloorMaterial()
        {
            if (floorMat != null) return floorMat;
            floorMat = NewLit();
            // Wet asphalt: very smooth and a bit metallic, so neon and the reflection probe show in it
            floorMat.SetColor("_BaseColor", Visuals.Hex("#080C18"));
            floorMat.SetFloat("_Smoothness", 0.93f);
            floorMat.SetFloat("_Metallic", 0.35f);
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
                    bool on = rng.NextDouble() < 0.18;
                    Color c = on ? lit[rng.Next(lit.Length)] : darkWindow;
                    for (int py = 3; py < 6; py++)
                        for (int px = 3; px < 5; px++)
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
