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
    /// <summary>Look of a level (CityVisuals builds buildings, streets, ground and atmosphere for it).</summary>
    public enum LevelTheme { NeonDistrict, SkylineZone }

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
        const int MaxNeonLights = 36;   // was 80: point lights were a big part of Level 1's GPU cost

        /// <summary>Call once before building a level.</summary>
        static LevelTheme theme = LevelTheme.NeonDistrict;
        /// <summary>Theme of the level being built/played (BugAI picks contrasting colours from it).</summary>
        public static LevelTheme CurrentTheme => theme;
        /// <summary>Skyline Zone: glass towers that go far down, walkways are elevated bridges over a deep city.</summary>
        static bool Sky => theme == LevelTheme.SkylineZone;
        const float SkyDepth = 9f;

        /// <summary>Main UI colour of a level (HUD frames, minimap ring, title card). BYTE stays cyan everywhere.</summary>
        public static Color AccentFor(LevelTheme t) => t == LevelTheme.SkylineZone ? Visuals.Hex("#FF9A3C") : Visuals.Cyan;

        // Skyline Zone "sunset above the clouds" palette
        static readonly Color SunAmber = Visuals.Hex("#FF9A3C");
        static readonly Color SunCream = Visuals.Hex("#FFE2B8");

        public static void BeginCity(Transform levelRoot, LevelTheme levelTheme = LevelTheme.NeonDistrict)
        {
            theme = levelTheme;
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
                h = cell.y >= level.Height ? Range(rng, 0.4f, 0.9f) : (Sky ? Range(rng, 6f, 12f) : Range(rng, 3.5f, 6.5f));
            else if (cell.y == 0)
                h = Sky ? Range(rng, 6f, 9f) : Range(rng, 3f, 4f);
            else if (level.IsWalkable(cell + Dirs[0]))
                h = Sky ? Range(rng, 1.0f, 1.4f) : Range(rng, 0.85f, 1.15f);   // in front of a street (camera side): low, so the street stays visible
            else
                h = Sky ? Range(rng, 3.2f, 4.4f) : Range(rng, 2.2f, 3.4f);
            h = Mathf.Round(h * 4f) / 4f;

            // Skyline: narrower towers (gaps show the city far below) that reach down to the deep ground
            float size = cs * (Sky ? 0.8f : 0.92f);
            float depth = Sky ? SkyDepth : 0f;
            var root = new GameObject($"Building_{cell.x}_{cell.y}").transform;
            root.SetParent(parent, false);
            root.position = level.CellToWorld(cell);

            // Neon District: real Kenney buildings in night colours (the south band near the camera stays low boxes)
            bool kenney = !Sky && !(filler && cell.y >= level.Height) && EnsureKenney();
            if (kenney)
            {
                GameObject prefab;
                if (filler) prefab = Pick(kenneyFill, rng);
                else if (cell.y == 0) prefab = Pick(kenneyTall, rng);
                else if (level.IsWalkable(cell + Dirs[0])) prefab = Pick(kenneyLow, rng);
                else prefab = Pick(kenneyMid, rng);
                h = PlaceKenney(prefab, root, size, rng, filler ? kenneyNightFar : kenneyNight);
            }
            else
            {
                Visuals.Shape(PrimitiveType.Cube, "Body", root, new Vector3(0, (h - depth) / 2f, 0),
                    new Vector3(size, h + depth, size), FacadeMaterial(h + depth, rng.Next(4)));
                Visuals.Shape(PrimitiveType.Cube, "Roof", root, new Vector3(0, h + 0.03f, 0),
                    new Vector3(size, 0.06f, size), RoofMaterial());
            }

            // Neon roof edge in one of the three district colors
            int rim = rng.Next(3);
            Color rimColor = Sky
                ? (rim == 0 ? SunCream : rim == 1 ? SunAmber : Visuals.Hex("#FF7A6A"))
                : (rim == 0 ? Pink : rim == 1 ? Visuals.Cyan : Visuals.Purple);
            foreach (var d in Dirs)
            {
                bool facesStreet = level.IsWalkable(cell + d);
                if (!facesStreet && !(filler && rng.NextDouble() < 0.12)) continue;   // calmer skyline (16 Nov, was 0.3)

                Vector3 w = ToWorld(d);
                Vector3 absW = new Vector3(Mathf.Abs(w.x), 0f, Mathf.Abs(w.z));
                Vector3 along = new Vector3(absW.z, 0f, absW.x);

                // Neon line along the roof edge
                Visuals.Shape(PrimitiveType.Cube, "NeonRim", root,
                    w * (size / 2f) + Vector3.up * (h + 0.07f),
                    along * size + absW * 0.04f + Vector3.up * 0.04f,
                    Visuals.GetMaterial(rimColor, rimColor, Sky ? 1.1f : 0.95f));   // Level 1 calmer (16 Nov, was 1.6)

                // Signs only on facades the camera can see (not the far side)
                if (d == Dirs[0] || h < 1.8f) continue;
                Vector3 facade = root.position + w * (size / 2f);
                double roll = rng.NextDouble();
                if (roll < 0.22 && !Sky)   // fewer signs (16 Nov, was 0.4)
                {
                    // Vertical neon sign
                    Color sc = PickSignColor(rng);
                    float sh = Range(rng, 0.45f, 0.9f);
                    float sy = Range(rng, 0.5f + sh / 2f, h - 0.2f - sh / 2f);
                    float sx = Range(rng, -size * 0.3f, size * 0.3f);
                    Visuals.Shape(PrimitiveType.Cube, "NeonSign", root,
                        w * (size / 2f + 0.03f) + along * sx + Vector3.up * sy,
                        along * 0.1f + absW * 0.04f + Vector3.up * sh,
                        Visuals.GetMaterial(sc, sc, 1.8f));
                    if (!filler) AddNeonGlow(facade + along * sx, w, sc, sy, rng.NextDouble() < 0.5, 0.5f);
                }
                else if (roll < 0.4 && h >= 2.2f)
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

            // FPS (16 Nov): only the building body casts a shadow. Roofs, neon rims and signs are thin details;
            // their shadows were invisible but doubled Level 2's draw calls (sun shadows).
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                if (r.name != "Body" && !r.name.StartsWith("Kenney")) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Kenney City Kit (Neon District) ----------

        static bool kenneyTried;
        static GameObject[] kenneyLow, kenneyMid, kenneyTall, kenneyFill;
        static Material kenneyNight, kenneyNightFar;
        static readonly Dictionary<GameObject, Bounds> kenneyBounds = new Dictionary<GameObject, Bounds>();

        /// <summary>Loads the Kenney models (Assets/ThirdParty/Resources/KenneyCity) and the night material once.</summary>
        static bool EnsureKenney()
        {
            if (kenneyTried) return kenneyNight != null;
            kenneyTried = true;
            var all = Resources.LoadAll<GameObject>("KenneyCity");
            if (all == null || all.Length == 0) return false;
            var byName = new Dictionary<string, GameObject>();
            foreach (var g in all) byName[g.name] = g;
            GameObject[] Get(params string[] names)
            {
                var list = new List<GameObject>();
                foreach (var n in names) if (byName.TryGetValue(n, out var g)) list.Add(g);
                return list.ToArray();
            }
            kenneyLow = Get("building-a", "building-b", "building-c", "building-d", "building-h", "building-e", "building-k");
            kenneyMid = Get("building-f", "building-g", "building-i", "building-l", "building-m", "building-skyscraper-a", "building-j", "building-n");
            kenneyTall = Get("building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e", "building-m");
            // (the low-detail models are left out: their walls use the window colour and would glow entirely)
            kenneyFill = Get("building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d",
                             "building-skyscraper-e", "building-m", "building-l");
            if (kenneyLow.Length == 0 || kenneyMid.Length == 0 || kenneyTall.Length == 0 || kenneyFill.Length == 0) return false;

            var albedo = Resources.Load<Texture2D>("KenneyCity/Textures/colormap_night");
            var glow = Resources.Load<Texture2D>("KenneyCity/Textures/colormap_night_emission");
            if (albedo == null) return false;
            var m = NewLit();
            m.SetTexture("_BaseMap", albedo);
            m.mainTexture = albedo;
            m.SetColor("_BaseColor", Color.white);
            if (glow != null)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", glow);
                m.SetColor("_EmissionColor", Color.white * 1.25f);   // calmer windows (16 Nov, was 1.8)
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            m.SetFloat("_Smoothness", 0.45f);
            m.SetFloat("_Metallic", 0.1f);
            kenneyNight = m;
            // Skyline band around the maze: dimmer windows, so the playable streets stand out
            kenneyNightFar = new Material(m) { name = "KenneyNightFar" };
            if (glow != null) kenneyNightFar.SetColor("_EmissionColor", Color.white * 0.55f);
            return true;
        }

        static GameObject Pick(GameObject[] list, System.Random rng) => list[rng.Next(list.Length)];

        static Bounds KenneyBounds(GameObject prefab)
        {
            if (kenneyBounds.TryGetValue(prefab, out var b)) return b;
            bool first = true;
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = mf.transform.TransformPoint(new Vector3((i & 1) == 0 ? mb.min.x : mb.max.x,
                        (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z)) - prefab.transform.position;
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                }
            }
            kenneyBounds[prefab] = b;
            return b;
        }

        /// <summary>Places a Kenney model in the cell (scaled to the cell footprint, random 90 degree turn). Returns its height.</summary>
        static float PlaceKenney(GameObject prefab, Transform root, float footprint, System.Random rng, Material mat)
        {
            var b = KenneyBounds(prefab);
            float s = footprint / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
            var inst = Object.Instantiate(prefab, root);
            inst.name = "Kenney_" + prefab.name;
            inst.transform.localRotation = Quaternion.Euler(0f, 90f * rng.Next(4), 0f);
            inst.transform.localScale = Vector3.one * s;
            // centre the footprint on the cell and stand it on the ground
            var c = inst.transform.localRotation * new Vector3(b.center.x, 0f, b.center.z) * s;
            inst.transform.localPosition = new Vector3(-c.x, -b.min.y * s, -c.z);
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.Destroy(col);
            return b.size.y * s;
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
                light.intensity = 1.1f;
                light.shadows = LightShadows.None;
            }

            if (reflectionMask == null) reflectionMask = Visuals.GradientTexture(true);
            if (!reflectionCache.TryGetValue(color, out var mat) || mat == null)
                reflectionCache[color] = mat = Visuals.Glow(color, 0.3f, 0f, reflectionMask, true);
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

        // ---------- Performance ----------

        /// <summary>
        /// Called once after the whole level is built (FPS fix, 12 Nov):
        ///  - City (skyline band around the maze, never faded): every building is merged into a few big meshes,
        ///    one per material and per 10x10-cell chunk (was ~1300 separate renderers: the main cost in Level 1).
        ///  - Walls (maze buildings, faded by BuildingFader): merged into 8x8-cell chunks by BuildingFader (16 Nov);
        ///    a building that fades is taken out of its chunk and drawn on its own until it is solid again.
        /// Meshes that are not readable are left as they are (Kenney models are imported with Read/Write on).
        /// </summary>
        public static void Optimize(Transform walls, Transform city, float cellSize)
        {
            if (!Application.isPlaying) return;   // edit-mode screenshot tool: keep the plain objects
            const int Chunk = 10;
            var groups = new Dictionary<(Material, int, int), List<CombineInstance>>();
            var merged = new List<GameObject>();
            Matrix4x4 toLocal = city.worldToLocalMatrix;
            foreach (Transform b in city)
            {
                if (!b.name.StartsWith("Building_")) continue;
                var filters = b.GetComponentsInChildren<MeshFilter>();
                bool ok = filters.Length > 0;
                foreach (var mf in filters)
                    if (mf.sharedMesh == null || !mf.sharedMesh.isReadable || mf.GetComponent<MeshRenderer>() == null) { ok = false; break; }
                if (!ok) continue;
                int cx = Mathf.FloorToInt(b.position.x / (cellSize * Chunk));
                int cz = Mathf.FloorToInt(b.position.z / (cellSize * Chunk));
                foreach (var mf in filters)
                {
                    var mesh = mf.sharedMesh;
                    var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        var m = mats.Length > 0 ? mats[Mathf.Min(s, mats.Length - 1)] : null;
                        if (m == null) continue;
                        var key = (m, cx, cz);
                        if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<CombineInstance>();
                        list.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = toLocal * mf.transform.localToWorldMatrix });
                    }
                }
                merged.Add(b.gameObject);
            }

            var root = new GameObject("SkylineMerged").transform;
            root.SetParent(city, false);
            foreach (var pair in groups)
            {
                var mesh = new Mesh { name = "Skyline_" + pair.Key.Item1.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                var go = Visuals.MeshPart(mesh.name, root, mesh, pair.Key.Item1);
                go.isStatic = true;
            }
            // Hide at once (Destroy only happens at the end of the frame), so nothing is drawn twice
            foreach (var go in merged) { go.SetActive(false); Object.Destroy(go); }

            // Walls: no static batching (it makes the meshes unreadable); BuildingFader merges them into chunks instead.
        }

        // ---------- Streets ----------

        /// <summary>
        /// Streets: a slightly lighter, faintly glowing road surface on every walkable cell
        /// and bright cyan curb lines where a street meets a building, so the maze reads at a glance.
        /// </summary>
        public static void BuildStreetLines(LevelBuilder level, Transform parent)
        {
            float cs = level.cellSize;
            var mat = Sky ? Visuals.GetMaterial(SunAmber, SunAmber, 1.3f) : Visuals.GetMaterial(Visuals.Cyan, Visuals.Cyan, 1.6f);

            var road = new MeshBuilder();
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!level.IsWalkable(c)) continue;
                    if (Sky) road.Box(level.CellToWorld(c) + Vector3.down * 0.12f, new Vector3(cs, 0.25f, cs), Quaternion.identity); // bridge deck
                    else road.Box(level.CellToWorld(c) + Vector3.up * 0.004f, new Vector3(cs, 0.008f, cs), Quaternion.identity);
                }
            var roadGo = Visuals.MeshPart("Streets", parent, road.ToMesh("Streets"), Sky ? DeckMaterial() : StreetMaterial());
            roadGo.GetComponent<Renderer>().receiveShadows = true;
            if (theme == LevelTheme.NeonDistrict) NeonDistrictFX.Build(level, parent);

            if (Sky)
            {
                // Slim pillars carrying the sky bridges down into the clouds (one mesh)
                var pillars = new MeshBuilder();
                for (int y = 0; y < level.Height; y++)
                    for (int x = 0; x < level.Width; x++)
                    {
                        var c = new Vector2Int(x, y);
                        if (x % 4 != 0 || y % 4 != 0 || !level.IsWalkable(c)) continue;
                        float len = SkyDepth - 0.25f;
                        pillars.Box(level.CellToWorld(c) + Vector3.down * (0.25f + len / 2f), new Vector3(0.16f, len, 0.16f), Quaternion.identity);
                    }
                Visuals.MeshPart("BridgePillars", parent, pillars.ToMesh("BridgePillars"), Visuals.Lit(Visuals.Hex("#8A7A70"), 0.4f, 0.5f));
            }

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
            if (Sky)
            {
                // Deep city far below the bridges: dark ground full of tiny lights
                float size = (Mathf.Max(level.Width, level.Height) + band * 2 + 400) * cs;
                // Sea of sunset clouds below the bridges, drifting slowly
                var clouds = Visuals.Shape(PrimitiveType.Cube, "CloudSea", parent, Vector3.zero,
                    new Vector3(size, 0.1f, size), CloudMaterial(size));
                clouds.transform.position = new Vector3((level.Width - 1) * cs / 2f, -6f, (level.Height - 1) * cs / 2f);
                clouds.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                clouds.AddComponent<UvScroll>().speed = new Vector2(0.004f, 0.0015f);
                // Flying cars over the district
                var traffic = new GameObject("SkyTraffic").AddComponent<FlyingTraffic>();
                traffic.transform.SetParent(parent, false);
                traffic.Setup(level, band);
                return;
            }
            var center = new Vector3((level.Width - 1) * cs / 2f, -0.05f, (level.Height - 1) * cs / 2f);
            var floor = Visuals.Shape(PrimitiveType.Cube, "Ground", parent, Vector3.zero,
                new Vector3((level.Width + band * 2) * cs, 0.1f, (level.Height + band * 2) * cs), FloorMaterial());
            floor.transform.position = center;
        }

        public static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Sky ? Visuals.Hex("#F09A78") : Visuals.Hex("#1A1038");   // sunset haze / purple haze
            RenderSettings.fogDensity = Sky ? 0.014f : 0.028f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Sky ? Visuals.Hex("#9A7AA0") : Visuals.Hex("#221E48");
            if (Sky) SetupSunset();

            // Post effects on a runtime volume with a higher priority than the scene's volume
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = Sky ? 0.8f : 0.75f;   // Level 1 calmer (user: "a bit overwhelming", again 16 Nov)
            bloom.threshold.value = Sky ? 1.15f : 1.05f;
            bloom.scatter.value = 0.75f;
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.15f;
            color.contrast.value = 18f;
            color.saturation.value = Sky ? 20f : 6f;   // Level 1 less saturated, easier on the eyes
            color.colorFilter.value = Sky ? Visuals.Hex("#FFE9D8") : Visuals.Hex("#E4E0FF");
            var split = profile.Add<SplitToning>(true);
            split.shadows.value = Sky ? Visuals.Hex("#4A2E7A") : Visuals.Hex("#5B3BC4");     // purple shadows
            split.highlights.value = Sky ? Visuals.Hex("#FFB070") : Visuals.Hex("#38D8FF");  // sunset / cyan highlights
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
            // FPS (16 Nov): Skyline towers are 6-21 units tall; whole-unit window tiling (a few % stretch, not visible)
            // means far fewer materials, so BuildingFader's merged chunks need far fewer draw calls.
            if (Sky) height = Mathf.Max(1f, Mathf.Round(height));
            int key = (Mathf.RoundToInt(height * 4f) * 8 + variant) * 2 + (Sky ? 1 : 0);
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
            mat.SetColor("_EmissionColor", Color.white * 1.0f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetFloat("_Smoothness", 0.35f);
            if (Sky)
            {
                // Pale teal glass curtain walls with warm sun glints (own texture: the night one is near-black)
                EnsureSkyGlassTextures();
                mat.SetTexture("_BaseMap", skyGlassBase);
                mat.mainTexture = skyGlassBase;
                mat.SetTexture("_EmissionMap", skyGlassGlow);
                mat.SetColor("_BaseColor", Color.white);
                mat.color = Color.white;
                mat.SetColor("_EmissionColor", Color.white * 0.6f);
                mat.SetFloat("_Smoothness", 0.85f);
                mat.SetFloat("_Metallic", 0.25f);
            }
            facadeCache[key] = mat;
            return mat;
        }

        static Material roofSkyMat;

        static Material RoofMaterial()
        {
            if (Sky)
            {
                // Warm stone rooftops that catch the low sun (dark roofs read as black boxes from above)
                if (roofSkyMat == null) roofSkyMat = Visuals.Lit(Visuals.Hex("#A88C86"), 0.1f, 0.35f);
                return roofSkyMat;
            }
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

        /// <summary>The ad materials made so far (NeonDistrictFX animates them).</summary>
        public static IEnumerable<Material> BillboardMaterials => billboardCache.Values;

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
            tex.wrapMode = TextureWrapMode.Repeat;   // Neon District scrolls the ads (NeonDistrictFX)
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

        /// <summary>Low orange sun with long soft shadows; camera background matches the haze.</summary>
        static void SetupSunset()
        {
            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Visuals.Hex("#FFB27A");
            sun.intensity = 2.0f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.Euler(34f, -62f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Visuals.Hex("#F09A78"); }
        }

        static Material deckMat, cloudMat;

        /// <summary>Skyline bridges: warm cream deck that reads clearly in the sunset light.</summary>
        static Material DeckMaterial()
        {
            if (deckMat != null) return deckMat;
            deckMat = Visuals.Lit(Visuals.Hex("#EAD8BE"), 0.05f, 0.4f);
            return deckMat;
        }

        /// <summary>Soft sunset clouds: noise texture from deep pink shadows to bright peach tops.</summary>
        static Material CloudMaterial(float size)
        {
            if (cloudMat != null) return cloudMat;
            const int T = 256;
            var tex = new Texture2D(T, T, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[T * T];
            Color low = Visuals.Hex("#B4607A"), mid = Visuals.Hex("#F0957A"), high = Visuals.Hex("#FFE0B8");
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T, n = 0f, amp = 0.5f;
                    int f = 4;
                    for (int o = 0; o < 4; o++)
                    {
                        // tileable noise: blend the four wrapped samples
                        float ox = o * 17.3f, oy = o * 9.1f;
                        float s00 = Mathf.PerlinNoise(u * f + ox, v * f + oy);
                        float s10 = Mathf.PerlinNoise((u - 1f) * f + ox, v * f + oy);
                        float s01 = Mathf.PerlinNoise(u * f + ox, (v - 1f) * f + oy);
                        float s11 = Mathf.PerlinNoise((u - 1f) * f + ox, (v - 1f) * f + oy);
                        float top = Mathf.Lerp(s00, s10, u), bottom = Mathf.Lerp(s01, s11, u);
                        n += amp * Mathf.Lerp(top, bottom, v);
                        amp *= 0.5f; f *= 2;
                    }
                    n = Mathf.Clamp01((n - 0.25f) * 1.6f);
                    px[y * T + x] = n < 0.5f ? Color.Lerp(low, mid, n * 2f) : Color.Lerp(mid, high, (n - 0.5f) * 2f);
                }
            tex.SetPixels(px);
            tex.Apply(true);
            var m = NewLit();
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetTextureScale("_BaseMap", Vector2.one * (size / 30f));
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", Color.white * 0.55f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetFloat("_Smoothness", 0.1f);
            cloudMat = m;
            return m;
        }

        static Material cityLightsMat;

        /// <summary>Skyline Zone ground far below: near-black with scattered warm and cyan lights.</summary>
        static Material CityLightsMaterial(float size)
        {
            if (cityLightsMat != null) return cityLightsMat;
            const int T = 128;
            var rng = new System.Random(7);
            var tex = new Texture2D(T, T, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[T * T];
            var bg = Visuals.Hex("#03060E");
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            Color[] lights = { Visuals.Hex("#FFD27A"), Visuals.Cyan, Visuals.Hex("#FF8AE2"), Visuals.Hex("#7FB4FF") };
            for (int n = 0; n < 420; n++)
            {
                int x = rng.Next(T), y = rng.Next(T);
                px[y * T + x] = lights[rng.Next(lights.Length)];
            }
            // a few glowing "streets" far below
            for (int k = 0; k < T; k++) { px[20 * T + k] = Visuals.Hex("#1B3C8C"); px[k * T + 84] = Visuals.Hex("#1B3C8C"); }
            tex.SetPixels(px);
            tex.filterMode = FilterMode.Point;
            tex.Apply(true);
            var m = NewLit();
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white * 0.3f);
            m.SetTextureScale("_BaseMap", Vector2.one * (size / 8f));
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", Color.white * 2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetFloat("_Smoothness", 0.2f);
            cityLightsMat = m;
            return m;
        }

        static Material streetMat;

        /// <summary>Wet road: lighter blue than the ground and rooftops, with a faint glow.</summary>
        static Material StreetMaterial()
        {
            if (streetMat != null) return streetMat;
            // Wet black-violet asphalt: dark enough that the pink and cyan neon reflections pop
            streetMat = Visuals.Lit(Visuals.Hex("#1A1530"), 0.45f, 0.92f, Visuals.Hex("#120C2A"), 0.7f);
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
        static Texture2D skyGlassBase, skyGlassGlow;

        /// <summary>Skyline Zone glass: pale teal panels, darker mullions, a few panels glinting with sunset light.</summary>
        static void EnsureSkyGlassTextures()
        {
            if (skyGlassBase != null) return;
            const int W = 32, H = 64, Cell = 8;
            skyGlassBase = new Texture2D(W, H, TextureFormat.RGBA32, true);
            skyGlassGlow = new Texture2D(W, H, TextureFormat.RGBA32, true);
            var mullion = Visuals.Hex("#4E6A78");
            Color[] panels = { Visuals.Hex("#8FB8C6"), Visuals.Hex("#A3C8D2"), Visuals.Hex("#7FA9BA") };
            Color[] glints = { Visuals.Hex("#FFC98A"), Visuals.Hex("#FF9F7A"), Visuals.Hex("#FFE2B8") };
            var rng = new System.Random(11);
            var basePx = new Color[W * H];
            var glowPx = new Color[W * H];
            for (int i = 0; i < basePx.Length; i++) { basePx[i] = mullion; glowPx[i] = Color.black; }
            for (int cy = 0; cy < H / Cell; cy++)
                for (int cx = 0; cx < W / Cell; cx++)
                {
                    bool glint = rng.NextDouble() < 0.14;
                    Color c = glint ? glints[rng.Next(glints.Length)] : panels[rng.Next(panels.Length)];
                    for (int py = 1; py < Cell; py++)
                        for (int px = 1; px < Cell; px++)
                        {
                            int idx = (cy * Cell + py) * W + cx * Cell + px;
                            float k = 0.9f + 0.1f * py / Cell;   // slight sky gradient inside each panel
                            basePx[idx] = c * k;
                            glowPx[idx] = glint ? c * 0.8f : Color.black;
                        }
                }
            skyGlassBase.SetPixels(basePx);
            skyGlassGlow.SetPixels(glowPx);
            foreach (var t in new[] { skyGlassBase, skyGlassGlow })
            {
                t.wrapMode = TextureWrapMode.Repeat;
                t.filterMode = FilterMode.Bilinear;
                t.Apply(true);
            }
        }

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
                    bool on = rng.NextDouble() < 0.12;
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
