using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus
{
    /// <summary>
    /// Bug: a corrupted drone, in the same tech style as the Pac-Man robot (no face). The main color
    /// depends on the Bug type (Chaser red, Patroller purple, Blocker magenta).
    /// Broken dark armor plates over a glass sphere with a flickering core, a spinning belt
    /// with spikes, a blinking antenna, a hover engine underneath and orbiting glitch shards.
    /// Every few seconds the drone "glitches": it jumps and stretches for a moment (digital distortion).
    /// All sizes are in units of the radius (R = 1), the "Drone" child is scaled to the real radius.
    /// </summary>
    public class BugModel : MonoBehaviour
    {
        public float spinSpeed = 70f;  // degrees per second for the belt

        static readonly Color MetalMid = Visuals.Hex("#3A2E36");

        Color Red;   // main glow color of this Bug

        const float GroundY = 0.015f;

        Transform drone, rotor, groundGlow;
        Light glowLight;
        BugAI ai;
        Material lightMat, coreMat, tipMat, beamMat, groundMat;
        readonly List<Transform> shards = new List<Transform>();
        readonly List<Vector3> shardBase = new List<Vector3>();
        readonly List<Transform> particles = new List<Transform>();
        readonly List<float> particleSpeed = new List<float>();
        float radius, t, seed, nextGlitch, glitchLeft, lightBase;

        public static BugModel Create(Transform body, float radius, Color color)
        {
            var model = body.gameObject.AddComponent<BugModel>();
            model.radius = radius;
            model.Red = color;
            var Red = color;
            model.seed = Random.value * 100f;
            model.nextGlitch = Random.Range(0.8f, 2.5f);
            model.drone = new GameObject("Drone").transform;
            model.drone.SetParent(body, false);
            model.drone.localScale = Vector3.one * radius;

            // Per drone materials, so each Bug flickers on its own
            model.lightMat = Visuals.Lit(Red, 0f, 0.5f, Red, 2.2f);
            model.coreMat = Visuals.Lit(Color.Lerp(Red, Color.white, 0.2f), 0f, 0.5f, Red, 2.5f);
            model.tipMat = Visuals.Lit(Red, 0f, 0.5f, Red, 3f);

            var armor = Visuals.Lit(Color.Lerp(Visuals.Hex("#16141C"), Red, 0.12f), 0.75f, 0.6f);
            var metal = Visuals.Lit(MetalMid, 0.75f, 0.7f);
            model.BuildBody(armor, metal);
            model.BuildRotor(metal);
            model.BuildEngine(metal);
            model.BuildGlitchShards();
            return model;
        }

        void BuildBody(Material armor, Material metal)
        {
            // Glass sphere and core, visible through the gaps in the armor
            var inner = new MeshBuilder();
            inner.SpherePart(0.9f, 0f, Mathf.PI * 2f, 0f, Mathf.PI, 32, 20);
            Visuals.MeshPart("InnerGlass", drone, inner.ToMesh("Bug_Inner"), Visuals.Glass(Red, 0.35f, Red * 0.25f));

            var core = new MeshBuilder();
            core.Icosahedron(Vector3.zero, 0.32f, 1, true);
            Visuals.MeshPart("Core", drone, core.ToMesh("Bug_Core"), coreMat);
            var cage = new MeshBuilder();
            foreach (var (a, b) in MeshBuilder.IcoEdges(0.44f)) cage.Tube(new List<Vector3> { a, b }, 0.012f, 4, false);
            Visuals.MeshPart("CoreCage", drone, cage.ToMesh("Bug_Cage"), lightMat);

            // Armor: 6 plates in two bands, with gaps; two plates are missing and some stick out (damaged)
            var plates = new MeshBuilder();
            var rng = new System.Random(7);
            const float gap = 0.1f;
            for (int band = 0; band < 2; band++)
                for (int i = 0; i < 6; i++)
                {
                    if ((band == 0 && i == 2) || (band == 1 && i == 5)) continue;
                    float r = 1f + (float)rng.NextDouble() * 0.06f;
                    float th0 = band == 0 ? 0.2f : 1.67f;
                    float thLen = band == 0 ? 1.27f : Mathf.PI - 0.25f - 1.67f;
                    plates.SpherePart(r, i * Mathf.PI / 3f + gap / 2f, Mathf.PI / 3f - gap, th0, thLen, 10, 10);
                }
            var plateMesh = plates.ToMesh("Bug_Plates");
            Visuals.MeshPart("Armor", drone, plateMesh, armor, true);
            Visuals.MeshPart("ArmorRim", drone, plateMesh, Visuals.Glow(Red, 1.6f, 2.4f));

            var caps = new MeshBuilder();
            caps.SpherePart(1.01f, 0f, Mathf.PI * 2f, 0f, 0.2f, 32, 4);
            caps.SpherePart(1.01f, 0f, Mathf.PI * 2f, Mathf.PI - 0.25f, 0.25f, 32, 4);
            caps.Frustum(0.14f, 0.18f, 0.06f, 0.99f, 6, true);       // hex top plate
            caps.Frustum(0.015f, 0.025f, 0.3f, 1.15f, 6, false);     // antenna mast
            Visuals.MeshPart("Caps", drone, caps.ToMesh("Bug_Caps"), metal, true);

            var topLights = new MeshBuilder();
            topLights.Tube(MeshBuilder.Circle(0.13f, 6, new Vector3(0f, 1.025f, 0f), MeshBuilder.Flat), 0.015f, 6, true);
            Visuals.MeshPart("TopRing", drone, topLights.ToMesh("Bug_TopRing"), lightMat);
            var tip = new MeshBuilder();
            tip.Icosahedron(new Vector3(0f, 1.32f, 0f), 0.05f, 1, false);
            Visuals.MeshPart("AntennaTip", drone, tip.ToMesh("Bug_Tip"), tipMat);
        }

        /// <summary>Equator belt with four spikes; spins slowly like a rotor.</summary>
        void BuildRotor(Material metal)
        {
            rotor = new GameObject("Rotor").transform;
            rotor.SetParent(drone, false);

            var belt = new MeshBuilder();
            belt.Band(1.04f, 0.2f, 0f, 0f, Mathf.PI * 2f, 64);
            var lights = new MeshBuilder();
            lights.Band(1.05f, 0.025f, 0.1f, 0f, Mathf.PI * 2f, 64);
            lights.Band(1.05f, 0.025f, -0.1f, 0f, Mathf.PI * 2f, 64);
            for (int k = 0; k < 4; k++)
            {
                var d = MeshBuilder.DirAt(Mathf.PI / 4f + k * Mathf.PI / 2f);
                var rot = Quaternion.LookRotation(d);
                belt.Box(d * 1.22f, new Vector3(0.08f, 0.06f, 0.4f), rot);
                lights.Box(d * 1.44f, new Vector3(0.09f, 0.07f, 0.08f), rot);
                // Small vent between the spikes
                var v = MeshBuilder.DirAt(k * Mathf.PI / 2f);
                lights.Box(v * 1.055f, new Vector3(0.18f, 0.04f, 0.02f), Quaternion.LookRotation(-v));
            }
            Visuals.MeshPart("Belt", rotor, belt.ToMesh("Bug_Belt"), metal, true);
            Visuals.MeshPart("BeltLights", rotor, lights.ToMesh("Bug_BeltLights"), lightMat);
        }

        void BuildEngine(Material metal)
        {
            var housing = new MeshBuilder();
            housing.Frustum(0.3f, 0.38f, 0.12f, -0.95f, 32, true);
            Visuals.MeshPart("EngineHousing", drone, housing.ToMesh("Bug_Housing"), metal, true);

            var ring = new MeshBuilder();
            ring.Tube(MeshBuilder.Circle(0.3f, 48, new Vector3(0f, -1.02f, 0f), MeshBuilder.Flat), 0.03f, 8, true);
            ring.Disc(new Vector3(0f, -1.025f, 0f), 0.25f, Vector3.down, 32);
            Visuals.MeshPart("EngineRing", drone, ring.ToMesh("Bug_EngineRing"), lightMat);

            var beam = new MeshBuilder();
            beam.Frustum(0.05f, 0.3f, 0.38f, -1.02f - 0.19f, 32, false);
            beamMat = Visuals.Glow(Red, 0.4f, 0f, Visuals.GradientTexture(false), true);
            Visuals.MeshPart("Beam", drone, beam.ToMesh("Bug_Beam"), beamMat);

            var spark = Visuals.Glow(Color.Lerp(Red, Color.white, 0.3f), 1.5f, 0f);
            for (int i = 0; i < 12; i++)
            {
                var p = Visuals.Shape(PrimitiveType.Sphere, "Particle", drone,
                    new Vector3(Random.Range(-0.3f, 0.3f), -1.1f - Random.value * 0.4f, Random.Range(-0.3f, 0.3f)),
                    Vector3.one * 0.05f, spark);
                p.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                particles.Add(p.transform);
                particleSpeed.Add(0.4f + Random.value * 0.6f);
            }

            groundMat = Visuals.Glow(Red, 0.8f, 0f, Visuals.GradientTexture(true), true);
            var glow = Visuals.Shape(PrimitiveType.Quad, "GroundGlow", transform, Vector3.zero, Vector3.one * radius * 3f, groundMat);
            glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            glow.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            groundGlow = glow.transform;
        }

        /// <summary>Small broken pieces that orbit the drone and jump around during a glitch.</summary>
        void BuildGlitchShards()
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                var pos = new Vector3(Mathf.Cos(a) * 1.4f, Random.Range(-0.4f, 0.5f), Mathf.Sin(a) * 1.4f);
                var s = Visuals.Shape(PrimitiveType.Cube, "Shard", drone, pos, new Vector3(0.14f, 0.025f, 0.08f), lightMat);
                s.transform.localRotation = Random.rotation;
                s.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                shards.Add(s.transform);
                shardBase.Add(pos);
            }
        }

        void Start()
        {
            // The point light is a sibling of the body (created by LevelBuilder)
            if (transform.parent != null) glowLight = transform.parent.GetComponentInChildren<Light>();
            if (glowLight != null) lightBase = glowLight.intensity;
            ai = GetComponentInParent<BugAI>();
        }

        /// <summary>Unsteady light level: slow noise plus short random dropouts, like a damaged circuit.</summary>
        float Flicker(float offset)
        {
            float f = 0.45f + Mathf.PerlinNoise(t * 9f, seed + offset) * 0.9f;
            return Random.value < 0.04f ? f * 0.15f : f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;

            // Frozen by an Energy Blast: lights turn cyan and steady, the rotor and glitches stop
            bool frozen = ai != null && ai.IsFrozen;
            if (!frozen) rotor.Rotate(0f, spinSpeed * dt, 0f, Space.Self);
            Color c = frozen ? Visuals.Cyan : Red;

            float boost = glitchLeft > 0f ? 1.6f : 1f;
            lightMat.SetColor("_EmissionColor", c * (2.2f * (frozen ? 0.8f : Flicker(0f)) * boost));
            coreMat.SetColor("_EmissionColor", c * (2.5f * (frozen ? 0.8f : Flicker(10f)) * boost));
            tipMat.SetColor("_EmissionColor", Red * (Mathf.Repeat(t, 0.8f) < 0.15f ? 5f : 0.3f));
            beamMat.SetFloat("_Intensity", 0.4f * Flicker(20f));
            groundMat.SetFloat("_Intensity", 0.8f * Flicker(30f));
            if (glowLight != null) glowLight.intensity = lightBase * Flicker(0f) * boost;

            // Shards orbit slowly
            for (int i = 0; i < shards.Count; i++)
            {
                shards[i].localPosition = Quaternion.Euler(0f, -t * 40f, 0f) * shardBase[i];
                shards[i].Rotate(90f * dt, 60f * dt, 0f, Space.Self);
            }

            if (!frozen) UpdateGlitch(dt);

            float groundLocal = (GroundY - drone.position.y) / drone.lossyScale.y;
            for (int i = 0; i < particles.Count; i++)
            {
                var pos = particles[i].localPosition;
                pos.y -= dt * particleSpeed[i];
                if (pos.y < groundLocal)
                    pos = new Vector3(Random.Range(-0.3f, 0.3f), -1.08f, Random.Range(-0.3f, 0.3f));
                particles[i].localPosition = pos;
            }
        }

        void UpdateGlitch(float dt)
        {
            if (glitchLeft > 0f)
            {
                glitchLeft -= dt;
                if (glitchLeft > 0f)
                {
                    // Jump and stretch a little every frame (digital distortion)
                    drone.localPosition = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.02f, 0.02f), Random.Range(-0.05f, 0.05f));
                    drone.localScale = radius * new Vector3(Random.Range(0.92f, 1.12f), Random.Range(0.9f, 1.08f), Random.Range(0.92f, 1.12f));
                    foreach (var s in shards) s.localPosition += Random.insideUnitSphere * 0.25f;
                    return;
                }
                drone.localPosition = Vector3.zero;
                drone.localScale = Vector3.one * radius;
                nextGlitch = Random.Range(0.8f, 2.8f);
            }
            nextGlitch -= dt;
            if (nextGlitch <= 0f) glitchLeft = Random.Range(0.06f, 0.18f);
        }

        void LateUpdate()
        {
            // The body bobs (BugAI), the ground glow stays on the street
            var p = transform.position;
            groundGlow.position = new Vector3(p.x, GroundY, p.z);
        }
    }
}
