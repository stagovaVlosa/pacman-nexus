using UnityEngine;

namespace Nexus
{
    /// <summary>Power-ups (GDD 3.1 abilities + 5.5 power-ups).</summary>
    public enum PowerUpType
    {
        /// <summary>The classic power pellet: Bugs turn blue, run away and can be eaten (GDD "Devour Mode").</summary>
        Devour,
        /// <summary>Pac-Man moves faster for a few seconds (GDD "Speed Burst").</summary>
        SpeedBurst,
        /// <summary>Absorbs one Bug hit (GDD "Energy Shield").</summary>
        Shield,
        /// <summary>Pulls nearby Energy Orbs to Pac-Man (GDD "Magnetic Field").</summary>
        Magnet
    }

    /// <summary>
    /// A power-up item on a street cell (layout: * Devour, &gt; Speed Burst, + Shield, % Magnet;
    /// when a layout has none, LevelBuilder places a default set). Collected when Pac-Man enters the cell.
    /// The look is built in code: every type has its own colour and shape so it reads at a glance.
    /// </summary>
    public class PowerUp : MonoBehaviour
    {
        public PowerUpType type;

        Transform visual, spinner;
        Material coreMat;
        float phase;

        public static Color ColorFor(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Devour: return Visuals.Hex("#FF3BD4");      // hot magenta
                case PowerUpType.SpeedBurst: return Visuals.Hex("#3DFF8C");  // electric green
                case PowerUpType.Shield: return Visuals.Hex("#4DA6FF");      // shield blue
                default: return Visuals.Hex("#C27BFF");                      // magnet violet
            }
        }

