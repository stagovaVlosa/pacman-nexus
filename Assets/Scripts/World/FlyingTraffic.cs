using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Skyline Zone: small hover cars flying in straight lanes over the district (GDD: "moving lights",
    /// "elevated transportation"). Purely visual: no collisions, they wrap around when they leave the area.
    /// Lanes run only over the skyline band around the maze, so they never hide Pac-Man.
    /// Night mode (Neon District): dark cars with pink/cyan underglow and light trails.
    /// </summary>
    public class FlyingTraffic : MonoBehaviour
    {
        class Car
        {
            public Transform t;
            public Vector3 dir;
            public float speed;
        }

        readonly List<Car> cars = new List<Car>();
        Vector3 min, max;

        public void Setup(LevelBuilder level, int band, bool night = false)
        {
            float cs = level.cellSize;
            min = new Vector3(-(band + 8) * cs, 0f, -(band + 8) * cs);
            max = new Vector3((level.Width + band + 8) * cs, 0f, (level.Height + band + 8) * cs);

            var rng = new System.Random(42);
            var body = night ? Visuals.Lit(Visuals.Hex("#3A3550"), 0.8f, 0.85f)
                             : Visuals.Lit(Visuals.Hex("#D8D2CC"), 0.6f, 0.8f);   // silver: dark cars read as holes
            var head = Visuals.GetMaterial(Color.white, Visuals.Hex("#FFF4DC"), 3f);
            var tail = Visuals.GetMaterial(Visuals.CorruptedRed, Visuals.CorruptedRed, 2.5f);
            var glow = Visuals.GetMaterial(Visuals.Hex("#FF9A3C"), Visuals.Hex("#FF9A3C"), 2f);
            // Realistic look (21 Nov): red tail lights instead of pink/cyan/violet neon streaks
            Color[] nightGlow = RealisticCity.Active
                ? new[] { Visuals.Hex("#FF2A1A"), Visuals.Hex("#E01818"), Visuals.Hex("#FF4A2A") }
                : new[] { Visuals.Hex("#FF2BD6"), Visuals.Cyan, Visuals.Hex("#8A5CFF") };

            for (int i = 0; i < (night ? 10 : 16); i++)   // Level 1: fewer hover cars (16 Nov, was 20)
            {
                if (night) glow = Visuals.GetMaterial(nightGlow[i % 3], nightGlow[i % 3], 2.6f);
                bool alongX = i % 2 == 0;
                float sign = rng.NextDouble() < 0.5 ? 1f : -1f;
                // Lanes run over the skyline band around the maze only (a car over the maze can hide Pac-Man)
                float laneCoord;
                bool outside = true;
                float span = (alongX ? level.Height : level.Width) * cs;
                if (outside)
                    laneCoord = rng.NextDouble() < 0.5 ? -(band + 5) * cs : span + (band + 5) * cs;
                else
                    laneCoord = (float)rng.NextDouble() * span;
                float height = 3f + (float)rng.NextDouble() * 4.5f;

                var root = new GameObject("HoverCar").transform;
                root.SetParent(transform, false);
                Visuals.Shape(PrimitiveType.Capsule, "Body", root, Vector3.zero, new Vector3(0.28f, 0.42f, 0.28f), body)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Visuals.Shape(PrimitiveType.Cube, "Underglow", root, new Vector3(0f, -0.13f, 0f), new Vector3(0.18f, 0.02f, 0.6f), glow);
                Visuals.Shape(PrimitiveType.Sphere, "HeadL", root, new Vector3(-0.08f, 0.02f, 0.4f), Vector3.one * 0.07f, head);
                Visuals.Shape(PrimitiveType.Sphere, "HeadR", root, new Vector3(0.08f, 0.02f, 0.4f), Vector3.one * 0.07f, head);
                Visuals.Shape(PrimitiveType.Sphere, "Tail", root, new Vector3(0f, 0.02f, -0.42f), Vector3.one * 0.08f, tail);

                if (night)
                {
                    // Light streak behind the car (neon traffic at night)
                    var tr = root.gameObject.AddComponent<TrailRenderer>();
                    tr.time = 0.45f;
                    tr.minVertexDistance = 0.1f;
                    tr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(1f, 0f));
                    tr.sharedMaterial = Visuals.Glow(nightGlow[i % 3], 1.4f, 0f, null, true);
                    tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                var dir = alongX ? new Vector3(sign, 0f, 0f) : new Vector3(0f, 0f, sign);
                float start = (float)rng.NextDouble();
                var pos = alongX
                    ? new Vector3(Mathf.Lerp(min.x, max.x, start), height, laneCoord)
                    : new Vector3(laneCoord, height, Mathf.Lerp(min.z, max.z, start));
                root.position = pos;
                root.rotation = Quaternion.LookRotation(dir);
                cars.Add(new Car { t = root, dir = dir, speed = 3.5f + (float)rng.NextDouble() * 4f });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var c in cars)
            {
                var p = c.t.position + c.dir * (c.speed * dt);
                // Wrap around the area
                bool wrapped = false;
                if (p.x > max.x) { p.x = min.x; wrapped = true; } else if (p.x < min.x) { p.x = max.x; wrapped = true; }
                if (p.z > max.z) { p.z = min.z; wrapped = true; } else if (p.z < min.z) { p.z = max.z; wrapped = true; }
                p.y += Mathf.Sin(Time.time * 1.3f + c.speed) * 0.002f;
                c.t.position = p;
                if (wrapped && c.t.TryGetComponent(out TrailRenderer tr)) tr.Clear();
            }
        }
    }
}
