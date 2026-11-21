using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nexus
{
    /// <summary>
    /// "Realistic" look for the Neon District (21 Nov). Switched on with LevelBuilder.realisticLook;
    /// with the checkbox off the old look (CityVisuals) is used, nothing of it was removed.
    ///
    /// Why the old city looked like Lego: one small cube per maze cell, flat toy colours, everything glowing.
    /// Here instead:
    ///  - one building per city block (greedy rectangles up to 4x4 cells): a low podium with lit shops along the
    ///    street and a tower on the camera side of the block, so the street behind it stays visible;
    ///  - photo-like procedural PBR textures made at load (no downloads): concrete offices, glass curtain walls,
    ///    brick flats, shopfronts, roof gravel, wet asphalt, sidewalk slabs - each with normal + metal/smooth maps;
    ///  - rooftop clutter (AC units, water tanks, antennas, parapets): from the game camera roofs are most of the view;
    ///  - sidewalks with curbs, sodium street lamps, a few neon signs (accents only);
    ///  - dark moonlit lighting, SSAO, fog, calm bloom, filmic grading and a touch of film grain.
    /// Pac-Man, the Bugs, the grid and the gameplay are not touched.
    /// </summary>
    public static class RealisticCity
    {
        /// <summary>True while a level is built/played with the realistic look (other scripts tone their neon down).</summary>
        public static bool Active;

        /// <summary>Skyline Zone (21 Nov): daylight sunset look, towers drop far below the sky bridges into the clouds.</summary>
        static bool Sky => CityVisuals.CurrentTheme == LevelTheme.SkylineZone;
        const float SkyDepth = 9f;
        /// <summary>Where building walls start: the ground in the Neon District, deep below the bridges in the Skyline Zone.</summary>
        static float BaseY => Sky ? -SkyDepth : 0f;

        static readonly Vector2Int N = new Vector2Int(0, -1), S = new Vector2Int(0, 1), E = new Vector2Int(1, 0), W = new Vector2Int(-1, 0);
        static readonly Vector2Int[] Dirs = { N, E, S, W };

        // ------------------------------------------------------------------ entry points

        /// <summary>Builds every maze building (replaces the per-cell CityVisuals.BuildBuilding loop).</summary>
        public static void BuildMazeBuildings(LevelBuilder level, Transform wallsRoot)
        {
            EnsureMaterials();
            lightsRoot = new GameObject("StreetLights").transform;
            lightsRoot.SetParent(level.transform, false);
            lightCount = 0; signLightCount = 0;
            var used = new HashSet<Vector2Int>();
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (level.IsWalkable(c) || used.Contains(c)) continue;
                    var r = GrowRect(c, used, p => level.InBounds(p) && !level.IsWalkable(p), 4, 4);
                    BuildBlock(level, r, wallsRoot, false);
                }
        }

        /// <summary>Background city around the maze (merged into big meshes later by CityVisuals.Optimize).</summary>
        public static void BuildSkyline(LevelBuilder level, Transform city, int band)
        {
            EnsureMaterials();
            var used = new HashSet<Vector2Int>();
            for (int y = -band; y < level.Height + band; y++)
                for (int x = -band; x < level.Width + band; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (level.InBounds(c) || used.Contains(c)) continue;
                    var r = GrowRect(c, used, p => !level.InBounds(p) && p.x >= -band && p.y >= -band
                                                   && p.x < level.Width + band && p.y < level.Height + band, 4, 4);
                    BuildBlock(level, r, city, true);
                }
            BuildFarCity(level, city, band);
        }

        /// <summary>
        /// 21 Nov (user: "outside the city there is nothing"): a whole city around the level, out to the horizon.
        /// Simple towers on a jittered 3-cell grid, merged into one mesh per material, no shadows; fog hides the edge.
        /// Night: lit office/glass/brick towers on the ground. Skyline Zone: glass and white towers rising out of the clouds.
        /// </summary>
        static void BuildFarCity(LevelBuilder level, Transform city, int band)
        {
            const int Far = 46, Step = 3;
            if (Sky) EnsureDayMaterials();
            var rng = new System.Random(77);
            var parts = new Dictionary<Material, RB>();
            RB Get(Material m) { if (!parts.TryGetValue(m, out var rb)) parts[m] = rb = new RB(Vector3.zero); return rb; }
            float cs = level.cellSize;
            for (int gy = -band - Far; gy < level.Height + band + Far; gy += Step)
                for (int gx = -band - Far; gx < level.Width + band + Far; gx += Step)
                {
                    if (gx + Step > -band && gx < level.Width + band && gy + Step > -band && gy < level.Height + band) continue;
                    if (rng.NextDouble() < 0.12) continue;
                    float dist = Mathf.Max(Mathf.Max(-band - gx, gx - level.Width - band), Mathf.Max(-band - gy, gy - level.Height - band));
                    float sx = Range(rng, 1.1f, 2.5f) * cs, sz = Range(rng, 1.1f, 2.5f) * cs;
                    var p = level.CellToWorld(new Vector2Int(gx, gy)) + new Vector3(Range(rng, 0.3f, Step - sx - 0.3f), 0f, -Range(rng, 0.3f, Step - sz - 0.3f));
                    float top = Sky ? Range(rng, -2.5f, 6f) + Mathf.Min(dist, 25f) * 0.08f : Range(rng, 1.4f, 5.5f) + Mathf.Min(dist, 25f) * 0.12f;
                    if (rng.NextDouble() < 0.12) top *= 1.6f;   // a few landmark towers
                    float y0 = Sky ? -SkyDepth : 0f;
                    Material m;
                    double v = rng.NextDouble();
                    if (Sky) m = v < 0.62 ? glassDayMats[rng.Next(glassDayMats.Length)] : officeDayMats[rng.Next(officeDayMats.Length)];
                    else m = v < 0.45 ? officeMats[rng.Next(officeMats.Length)] : v < 0.8 ? glassMats[rng.Next(glassMats.Length)] : brickMats[rng.Next(brickMats.Length)];
                    var c = new Vector3(p.x + sx / 2f, (y0 + top) / 2f, p.z - sz / 2f);
                    var size = new Vector3(sx, top - y0, sz);
                    Get(m).Sides(c, size, 2f, 2f, rng.Next(8) * 0.25f);
                    Get(Sky ? roofDayMat : roofMat).Top(c, size, 2f);
                    if (rng.NextDouble() < 0.35)   // setback tier
                    {
                        float k = Range(rng, 0.55f, 0.75f), add = Range(rng, 0.8f, 2.2f);
                        var c2 = new Vector3(c.x, top + add / 2f, c.z);
                        var s2 = new Vector3(sx * k, add, sz * k);
                        Get(m).Sides(c2, s2, 2f, 2f, 0.5f);
                        Get(Sky ? roofDayMat : roofMat).Top(c2, s2, 2f);
                    }
                }
            foreach (var pair in parts) Part("FarCity", city, pair.Value, pair.Key, false, false);
        }

        /// <summary>Asphalt, sidewalks with curbs, street lamps, the big ground and the Neon District FX (rain, puddles...).</summary>
        public static void BuildStreets(LevelBuilder level, Transform city, int band)
        {
            if (Sky) { BuildSkyBridges(level, city, band); return; }
            EnsureMaterials();
            float cs = level.cellSize;

            // Ground under everything (also the far background), world-mapped asphalt
            var ground = new RB(Vector3.zero);
            float pad = band + 52;
            var gMin = level.CellToWorld(new Vector2Int(0, level.Height - 1)) - new Vector3(pad + 0.5f, 0, pad + 0.5f) * cs;
            float gw = (level.Width + pad * 2) * cs, gd = (level.Height + pad * 2) * cs;
            ground.Quad(new Vector3(gMin.x, -0.01f, gMin.z), Vector3.right * gw, Vector3.forward * gd,
                new Vector2(gMin.x, gMin.z), asphaltTile, asphaltTile);
            Part("Ground", city, ground, asphaltMat, false, true);

            // Sidewalks: a slab + curb along every street edge that touches a building
            var walk = new RB(Vector3.zero);
            const float SW = 0.16f, SH = 0.035f;
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!level.IsWalkable(c)) continue;
                    var p = level.CellToWorld(c);
                    foreach (var d in Dirs)
                    {
                        if (level.IsWalkable(c + d)) continue;
                        var w = new Vector3(d.x, 0, -d.y);
                        var along = new Vector3(Mathf.Abs(w.z), 0, Mathf.Abs(w.x));
                        var center = p + w * (cs * 0.5f - SW * 0.5f) + Vector3.up * (SH * 0.5f);
                        walk.Box(center, along * cs + new Vector3(Mathf.Abs(w.x), 0, Mathf.Abs(w.z)) * SW + Vector3.up * SH, sidewalkTile);
                    }
                }
            Part("Sidewalks", city, walk, sidewalkMat, false, true);

            // Road markings so the streets read at a glance: a bright painted edge line along every curb,
            // and a dashed centre line on straight street stretches
            var edge = new RB(Vector3.zero);
            var dash = new RB(Vector3.zero);
            const float LW = 0.03f, LY = 0.006f;
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!level.IsWalkable(c)) continue;
                    var p = level.CellToWorld(c);
                    foreach (var d in Dirs)
                    {
                        if (level.IsWalkable(c + d)) continue;
                        var w = new Vector3(d.x, 0, -d.y);
                        var along = new Vector3(Mathf.Abs(w.z), 0, Mathf.Abs(w.x));
                        edge.Box(p + w * (cs * 0.5f - SW - 0.035f) + Vector3.up * LY, along * cs + Abs(w) * LW + Vector3.up * 0.002f, 1f);
                    }
                    bool ew = level.IsWalkable(c + E) && level.IsWalkable(c + W) && !level.IsWalkable(c + N) && !level.IsWalkable(c + S);
                    bool ns = level.IsWalkable(c + N) && level.IsWalkable(c + S) && !level.IsWalkable(c + E) && !level.IsWalkable(c + W);
                    if (ew) dash.Box(p + Vector3.up * LY, new Vector3(0.34f * cs, 0.002f, LW), 1f);
                    if (ns) dash.Box(p + Vector3.up * LY, new Vector3(LW, 0.002f, 0.34f * cs), 1f);
                }
            Part("RoadEdgeLines", city, edge, paintWhiteMat, false, true);
            Part("RoadCentreLines", city, dash, paintYellowMat, false, true);

            // Street lamps along every street (about every 2.5 cells, sides alternate), warm sodium + some cool LED
            var lamps = new RB(Vector3.zero);
            var heads = new RB(Vector3.zero);
            var placed = new List<Vector2Int>();
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!level.IsWalkable(c)) continue;
                    bool near = false;
                    foreach (var q in placed) { int dx = q.x - c.x, dy = q.y - c.y; if (dx * dx + dy * dy < 7) { near = true; break; } }
                    if (near) continue;
                    // a side of the street with a building (the pole stands on that sidewalk)
                    var sides = new List<Vector2Int>();
                    foreach (var d in Dirs) if (!level.IsWalkable(c + d)) sides.Add(d);
                    if (sides.Count == 0 || sides.Count >= 3) continue;
                    var side = sides[(x + y) % sides.Count];
                    placed.Add(c);
                    var rng = Rng(c, 91);
                    var w = new Vector3(side.x, 0, -side.y);
                    var basePos = level.CellToWorld(c) + w * (cs * 0.42f);
                    const float LH = 0.62f;
                    lamps.Box(basePos + Vector3.up * (LH / 2f), new Vector3(0.035f, LH, 0.035f), 1f);
                    var arm = -w;   // arm reaches over the street
                    var headPos = basePos + arm * 0.14f + Vector3.up * LH;
                    lamps.Box(basePos + arm * 0.07f + Vector3.up * LH, Abs(arm) * 0.15f + new Vector3(0.022f, 0.022f, 0.022f), 1f);
                    heads.Box(headPos + Vector3.down * 0.02f, new Vector3(0.1f, 0.018f, 0.04f) , 1f);   // flat LED panel, not a glowing ball
                    // 21 Nov: cool white LED only - warm round light pools looked like Nexus Cores (yellow = collectibles)
                    float tintVar = (float)rng.NextDouble();
                    AddLight(headPos + Vector3.down * 0.08f, Color.Lerp(Hex("#DCE6FF"), Hex("#F4F6FF"), tintVar), 2.4f, 2.2f, true);
                }
            Part("LampPoles", city, lamps, darkMetalMat, true, false);
            Part("LampHeads", city, heads, lampHeadMat, false, false);

            NeonDistrictFX.Build(level, city);
        }

        /// <summary>Night lighting and post effects for the realistic look.</summary>
        public static void ApplyAtmosphere()
        {
            if (Sky) { ApplySkyAtmosphere(); return; }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Hex("#10131D");
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#5A6A95");
            RenderSettings.ambientEquatorColor = Hex("#3A3F55");
            RenderSettings.ambientGroundColor = Hex("#1A1C24");

            // Moonlight: cool, weak, soft shadows that give the buildings volume
            Light moon = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { moon = l; break; }
            if (moon == null) moon = new GameObject("Moon").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = Hex("#9FB3E0");
            moon.intensity = 1.1f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.85f;
            moon.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
            RenderSettings.sun = moon;
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex("#10131D"); }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.45f;
            bloom.threshold.value = 1.15f;
            bloom.scatter.value = 0.6f;
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.55f;
            color.contrast.value = 14f;
            color.saturation.value = -8f;
            color.colorFilter.value = Color.white;
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.value = -6f;
            var split = profile.Add<SplitToning>(true);
            split.shadows.value = Hex("#3A4466");     // slightly blue shadows
            split.highlights.value = Hex("#FFC890");  // warm lamp highlights
            split.balance.value = -10f;
            var vignette = profile.Add<Vignette>(true);
            vignette.color.value = Color.black;
            vignette.intensity.value = 0.3f;
            vignette.smoothness.value = 0.45f;
            var grain = profile.Add<FilmGrain>(true);
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.value = 0.18f;
            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.value = 0.06f;
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;

            var volume = new GameObject("RealisticPostFX").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 11f;
            volume.sharedProfile = profile;
        }

        /// <summary>
        /// Skyline Zone streets: asphalt bridge decks (top at y 0) with concrete parapets and a metal rail, white edge
        /// lines, dashed dividers between the two lanes, slim pillars down into the clouds, unlit lamp posts.
        /// The drifting cloud sea and the silver hover cars come from CityVisuals.BuildGround.
        /// </summary>
        static void BuildSkyBridges(LevelBuilder level, Transform city, int band)
        {
            EnsureDayMaterials();
            float cs = level.cellSize;
            CityVisuals.BuildGround(level, city, band);

            var deck = new RB(Vector3.zero);
            var parapet = new RB(Vector3.zero);
            var rail = new RB(Vector3.zero);
            var edge = new RB(Vector3.zero);
            var dash = new RB(Vector3.zero);
            var pillars = new RB(Vector3.zero);
            var poles = new RB(Vector3.zero);
            const float DeckT = 0.28f, PH = 0.07f, PT = 0.05f;
            bool Wk(Vector2Int c) => level.IsWalkable(c);
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!Wk(c)) continue;
                    var p = level.CellToWorld(c);
                    deck.Box(p + Vector3.down * (DeckT / 2f), new Vector3(cs, DeckT, cs), asphaltTile);
                    foreach (var d in Dirs)
                    {
                        if (Wk(c + d)) continue;
                        var w = new Vector3(d.x, 0, -d.y);
                        var along = new Vector3(Mathf.Abs(w.z), 0, Mathf.Abs(w.x));
                        var aw = Abs(w);
                        parapet.Box(p + w * (cs * 0.5f - PT * 0.5f) + Vector3.up * (PH / 2f), along * cs + aw * PT + Vector3.up * PH, 1f);
                        rail.Box(p + w * (cs * 0.5f - PT * 0.5f) + Vector3.up * (PH + 0.05f), along * cs + aw * 0.014f + Vector3.up * 0.014f, 1f);
                        rail.Box(p + w * (cs * 0.5f - PT * 0.5f) + Vector3.up * (PH + 0.025f), along * 0.014f + aw * 0.014f + Vector3.up * 0.05f, 1f);
                        edge.Box(p + w * (cs * 0.5f - PT - 0.06f) + Vector3.up * 0.004f, along * cs + aw * 0.025f + Vector3.up * 0.002f, 1f);
                        if ((x * 7 + y * 3) % 5 == 0)
                        {
                            // lamp post on the parapet, arm reaching over the lane (daytime: unlit)
                            var b = p + w * (cs * 0.5f - PT * 0.5f);
                            poles.Box(b + Vector3.up * (PH + 0.3f), new Vector3(0.025f, 0.6f, 0.025f), 1f);
                            poles.Box(b - w * 0.07f + Vector3.up * (PH + 0.6f), aw * 0.15f + along * 0.02f + Vector3.up * 0.02f, 1f);
                            poles.Box(b - w * 0.14f + Vector3.up * (PH + 0.585f), aw * 0.06f + along * 0.05f + Vector3.up * 0.015f, 1f);
                        }
                    }
                    // dashed divider on the shared edge of two parallel lanes
                    if (Wk(c + S) && !Wk(c + N) && !Wk(c + S + S) && Wk(c + E) && Wk(c + W) && Wk(c + S + E) && Wk(c + S + W))
                        dash.Box(p + new Vector3(0f, 0.004f, -cs * 0.5f), new Vector3(0.36f * cs, 0.002f, 0.025f), 1f);
                    if (Wk(c + E) && !Wk(c + W) && !Wk(c + E + E) && Wk(c + N) && Wk(c + S) && Wk(c + E + N) && Wk(c + E + S))
                        dash.Box(p + new Vector3(cs * 0.5f, 0.004f, 0f), new Vector3(0.025f, 0.002f, 0.36f * cs), 1f);
                    if (x % 4 == 0 && y % 4 == 0)
                    {
                        float len = SkyDepth - DeckT;
                        pillars.Box(p + Vector3.down * (DeckT + len / 2f), new Vector3(0.18f, len, 0.18f), 1f);
                    }
                }
            Part("BridgeDeck", city, deck, deckMat, true, true);
            Part("Parapets", city, parapet, parapetMat, true, true);
            Part("Rails", city, rail, railMat, false, true);
            Part("RoadEdgeLines", city, edge, paintWhiteMat, false, true);
            Part("LaneDividers", city, dash, paintWhiteMat, false, true);
            Part("BridgePillars", city, pillars, pillarMat, true, true);
            Part("LampPosts", city, poles, darkMetalMat, true, true);
        }

        /// <summary>Skyline Zone light: low warm sun with long soft shadows, real procedural sky, warm haze, filmic grade.</summary>
        static void ApplySkyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Hex("#E2A98A");
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#8E9CC0");
            RenderSettings.ambientEquatorColor = Hex("#C49686");
            RenderSettings.ambientGroundColor = Hex("#584852");

            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex("#FFD8B4");
            sun.intensity = 1.75f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(26f, -58f, 0f);
            RenderSettings.sun = sun;

            var cam = Camera.main;
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader) { name = "SkylineSky" };
                sky.SetFloat("_SunDisk", 2f);
                sky.SetFloat("_SunSize", 0.045f);
                sky.SetFloat("_SunSizeConvergence", 6f);
                sky.SetFloat("_AtmosphereThickness", 1.45f);
                sky.SetColor("_SkyTint", Hex("#8C9CC4"));
                sky.SetColor("_GroundColor", Hex("#D49478"));
                sky.SetFloat("_Exposure", 1.1f);
                RenderSettings.skybox = sky;
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            }
            else if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex("#E2A98A"); }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.5f;
            bloom.threshold.value = 1.1f;
            bloom.scatter.value = 0.6f;
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.05f;
            color.contrast.value = 14f;
            color.saturation.value = -4f;
            color.colorFilter.value = Color.white;
            profile.Add<WhiteBalance>(true).temperature.value = 2f;
            var split = profile.Add<SplitToning>(true);
            split.shadows.value = Hex("#3E4A78");
            split.highlights.value = Hex("#FFB47A");
            split.balance.value = -5f;
            var vignette = profile.Add<Vignette>(true);
            vignette.color.value = Color.black;
            vignette.intensity.value = 0.25f;
            vignette.smoothness.value = 0.45f;
            var grain = profile.Add<FilmGrain>(true);
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.value = 0.12f;
            profile.Add<ChromaticAberration>(true).intensity.value = 0.04f;
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;
            var volume = new GameObject("RealisticPostFX").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 11f;
            volume.sharedProfile = profile;
        }

        /// <summary>Turns the URP Screen Space Ambient Occlusion feature on or off (soft contact shadows).</summary>
        public static void SetSSAO(bool on)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return;
            var field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!(field?.GetValue(urp) is ScriptableRendererData[] list)) return;
            foreach (var rd in list)
            {
                if (rd == null) continue;
                foreach (var f in rd.rendererFeatures)
                    if (f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion" && f.isActive != on) f.SetActive(on);
            }
        }

        // ------------------------------------------------------------------ blocks

        struct Block { public int x, y, w, d; }

        static Block GrowRect(Vector2Int start, HashSet<Vector2Int> used, System.Func<Vector2Int, bool> ok, int maxW, int maxD)
        {
            int w = 1;
            while (w < maxW) { var p = new Vector2Int(start.x + w, start.y); if (!ok(p) || used.Contains(p)) break; w++; }
            int d = 1;
            while (d < maxD)
            {
                bool row = true;
                for (int i = 0; i < w; i++) { var p = new Vector2Int(start.x + i, start.y + d); if (!ok(p) || used.Contains(p)) { row = false; break; } }
                if (!row) break;
                d++;
            }
            for (int j = 0; j < d; j++) for (int i = 0; i < w; i++) used.Add(new Vector2Int(start.x + i, start.y + j));
            return new Block { x = start.x, y = start.y, w = w, d = d };
        }

        static void BuildBlock(LevelBuilder level, Block r, Transform parent, bool filler)
        {
            float cs = level.cellSize;
            var rng = Rng(new Vector2Int(r.x, r.y), filler ? 7 : 3);
            // World bounds (grid y grows toward the camera = world -z)
            var nw = level.CellToWorld(new Vector2Int(r.x, r.y));
            float xMin = nw.x - cs * 0.5f, xMax = xMin + r.w * cs;
            float zMax = nw.z + cs * 0.5f, zMin = zMax - r.d * cs;
            float M = Sky ? 0.14f : 0.03f;
            xMin += M; xMax -= M; zMin += M; zMax -= M;

            var root = new GameObject($"Building_{r.x}_{r.y}").transform;
            root.SetParent(parent, false);
            root.position = new Vector3((xMin + xMax) / 2f, 0f, (zMin + zMax) / 2f);
            var b = new BuildingParts(root.position);

            bool topBorder = !filler && r.y == 0;
            bool bottomBorder = !filler && r.y + r.d - 1 == level.Height - 1;
            bool southBand = filler && r.y >= level.Height;
            bool northBand = filler && r.y + r.d - 1 < 0;
            bool streetNorth = false;
            if (!filler)
                for (int i = 0; i < r.w; i++) if (level.IsWalkable(new Vector2Int(r.x + i, r.y - 1))) { streetNorth = true; break; }

            if (Sky)
            {
                SkyBlock(level, r, b, rng, filler, topBorder || northBand, southBand || bottomBorder, xMin, xMax, zMin, zMax);
            }
            else if (southBand || bottomBorder)
            {
                // Near the camera: low buildings, so nothing hides the bottom streets
                float h = Range(rng, 0.22f, southBand ? 0.45f : 0.38f);
                Mass(b, rng, xMin, xMax, zMin, zMax, h, Facade.Shop, true);
            }
            else if (topBorder || northBand || (filler && !southBand))
            {
                // Far side and sides: the skyline, tall towers
                float h = northBand || topBorder ? Range(rng, 2.6f, 5.2f) : Range(rng, 1.4f, 3.6f);
                if (Sky) h = northBand || topBorder ? Range(rng, 4f, 8f) : Range(rng, 2.4f, 6f);
                var f = PickFacade(rng, true);
                if (r.w >= 3 && rng.NextDouble() < 0.5)
                {
                    float mid = Mathf.Lerp(xMin, xMax, Range(rng, 0.4f, 0.6f));
                    Mass(b, rng, xMin, mid - 0.04f, zMin, zMax, h, f, true);
                    Mass(b, rng, mid + 0.04f, xMax, zMin, zMax, h * Range(rng, 0.6f, 1.2f), PickFacade(rng, true), true);
                }
                else Tower(b, rng, xMin, xMax, zMin, zMax, h, f);
            }
            else if (r.d >= 3 && r.w >= 2)
            {
                // City block: podium with shops all around, tower on the camera side (south) of the block.
                // A height h hides about 0.445*h behind it (66 degree camera), so the tower stays below what
                // the podium in front of it (north) can "absorb" and the street north of the block stays visible.
                float podium = Range(rng, 0.3f, 0.46f);
                Mass(b, rng, xMin, xMax, zMin, zMax, podium, Facade.Shop, true);
                int towerRows = r.d - Mathf.CeilToInt(r.d / 2f);
                float towerZMax = zMin + towerRows * cs - 0.06f;
                float shelter = (zMax - towerZMax);
                float hMax = Mathf.Clamp(shelter / 0.445f * 0.95f, 1.2f, Sky ? 5.2f : 4.4f);
                float h = Range(rng, Mathf.Max(1.0f, hMax * 0.45f), hMax);
                float ix = 0.08f;
                if (r.w >= 4 && rng.NextDouble() < 0.55)
                {
                    float mid = Mathf.Lerp(xMin, xMax, Range(rng, 0.42f, 0.58f));
                    Tower(b, rng, xMin + ix, mid - 0.06f, zMin + 0.02f, towerZMax, h, PickFacade(rng, false));
                    Tower(b, rng, mid + 0.06f, xMax - ix, zMin + 0.02f, towerZMax - Range(rng, 0f, 0.4f), h * Range(rng, 0.55f, 0.95f), PickFacade(rng, false));
                }
                else Tower(b, rng, xMin + ix, xMax - ix, zMin + 0.02f, towerZMax, h, PickFacade(rng, false));
            }
            else
            {
                // Small pieces (thin strips, diagonal fragments, side borders)
                float h = streetNorth ? Range(rng, 0.45f, 0.95f) : Range(rng, 0.9f, 1.9f);
                Mass(b, rng, xMin, xMax, zMin, zMax, h, h < 0.6f ? Facade.Shop : PickFacade(rng, false), true);
            }

            b.Finish(root);
            if (!filler && !Sky) AddSigns(level, r, b, rng, xMin, xMax, zMin, root);
        }

        /// <summary>
        /// Skyline Zone block (21 Nov v2): no podium (the ground is far below). Camera-side blocks and some inner blocks are
        /// "deep": their towers end below the bridges, so you look down into the city. Others get one or two slim towers
        /// (glass or white concrete) on the camera side of the block, low enough that the bridge behind stays visible.
        /// </summary>
        static void SkyBlock(LevelBuilder level, Block r, BuildingParts b, System.Random rng, bool filler, bool far, bool near,
                             float xMin, float xMax, float zMin, float zMax)
        {
            float cs = level.cellSize;
            Facade F() => rng.NextDouble() < 0.65 ? Facade.Glass : Facade.Office;
            if (near || (!far && rng.NextDouble() < (filler ? 0.45 : 0.3)))
            {
                // deep: tops 0.8-4 units below the bridge decks
                float ix = 0.12f;
                if (r.w >= 3 && rng.NextDouble() < 0.6)
                {
                    float mid = Mathf.Lerp(xMin, xMax, Range(rng, 0.4f, 0.6f));
                    Tower(b, rng, xMin + ix, mid - 0.15f, zMin + ix, zMax - ix, Range(rng, -4f, -0.8f), F());
                    Tower(b, rng, mid + 0.15f, xMax - ix, zMin + ix, zMax - ix, Range(rng, -4f, -0.8f), F());
                }
                else Tower(b, rng, xMin + ix, xMax - ix, zMin + ix, zMax - ix, Range(rng, -4f, -0.8f), F());
                return;
            }
            if (far)
            {
                float h = Range(rng, 4f, 9f);
                if (r.w >= 3 && rng.NextDouble() < 0.6)
                {
                    float mid = Mathf.Lerp(xMin, xMax, Range(rng, 0.4f, 0.6f));
                    Tower(b, rng, xMin + 0.1f, mid - 0.12f, zMin + 0.1f, zMax - 0.1f, h, F());
                    Tower(b, rng, mid + 0.12f, xMax - 0.1f, zMin + 0.1f, zMax - 0.1f, h * Range(rng, 0.6f, 1.1f), F());
                }
                else Tower(b, rng, xMin + 0.15f, xMax - 0.15f, zMin + 0.15f, zMax - 0.15f, h, F());
                return;
            }
            // slim tower(s) on the camera side of the block, height limited by the free depth behind it
            int rows = Mathf.Max(1, r.d - Mathf.CeilToInt(r.d / 2f));
            float tz1 = zMin + rows * cs - 0.1f;
            float shelter = zMax - tz1 + 0.6f;   // + the bridge parapet / gap in front
            float hMax = Mathf.Clamp(shelter / 0.445f, 1.2f, 5.5f);
            float hh = Range(rng, Mathf.Max(1.2f, hMax * 0.55f), hMax);
            float ix2 = 0.18f;
            if (r.w >= 3 && rng.NextDouble() < 0.65)
            {
                float mid = Mathf.Lerp(xMin, xMax, Range(rng, 0.42f, 0.58f));
                Tower(b, rng, xMin + ix2, mid - 0.14f, zMin + ix2, tz1, hh, F());
                Tower(b, rng, mid + 0.14f, xMax - ix2, zMin + ix2, tz1, rng.NextDouble() < 0.5 ? hh * Range(rng, 0.6f, 0.9f) : Range(rng, -3f, -0.8f), F());
            }
            else Tower(b, rng, xMin + ix2, xMax - ix2, zMin + ix2, tz1, hh, F());
            // the back half of the block: a deep tower, so the gap shows the city below
            if (zMax - tz1 > 0.9f)
                Tower(b, rng, xMin + ix2, xMax - ix2, tz1 + 0.25f, zMax - ix2, Range(rng, -3.5f, -1f), F());
        }

        enum Facade { Office, Glass, Brick, Shop }

        static Facade PickFacade(System.Random rng, bool tall)
        {
            double v = rng.NextDouble();
            if (tall) return v < 0.5 ? Facade.Glass : v < 0.85 ? Facade.Office : Facade.Brick;
            return v < 0.38 ? Facade.Office : v < 0.68 ? Facade.Brick : Facade.Glass;
        }

        /// <summary>Tower with parapet and roof clutter; tall ones may get a smaller upper tier.</summary>
        static void Tower(BuildingParts b, System.Random rng, float x0, float x1, float z0, float z1, float h, Facade f)
        {
            if (x1 - x0 < 0.2f || z1 - z0 < 0.2f) return;
            if (h > 2.4f && rng.NextDouble() < (Sky ? 0.6 : 0.45))
            {
                float lower = Sky ? Mathf.Lerp(BaseY, h, Range(rng, 0.8f, 0.9f)) : h * Range(rng, 0.6f, 0.75f);
                Mass(b, rng, x0, x1, z0, z1, lower, f, true);
                float sx = (x1 - x0) * Range(rng, 0.12f, 0.2f), sz = (z1 - z0) * Range(rng, 0.12f, 0.2f);
                Mass(b, rng, x0 + sx, x1 - sx, z0 + sz * 0.3f, z1 - sz, h, f, true, lower);
            }
            else Mass(b, rng, x0, x1, z0, z1, h, f, true);
        }

        /// <summary>One box-shaped building volume from y0 to h, facade on the sides, gravel roof + clutter on top.</summary>
        static void Mass(BuildingParts b, System.Random rng, float x0, float x1, float z0, float z1, float h, Facade f, bool clutter, float y0 = 0f)
        {
            if (y0 == 0f) y0 = BaseY;
            var mat = FacadeMat(f, rng);
            float tile = f == Facade.Shop ? 2f : 2f;
            var rb = b.Get(mat);
            var c = new Vector3((x0 + x1) / 2f, (y0 + h) / 2f, (z0 + z1) / 2f);
            var size = new Vector3(x1 - x0, h - y0, z1 - z0);
            // facade sides; u starts at a random whole window so window columns differ between buildings
            float uOff = rng.Next(8) * 0.25f;
            rb.Sides(c, size, tile, f == Facade.Shop ? 1f : tile, uOff);
            // roof
            b.Get(Sky ? (rng.NextDouble() < 0.6 ? roofDayMat : roofDayMat2) : (rng.NextDouble() < 0.55 ? roofMat : roofMat2)).Top(c, size, 2f);
            // parapet
            var pm = b.Get(concreteMat);
            const float PT = 0.035f, PH = 0.05f;
            float top = h;
            pm.Box(new Vector3(c.x, top + PH / 2, z0 + PT / 2), new Vector3(size.x, PH, PT), 1f);
            pm.Box(new Vector3(c.x, top + PH / 2, z1 - PT / 2), new Vector3(size.x, PH, PT), 1f);
            pm.Box(new Vector3(x0 + PT / 2, top + PH / 2, c.z), new Vector3(PT, PH, size.z - PT * 2), 1f);
            pm.Box(new Vector3(x1 - PT / 2, top + PH / 2, c.z), new Vector3(PT, PH, size.z - PT * 2), 1f);
            if (clutter) RoofClutter(b, rng, x0 + 0.08f, x1 - 0.08f, z0 + 0.08f, z1 - 0.08f, top);
        }

        static void RoofClutter(BuildingParts b, System.Random rng, float x0, float x1, float z0, float z1, float y)
        {
            float area = (x1 - x0) * (z1 - z0);
            if (area < 0.05f) return;
            var metal = b.Get(Sky ? roofUnitDayMat : metalMat);
            int n = Sky ? Mathf.Clamp(Mathf.RoundToInt(area * Range(rng, 0.5f, 1.1f)), 1, 4)
                        : Mathf.Clamp(Mathf.RoundToInt(area * Range(rng, 2.0f, 3.6f)), 1, 14);
            for (int i = 0; i < n; i++)
            {
                // AC units / vents
                float sx = Range(rng, 0.1f, 0.24f), sz = Range(rng, 0.1f, 0.22f), sy = Range(rng, 0.05f, 0.12f);
                if (x1 - x0 < sx || z1 - z0 < sz) continue;
                float px = Range(rng, x0 + sx / 2, x1 - sx / 2), pz = Range(rng, z0 + sz / 2, z1 - sz / 2);
                metal.Box(new Vector3(px, y + sy / 2, pz), new Vector3(sx, sy, sz), 0.5f);
                if (rng.NextDouble() < 0.4) // fan grille on top
                    b.Get(darkMetalMat).Box(new Vector3(px, y + sy + 0.004f, pz), new Vector3(sx * 0.6f, 0.008f, sz * 0.6f), 0.5f);
            }
            // Skylights: warm glowing panels (rooms below are lit), so big roofs are not dead dark patches
            int sky = Sky ? 0 : area > 0.8f ? rng.Next(1, 4) : (area > 0.3f && rng.NextDouble() < 0.4 ? 1 : 0);
            for (int i = 0; i < sky; i++)
            {
                float sx = Range(rng, 0.14f, 0.32f), sz = Range(rng, 0.1f, 0.2f);
                if (x1 - x0 < sx + 0.1f || z1 - z0 < sz + 0.1f) continue;
                float px = Range(rng, x0 + sx / 2, x1 - sx / 2), pz = Range(rng, z0 + sz / 2, z1 - sz / 2);
                b.Get(concreteMat).Box(new Vector3(px, y + 0.02f, pz), new Vector3(sx + 0.03f, 0.04f, sz + 0.03f), 1f);
                b.Get(rng.NextDouble() < 0.75 ? skylightMat : skylightCoolMat).Box(new Vector3(px, y + 0.042f, pz), new Vector3(sx, 0.006f, sz), 1f);
            }
            if (!Sky && area > 0.6f && rng.NextDouble() < 0.5)
            {
                // Water tank on short legs
                float rad = Range(rng, 0.08f, 0.13f);
                float px = Range(rng, x0 + rad, x1 - rad), pz = Range(rng, z0 + rad, z1 - rad);
                b.Get(tankMat).Prism(new Vector3(px, y + 0.06f, pz), rad, Range(rng, 0.16f, 0.24f), 10);
                var dm = b.Get(darkMetalMat);
                dm.Box(new Vector3(px, y + 0.03f, pz), new Vector3(rad * 1.6f, 0.06f, 0.02f), 0.5f);
                dm.Box(new Vector3(px, y + 0.03f, pz), new Vector3(0.02f, 0.06f, rad * 1.6f), 0.5f);
            }
            if (y > 2.2f && rng.NextDouble() < 0.55)
            {
                // Antenna with a red aircraft light
                float px = Range(rng, x0, x1), pz = Range(rng, z0, z1), ah = Range(rng, 0.3f, 0.7f);
                b.Get(darkMetalMat).Box(new Vector3(px, y + ah / 2, pz), new Vector3(0.018f, ah, 0.018f), 0.5f);
                b.Get(redLightMat).Box(new Vector3(px, y + ah + 0.015f, pz), new Vector3(0.035f, 0.03f, 0.035f), 0.5f);
            }
        }

        /// <summary>A few vertical neon signs on tower facades that face a street (the neon is now an accent, not everything).</summary>
        static void AddSigns(LevelBuilder level, Block r, BuildingParts b, System.Random rng, float xMin, float xMax, float zMin, Transform root)
        {
            if (rng.NextDouble() > 0.38) return;
            var col = SignColors[rng.Next(SignColors.Length)];
            var mat = SignMat(col);
            // on the south (camera) face, near one end
            float x = rng.NextDouble() < 0.5 ? xMin + Range(rng, 0.15f, 0.4f) : xMax - Range(rng, 0.15f, 0.4f);
            float sh = Range(rng, 0.35f, 0.6f), sy = Range(rng, 0.5f, 0.75f) + sh / 2f;
            var sb = new RB(root.position);
            var pos = new Vector3(x, sy, zMin - 0.05f);
            sb.Box(pos, new Vector3(0.07f, sh, 0.03f), 1f);
            sb.Box(pos + new Vector3(0, 0, 0.03f), new Vector3(0.012f, 0.012f, 0.05f), 1f);   // bracket
            Part("NeonSign", root, sb, mat, false, false);
            AddLight(pos + new Vector3(0, -0.1f, -0.3f), col, 1.8f, 1.2f, false);
        }

        static readonly Color[] SignColors =
            { Hex("#FF2BD6"), Hex("#00D9FF"), Hex("#A855F7"), Hex("#FF8A2A"), Hex("#FF3158") };

        // ------------------------------------------------------------------ city power (intro blackout / power-on wave)

        static readonly Dictionary<Material, Color> emissionBase = new Dictionary<Material, Color>();
        static readonly Dictionary<Light, float> lightBase = new Dictionary<Light, float>();

        static void CollectPower()
        {
            if (emissionBase.Count > 0) return;
            void Add(Material m) { if (m != null && !emissionBase.ContainsKey(m)) emissionBase[m] = m.GetColor("_EmissionColor"); }
            if (officeMats != null) foreach (var m in officeMats) Add(m);
            if (glassMats != null) foreach (var m in glassMats) Add(m);
            if (brickMats != null) foreach (var m in brickMats) Add(m);
            Add(shopMat); Add(skylightMat); Add(skylightCoolMat); Add(lampHeadMat);
            foreach (var m in signCache.Values) Add(m);
            if (lightsRoot != null)
                foreach (var l in lightsRoot.GetComponentsInChildren<Light>(true)) lightBase[l] = l.intensity;
        }

        /// <summary>
        /// Intro (21 Nov): how much of the city has power, 0 = blackout, 1 = normal. Windows, shops, skylights,
        /// signs and street lamps follow it. With waveRadius >= 0 the lamps switch on in a growing circle around
        /// waveCenter (lamps outside the circle use 'k').
        /// </summary>
        public static void SetCityPower(float k, Vector3 waveCenter = default, float waveRadius = -1f)
        {
            if (!matsReady) return;
            CollectPower();
            foreach (var pair in emissionBase)
                if (pair.Key != null) pair.Key.SetColor("_EmissionColor", pair.Value * k);
            foreach (var pair in lightBase)
            {
                if (pair.Key == null) continue;
                float lk = k;
                if (waveRadius >= 0f)
                {
                    var d = pair.Key.transform.position - waveCenter; d.y = 0f;
                    lk = d.magnitude < waveRadius ? 1f : k;
                }
                pair.Key.intensity = pair.Value * lk;
                pair.Key.enabled = lk > 0.01f;
            }
        }

        // ------------------------------------------------------------------ lights

        static Transform lightsRoot;
        static int lightCount, signLightCount;
        const int MaxLampLights = 140, MaxSignLights = 18;   // Forward+ renders many small lights cheaply

        static void AddLight(Vector3 pos, Color c, float range, float intensity, bool lamp)
        {
            if (lightsRoot == null) return;
            if (lamp) { if (lightCount >= MaxLampLights) return; lightCount++; }
            else { if (signLightCount >= MaxSignLights) return; signLightCount++; }
            var l = new GameObject("StreetLight").AddComponent<Light>();
            l.transform.SetParent(lightsRoot, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>Collects geometry per material for one building, then makes one child renderer per material.</summary>
        class BuildingParts
        {
            readonly Vector3 origin;
            readonly Dictionary<Material, RB> parts = new Dictionary<Material, RB>();
            public BuildingParts(Vector3 origin) { this.origin = origin; }
            public RB Get(Material m)
            {
                if (!parts.TryGetValue(m, out var rb)) parts[m] = rb = new RB(origin);
                return rb;
            }
            public void Finish(Transform root)
            {
                foreach (var p in parts)
                {
                    bool body = p.Key != redLightMat && p.Key != skylightMat && p.Key != skylightCoolMat;
                    Part(p.Key == roofMat || p.Key == roofMat2 || p.Key == roofDayMat || p.Key == roofDayMat2 ? "Roof" : (p.Key.name.StartsWith("Facade") ? "Body" : "Detail"), root, p.Value, p.Key, body, true);
                }
            }
        }

        static GameObject Part(string name, Transform parent, RB rb, Material mat, bool castShadows, bool receive)
        {
            if (rb.Empty) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = rb.Origin;
            go.AddComponent<MeshFilter>().sharedMesh = rb.ToMesh(name);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = receive;
            return go;
        }

        /// <summary>Tiny mesh builder with world-space UVs (so textures keep real scale on every building).</summary>
        class RB
        {
            public readonly Vector3 Origin;
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<Vector3> n = new List<Vector3>();
            readonly List<Vector4> t = new List<Vector4>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> tri = new List<int>();
            public RB(Vector3 origin) { Origin = origin; }
            public bool Empty => v.Count == 0;

            /// <summary>Quad from corner p0 along 'right' and 'up'. Normal = cross(up, right). uv = (uv0 + lengths) / tile.</summary>
            public void Quad(Vector3 p0, Vector3 right, Vector3 up, Vector2 uv0, float tileU, float tileV)
            {
                int i = v.Count;
                var nrm = Vector3.Cross(up, right).normalized;
                var tan = right.normalized;
                float wSign = Vector3.Dot(Vector3.Cross(nrm, tan), up) >= 0 ? 1f : -1f;
                var p = p0 - Origin;
                v.Add(p); v.Add(p + right); v.Add(p + right + up); v.Add(p + up);
                float ru = right.magnitude / tileU, uu = up.magnitude / tileV;
                var a = new Vector2(uv0.x / tileU, uv0.y / tileV);
                uv.Add(a); uv.Add(a + new Vector2(ru, 0)); uv.Add(a + new Vector2(ru, uu)); uv.Add(a + new Vector2(0, uu));
                for (int k = 0; k < 4; k++) { n.Add(nrm); t.Add(new Vector4(tan.x, tan.y, tan.z, wSign)); }
                tri.Add(i); tri.Add(i + 3); tri.Add(i + 2);
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 1);
            }

            /// <summary>Four vertical sides of a box. u runs along each face from uOff, v = height above the ground.</summary>
            public void Sides(Vector3 c, Vector3 s, float tileU, float tileV, float uOff)
            {
                float x0 = c.x - s.x / 2, x1 = c.x + s.x / 2, y0 = c.y - s.y / 2, z0 = c.z - s.z / 2, z1 = c.z + s.z / 2;
                var up = Vector3.up * s.y;
                Quad(new Vector3(x0, y0, z0), Vector3.right * s.x, up, new Vector2(uOff, y0), tileU, tileV);            // south
                Quad(new Vector3(x1, y0, z1), Vector3.left * s.x, up, new Vector2(uOff + 0.5f, y0), tileU, tileV);      // north
                Quad(new Vector3(x1, y0, z0), Vector3.forward * s.z, up, new Vector2(uOff + 0.25f, y0), tileU, tileV);  // east
                Quad(new Vector3(x0, y0, z1), Vector3.back * s.z, up, new Vector2(uOff + 0.75f, y0), tileU, tileV);     // west
            }

            public void Top(Vector3 c, Vector3 s, float tile)
            {
                float x0 = c.x - s.x / 2, y1 = c.y + s.y / 2, z0 = c.z - s.z / 2;
                Quad(new Vector3(x0, y1, z0), Vector3.right * s.x, Vector3.forward * s.z, new Vector2(x0, z0), tile, tile);
            }

            public void Box(Vector3 c, Vector3 s, float tile)
            {
                Sides(c, s, tile, tile, c.x + c.z);
                Top(c, s, tile);
            }

            /// <summary>Upright n-sided prism (water tanks).</summary>
            public void Prism(Vector3 bottomCenter, float r, float h, int sides)
            {
                for (int k = 0; k < sides; k++)
                {
                    float a0 = k * Mathf.PI * 2 / sides, a1 = (k + 1) * Mathf.PI * 2 / sides;
                    var p0 = bottomCenter + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r;
                    var p1 = bottomCenter + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r;
                    // normal = cross(up, p1 - p0) points outward
                    Quad(p0, p1 - p0, Vector3.up * h, new Vector2(k * 0.1f, 0), 0.5f, 0.5f);
                    // lid triangle
                    int i = v.Count;
                    var top = bottomCenter + Vector3.up * h - Origin;
                    v.Add(top); v.Add(p0 + Vector3.up * h - Origin); v.Add(p1 + Vector3.up * h - Origin);
                    for (int q = 0; q < 3; q++) { n.Add(Vector3.up); t.Add(new Vector4(1, 0, 0, 1)); uv.Add(new Vector2(0.5f, 0.5f)); }
                    tri.Add(i); tri.Add(i + 2); tri.Add(i + 1);
                }
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(v); m.SetNormals(n); m.SetTangents(t); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        // ------------------------------------------------------------------ materials & procedural PBR textures

        static bool matsReady;
        static Material[] officeMats, glassMats, brickMats;
        static Material shopMat, roofMat, roofMat2, concreteMat, metalMat, darkMetalMat, tankMat, redLightMat, paintWhiteMat, paintYellowMat, skylightMat, skylightCoolMat, lampHeadMat, asphaltMat, sidewalkMat;
        static readonly Dictionary<Color, Material> signCache = new Dictionary<Color, Material>();
        const float asphaltTile = 4f, sidewalkTile = 1f;

        static Material FacadeMat(Facade f, System.Random rng)
        {
            if (Sky)
            {
                EnsureDayMaterials();
                switch (f)
                {
                    case Facade.Glass: return glassDayMats[rng.Next(glassDayMats.Length)];
                    case Facade.Brick: return stoneDayMat;
                    case Facade.Shop: return officeDayMats[1];
                    default: return officeDayMats[rng.Next(officeDayMats.Length)];
                }
            }
            switch (f)
            {
                case Facade.Office: return officeMats[rng.Next(officeMats.Length)];
                case Facade.Glass: return glassMats[rng.Next(glassMats.Length)];
                case Facade.Brick: return brickMats[rng.Next(brickMats.Length)];
                default: return shopMat;
            }
        }

        static void EnsureMaterials()
        {
            if (matsReady && roofMat != null) return;
            matsReady = true;
            emissionBase.Clear();
            lightBase.Clear();
            officeMats = new[] { Pbr("FacadeOffice", MakeOffice(1), Color.white), Pbr("FacadeOffice2", MakeOffice(2), Hex("#E8E2DA")) };
            glassMats = new[] { Pbr("FacadeGlass", MakeGlass(3), Color.white), Pbr("FacadeGlass2", MakeGlass(4), Hex("#D8E4EC")) };
            brickMats = new[] { Pbr("FacadeBrick", MakeBrick(5), Color.white), Pbr("FacadeBrick2", MakeBrick(6), Hex("#C9C3BE")) };
            shopMat = Pbr("FacadeShop", MakeShop(7), Color.white);
            roofMat = Pbr("RoofGravel", MakeGravel(8), Color.white);
            roofMat2 = Pbr("RoofTar", MakeGravel(11), Color.white);
            asphaltMat = Pbr("Asphalt", MakeAsphalt(9), Color.white);
            sidewalkMat = Pbr("Sidewalk", MakeSidewalk(10), Color.white);
            concreteMat = Plain("Concrete", Hex("#3C3F45"), 0f, 0.25f);
            metalMat = Plain("RoofMetal", Hex("#5C6169"), 0.65f, 0.45f);
            darkMetalMat = Plain("DarkMetal", Hex("#202328"), 0.7f, 0.5f);
            tankMat = Plain("Tank", Hex("#3A3530"), 0.3f, 0.3f);
            redLightMat = Plain("AircraftLight", Hex("#FF2020"), 0f, 0.5f, Hex("#FF1A1A") * 3f);
            skylightMat = Plain("Skylight", Hex("#1E1A14"), 0.2f, 0.9f, Hex("#FFB86A") * 0.38f);
            skylightCoolMat = Plain("SkylightCool", Hex("#141A24"), 0.2f, 0.9f, Hex("#AFC8F0") * 0.3f);
            paintWhiteMat = Plain("PaintWhite", Hex("#D8D8D0"), 0f, 0.55f, Hex("#FFFFFF") * 0.35f);
            paintYellowMat = Plain("PaintYellow", Hex("#D9A93A"), 0f, 0.55f, Hex("#FFC040") * 0.3f);
            lampHeadMat = Plain("LampHead", Hex("#C8D2E6"), 0f, 0.5f, Hex("#DCE6FF") * 1.1f);
        }

        // ---- Skyline Zone daylight materials
        static Material[] glassDayMats, officeDayMats;
        static Material roofUnitDayMat;
        static Material stoneDayMat, roofDayMat, roofDayMat2, deckMat, parapetMat, railMat, pillarMat;

        static void EnsureDayMaterials()
        {
            EnsureMaterials();
            if (glassDayMats != null && glassDayMats[0] != null) return;
            glassDayMats = new[]
            {
                Pbr("FacadeGlassDay", MakeGlassDay(21, Hex("#16283A"), Hex("#8FB0C8")), Color.white),
                Pbr("FacadeGlassDay2", MakeGlassDay(22, Hex("#1E2A3A"), Hex("#AFC0D2")), Color.white),
                Pbr("FacadeGlassDay3", MakeGlassDay(23, Hex("#183236"), Hex("#9CBCC0")), Color.white),
            };
            officeDayMats = new[]
            {
                Pbr("FacadeOfficeDay", MakeOfficeDay(24, Hex("#C9CACB"), Hex("#E2E2E0")), Color.white),
                Pbr("FacadeOfficeDay2", MakeOfficeDay(25, Hex("#9EA4AB"), Hex("#B8BDC3")), Color.white),
            };
            stoneDayMat = Pbr("FacadeStoneDay", MakeOfficeDay(26, Hex("#8E949B"), Hex("#A7ADB4")), Color.white);
            roofDayMat = Pbr("RoofDay", MakeGravelDay(27, Hex("#9A9C9E"), Hex("#B4B6B8")), Color.white);
            roofDayMat2 = Pbr("RoofDay2", MakeGravelDay(28, Hex("#5E6266"), Hex("#73777C")), Color.white);
            roofUnitDayMat = Plain("RoofUnitDay", Hex("#C8CCD0"), 0.5f, 0.5f);
            deckMat = Pbr("BridgeDeck", MakeAsphalt(29, false), Color.white);
            parapetMat = Plain("Parapet", Hex("#B8B1A6"), 0f, 0.3f);
            railMat = Plain("Rail", Hex("#9AA2AC"), 0.85f, 0.7f);
            pillarMat = Plain("Pillar", Hex("#8E867E"), 0f, 0.35f);
        }

        static Material SignMat(Color c)
        {
            if (signCache.TryGetValue(c, out var m) && m != null) return m;
            return signCache[c] = Plain("Sign", c, 0f, 0.6f, c * 2.6f);
        }

        static Material NewLit()
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            return new Material(sh);
        }

        static Material Plain(string name, Color c, float metal, float smooth, Color emission = default)
        {
            var m = NewLit();
            m.name = name;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", smooth);
            if (emission.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        }

        class TexSet { public Texture2D albedo, normal, mask, emission; public float emissionStrength = 1f; }

        static Material Pbr(string name, TexSet t, Color tint)
        {
            var m = NewLit();
            m.name = name;
            m.SetTexture("_BaseMap", t.albedo);
            m.mainTexture = t.albedo;
            m.SetColor("_BaseColor", tint);
            m.SetTexture("_BumpMap", t.normal);
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", t.mask);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Metallic", 1f);
            m.SetFloat("_Smoothness", 1f);
            if (t.emission != null)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", t.emission);
                m.SetColor("_EmissionColor", Color.white * t.emissionStrength);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        }

        // pixel buffers -> textures
        class PixelCanvas
        {
            public readonly int w, h;
            public readonly Color[] alb, emi;
            public readonly float[] height, metal, smooth;
            public bool hasEmission;
            public PixelCanvas(int w, int h) { this.w = w; this.h = h; alb = new Color[w * h]; emi = new Color[w * h]; height = new float[w * h]; metal = new float[w * h]; smooth = new float[w * h]; }

            public TexSet Build(float normalStrength, float emissionStrength = 1f)
            {
                var ts = new TexSet { emissionStrength = emissionStrength };
                ts.albedo = NewTex(false); ts.albedo.SetPixels(alb); ts.albedo.Apply(true);
                var np = new Color[w * h];
                var mp = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = height[y * w + (x + 1) % w] - height[y * w + (x - 1 + w) % w];
                        float dy = height[((y + 1) % h) * w + x] - height[((y - 1 + h) % h) * w + x];
                        var nv = new Vector3(-dx * normalStrength, -dy * normalStrength, 1f).normalized;
                        np[y * w + x] = new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f);
                        mp[y * w + x] = new Color(metal[y * w + x], 0f, 0f, smooth[y * w + x]);
                    }
                ts.normal = NewTex(true); ts.normal.SetPixels(np); ts.normal.Apply(true);
                ts.mask = NewTex(true); ts.mask.SetPixels(mp); ts.mask.Apply(true);
                if (hasEmission) { ts.emission = NewTex(false); ts.emission.SetPixels(emi); ts.emission.Apply(true); }
                return ts;
            }

            Texture2D NewTex(bool linear) =>
                new Texture2D(w, h, TextureFormat.RGBA32, true, linear) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };

            public void Set(int x, int y, Color a, float hgt, float met, float sm)
            {
                int i = y * w + x; alb[i] = a; height[i] = hgt; metal[i] = met; smooth[i] = sm;
            }
        }

        // ---- noise
        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + s * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>Tileable value noise: u, v in [0,1), 'period' cells across the texture.</summary>
        static float VNoise(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int Wr(int a) => ((a % period) + period) % period;
            float a00 = Hash(Wr(x0), Wr(y0), seed), a10 = Hash(Wr(x0 + 1), Wr(y0), seed);
            float a01 = Hash(Wr(x0), Wr(y0 + 1), seed), a11 = Hash(Wr(x0 + 1), Wr(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a00, a10, fx), Mathf.Lerp(a01, a11, fx), fy);
        }

        static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int o = 0; o < octaves; o++) { sum += VNoise(u, v, period, seed + o * 31) * amp; norm += amp; amp *= 0.5f; period *= 2; }
            return sum / norm;
        }

        static readonly Color[] WarmLights = { Hex("#FFD9A0"), Hex("#FFC27A"), Hex("#FFE4BC"), Hex("#FFB45E") };
        static readonly Color[] CoolLights = { Hex("#CFE2FF"), Hex("#A8C8FF"), Hex("#E8F0FF") };

        static Color LitColor(float r, float r2) =>
            r2 < 0.22f ? CoolLights[(int)(r * CoolLights.Length) % CoolLights.Length] : WarmLights[(int)(r * WarmLights.Length) % WarmLights.Length];

        /// <summary>Concrete office: 2 x 2 units, 8 floors x 8 windows, recessed windows with frames, ~18% lit.</summary>
        static TexSet MakeOffice(int seed)
        {
            const int T = 512, F = 64;
            var c = new PixelCanvas(T, T) { hasEmission = true };
            Color c1 = Hex("#3B3E44"), c2 = Hex("#50525A");
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    int fx = x % F, fy = y % F, col = x / F, fl = y / F;
                    float n = Fbm(u, v, 8, 4, seed) * 0.7f + Hash(x, y, seed) * 0.3f;
                    var conc = Color.Lerp(c1, c2, n);
                    bool inWin = fx >= 10 && fx < 54 && fy >= 16 && fy < 54;
                    bool frame = inWin && (fx < 13 || fx >= 51 || fy < 19 || fy >= 51);
                    if (!inWin)
                    {
                        // rain streaks below windows, darker floor slab line
                        float streak = (fx >= 12 && fx < 52 && fy < 16) ? Mathf.Lerp(0.82f, 1f, fy / 16f) * (0.9f + 0.1f * Hash(x, 0, seed)) : 1f;
                        if (fy >= 58 && fy < 60) streak *= 0.8f;
                        c.Set(x, y, conc * streak, 1f, 0f, 0.18f + n * 0.12f);
                    }
                    else if (frame) c.Set(x, y, Hex("#2A2D33"), 0.85f, 0.6f, 0.5f);
                    else
                    {
                        float r = Hash(col, fl, seed + 5), r2 = Hash(col, fl, seed + 9);
                        bool lit = r < 0.32f;
                        if (lit)
                        {
                            var lc = LitColor(r * 3f, r2);
                            bool blinds = Hash(col, fl, seed + 13) < 0.45f;
                            float k = blinds && (fy % 5 == 0) ? 0.45f : 1f;
                            float inner = 0.75f + 0.25f * Mathf.Sin((fx - 10) / 44f * Mathf.PI);
                            c.Set(x, y, lc * 0.35f * k, 0.55f, 0.1f, 0.6f);
                            c.emi[y * T + x] = lc * (0.55f + 0.45f * Hash(col, fl, seed + 2)) * k * inner;
                        }
                        else
                        {
                            float sky = (fy - 19) / 32f;
                            var g = Color.Lerp(Hex("#0B121C"), Hex("#1C2A3C"), sky * 0.8f + Hash(col, fl, seed) * 0.2f);
                            c.Set(x, y, g, 0.55f, 0.35f, 0.94f);
                        }
                    }
                }
            return c.Build(6f, 1.1f);
        }

        /// <summary>Glass curtain wall: 2 x 2 units, mullions every 1/8 unit, dark spandrel per floor, lit office runs.</summary>
        static TexSet MakeGlass(int seed)
        {
            const int T = 512, F = 64, P = 32;
            var c = new PixelCanvas(T, T) { hasEmission = true };
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    int fx = x % P, fy = y % F, seg = x / (P * 4), fl = y / F;
                    if (fy < 9)
                    {
                        float n = Hash(x, y, seed) * 0.15f;
                        c.Set(x, y, Hex("#1C1F25") * (0.9f + n), 0.95f, 0.6f, 0.45f);
                    }
                    else if (fx < 2)
                        c.Set(x, y, Hex("#3A3E46"), 1f, 0.8f, 0.6f);
                    else
                    {
                        float r = Hash(seg, fl, seed + 4), r2 = Hash(seg, fl, seed + 8);
                        bool lit = r < 0.4f;
                        // big soft reflection gradient so the glass does not look flat
                        float refl = Fbm(u * 0.5f + v * 0.25f, v, 2, 2, seed) * 0.6f + (fy - 9) / 55f * 0.4f;
                        var glass = Color.Lerp(Hex("#08141E"), Hex("#1A3044"), refl);
                        if (lit)
                        {
                            var lc = LitColor(r * 3f, r2 * 0.6f);
                            float desk = fy < 26 ? 0.55f : 1f;   // darker band = desks / furniture
                            float colVar = 0.7f + 0.3f * Hash(x / P, fl, seed + 3);
                            c.Set(x, y, Color.Lerp(glass, lc * 0.4f, 0.6f), 0.6f, 0.3f, 0.85f);
                            c.emi[y * T + x] = lc * 0.55f * desk * colVar;
                        }
                        else c.Set(x, y, glass, 0.6f, 0.5f, 0.96f);
                    }
                }
            return c.Build(5f, 1f);
        }

        /// <summary>Brick flats: 2 x 2 units, brick courses + mortar, smaller windows with light frames, ~28% lit.</summary>
        static TexSet MakeBrick(int seed)
        {
            const int T = 512, F = 64;
            var c = new PixelCanvas(T, T) { hasEmission = true };
            Color[] bricks = { Hex("#5A3328"), Hex("#4A2A22"), Hex("#62392C"), Hex("#523026") };
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    int fx = x % F, fy = y % F, col = x / F, fl = y / F;
                    bool inWin = fx >= 18 && fx < 46 && fy >= 14 && fy < 50;
                    bool frame = inWin && (fx < 21 || fx >= 43 || fy < 17 || fy >= 47 || (fy >= 30 && fy < 32));
                    bool sill = !inWin && fx >= 16 && fx < 48 && fy >= 11 && fy < 14;
                    if (sill) { c.Set(x, y, Hex("#6E6A64"), 1.05f, 0f, 0.3f); continue; }
                    if (!inWin)
                    {
                        int row = y / 8;
                        int bx = (x + (row % 2) * 12) / 24;
                        bool mortar = y % 8 == 0 || (x + (row % 2) * 12) % 24 == 0;
                        float n = Fbm(u, v, 16, 3, seed) * 0.5f + Hash(x, y, seed) * 0.2f;
                        var bc = bricks[(int)(Hash(bx, row, seed) * bricks.Length) % bricks.Length] * (0.85f + n * 0.4f);
                        float dirt = Mathf.Lerp(0.75f, 1f, Fbm(u, v * 0.5f, 4, 2, seed + 1));
                        if (mortar) c.Set(x, y, Hex("#3E3C3A") * dirt, 0.8f, 0f, 0.15f);
                        else c.Set(x, y, bc * dirt, 1f, 0f, 0.22f);
                    }
                    else if (frame) c.Set(x, y, Hex("#8C8A84"), 0.9f, 0.2f, 0.4f);
                    else
                    {
                        float r = Hash(col, fl, seed + 5), r2 = Hash(col, fl, seed + 6);
                        if (r < 0.38f)
                        {
                            var lc = r2 < 0.12f ? Hex("#9FC8FF") : WarmLights[(int)(r * 40) % WarmLights.Length];
                            bool curtain = Hash(col, fl, seed + 7) < 0.5f && (fx < 26 || fx >= 38);
                            float k = curtain ? 0.5f : 1f;
                            c.Set(x, y, lc * 0.35f * k, 0.55f, 0f, 0.6f);
                            c.emi[y * T + x] = lc * 0.75f * k;
                        }
                        else c.Set(x, y, Color.Lerp(Hex("#0A0F16"), Hex("#18222E"), (fy - 17) / 30f), 0.55f, 0.3f, 0.93f);
                    }
                }
            return c.Build(7f, 1.05f);
        }

        /// <summary>Podium / low building: 2 units wide x 1 unit tall. Lit shops with sign bands at the bottom, offices above.</summary>
        static TexSet MakeShop(int seed)
        {
            const int TW = 512, TH = 256, SHOP = 128, GF = 72;
            var c = new PixelCanvas(TW, TH) { hasEmission = true };
            Color[] neon = { Hex("#FF2BD6"), Hex("#00D9FF"), Hex("#FF8A2A"), Hex("#A855F7"), Hex("#FFE14A"), Hex("#4DFF9A") };
            for (int y = 0; y < TH; y++)
                for (int x = 0; x < TW; x++)
                {
                    float u = (float)x / TW, v = (float)y / TH;
                    int shop = x / SHOP, sx = x % SHOP;
                    float n = Fbm(u, v, 8, 3, seed) * 0.6f + Hash(x, y, seed) * 0.4f;
                    var conc = Color.Lerp(Hex("#2E3036"), Hex("#44464C"), n);
                    if (y < GF)
                    {
                        float r = Hash(shop, 0, seed), r2 = Hash(shop, 1, seed), r3 = Hash(shop, 2, seed);
                        bool pillar = sx < 7 || sx >= SHOP - 3;
                        if (pillar) { c.Set(x, y, conc * 0.8f, 1f, 0f, 0.25f); continue; }
                        if (y >= 56 && y < 69)
                        {
                            // sign band
                            bool lit = r2 < 0.65f;
                            var sc = neon[(int)(r3 * neon.Length) % neon.Length];
                            bool letters = lit && y >= 59 && y < 66 && sx > 20 && sx < SHOP - 20 && ((sx / 4) % 3 != 0);
                            c.Set(x, y, letters ? sc * 0.5f : Hex("#141518"), 0.95f, 0.3f, 0.5f);
                            if (letters) c.emi[y * TW + x] = sc * 1.8f;
                            continue;
                        }
                        if (y >= 69) { c.Set(x, y, Hex("#1E2024"), 1f, 0.5f, 0.4f); continue; }
                        bool door = r3 < 0.6f && sx >= 14 && sx < 34;
                        bool winArea = y >= 5 && y < 52;
                        if (!winArea) { c.Set(x, y, conc * 0.7f, 0.95f, 0f, 0.3f); continue; }
                        bool lit2 = r < 0.8f;
                        bool mullion = (sx % 38) < 2;
                        if (mullion) { c.Set(x, y, Hex("#1A1C20"), 0.9f, 0.7f, 0.5f); continue; }
                        if (door) { c.Set(x, y, Hex("#0E1014"), 0.6f, 0.4f, 0.8f); if (lit2) c.emi[y * TW + x] = Hex("#FFD49A") * 0.25f; continue; }
                        if (lit2)
                        {
                            var lc = r2 < 0.15f ? neon[(int)(r * 30) % neon.Length] : (r2 < 0.45f ? Hex("#F2F5FF") : Hex("#FFD49A"));
                            // interior: shelves / products as darker blobs
                            float shelf = (y % 12 < 2 || Hash(x / 6, y / 6, seed + shop) < 0.18f) ? 0.55f : 1f;
                            c.Set(x, y, lc * 0.4f * shelf, 0.6f, 0.2f, 0.85f);
                            c.emi[y * TW + x] = lc * 1.15f * shelf;
                        }
                        else c.Set(x, y, Hex("#0B1018"), 0.6f, 0.4f, 0.95f);
                    }
                    else
                    {
                        // upper floors: 46 px floors, small windows
                        int fy = (y - GF) % 46, fl = (y - GF) / 46, fx = x % 64, col = x / 64;
                        bool win = fx >= 12 && fx < 52 && fy >= 12 && fy < 38;
                        if (!win) c.Set(x, y, conc, 1f, 0f, 0.2f + n * 0.1f);
                        else
                        {
                            float r = Hash(col, fl, seed + 11);
                            if (r < 0.22f) { var lc = WarmLights[(int)(r * 50) % WarmLights.Length]; c.Set(x, y, lc * 0.3f, 0.6f, 0f, 0.6f); c.emi[y * TW + x] = lc * 0.6f; }
                            else c.Set(x, y, Hex("#0D141E"), 0.6f, 0.3f, 0.92f);
                        }
                    }
                }
            return c.Build(5f, 1f);
        }

        /// <summary>Roof gravel / tar: 2 x 2 units.</summary>
        /// <summary>Daylight glass curtain wall (2 x 2 units): sky-reflecting panels (dark at the bottom, light toward the top),
        /// light metal mullions every 1/8 unit, a spandrel band per floor, a few warm sun glints.</summary>
        static TexSet MakeGlassDay(int seed, Color deep, Color sky)
        {
            const int T = 512, F = 64, P = 32;
            var c = new PixelCanvas(T, T) { hasEmission = true };
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    int fx = x % P, fy = y % F, col = x / P, fl = y / F;
                    if (fy < 8) { c.Set(x, y, Color.Lerp(deep, Hex("#1E2328"), 0.6f) * (0.95f + Hash(x, y, seed) * 0.1f), 0.95f, 0.5f, 0.55f); continue; }
                    if (fx < 2) { c.Set(x, y, Hex("#9AA1A9"), 1f, 0.9f, 0.7f); continue; }
                    // big soft reflection of the sky and clouds + small panel-to-panel tilt differences
                    float cloud = Fbm(u * 0.5f + 0.3f, v * 0.5f, 2, 3, seed);
                    float tilt = (Hash(col, fl, seed) - 0.5f) * 0.18f;
                    float k = Mathf.Clamp01(0.35f + (fy - 8) / 56f * 0.35f + cloud * 0.35f + tilt);
                    var g = Color.Lerp(deep, sky, k);
                    c.Set(x, y, g, 0.6f, 0.55f, 0.95f);
                    if (Hash(col, fl, seed + 7) < 0.05f && fy > 30) c.emi[y * T + x] = Hex("#FFD2A0") * 0.35f;
                }
            return c.Build(4f, 1f);
        }

        /// <summary>Daylight concrete / stone office (2 x 2 units): light wall, recessed dark reflective windows, soft dirt streaks.</summary>
        static TexSet MakeOfficeDay(int seed, Color wallA, Color wallB)
        {
            const int T = 512, F = 64;
            var c = new PixelCanvas(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    int fx = x % F, fy = y % F, col = x / F, fl = y / F;
                    float n = Fbm(u, v, 8, 4, seed) * 0.6f + Hash(x, y, seed) * 0.4f;
                    var wall = Color.Lerp(wallA, wallB, n);
                    bool win = fx >= 12 && fx < 52 && fy >= 16 && fy < 52;
                    bool frame = win && (fx < 14 || fx >= 50 || fy < 18 || fy >= 50 || (fx >= 31 && fx < 33));
                    if (!win)
                    {
                        float streak = (fx >= 14 && fx < 50 && fy < 16) ? Mathf.Lerp(0.86f, 1f, fy / 16f) : 1f;
                        if (fy >= 58 && fy < 61) streak *= 0.88f;
                        c.Set(x, y, wall * streak, 1f, 0f, 0.2f + n * 0.1f);
                    }
                    else if (frame) c.Set(x, y, Hex("#4A4E54"), 0.85f, 0.7f, 0.55f);
                    else
                    {
                        float sky = (fy - 18) / 32f;
                        var g = Color.Lerp(Hex("#1F2A36"), Hex("#6E8696"), sky * 0.7f + Hash(col, fl, seed) * 0.3f);
                        c.Set(x, y, g, 0.55f, 0.45f, 0.94f);
                    }
                }
            return c.Build(6f);
        }

        static TexSet MakeGravelDay(int seed, Color a, Color b)
        {
            const int T = 256;
            var c = new PixelCanvas(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    float big = Fbm(u, v, 4, 3, seed), fine = Hash(x, y, seed);
                    c.Set(x, y, Color.Lerp(a, b, fine * 0.6f + big * 0.4f), fine * 0.6f + big * 0.4f, 0f, 0.15f);
                }
            return c.Build(3f);
        }

        static TexSet MakeGravel(int seed)
        {
            const int T = 256;
            var c = new PixelCanvas(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    float big = Fbm(u, v, 4, 3, seed);
                    float fine = Hash(x, y, seed);
                    float tar = big < 0.38f ? 0.7f : 1f;
                    var col = (seed % 2 == 0 ? Color.Lerp(Hex("#34363A"), Hex("#46484C"), fine * 0.6f + big * 0.4f) : Color.Lerp(Hex("#26282C"), Hex("#34363B"), fine * 0.5f + big * 0.5f)) * tar;
                    c.Set(x, y, col, fine * 0.6f + big * 0.4f, 0f, tar < 1f ? 0.45f : 0.12f);
                }
            return c.Build(3f);
        }

        /// <summary>Wet asphalt: 4 x 4 units. Aggregate speckle, wet patches (darker + mirror-like), slight bumps.</summary>
        static TexSet MakeAsphalt(int seed) => MakeAsphalt(seed, true);

        static TexSet MakeAsphalt(int seed, bool wetPatches)
        {
            const int T = 512;
            var c = new PixelCanvas(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    float big = Fbm(u, v, 4, 4, seed);
                    float wet = wetPatches ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.62f, Fbm(u, v, 3, 3, seed + 50))) : 0f;
                    float speck = Hash(x, y, seed);
                    var col = Color.Lerp(Hex("#2E3137"), Hex("#42454C"), big * 0.6f + speck * 0.4f);
                    if (speck > 0.93f) col *= 1.5f;
                    col *= Mathf.Lerp(1f, 0.72f, wet);
                    float hgt = speck * 0.5f + big * 0.5f;
                    hgt = Mathf.Lerp(hgt, 0.4f, wet);   // water fills the bumps
                    c.Set(x, y, col, hgt, 0f, Mathf.Lerp(0.38f, 0.95f, wet));
                }
            return c.Build(2.5f);
        }

        /// <summary>Sidewalk slabs: 1 x 1 unit, 4 x 4 slabs with recessed seams.</summary>
        static TexSet MakeSidewalk(int seed)
        {
            const int T = 256, SL = 64;
            var c = new PixelCanvas(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float u = (float)x / T, v = (float)y / T;
                    bool seam = x % SL < 2 || y % SL < 2;
                    float slab = Hash(x / SL, y / SL, seed);
                    float n = Fbm(u, v, 8, 3, seed) * 0.5f + Hash(x, y, seed) * 0.3f + slab * 0.2f;
                    var col = Color.Lerp(Hex("#5C5F65"), Hex("#7A7D83"), n);
                    float wet = Fbm(u, v, 2, 2, seed + 9) > 0.55f ? 1f : 0f;
                    col *= Mathf.Lerp(1f, 0.75f, wet);
                    if (seam) c.Set(x, y, col * 0.55f, 0.4f, 0f, 0.3f);
                    else c.Set(x, y, col, 1f, 0f, Mathf.Lerp(0.3f, 0.75f, wet));
                }
            return c.Build(4f);
        }

        // ------------------------------------------------------------------ helpers

        static Color Hex(string h) => Visuals.Hex(h);
        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static System.Random Rng(Vector2Int c, int salt) => new System.Random((c.x * 73856093) ^ (c.y * 19349663) ^ (salt * 83492791));
    }
}
