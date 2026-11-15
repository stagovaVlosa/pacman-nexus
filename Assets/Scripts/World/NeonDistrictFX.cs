using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Level 1 "rainy night market" signature effects (see the project plan doc):
    ///  - rain around Pac-Man (stretched streaks, follows him, hidden during the intro cinematic),
    ///  - puddles on some street cells (black mirror-like discs that pick up the neon from the reflection probe),
    ///  - steam rising from a few street grates.
    /// Added 12 Nov (user: "make Level 1 more interesting"):
    ///  - night hover traffic with pink/cyan light trails over the skyline band,
    ///  - billboards that scroll and flicker (animated ads),
    ///  - glowing zebra crossings at the street crossings,
    ///  - a holographic ring beacon above the Generator plaza.
    /// Purely visual; nothing here touches the grid or the gameplay.
    /// </summary>
    public class NeonDistrictFX : MonoBehaviour
    {
        ParticleSystem rain;
        Renderer rainRenderer;
        Transform[] beaconRings;
        Material[] beaconMats;
        static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

        public static void Build(LevelBuilder level, Transform parent)
        {
            var root = new GameObject("NeonDistrictFX");
            root.transform.SetParent(parent, false);
            var fx = root.AddComponent<NeonDistrictFX>();
            fx.CreateRain();
            CreatePuddlesAndSteam(level, root.transform);
            CreateCrossings(level, root.transform);
            fx.CreateBeacon(level);
            var traffic = new GameObject("NightTraffic").AddComponent<FlyingTraffic>();
            traffic.transform.SetParent(root.transform, false);
            traffic.Setup(level, level.skylineBand, true);
        }

        /// <summary>Zebra stripes on the street cells next to every crossing (one mesh, faint glow).</summary>
        static void CreateCrossings(LevelBuilder level, Transform parent)
        {
            bool Crossing(Vector2Int c) =>
                level.IsWalkable(c)
                && (level.IsWalkable(c + Vector2Int.left) || level.IsWalkable(c + Vector2Int.right))
                && (level.IsWalkable(c + Vector2Int.up) || level.IsWalkable(c + Vector2Int.down));

            var mb = new MeshBuilder();
            var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            float cs = level.cellSize;
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!Crossing(c)) continue;
                    foreach (var d in dirs)
                    {
                        var n = c + d;
                        if (!level.IsWalkable(n) || Crossing(n)) continue;
                        var center = level.CellToWorld(n) + Vector3.up * 0.011f;
                        // Bars run along the street (d), side by side across it
                        bool alongX = d.x != 0;
                        for (int i = 0; i < 5; i++)
                        {
                            float off = (-0.32f + i * 0.16f) * cs;
                            var pos = center + (alongX ? new Vector3(0f, 0f, off) : new Vector3(off, 0f, 0f));
                            var size = alongX ? new Vector3(0.42f * cs, 0.004f, 0.075f * cs) : new Vector3(0.075f * cs, 0.004f, 0.42f * cs);
                            mb.Box(pos, size, Quaternion.identity);
                        }
                    }
                }
            var mat = Visuals.GetMaterial(Visuals.Hex("#C9D6FF"), Visuals.Hex("#8FB0FF"), 0.35f);
            Visuals.MeshPart("ZebraCrossings", parent, mb.ToMesh("ZebraCrossings"), mat);
        }

        /// <summary>Three holographic rings slowly turning above the Generator plaza (seen from anywhere nearby).</summary>
        void CreateBeacon(LevelBuilder level)
        {
            var root = new GameObject("PlazaBeacon").transform;
            root.SetParent(transform, false);
            root.position = level.CellToWorld(level.GeneratorCell);
            Color[] colors = { Visuals.Hex("#FF2BD6"), Visuals.Cyan, Visuals.Hex("#8A5CFF") };
            float[] radius = { 1.5f, 1.15f, 0.8f };
            float[] height = { 2.4f, 3.0f, 3.5f };
            beaconRings = new Transform[3];
            beaconMats = new Material[3];
            for (int i = 0; i < 3; i++)
            {
                var mb = new MeshBuilder();
                mb.Tube(MeshBuilder.Circle(radius[i], 64, Vector3.zero, MeshBuilder.Flat), 0.03f, 5, true);
                // little "data blocks" on the ring
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI * 2f / 8f;
                    mb.Box(new Vector3(Mathf.Cos(a) * radius[i], 0f, Mathf.Sin(a) * radius[i]), Vector3.one * 0.09f, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
                }
                beaconMats[i] = Visuals.Glow(colors[i], 1.6f, 0f, null, true);
                var ring = Visuals.MeshPart("BeaconRing", root, mb.ToMesh("BeaconRing"), beaconMats[i]).transform;
                ring.localPosition = Vector3.up * height[i];
                beaconRings[i] = ring;
            }
            // Glowing circle on the plaza floor
            var floor = new MeshBuilder();
            floor.Tube(MeshBuilder.Circle(2.1f, 96, Vector3.zero, MeshBuilder.Flat), 0.035f, 5, true);
            floor.Tube(MeshBuilder.Circle(1.75f, 96, Vector3.zero, MeshBuilder.Flat), 0.015f, 4, true);
            Visuals.MeshPart("PlazaCircle", root, floor.ToMesh("PlazaCircle"), Visuals.Glow(Visuals.Cyan, 1.2f, 0f, null, true))
                .transform.localPosition = Vector3.up * 0.02f;
        }

        static Material ParticleMaterial(Texture2D tex, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            m.color = color;
            // transparent, alpha blended
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        void CreateRain()
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(80f, 10f, 0f);   // shape forward = down, slightly slanted by the wind
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = rain.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = 0.75f;
            main.startSpeed = 16f;
            main.startSize = 0.03f;
            main.maxParticles = 3000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.8f, 0.9f, 1f, 0.28f);
            main.gravityModifier = 0.3f;

            var emission = rain.emission;
            emission.rateOverTime = 800f;   // light drizzle: heavier rain made the level feel overwhelming

            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 22f, 0.1f);

            rainRenderer = go.GetComponent<ParticleSystemRenderer>();
            var pr = (ParticleSystemRenderer)rainRenderer;
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.lengthScale = 2.5f;
            pr.velocityScale = 0.05f;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;
            pr.sharedMaterial = ParticleMaterial(null, new Color(0.8f, 0.9f, 1f, 0.28f));

            rain.Play();
        }

        void LateUpdate()
        {
            if (rain == null) return;
            // No rain inside the intro lab; around Pac-Man (a bit ahead, towards the camera view) otherwise
            bool show = !IntroSequence.Active;
            if (rainRenderer.enabled != show) rainRenderer.enabled = show;
            var player = PlayerMovement.Instance;
            if (player != null) rain.transform.position = player.transform.position + new Vector3(0f, 9f, 2f);

            float t = Time.time;
            // Beacon rings: turn in opposite directions with a slight wobble, brightness pulses one after another
            if (beaconRings != null)
                for (int i = 0; i < beaconRings.Length; i++)
                {
                    beaconRings[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f + i) * 8f, t * (i % 2 == 0 ? 30f : -45f), Mathf.Cos(t * 0.6f + i) * 8f);
                    beaconMats[i].SetFloat("_Intensity", 1.1f + 0.7f * Mathf.Max(0f, Mathf.Sin(t * 2.2f - i * 1.1f)));
                }

            // Animated ads: slow sideways scroll, with a short flicker now and then
            int n = 0;
            foreach (var m in CityVisuals.BillboardMaterials)
            {
                if (m == null) continue;
                n++;
                float speed = 0.06f + 0.03f * (n % 3);
                m.SetVector(BaseMapST, new Vector4(1f, 1f, Mathf.Repeat(t * speed * (n % 2 == 0 ? 1f : -1f), 1f), 0f));
                bool flicker = Mathf.PerlinNoise(t * 3f, n * 7.3f) < 0.18f;
                m.SetColor("_EmissionColor", Color.white * (flicker ? 0.5f : 2.2f));
            }
        }

        static void CreatePuddlesAndSteam(LevelBuilder level, Transform parent)
        {
            var rng = new System.Random(2087);
            var puddleMat = Visuals.Lit(Visuals.Hex("#05060C"), 0.85f, 0.98f);
            var grateMat = Visuals.Lit(Visuals.Hex("#1A1D26"), 0.7f, 0.5f);
            var grateGlow = Visuals.GetMaterial(Visuals.Hex("#FF8A3C"), Visuals.Hex("#FF8A3C"), 1.4f);
            var steamTex = Visuals.GradientTexture(true);
            Material steamMat = null;
            int steam = 0;

            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!level.IsWalkable(c)) continue;
                    if (c == level.PlayerStart || c == level.GeneratorCell) continue;
                    double roll = rng.NextDouble();
                    var pos = level.CellToWorld(c);

                    if (roll < 0.11)
                    {
                        // Puddle: flat dark disc, slightly off-centre, not perfectly round
                        float d = 0.45f + (float)rng.NextDouble() * 0.45f;
                        var p = Visuals.Shape(PrimitiveType.Cylinder, "Puddle", parent,
                            Vector3.zero, new Vector3(d, 0.003f, d * (0.6f + (float)rng.NextDouble() * 0.4f)), puddleMat);
                        p.transform.position = pos + new Vector3((float)rng.NextDouble() * 0.3f - 0.15f, 0.012f, (float)rng.NextDouble() * 0.3f - 0.15f);
                        p.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
                        p.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    else if (roll > 0.985 && steam < 7)
                    {
                        steam++;
                        // Street grate with a warm glow under it, and soft steam rising
                        Visuals.Shape(PrimitiveType.Cube, "Grate", parent, pos + Vector3.up * 0.01f, new Vector3(0.42f, 0.01f, 0.42f), grateMat);
                        Visuals.Shape(PrimitiveType.Cube, "GrateGlow", parent, pos + Vector3.up * 0.005f, new Vector3(0.48f, 0.006f, 0.48f), grateGlow);
                        if (steamMat == null) steamMat = ParticleMaterial(steamTex, new Color(1f, 1f, 1f, 0.16f));
                        CreateSteam(parent, pos, steamMat);
                    }
                }
        }

        static void CreateSteam(Transform parent, Vector3 pos, Material mat)
        {
            var go = new GameObject("Steam");
            go.transform.SetParent(parent, false);
            go.transform.position = pos + Vector3.up * 0.05f;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // emit upwards
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 2.6f;
            main.startSpeed = 0.55f;
            main.startSize = 0.5f;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.85f, 0.85f, 1f, 0.16f);
            var emission = ps.emission;
            emission.rateOverTime = 7f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.15f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }
    }
}
