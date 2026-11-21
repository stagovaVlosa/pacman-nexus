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
    /// The look is built in code: neon hologram fruits, every type has its own colour and fruit.
    /// </summary>
    public class PowerUp : MonoBehaviour
    {
        public PowerUpType type;

        /// <summary>Size of the fruit (1 = old size from before 17 Nov).</summary>
        const float VisualScale = 1.5f;
        const float VisualHeight = 0.6f;

        Transform visual, spinner;
        Material coreMat;
        float phase;

        public static Color ColorFor(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Devour: return Visuals.Hex("#FF3BD4");      // hot magenta
                case PowerUpType.SpeedBurst: return Visuals.Hex("#A6FF2E");  // lime green (not cyan: stays apart from the orbs)
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

        /// <summary>Fruit the power-up looks like (HUD legend and minimap use the same names).</summary>
        public static string FruitFor(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Devour: return "APPLE";
                case PowerUpType.SpeedBurst: return "LIME";
                case PowerUpType.Shield: return "PLUM";
                default: return "GRAPES";
            }
        }

        /// <summary>Very short explanation for the HUD legend.</summary>
        public static string EffectFor(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Devour: return "Eat the Bugs";
                case PowerUpType.SpeedBurst: return "Move faster";
                case PowerUpType.Shield: return "Blocks 1 hit";
                default: return "Pulls orbs";
            }
        }

        /// <summary>
        /// Neon hologram fruits (calm look, 16 Nov): Apple = Devour, Lime = Speed Burst, Plum = Shield, Grapes = Magnet.
        /// Soft glow only (no big halo), slow spin, small ring on the street.
        /// </summary>
        void Build()
        {
            Color c = ColorFor(type);
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, VisualHeight, 0f);
            visual.localScale = Vector3.one * VisualScale;   // 17 Nov: fruits bigger, easier to see
            spinner = new GameObject("Spinner").transform;
            spinner.SetParent(visual, false);

            coreMat = Visuals.Lit(c, 0f, 0.8f, c, 1.0f);   // own colour, soft glow (stronger washes it out to white)
            var leafMat = Visuals.GetMaterial(Visuals.Hex("#3DFF8C"), Visuals.Hex("#3DFF8C"), 1.2f);
            var stemMat = Visuals.Lit(Visuals.Hex("#2A1C14"), 0f, 0.3f);

            switch (type)
            {
                case PowerUpType.Devour:
                {
                    // Apple: round body, small stem and one leaf
                    Visuals.Shape(PrimitiveType.Sphere, "Fruit", spinner, Vector3.zero, new Vector3(0.36f, 0.32f, 0.36f), coreMat);
                    Visuals.Shape(PrimitiveType.Cylinder, "Stem", spinner, new Vector3(0f, 0.19f, 0f), new Vector3(0.025f, 0.05f, 0.025f), stemMat);
                    var leaf = Visuals.Shape(PrimitiveType.Sphere, "Leaf", spinner, new Vector3(0.06f, 0.2f, 0f), new Vector3(0.12f, 0.02f, 0.06f), leafMat);
                    leaf.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
                    break;
                }
                case PowerUpType.SpeedBurst:
                {
                    // Lime: stretched oval with two small pointy ends
                    Visuals.Shape(PrimitiveType.Sphere, "Fruit", spinner, Vector3.zero, new Vector3(0.26f, 0.26f, 0.36f), coreMat);
                    Visuals.Shape(PrimitiveType.Sphere, "TipF", spinner, new Vector3(0f, 0f, 0.18f), Vector3.one * 0.07f, coreMat);
                    Visuals.Shape(PrimitiveType.Sphere, "TipB", spinner, new Vector3(0f, 0f, -0.18f), Vector3.one * 0.07f, coreMat);
                    break;
                }
                case PowerUpType.Shield:
                {
                    // Plum: slightly oval, short stem, leaf
                    Visuals.Shape(PrimitiveType.Sphere, "Fruit", spinner, Vector3.zero, new Vector3(0.3f, 0.34f, 0.3f), coreMat);
                    Visuals.Shape(PrimitiveType.Cylinder, "Stem", spinner, new Vector3(0f, 0.2f, 0f), new Vector3(0.02f, 0.04f, 0.02f), stemMat);
                    var leaf = Visuals.Shape(PrimitiveType.Sphere, "Leaf", spinner, new Vector3(-0.05f, 0.2f, 0f), new Vector3(0.1f, 0.02f, 0.05f), leafMat);
                    leaf.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                    break;
                }
                default:
                {
                    // Grapes: small berries in an upside-down cone, stem on top
                    var berries = new[]
                    {
                        new Vector3(-0.09f, 0.08f, 0f), new Vector3(0f, 0.08f, 0.08f), new Vector3(0.09f, 0.08f, 0f), new Vector3(0f, 0.08f, -0.08f),
                        new Vector3(-0.045f, -0.02f, 0.045f), new Vector3(0.045f, -0.02f, -0.045f), new Vector3(0.045f, -0.02f, 0.045f),
                        new Vector3(0f, -0.11f, 0f),
                    };
                    foreach (var b in berries) Visuals.Shape(PrimitiveType.Sphere, "Grape", spinner, b, Vector3.one * 0.12f, coreMat);
                    Visuals.Shape(PrimitiveType.Cylinder, "Stem", spinner, new Vector3(0f, 0.17f, 0f), new Vector3(0.02f, 0.04f, 0.02f), stemMat);
                    break;
                }
            }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GroundRing.Create(transform, c, 0.32f * VisualScale, 0.5f);
            if (RealisticCity.Active) BuildIcon();
        }

        Transform icon;

        /// <summary>
        /// Realistic look (21 Nov, user: "can't see the shapes of the fruits"): the same clear fruit picture as on the
        /// minimap, floating above the fruit and always facing the camera, so every power reads at a glance.
        /// The 3D fruit is hidden (from the high camera it was a small blob).
        /// </summary>
        void BuildIcon()
        {
            spinner.gameObject.SetActive(false);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return;
            var m = new Material(shader);
            m.SetTexture("_BaseMap", Minimap.FruitIcon(type));
            m.SetColor("_BaseColor", Color.white * 1.15f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "FruitIcon";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(visual, false);
            q.transform.localScale = Vector3.one * 0.42f;
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            icon = q.transform;
        }

        void Update()
        {
            if (visual == null) return;
            float t = Time.time + phase;
            visual.localPosition = new Vector3(0f, VisualHeight + Mathf.Sin(t * 1.8f) * 0.05f, 0f);
            spinner.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
            if (icon != null && Camera.main != null) icon.rotation = Camera.main.transform.rotation;   // billboard
            // Gentle breathing glow (no hard blinking)
            float pulse = 0.85f + 0.15f * Mathf.Sin(t * 2f);
            if (coreMat != null) coreMat.SetColor("_EmissionColor", ColorFor(type) * (1.0f * pulse));
        }

        public void Collect()
        {
            if (PlayerPowers.Instance != null) PlayerPowers.Instance.Activate(type, transform.position);
            Destroy(gameObject);
        }
    }
}
