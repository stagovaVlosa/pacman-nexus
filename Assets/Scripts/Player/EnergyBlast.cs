using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Energy Blast (GDD 3.1, ability 3), Space key: costs energy, sends out an expanding cyan ring,
    /// and every Bug within 'radius' cells is pushed away through the maze and frozen for a moment.
    /// </summary>
    public class EnergyBlast : MonoBehaviour
    {
        public float cost = 30f;
        public float cooldown = 3f;
        public float radius = 3f;        // cells
        public int pushCells = 2;
        public float freezeTime = 2f;

        public float CooldownLeft { get; private set; }
        /// <summary>0 = just used, 1 = ready (for the HUD).</summary>
        public float CooldownProgress => cooldown <= 0f ? 1f : 1f - CooldownLeft / cooldown;
        public bool CanUse => CooldownLeft <= 0f && GameManager.Instance != null
                              && GameManager.Instance.IsPlaying && GameManager.Instance.Energy >= cost;

        PlayerMovement mover;

        void Start() => mover = GetComponent<PlayerMovement>();

        void Update()
        {
            if (CooldownLeft > 0f) CooldownLeft = Mathf.Max(0f, CooldownLeft - Time.deltaTime);
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) TryBlast();
        }

        /// <summary>Fires the blast if possible. Returns true if it fired.</summary>
        public bool TryBlast()
        {
            if (!CanUse || mover == null || !GameManager.Instance.UseEnergy(cost)) return false;
            CooldownLeft = cooldown;

            var level = LevelBuilder.Instance;
            float cs = level != null ? level.cellSize : 1f;
            BlastWave.Spawn(new Vector3(transform.position.x, 0.05f, transform.position.z), radius * cs);

            foreach (var bug in BugAI.All)
            {
                var d = bug.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude <= radius * cs) bug.Knockback(mover.CurrentCell, pushCells, freezeTime);
            }
            return true;
        }
    }

    /// <summary>Expanding cyan ring and flash on the ground; removes itself when done.</summary>
    public class BlastWave : MonoBehaviour
    {
        const float Duration = 0.45f;

        static Mesh ringMesh;
        static Texture2D flashMask;

        float radius, t;
        Material ringMat, flashMat;
        Transform flash;

        public static void Spawn(Vector3 position, float radius)
        {
            if (ringMesh == null)
            {
                var mb = new MeshBuilder();
                mb.Tube(MeshBuilder.Circle(1f, 64, Vector3.zero, MeshBuilder.Flat), 0.035f, 6, true);
                ringMesh = mb.ToMesh("BlastRing");
            }
            if (flashMask == null) flashMask = Visuals.GradientTexture(true);

            var wave = new GameObject("EnergyBlast").AddComponent<BlastWave>();
            wave.transform.position = position;
            wave.radius = radius;
            wave.ringMat = Visuals.Glow(Visuals.Cyan, 4f, 0f);
            Visuals.MeshPart("Ring", wave.transform, ringMesh, wave.ringMat);
            wave.flashMat = Visuals.Glow(Visuals.Cyan, 1.5f, 0f, flashMask, true);
            var f = Visuals.Shape(PrimitiveType.Quad, "Flash", wave.transform, Vector3.zero, Vector3.one, wave.flashMat);
            f.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            f.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wave.flash = f.transform;
            wave.transform.localScale = Vector3.one * 0.1f;
        }

        void Update()
        {
            t += Time.deltaTime / Duration;
            if (t >= 1f)
            {
                Destroy(ringMat);
                Destroy(flashMat);
                Destroy(gameObject);
                return;
            }
            float ease = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Max(0.1f, ease * radius);
            ringMat.SetFloat("_Intensity", 4f * (1f - t));
            flash.localScale = Vector3.one * 2f;
            flashMat.SetFloat("_Intensity", 1.5f * (1f - t) * (1f - t));
        }
    }
}
