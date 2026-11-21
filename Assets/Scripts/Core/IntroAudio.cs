using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Sounds for the Blur-style intro (21 Nov), synthesised in code at start-up (no audio files, nothing to download):
    /// a low looping drone, whooshes for the cuts, a deep impact for the hits, a riser before the title,
    /// a heartbeat thump for the lab, a glitch burst, a UI tick and a "denied" buzz.
    /// </summary>
    public static class IntroAudio
    {
        const int SR = 44100;
        static GameObject go;
        static AudioSource drone, fx;
        static AudioClip droneClip, whoosh, impact, riser, tick, glitch, thump, deny;
        static System.Random rng = new System.Random(4242);

        public static void Init()
        {
            if (go != null) return;
            go = new GameObject("IntroAudio");
            drone = go.AddComponent<AudioSource>();
            drone.loop = true; drone.playOnAwake = false; drone.spatialBlend = 0f; drone.volume = 0f;
            fx = go.AddComponent<AudioSource>();
            fx.playOnAwake = false; fx.spatialBlend = 0f;

            droneClip = MakeDrone();
            whoosh = MakeWhoosh();
            impact = MakeImpact();
            riser = MakeRiser();
            tick = Make("Tick", 0.06f, (t, i) => Mathf.Sin(2f * Mathf.PI * 2300f * t) * Mathf.Exp(-t * 80f) * 0.5f);
            deny = Make("Deny", 0.28f, (t, i) => (Mathf.Repeat(t * 110f, 1f) < 0.5f ? 0.25f : -0.25f) * (1f - t / 0.28f));
            thump = Make("Thump", 0.4f, (t, i) => Mathf.Sin(2f * Mathf.PI * (38f + 30f * Mathf.Exp(-t * 14f)) * t) * Mathf.Exp(-t * 9f) * 0.9f);
            glitch = MakeGlitch();
            drone.clip = droneClip;
        }

        public static void DroneVolume(float v)
        {
            if (drone == null) return;
            if (v > 0f && !drone.isPlaying) drone.Play();
            drone.volume = Mathf.Clamp01(v);
        }

        public static float DroneLevel => drone != null ? drone.volume : 0f;

        public static void Whoosh(float v = 0.5f) => Play(whoosh, v);
        public static void Impact(float v = 0.9f) => Play(impact, v);
        public static void Riser(float v = 0.55f) => Play(riser, v);
        public static void Tick(float v = 0.35f) => Play(tick, v);
        public static void Glitch(float v = 0.4f) => Play(glitch, v);
        public static void Thump(float v = 0.7f) => Play(thump, v);
        public static void Deny(float v = 0.35f) => Play(deny, v);

        static void Play(AudioClip c, float v) { if (fx != null && c != null) fx.PlayOneShot(c, v); }

        /// <summary>Removes the audio object (after 'delay' seconds, so the last hit can ring out).</summary>
        public static void Dispose(float delay = 0f)
        {
            if (go != null) Object.Destroy(go, delay);
            go = null;
        }

        // ---------------------------------------------------------------- synthesis

        static float Noise() => (float)rng.NextDouble() * 2f - 1f;

        static AudioClip Make(string name, float seconds, System.Func<float, int, float> f)
        {
            int n = Mathf.CeilToInt(SR * seconds);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = f(i / (float)SR, i);
            return ToClip(name, d);
        }

        static AudioClip ToClip(string name, float[] d)
        {
            var c = AudioClip.Create(name, d.Length, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>8 s seamless loop: two detuned saws through a slowly moving low-pass + a sub sine.</summary>
        static AudioClip MakeDrone()
        {
            const float L = 8f;
            int n = (int)(SR * L);
            var d = new float[n];
            float lp = 0f, lp2 = 0f;
            // run twice so the filter state at the loop start matches the end (no click)
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SR;
                    float s1 = 2f * Mathf.Repeat(t * 55f, 1f) - 1f;
                    float s2 = 2f * Mathf.Repeat(t * 55.25f, 1f) - 1f;   // 442 cycles in 8 s: loops cleanly
                    float sub = Mathf.Sin(2f * Mathf.PI * 27.5f * t);
                    float a = 0.018f + 0.012f * Mathf.Sin(2f * Mathf.PI * t / L);
                    lp += ((s1 + s2) * 0.5f - lp) * a;
                    lp2 += (lp - lp2) * a;
                    float swell = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * t / L * 2f);
                    if (pass == 1) d[i] = (lp2 * 0.75f + sub * 0.4f) * 0.45f * swell;
                }
            return ToClip("Drone", d);
        }

        static AudioClip MakeWhoosh()
        {
            const float L = 0.75f;
            int n = (int)(SR * L);
            var d = new float[n];
            float lp = 0f, slow = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, u = t / L;
                float bell = Mathf.Sin(Mathf.PI * u);
                lp += (Noise() - lp) * Mathf.Lerp(0.03f, 0.4f, bell * bell);
                slow += (lp - slow) * 0.01f;
                d[i] = (lp - slow) * bell * bell * 1.4f;
            }
            return ToClip("Whoosh", d);
        }

        static AudioClip MakeImpact()
        {
            const float L = 2.8f;
            int n = (int)(SR * L);
            var d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = 32f + 75f * Mathf.Exp(-t * 7f);
                phase += 2f * Mathf.PI * f / SR;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t * 1.5f);
                lp += (Noise() - lp) * 0.25f;
                float hit = lp * Mathf.Exp(-t * 22f) * 0.9f;
                float ring = Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 4.5f) * 0.12f;
                d[i] = (float)System.Math.Tanh((body + hit + ring) * 1.4f) * 0.85f;
            }
            return ToClip("Impact", d);
        }

        static AudioClip MakeRiser()
        {
            const float L = 2.4f;
            int n = (int)(SR * L);
            var d = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, u = t / L;
                lp += (Noise() - lp) * Mathf.Lerp(0.01f, 0.45f, u * u);
                float f = 110f * Mathf.Pow(8f, u);
                phase += 2f * Mathf.PI * f / SR;
                float env = Mathf.Pow(u, 1.6f) * (u > 0.97f ? (1f - u) / 0.03f : 1f);
                d[i] = (lp * 0.6f + Mathf.Sin(phase) * 0.25f) * env;
            }
            return ToClip("Riser", d);
        }

        static AudioClip MakeGlitch()
        {
            const float L = 0.32f;
            int n = (int)(SR * L);
            var d = new float[n];
            float hold = 0f;
            int step = 1;
            for (int i = 0; i < n; i++)
            {
                if (i % 180 == 0) { hold = Noise(); step = 1 + rng.Next(6); }
                float v = Mathf.Round(hold * step) / step;
                float env = 1f - i / (float)n;
                d[i] = v * env * 0.45f * (((i / 900) % 2) == 0 ? 1f : 0.3f);
            }
            return ToClip("Glitch", d);
        }
    }
}
