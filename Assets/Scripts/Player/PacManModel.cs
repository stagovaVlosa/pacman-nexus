using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus
{
    /// <summary>
    /// Pac-Man as a flying spherical robot (Docs/PacManRobot_Design.md), built from code.
    /// No face: the Pac-Man silhouette comes from two glass shell halves (left/right) that swing open
    /// around the vertical (Y) axis, so a wedge opens at the front (+Z), visible from the top-down camera.
    /// The hubs (top plate, hover engine) and the interior (core, gyros, cables) do not rotate with the shell.
    /// All sizes are in units of the radius (R = 1), the "Robot" child is scaled to the real radius.
    /// </summary>
    public class PacManModel : MonoBehaviour
    {
        public float maxOpen = 32f;   // degrees per half
        public float idleOpen = 25f;
        public float chompSpeed = 3.2f;

        // Palette (design doc)
        static readonly Color GlassYellow = Visuals.Hex("#FFC400");
        static readonly Color Amber       = Visuals.Hex("#FF8C00");
        static readonly Color MetalDark   = Visuals.Hex("#1A1F2B");
        static readonly Color MetalMid    = Visuals.Hex("#3A4252");

        const float Seam = 3f * Mathf.Deg2Rad;
        const float GroundY = 0.015f;

        Transform robot, left, right, core, cage, gyroA, gyroB, groundGlow;
        Material coreMat, beamMat, groundMat;
        Color coreEmission;
        readonly List<Pulse> pulses = new List<Pulse>();
        readonly List<Transform> particles = new List<Transform>();
        readonly List<float> particleSpeed = new List<float>();
        float phase, current, t;

        class Pulse { public Transform tr; public Cable cable; public float t, speed; }

        /// <summary>A cable path sampled into points, so pulses can travel along it at even speed.</summary>
        class Cable
        {
            public List<Vector3> pts;
            float[] dist;

            public Cable(List<Vector3> points)
            {
                pts = points;
                dist = new float[pts.Count];
                for (int i = 1; i < pts.Count; i++) dist[i] = dist[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            }

            public Vector3 PointAt(float u)
            {
                float want = u * dist[dist.Length - 1];
                for (int i = 1; i < dist.Length; i++)
                    if (dist[i] >= want)
                        return Vector3.Lerp(pts[i - 1], pts[i], Mathf.InverseLerp(dist[i - 1], dist[i], want));
                return pts[pts.Count - 1];
            }
        }

        public static PacManModel Create(Transform body, float radius)
        {
            var model = body.gameObject.AddComponent<PacManModel>();
            model.robot = new GameObject("Robot").transform;
            model.robot.SetParent(body, false);
            model.robot.localScale = Vector3.one * radius;

            var mat = new Mats();
            model.right = MakeHalf("Right", model.robot, true, mat);
            model.left = MakeHalf("Left", model.robot, false, mat);
            model.BuildHub(mat);
            model.BuildInterior(mat);
            model.BuildHoverEffects(body, radius, mat);
            return model;
        }

        // ---------- materials ----------

        class Mats
        {
            public Material metalDark = Visuals.Lit(MetalDark, 0.75f, 0.68f);
            public Material metalMid = Visuals.Lit(MetalMid, 0.75f, 0.72f);
            public Material rubber = Visuals.Lit(Visuals.Hex("#12161F"), 0.2f, 0.4f);
            public Material cyan = Visuals.Lit(Visuals.Cyan, 0f, 0.6f, Visuals.Cyan, 2f);
            public Material amber = Visuals.Lit(Visuals.Hex("#FFA31A"), 0f, 0.6f, Amber, 1.6f);
            public Material glass = Visuals.Glass(GlassYellow, 0.28f, Visuals.Hex("#3A2A00"));
            public Material rim = Visuals.Glow(Visuals.Hex("#FFB000"), 2f, 2.6f);
        }

        // ---------- shell halves ----------

        /// <summary>Half shell on the +X (right) or -X (left) side. The cut edge at the front gets a metal lip.</summary>
        static Transform MakeHalf(string name, Transform parent, bool isRight, Mats mat)
        {
            var half = new GameObject(name).transform;
            half.SetParent(parent, false);
            float p0 = isRight ? Mathf.PI / 2f + Seam : 3f * Mathf.PI / 2f;
            float len = Mathf.PI - Seam;
            float edge = isRight ? p0 : p0 + len;

            var shell = new MeshBuilder();
            shell.SpherePart(1f, p0, len, 0f, Mathf.PI, 48, 32);
            var shellMesh = shell.ToMesh(name + "_Shell");
            Visuals.MeshPart("Shell", half, shellMesh, mat.glass);
            Visuals.MeshPart("Rim", half, shellMesh, mat.rim);

            var metal = new MeshBuilder();
            metal.Tube(Meridian(edge, 1f), 0.035f, 8, false);
            metal.Band(1.025f, 0.16f, 0f, p0, len, 48);
            Visuals.MeshPart("Metal", half, metal.ToMesh(name + "_Metal"), mat.metalDark, true);

            // Glow lines are a bit thicker than in the reference, so they still read from the game camera
            var amber = new MeshBuilder();
            amber.Tube(Meridian(edge, 1.03f), 0.016f, 6, false);
            for (int k = 1; k < 4; k++) amber.Tube(Meridian(p0 + len * k / 4f, 1.004f), 0.01f, 5, false);
            amber.Band(1.03f, 0.022f, 0.08f, p0, len, 48);
            amber.Band(1.03f, 0.022f, -0.08f, p0, len, 48);
            Visuals.MeshPart("AmberGlow", half, amber.ToMesh(name + "_Amber"), mat.amber);

            var vents = new MeshBuilder();
            for (int i = 0; i < 4; i++)
            {
                var d = MeshBuilder.DirAt(p0 + len * (i + 0.5f) / 4f);
                vents.Box(d * 1.035f, new Vector3(0.16f, 0.04f, 0.02f), Quaternion.LookRotation(-d));
            }
            Visuals.MeshPart("Vents", half, vents.ToMesh(name + "_Vents"), mat.cyan);
            return half;
        }

        /// <summary>Half circle from the bottom pole to the top pole, at direction phi.</summary>
        static List<Vector3> Meridian(float phi, float r, int seg = 40)
        {
            var pts = new List<Vector3>();
            for (int i = 0; i <= seg; i++) pts.Add(MeshBuilder.SpherePoint(phi, Mathf.PI * i / seg) * r);
            return pts;
        }

        // ---------- hubs (do not rotate) ----------

        void BuildHub(Mats mat)
        {
            var hub = new GameObject("Hub").transform;
            hub.SetParent(robot, false);

            var dark = new MeshBuilder();
            dark.SpherePart(1.012f, 0f, Mathf.PI * 2f, 0f, 0.32f, 48, 6);                 // top cap
            dark.SpherePart(1.012f, 0f, Mathf.PI * 2f, Mathf.PI - 0.42f, 0.42f, 48, 6);   // bottom cap
            Visuals.MeshPart("Caps", hub, dark.ToMesh("Hub_Caps"), mat.metalDark, true);

            var mid = new MeshBuilder();
            mid.Frustum(0.16f, 0.2f, 0.06f, 1.0f, 6, true);      // hexagonal top plate
            mid.Frustum(0.34f, 0.42f, 0.12f, -0.97f, 48, true);  // engine housing
            Visuals.MeshPart("Plates", hub, mid.ToMesh("Hub_Plates"), mat.metalMid, true);

            var cyan = new MeshBuilder();
            cyan.Tube(MeshBuilder.Circle(0.15f, 6, new Vector3(0f, 1.035f, 0f), MeshBuilder.Flat), 0.015f, 6, true);
            cyan.Tube(MeshBuilder.Circle(0.33f, 64, new Vector3(0f, -1.04f, 0f), MeshBuilder.Flat), 0.03f, 10, true);
            Visuals.MeshPart("CyanRings", hub, cyan.ToMesh("Hub_Cyan"), mat.cyan);

            var amber = new MeshBuilder();
            float capR = Mathf.Sin(0.32f) * 1.014f;
            amber.Tube(MeshBuilder.Circle(capR, 64, new Vector3(0f, Mathf.Cos(0.32f) * 1.014f, 0f), MeshBuilder.Flat), 0.015f, 6, true);
            Visuals.MeshPart("CapRing", hub, amber.ToMesh("Hub_Amber"), mat.amber);

            var disc = new MeshBuilder();
            disc.Disc(new Vector3(0f, -1.045f, 0f), 0.28f, Vector3.down, 48);
            Visuals.MeshPart("EngineDisc", hub, disc.ToMesh("Hub_Disc"),
                Visuals.Lit(Visuals.Hex("#7FF4FF"), 0f, 0.5f, Visuals.Hex("#7FF4FF"), 3f));

            // Light cone pointing down, short enough to just reach the street
            var beam = new MeshBuilder();
            beam.Frustum(0.05f, 0.32f, 0.38f, -1.04f - 0.19f, 40, false);
            beamMat = Visuals.Glow(Visuals.Cyan, 0.35f, 0f, Visuals.GradientTexture(false), true);
            Visuals.MeshPart("Beam", hub, beam.ToMesh("Hub_Beam"), beamMat);
        }

        // ---------- interior (static, seen through the glass) ----------

        void BuildInterior(Mats mat)
        {
            var inner = new GameObject("Interior").transform;
            inner.SetParent(robot, false);

            // Energy core in a wireframe cage
            var coreMesh = new MeshBuilder();
            coreMesh.Icosahedron(Vector3.zero, 0.2f, 1, true);
            coreEmission = Visuals.Hex("#FFC93A");
            coreMat = Visuals.Lit(Visuals.Hex("#FFF0B0"), 0f, 0.5f, coreEmission, 2.4f);
            core = Visuals.MeshPart("Core", inner, coreMesh.ToMesh("Core"), coreMat).transform;

            var cageMesh = new MeshBuilder();
            foreach (var (a, b) in MeshBuilder.IcoEdges(0.28f))
                cageMesh.Tube(new List<Vector3> { a, b }, 0.008f, 4, false);
            cage = Visuals.MeshPart("CoreCage", inner, cageMesh.ToMesh("CoreCage"), mat.cyan).transform;

            gyroA = MakeGyro("GyroA", inner, 0.42f, 0.014f, mat);
            gyroB = MakeGyro("GyroB", inner, 0.5f, 0.012f, mat);

            // Frame ribs and circuit modules
            var frame = new MeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                var rot = Quaternion.Euler(i == 0 ? 90f : 0f, i * 60f, i == 2 ? 45f : 0f);
                frame.Tube(MeshBuilder.Circle(0.86f, 96, Vector3.zero, rot), 0.011f, 6, true);
            }
            var cyanStrips = new MeshBuilder();
            var amberStrips = new MeshBuilder();
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                float y = i % 2 == 1 ? 0.35f : -0.3f;
                var pos = new Vector3(Mathf.Cos(a) * 0.72f, y, Mathf.Sin(a) * 0.72f);
                var rot = Quaternion.LookRotation(new Vector3(-pos.x, 0f, -pos.z));
                frame.Box(pos, new Vector3(0.16f, 0.1f, 0.05f), rot);
                (i % 3 != 0 ? cyanStrips : amberStrips).Box(pos + rot * new Vector3(0f, 0.025f, 0f),
                    new Vector3(0.12f, 0.012f, 0.052f), rot);
            }
            Visuals.MeshPart("Frame", inner, frame.ToMesh("Frame"), mat.metalDark);
            Visuals.MeshPart("ModuleLightsCyan", inner, cyanStrips.ToMesh("ModuleCyan"), mat.cyan);
            Visuals.MeshPart("ModuleLightsAmber", inner, amberStrips.ToMesh("ModuleAmber"), mat.amber);

            BuildCables(inner, mat);
        }

        Transform MakeGyro(string name, Transform parent, float r, float tube, Mats mat)
        {
            var ring = new MeshBuilder();
            ring.Tube(MeshBuilder.Circle(r, 96, Vector3.zero, Quaternion.identity), tube, 6, true);
            var gyro = Visuals.MeshPart(name, parent, ring.ToMesh(name), mat.metalMid).transform;
            var lights = new MeshBuilder();
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 2f;
                lights.Icosahedron(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f), 0.022f, 1, false);
            }
            Visuals.MeshPart("Lights", gyro, lights.ToMesh(name + "_Lights"), mat.cyan);
            return gyro;
        }

        /// <summary>18 cables from the core to the shell: 1/3 rubber, 1/3 cyan, 1/3 amber, with energy pulses.</summary>
        void BuildCables(Transform inner, Mats mat)
        {
            long seed = 11; // same pseudo-random sequence as the three.js reference
            float Rnd() { seed = seed * 16807 % 2147483647; return (seed - 1) / 2147483646f; }
            Vector3 RandDir() => new Vector3(Rnd() * 2f - 1f, Rnd() * 2f - 1f, Rnd() * 2f - 1f).normalized;

            var builders = new[] { new MeshBuilder(), new MeshBuilder(), new MeshBuilder() };
            var pulseMats = new[] { null,
                Visuals.Lit(Visuals.Hex("#9FF8FF"), 0f, 0.5f, Visuals.Hex("#9FF8FF"), 3f),
                Visuals.Lit(Visuals.Hex("#FFE0A0"), 0f, 0.5f, Visuals.Hex("#FFE0A0"), 3f) };
            for (int i = 0; i < 18; i++)
            {
                var d0 = RandDir();
                var d1 = Vector3.Lerp(RandDir(), d0, 0.5f).normalized;
                var d2 = Vector3.Lerp(RandDir(), d0, 0.3f).normalized;
                var ctrl = new[] { d0 * 0.22f, d1 * 0.5f + new Vector3(0f, (Rnd() - 0.5f) * 0.2f, 0f), d2 * 0.72f, d2 * 0.88f };
                int kind = i % 3;
                float rad = kind == 0 ? 0.028f : 0.018f + Rnd() * 0.008f;
                var pts = CatmullRom(ctrl, 16);
                builders[kind].Tube(pts, rad, 8, false);
                if (kind == 0) continue;

                var cable = new Cable(pts);
                for (int k = 0; k < 2; k++)
                {
                    var p = Visuals.Shape(PrimitiveType.Sphere, "Pulse", inner, Vector3.zero, Vector3.one * 0.06f, pulseMats[kind]);
                    p.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    pulses.Add(new Pulse { tr = p.transform, cable = cable, t = (i * 0.37f + k * 0.5f) % 1f, speed = 0.35f + (i * 7 % 5) * 0.06f });
                }
            }
            Visuals.MeshPart("CablesRubber", inner, builders[0].ToMesh("CablesRubber"), mat.rubber);
            Visuals.MeshPart("CablesCyan", inner, builders[1].ToMesh("CablesCyan"), mat.cyan);
            Visuals.MeshPart("CablesAmber", inner, builders[2].ToMesh("CablesAmber"), mat.amber);
        }

        /// <summary>Smooth curve through the control points (uniform Catmull-Rom).</summary>
        static List<Vector3> CatmullRom(Vector3[] c, int perSegment)
        {
            var pts = new List<Vector3>();
            for (int s = 0; s < c.Length - 1; s++)
            {
                Vector3 p0 = c[Mathf.Max(s - 1, 0)], p1 = c[s], p2 = c[s + 1], p3 = c[Mathf.Min(s + 2, c.Length - 1)];
                for (int i = 0; i < perSegment || (s == c.Length - 2 && i == perSegment); i++)
                {
                    float u = (float)i / perSegment;
                    pts.Add(0.5f * (2f * p1 + (p2 - p0) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u
                                    + (3f * p1 - p0 - 3f * p2 + p3) * u * u * u));
                }
            }
            return pts;
        }

        // ---------- hover engine effects ----------

        void BuildHoverEffects(Transform body, float radius, Mats mat)
        {
            var spark = Visuals.Glow(Visuals.Hex("#7FF4FF"), 1.5f, 0f);
            for (int i = 0; i < 16; i++)
            {
                var p = Visuals.Shape(PrimitiveType.Sphere, "Particle", robot,
                    new Vector3(Random.Range(-0.3f, 0.3f), -1.1f - Random.value * 0.4f, Random.Range(-0.3f, 0.3f)),
                    Vector3.one * 0.04f, spark);
                p.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                particles.Add(p.transform);
                particleSpeed.Add(0.4f + Random.value * 0.6f);
            }

            // Cyan glow on the street under the robot (repositioned to ground level every frame)
            groundMat = Visuals.Glow(Visuals.Cyan, 0.9f, 0f, Visuals.GradientTexture(true), true);
            var glow = Visuals.Shape(PrimitiveType.Quad, "GroundGlow", body, Vector3.zero,
                Vector3.one * radius * 3.2f, groundMat);
            glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            glow.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            groundGlow = glow.transform;
        }

        // ---------- animation ----------

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;

            // Wedge: each half swings away from the front around the vertical axis
            bool moving = PlayerMovement.Instance != null && PlayerMovement.Instance.IsMoving;
            if (moving)
            {
                phase += dt * chompSpeed;
                current = Mathf.Abs(Mathf.Sin(phase)) * maxOpen;
            }
            else
            {
                current = Mathf.MoveTowards(current, idleOpen, 120f * dt);
            }
            // Positive Y rotation swings the front edge toward +X (right), negative toward -X (left)
            right.localRotation = Quaternion.Euler(0f, current, 0f);
            left.localRotation = Quaternion.Euler(0f, -current, 0f);

            core.Rotate(30f * dt, 46f * dt, 0f, Space.Self);
            cage.Rotate(0f, -23f * dt, 0f, Space.Self);
            coreMat.SetColor("_EmissionColor", coreEmission * (2.2f + Mathf.Sin(t * 3f) * 0.4f));
            gyroA.Rotate(52f * dt, 17f * dt, 0f, Space.Self);
            gyroB.Rotate(-23f * dt, 0f, 34f * dt, Space.Self);

            foreach (var p in pulses)
            {
                p.t = (p.t + dt * p.speed) % 1f;
                p.tr.localPosition = p.cable.PointAt(p.t);
                p.tr.localScale = Vector3.one * 0.06f * (1f - p.t * 0.5f);
            }

            // Particles fall from the engine to the street
            float groundLocal = (GroundY - robot.position.y) / robot.lossyScale.y;
            for (int i = 0; i < particles.Count; i++)
            {
                var pos = particles[i].localPosition;
                pos.y -= dt * particleSpeed[i];
                if (pos.y < groundLocal)
                    pos = new Vector3(Random.Range(-0.3f, 0.3f), -1.08f, Random.Range(-0.3f, 0.3f));
                particles[i].localPosition = pos;
            }

            beamMat.SetFloat("_Intensity", 0.35f + Mathf.Sin(t * 6f) * 0.1f);
            groundMat.SetFloat("_Intensity", 0.9f + Mathf.Sin(t * 1.8f) * 0.2f);
        }

        void LateUpdate()
        {
            // The body bobs up and down (PlayerMovement), the ground glow stays on the street
            var p = robot.position;
            groundGlow.position = new Vector3(p.x, GroundY, p.z);
        }
    }
}
