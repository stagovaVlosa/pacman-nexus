using System;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Pac-Man's power-ups (GDD 3.1 Devour Mode, 5.5 Speed Burst / Energy Shield / Magnetic Field).
    ///  - Devour:  like the classic power pellet. Every Bug turns blue, runs away and can be eaten for a few seconds
    ///             (200, 400, 800, 1600 points in a row). Blue Bugs blink white when the time is almost over.
    ///             An eaten Bug flies home and comes back after a few seconds.
    ///  - Speed Burst: faster for a few seconds, with a light trail.
    ///  - Energy Shield: a bubble that absorbs the next Bug hit (the Bug is pushed away and stunned).
    ///  - Magnet: pulls Energy Orbs within a few cells to Pac-Man.
    /// Losing a life ends Devour, Speed and Magnet (the shield stays until used).
    /// </summary>
    public class PlayerPowers : MonoBehaviour
    {
        public static PlayerPowers Instance { get; private set; }

        [Header("Durations (seconds)")]
        public float devourTime = 8f;
        public float speedTime = 6f;
        public float magnetTime = 10f;
        [Tooltip("Blue Bugs blink white during the last seconds")]
        public float devourWarning = 2f;

        [Header("Effects")]
        public float speedMultiplier = 1.45f;
        [Tooltip("Magnet radius in cells")]
        public int magnetRadius = 3;

        public float DevourLeft { get; private set; }
        public float SpeedLeft { get; private set; }
        public float MagnetLeft { get; private set; }
        public bool HasShield { get; private set; }
        public bool DevourActive => DevourLeft > 0f;

        /// <summary>Fired when a power-up is picked up (HUD message, BYTE line).</summary>
        public event Action<PowerUpType> OnActivated;

        PlayerMovement mover;
        float baseSpeed;
        int eatChain;
        float magnetTick;
        TrailRenderer trail;
        Transform shieldBubble, magnetRing, body;
        Material shieldMat, magnetMat;
        Light glow;
        Color glowColor;
        float glowIntensity, glowRange;

        void Awake() => Instance = this;

        void Start()
        {
            mover = GetComponent<PlayerMovement>();
            baseSpeed = mover != null ? mover.speed : 4.5f;
            body = transform.Find("Body");
            var g = transform.Find("Glow");
            if (g != null && g.TryGetComponent(out glow)) { glowColor = glow.color; glowIntensity = glow.intensity; glowRange = glow.range; }
            BuildEffects();
            if (GameManager.Instance != null) GameManager.Instance.OnLifeLost += OnLifeLost;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnLifeLost -= OnLifeLost;
        }

        void BuildEffects()
        {
            // Speed Burst trail (green light streak behind Pac-Man)
            var trailGo = new GameObject("SpeedTrail");
            trailGo.transform.SetParent(transform, false);
            trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.minVertexDistance = 0.05f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 0f));
            var c = PowerUp.ColorFor(PowerUpType.SpeedBurst);
            trail.sharedMaterial = Visuals.Glow(c, 1.6f, 0f, null, true);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;

            // Energy Shield bubble (rim glow only, so Pac-Man stays visible inside)
            shieldMat = Visuals.Glow(PowerUp.ColorFor(PowerUpType.Shield), 1.4f, 2.5f);
            var bubble = Visuals.Shape(PrimitiveType.Sphere, "ShieldBubble", transform, Vector3.zero, Vector3.one * 1.25f, shieldMat);
            bubble.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shieldBubble = bubble.transform;
            shieldBubble.gameObject.SetActive(false);

            // Magnetic field: a violet ring on the street showing the pull radius
            var mb = new MeshBuilder();
            mb.Tube(MeshBuilder.Circle(1f, 72, Vector3.zero, MeshBuilder.Flat), 0.012f, 5, true);
            magnetMat = Visuals.Glow(PowerUp.ColorFor(PowerUpType.Magnet), 0.7f, 0f, null, true, true);
            magnetRing = Visuals.MeshPart("MagnetRing", transform, mb.ToMesh("MagnetRing"), magnetMat).transform;
            magnetRing.gameObject.SetActive(false);
        }

        /// <summary>Starts a power-up (called by PowerUp when Pac-Man takes it).</summary>
        public void Activate(PowerUpType type, Vector3 at)
        {
            switch (type)
            {
                case PowerUpType.Devour:
                    DevourLeft = devourTime;
                    eatChain = 0;
                    foreach (var bug in BugAI.All) bug.MakeVulnerable(devourTime, devourWarning);
                    break;
                case PowerUpType.SpeedBurst: SpeedLeft = speedTime; break;
                case PowerUpType.Shield: HasShield = true; break;
                case PowerUpType.Magnet: MagnetLeft = magnetTime; break;
            }
            if (GameManager.Instance != null) GameManager.Instance.AddScore(50);
            if (GameHUD.Instance != null) GameHUD.Instance.Popup(at + Vector3.up * 0.8f, PowerUp.NameFor(type), PowerUp.ColorFor(type), 1.4f);
            if (byteCompanion == null) byteCompanion = FindFirstObjectByType<ByteCompanion>();
            if (byteCompanion != null) byteCompanion.Say(ByteLine(type), 3.5f);
            OnActivated?.Invoke(type);
        }

        ByteCompanion byteCompanion;

        static string ByteLine(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Devour: return "Overcharge! The Bugs are weak now - eat them!";
                case PowerUpType.SpeedBurst: return "Speed Burst! Go, go, go!";
                case PowerUpType.Shield: return "Energy Shield up. It blocks one hit.";
                default: return "Magnetic Field! The orbs come to you.";
            }
        }

        /// <summary>Points for the next Bug eaten in this Devour Mode: 200, 400, 800, 1600.</summary>
        public int NextEatScore()
        {
            int s = 200 << Mathf.Min(eatChain, 3);
            eatChain++;
            return s;
        }

        /// <summary>Uses up the shield if Pac-Man has one. Returns true when a hit was absorbed.</summary>
        public bool ConsumeShield()
        {
            if (!HasShield) return false;
            HasShield = false;
            BlastWave.Spawn(new Vector3(transform.position.x, 0.05f, transform.position.z), 1.6f);
            return true;
        }

        void OnLifeLost()
        {
            DevourLeft = SpeedLeft = MagnetLeft = 0f;
            foreach (var bug in BugAI.All) bug.EndVulnerable();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool playing = gm == null || gm.IsPlaying;
            float dt = playing ? Time.deltaTime : 0f;

            if (DevourLeft > 0f)
            {
                DevourLeft = Mathf.Max(0f, DevourLeft - dt);
                if (DevourLeft <= 0f) eatChain = 0;
            }
            SpeedLeft = Mathf.Max(0f, SpeedLeft - dt);
            MagnetLeft = Mathf.Max(0f, MagnetLeft - dt);

            if (mover != null) mover.speed = baseSpeed * (SpeedLeft > 0f ? speedMultiplier : 1f);
            if (trail != null) trail.emitting = SpeedLeft > 0f;

            // Magnet: pull orbs a few times per second
            if (MagnetLeft > 0f && playing && mover != null && LevelBuilder.Instance != null)
            {
                magnetTick -= Time.deltaTime;
                if (magnetTick <= 0f)
                {
                    magnetTick = 0.1f;
                    LevelBuilder.Instance.PullOrbs(mover.CurrentCell, magnetRadius, transform);
                }
            }

            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            float t = Time.time;
            if (shieldBubble != null)
            {
                if (shieldBubble.gameObject.activeSelf != HasShield) shieldBubble.gameObject.SetActive(HasShield);
                if (HasShield)
                {
                    shieldBubble.localScale = Vector3.one * (1.2f + Mathf.Sin(t * 3f) * 0.05f);
                    shieldMat.SetFloat("_Intensity", 1.1f + Mathf.Sin(t * 5f) * 0.4f);
                }
            }
            if (magnetRing != null)
            {
                bool on = MagnetLeft > 0f;
                if (magnetRing.gameObject.activeSelf != on) magnetRing.gameObject.SetActive(on);
                if (on)
                {
                    float cs = LevelBuilder.Instance != null ? LevelBuilder.Instance.cellSize : 1f;
                    float r = (magnetRadius + 0.5f) * cs * (0.96f + Mathf.Sin(t * 4f) * 0.04f);
                    magnetRing.position = new Vector3(transform.position.x, 0.04f, transform.position.z);
                    magnetRing.rotation = Quaternion.identity;
                    magnetRing.localScale = new Vector3(r, 1f, r);
                    bool ending = MagnetLeft < 2f && Mathf.Repeat(t * 6f, 1f) < 0.5f;
                    magnetMat.SetFloat("_Intensity", ending ? 0.15f : 0.55f + Mathf.Sin(t * 5f) * 0.15f);
                }
            }
            // Devour: Pac-Man a little bigger with a soft magenta-white light (calm, 16 Nov: no fast flicker)
            bool devour = DevourActive;
            if (body != null) body.localScale = Vector3.one * (devour ? 1.08f : 1f);
            if (glow != null)
            {
                if (devour)
                {
                    glow.color = Color.Lerp(PowerUp.ColorFor(PowerUpType.Devour), Color.white, 0.45f + 0.1f * Mathf.Sin(t * 2f));
                    glow.intensity = glowIntensity * 1.2f;
                    glow.range = glowRange * 1.15f;
                }
                else if (glow.color != glowColor)
                {
                    glow.color = glowColor;
                    glow.intensity = glowIntensity;
                    glow.range = glowRange;
                }
            }
        }
    }
}
