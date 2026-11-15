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
        public float behind = 0.7f;
        [Tooltip("Sideways offset so BYTE does not hide Pac-Man")]
        public float side = 0.5f;
        [Tooltip("Height above Pac-Man")]
        public float above = 0.75f;
        public float smoothTime = 0.3f;
        public float lineTime = 4f;

        Vector3 velocity;
        Transform model, ring, eye;
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

            // GDD 3.2: small rounded-rectangle hologram with ONE big digital eye.
            // Semi-transparent cyan body, dark screen face, glowing frame, big eye, antenna, hover ring.
            shellMat = Visuals.Glass(holo, 0.42f, holo * 0.7f);
            Visuals.Shape(PrimitiveType.Cube, "Body", model, Vector3.zero, new Vector3(0.46f, 0.34f, 0.28f), shellMat);
            Visuals.Shape(PrimitiveType.Sphere, "EarL", model, new Vector3(-0.25f, 0f, 0f), new Vector3(0.08f, 0.16f, 0.16f), shellMat);
            Visuals.Shape(PrimitiveType.Sphere, "EarR", model, new Vector3(0.25f, 0f, 0f), new Vector3(0.08f, 0.16f, 0.16f), shellMat);

            var screen = Visuals.Lit(Visuals.Hex("#031820"), 0f, 0.9f, Visuals.Hex("#00313D"), 1f);
            Visuals.Shape(PrimitiveType.Cube, "Screen", model, new Vector3(0f, 0f, 0.142f), new Vector3(0.38f, 0.26f, 0.01f), screen);

            coreMat = Visuals.Glow(holo, 2.5f, 0f);
            // Glowing frame around the screen
            Visuals.Shape(PrimitiveType.Cube, "FrameTop", model, new Vector3(0f, 0.14f, 0.15f), new Vector3(0.40f, 0.018f, 0.012f), coreMat);
            Visuals.Shape(PrimitiveType.Cube, "FrameBottom", model, new Vector3(0f, -0.14f, 0.15f), new Vector3(0.40f, 0.018f, 0.012f), coreMat);
            Visuals.Shape(PrimitiveType.Cube, "FrameLeft", model, new Vector3(-0.2f, 0f, 0.15f), new Vector3(0.018f, 0.28f, 0.012f), coreMat);
            Visuals.Shape(PrimitiveType.Cube, "FrameRight", model, new Vector3(0.2f, 0f, 0.15f), new Vector3(0.018f, 0.28f, 0.012f), coreMat);

            // One big digital eye (a disc facing forward) with a white pupil
            eye = new GameObject("Eye").transform;
            eye.SetParent(model, false);
            eye.localPosition = new Vector3(0f, 0.01f, 0.152f);
            var iris = Visuals.Shape(PrimitiveType.Cylinder, "Iris", eye, Vector3.zero, new Vector3(0.15f, 0.004f, 0.15f), coreMat);
            iris.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var pupil = Visuals.Shape(PrimitiveType.Cylinder, "Pupil", eye, new Vector3(0f, 0f, 0.003f), new Vector3(0.06f, 0.004f, 0.06f),
                Visuals.Glow(Color.white, 2.5f, 0f));
            pupil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Visuals.Shape(PrimitiveType.Cylinder, "Antenna", model, new Vector3(0.08f, 0.24f, 0f), new Vector3(0.015f, 0.07f, 0.015f), coreMat);
            Visuals.Shape(PrimitiveType.Sphere, "AntennaTip", model, new Vector3(0.08f, 0.32f, 0f), Vector3.one * 0.05f, coreMat);

            // Hover ring under the body
            var mb = new MeshBuilder();
            mb.Tube(MeshBuilder.Circle(0.2f, 48, new Vector3(0f, -0.26f, 0f), MeshBuilder.Flat), 0.012f, 6, true);
            ringMat = Visuals.Glow(holo, 2f, 0f);
            ring = Visuals.MeshPart("Ring", model, mb.ToMesh("Byte_Ring"), ringMat).transform;

            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

            glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.type = LightType.Point;
            glow.color = holo;
            glow.range = 2.2f;
            glow.intensity = 1.5f;
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
        void SayComplete() => Say($"Generator online. {GameManager.Instance.levelName} restored!", float.PositiveInfinity);

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
            // Face the camera so the player always sees BYTE's eye
            var cam = Camera.main;
            if (cam != null)
            {
                var look = cam.transform.position - transform.position;
                look.y *= 0.4f;
                if (look.sqrMagnitude > 0.01f)
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(look), dt * 6f);
            }
            ring.localRotation = Quaternion.Euler(0f, t * 140f, 0f);
            // Blink every few seconds
            float blink = Mathf.Repeat(t + seed, 3.7f) < 0.12f ? 0.15f : 1f;
            eye.localScale = new Vector3(1f, blink, 1f);

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
            if (!Talking || IntroSequence.Active) return;
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
