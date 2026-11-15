using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Energy Orb or Nexus Core. Collected by LevelBuilder when the player enters its cell.
    /// Both bob gently; a Nexus Core also spins and pulses (core glow, aura and light).
    /// FPS (12 Nov): Energy Orbs only bob while they are near Pac-Man (~500 orbs moving every frame was
    /// a needless cost; far away the bob is not visible anyway).
    /// Magnet power-up: FlyTo() pulls the orb to Pac-Man and collects it on arrival.
    /// </summary>
    public class Collectible : MonoBehaviour
    {
        public enum Kind { Orb, NexusCore }
        public Kind kind;

        [Header("Nexus Core pulse")]
        public float pulseSpeed = 4f;

        const float OrbAnimateRange = 11f;   // world units around Pac-Man

        Transform visual, halo;
        Material coreMat, haloMat;
        Light glow;
        float phase;
        Transform flyTarget;
        float flyT;
        Vector3 flyFrom;

        void Start()
        {
            visual = transform.Find("Visual");
            phase = Random.value * 10f;
            if (kind != Kind.NexusCore || visual == null) return;

            // Materials made for this core by LevelBuilder (not shared), so changing them is safe
            if (visual.TryGetComponent(out Renderer r)) coreMat = r.sharedMaterial;
            halo = visual.Find("Halo");
            if (halo != null && halo.TryGetComponent(out Renderer hr)) haloMat = hr.sharedMaterial;
            var g = transform.Find("Glow");
            if (g != null) glow = g.GetComponent<Light>();
        }

        /// <summary>Magnet: flies to the target (Pac-Man) in a short arc, then counts as collected.</summary>
        public void FlyTo(Transform target)
        {
            flyTarget = target;
            flyT = 0f;
            flyFrom = transform.position;
        }

        void Update()
        {
            if (flyTarget != null)
            {
                flyT += Time.deltaTime / 0.35f;
                var to = flyTarget.position - Vector3.up * 0.45f;
                transform.position = Vector3.Lerp(flyFrom, to, flyT * flyT) + Vector3.up * Mathf.Sin(flyT * Mathf.PI) * 0.6f;
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, flyT);
                if (flyT >= 1f) Collect();
                return;
            }
            if (visual == null) return;

            if (kind == Kind.Orb)
            {
                var player = PlayerMovement.Instance;
                if (player == null) return;
                var d = player.transform.position - transform.position;
                if (d.x * d.x + d.z * d.z > OrbAnimateRange * OrbAnimateRange) return;
            }

            float t = Time.time + phase;
            var p = visual.localPosition;
            p.y = (kind == Kind.Orb ? 0.45f : 0.6f) + Mathf.Sin(t * 2.5f) * (kind == Kind.Orb ? 0.04f : 0.08f);
            visual.localPosition = p;
            if (kind != Kind.NexusCore) return;

            visual.Rotate(0f, 90f * Time.deltaTime, 0f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed);   // 0..1
            if (coreMat != null) coreMat.SetColor("_EmissionColor", Visuals.NeonYellow * (3f + 3f * pulse));
            if (haloMat != null) haloMat.SetFloat("_Intensity", 0.7f + 1.3f * pulse);
            if (halo != null) halo.localScale = Vector3.one * (1.6f + 0.25f * pulse);
            if (glow != null) glow.intensity = 1.8f + 2f * pulse;
        }

        public void Collect()
        {
            if (GameManager.Instance != null)
            {
                if (kind == Kind.Orb) GameManager.Instance.CollectOrb();
                else GameManager.Instance.CollectCore();
            }
            Destroy(gameObject);
        }
    }
}
