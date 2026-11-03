using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus
{
    /// <summary>
    /// BYTE: small floating cyan hologram robot that follows Pac-Man from slightly behind and above.
    /// Built from simple primitives with emissive cyan materials (flickers like a hologram).
    /// Says one short line at the start, when all Nexus Cores are collected and at Level Complete
    /// (shown in a dialogue panel above the energy bar).
    /// </summary>
    public class ByteCompanion : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Distance behind Pac-Man (opposite his facing direction)")]
        public float behind = 0.75f;
        [Tooltip("Sideways offset so BYTE does not hide Pac-Man")]
        public float side = 0.35f;
        [Tooltip("Height above Pac-Man")]
        public float above = 0.6f;
        public float smoothTime = 0.3f;
        public float lineTime = 4f;

        Vector3 velocity;
        Transform model, ring;
        Material coreMat, ringMat, shellMat;
        Light glow;
        float t, seed;

        string line;
        float lineStart, lineUntil;
        GUIStyle nameStyle, textStyle;

        public static ByteCompanion Create(Transform player)
        {
            var go = new GameObject("BYTE");
            var b = go.AddComponent<ByteCompanion>();
            b.target = player;
            b.seed = Random.value * 100f;
            b.Build();
            go.transform.position = b.FollowPoint();
            return b;
        }

        void Build()
        {
            var holo = Visuals.Cyan;
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);

            // Transparent hologram shell with a bright core inside
            shellMat = Visuals.Glass(holo, 0.25f, holo * 0.6f);
            Visuals.Shape(PrimitiveType.Sphere, "Shell", model, Vector3.zero, Vector3.one * 0.28f, shellMat);
            coreMat = Visuals.Glow(holo, 2.5f, 0f);
            Visuals.Shape(PrimitiveType.Sphere, "Core", model, Vector3.zero, Vector3.one * 0.11f, coreMat);
            // Visor bar on the front and a small antenna
            Visuals.Shape(PrimitiveType.Cube, "Visor", model, new Vector3(0f, 0.03f, 0.12f), new Vector3(0.15f, 0.035f, 0.04f), coreMat);
            Visuals.Shape(PrimitiveType.Cylinder, "Antenna", model, new Vector3(0f, 0.19f, 0f), new Vector3(0.015f, 0.05f, 0.015f), coreMat);
            Visuals.Shape(PrimitiveType.Sphere, "AntennaTip", model, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.045f, coreMat);

            // Thin spinning ring around the body
            var mb = new MeshBuilder();
            mb.Tube(MeshBuilder.Circle(0.21f, 48, Vector3.zero, MeshBuilder.Flat), 0.01f, 6, true);
            ringMat = Visuals.Glow(holo, 2f, 0f);
            ring = Visuals.MeshPart("Ring", model, mb.ToMesh("Byte_Ring"), ringMat).transform;

            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

            glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.type = LightType.Point;
            glow.color = holo;
            glow.range = 1.6f;
            glow.intensity = 1.2f;
            glow.shadows = LightShadows.None;
        }

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnAllCoresCollected += SayReturn;
            gm.OnLevelComplete += SayComplete;
            Say($"Find the {gm.CoresRequired} Nexus Cores", lineTime);
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnAllCoresCollected -= SayReturn;
            gm.OnLevelComplete -= SayComplete;
        }

        void SayReturn() => Say("Return to the Generator", lineTime + 1f);
        void SayComplete() => Say("Generator online. Neon District restored!", float.PositiveInfinity);

        /// <summary>Shows a line in the dialogue panel for 'seconds' (Infinity = until the scene ends).</summary>
        public void Say(string text, float seconds)
        {
            line = text;
            lineStart = Time.time;
            lineUntil = Time.time + seconds;
        }

        bool Talking => line != null && Time.time < lineUntil;

        /// <summary>Behind Pac-Man (opposite the way he faces), a bit to the side and above.</summary>
        Vector3 FollowPoint()
        {
            var fwd = target.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            return target.position - fwd * behind + right * side + Vector3.up * above;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            t += dt;

            var goal = FollowPoint();
            // Far away (Pac-Man respawned): jump there instead of flying across the city
            if ((transform.position - goal).sqrMagnitude > 16f)
            {
                transform.position = goal;
                velocity = Vector3.zero;
            }
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);

            // Gentle bob, face the same way as Pac-Man, spin the ring
            model.localPosition = new Vector3(0f, Mathf.Sin(t * 2.2f) * 0.05f, 0f);
            var look = target.forward;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(look), dt * 6f);
            ring.localRotation = Quaternion.Euler(12f, t * 140f, 0f);

            // Hologram flicker; brighter while talking
            float f = 0.8f + Mathf.PerlinNoise(t * 7f, seed) * 0.3f;
            if (Random.value < 0.02f) f *= 0.3f;
            float boost = Talking ? 1.4f : 1f;
            coreMat.SetFloat("_Intensity", 2.5f * f * boost);
            ringMat.SetFloat("_Intensity", 2f * f * boost);
            glow.intensity = 1.2f * f * boost;
        }

        void OnGUI()
        {
            if (!Talking) return;
            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                nameStyle.normal.textColor = Visuals.Cyan;
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
                textStyle.normal.textColor = Color.white;
            }

            // Fade in, and fade out at the end (lines that never end just stay)
            float alpha = Mathf.Clamp01((Time.time - lineStart) / 0.25f);
            if (!float.IsInfinity(lineUntil)) alpha = Mathf.Min(alpha, Mathf.Clamp01((lineUntil - Time.time) / 0.4f));

            var r = new Rect((Screen.width - 520f) / 2f, Screen.height - 132f, 520f, 46f);
            GameHUD.DrawPanel(r, alpha);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(r.x + 14f, r.y, 70f, r.height), "BYTE", nameStyle);
            GUI.Label(new Rect(r.x + 74f, r.y, r.width - 88f, r.height), line, textStyle);
            GUI.color = old;
        }
    }
}