        public static string NameFor(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Devour: return "DEVOUR MODE";
                case PowerUpType.SpeedBurst: return "SPEED BURST";
                case PowerUpType.Shield: return "ENERGY SHIELD";
                default: return "MAGNETIC FIELD";
            }
        }

        public static PowerUp Create(PowerUpType type, Vector3 position, Transform parent)
        {
            var go = new GameObject("PowerUp_" + type);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var p = go.AddComponent<PowerUp>();
            p.type = type;
            p.phase = Random.value * 6f;
            p.Build();
            return p;
        }

        void Build()
        {
            Color c = ColorFor(type);
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, 0.55f, 0f);
            spinner = new GameObject("Spinner").transform;
            spinner.SetParent(visual, false);

            coreMat = Visuals.Lit(Color.Lerp(c, Color.white, 0.35f), 0f, 0.6f, c, 3.5f);
            var rimMat = Visuals.Glow(c, 2.2f, 0f);
            var halo = Visuals.Glow(c, 1.1f, 2.2f);

            switch (type)
            {
                case PowerUpType.Devour:
                {
                    // Big overcharged energy pellet: white-hot core, magenta aura and two crossing rings
                    Visuals.Shape(PrimitiveType.Sphere, "Core", visual, Vector3.zero, Vector3.one * 0.34f,
                        Visuals.Lit(Color.white, 0f, 0.6f, Color.Lerp(c, Color.white, 0.5f), 4f));
                    Ring(spinner, 0.32f, 0.025f, rimMat, Quaternion.Euler(70f, 0f, 0f));
                    Ring(spinner, 0.32f, 0.025f, rimMat, Quaternion.Euler(70f, 90f, 0f));
                    Visuals.Shape(PrimitiveType.Sphere, "Halo", visual, Vector3.zero, Vector3.one * 0.75f, halo);
                    break;
                }
                case PowerUpType.SpeedBurst:
                {
                    // Double chevron (>>) pointing forward, spinning around
                    for (int i = 0; i < 2; i++)
                    {
                        float z = -0.1f + i * 0.17f;
                        var a = Visuals.Shape(PrimitiveType.Cube, "Chevron", spinner, new Vector3(-0.08f, 0f, z), new Vector3(0.06f, 0.06f, 0.24f), coreMat);
                        a.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
                        var b = Visuals.Shape(PrimitiveType.Cube, "Chevron", spinner, new Vector3(0.08f, 0f, z), new Vector3(0.06f, 0.06f, 0.24f), coreMat);
                        b.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
                    }
                    Visuals.Shape(PrimitiveType.Sphere, "Halo", visual, Vector3.zero, Vector3.one * 0.6f, halo);
                    break;
                }
                case PowerUpType.Shield:
                {
                    // Hexagon frame standing up, with a small core in the middle
                    var mb = new MeshBuilder();
                    mb.Tube(MeshBuilder.Circle(0.24f, 6, Vector3.zero, Quaternion.identity), 0.035f, 6, true);
                    Visuals.MeshPart("Hex", spinner, mb.ToMesh("ShieldHex"), coreMat);
                    Visuals.Shape(PrimitiveType.Sphere, "Core", spinner, Vector3.zero, Vector3.one * 0.14f, rimMat);
                    Visuals.Shape(PrimitiveType.Sphere, "Halo", visual, Vector3.zero, Vector3.one * 0.62f, halo);
                    break;
                }
                default:
                {
                    // Horseshoe magnet: half ring with two bright tips
                    var arc = new System.Collections.Generic.List<Vector3>();
                    for (int i = 0; i <= 16; i++)
                    {
                        float a = Mathf.PI * i / 16f;
                        arc.Add(new Vector3(Mathf.Cos(a) * 0.17f, -Mathf.Sin(a) * 0.17f, 0f));
                    }
                    arc.Insert(0, new Vector3(0.17f, 0.12f, 0f));
                    arc.Add(new Vector3(-0.17f, 0.12f, 0f));
                    var mb = new MeshBuilder();
                    mb.Tube(arc, 0.05f, 8, false);
                    Visuals.MeshPart("Magnet", spinner, mb.ToMesh("Magnet"), coreMat);
                    Visuals.Shape(PrimitiveType.Cube, "TipL", spinner, new Vector3(0.17f, 0.15f, 0f), Vector3.one * 0.1f, Visuals.GetMaterial(Color.white, Color.white, 2.5f));
                    Visuals.Shape(PrimitiveType.Cube, "TipR", spinner, new Vector3(-0.17f, 0.15f, 0f), Vector3.one * 0.1f, Visuals.GetMaterial(Color.white, Color.white, 2.5f));
                    Visuals.Shape(PrimitiveType.Sphere, "Halo", visual, Vector3.zero, Vector3.one * 0.62f, halo);
                    break;
                }
            }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GroundRing.Create(transform, c, 0.42f, 0.9f);
        }

        static void Ring(Transform parent, float r, float thickness, Material mat, Quaternion rot)
        {
            var mb = new MeshBuilder();
            mb.Tube(MeshBuilder.Circle(r, 40, Vector3.zero, Quaternion.identity), thickness, 6, true);
            var go = Visuals.MeshPart("Ring", parent, mb.ToMesh("PowerRing"), mat);
            go.transform.localRotation = rot;
        }

        void Update()
        {
            if (visual == null) return;
            float t = Time.time + phase;
            visual.localPosition = new Vector3(0f, 0.55f + Mathf.Sin(t * 2.2f) * 0.07f, 0f);
            spinner.Rotate(0f, (type == PowerUpType.Devour ? 140f : 90f) * Time.deltaTime, 0f, Space.World);
            // Devour pellet blinks like the classic power pellet
            float pulse = type == PowerUpType.Devour
                ? (Mathf.Repeat(t * 2.5f, 1f) < 0.5f ? 1f : 0.45f)
                : 0.75f + 0.25f * Mathf.Sin(t * 4f);
            visual.localScale = Vector3.one * (type == PowerUpType.Devour ? 0.9f + 0.15f * pulse : 1f);
            if (coreMat != null) coreMat.SetColor("_EmissionColor", ColorFor(type) * (3.5f * pulse));
        }

        public void Collect()
        {
            if (PlayerPowers.Instance != null) PlayerPowers.Instance.Activate(type, transform.position);
            Destroy(gameObject);
        }
    }
}
