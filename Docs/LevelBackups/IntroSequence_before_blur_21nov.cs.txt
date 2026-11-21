using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Opening cinematic (GDD 2.2 + 2.3), plays once when the game starts:
    ///  1. Black screen, story lines.
    ///  2. Abandoned lab under the Neon District: lights flicker on, terminal reboots Pac-Man in his pod.
    ///  3. The pod opens, Pac-Man wakes up and floats out.
    ///  4. BYTE appears and explains the mission (dialogue box).
    ///  5. Title screen over the city (camera orbits): PAC-MAN: NEXUS - THE LOST CITY, press ENTER.
    ///  6. Camera swoops down to Pac-Man and the level starts.
    /// ENTER / click = next line, ESC = skip to the title. While Active, GameManager.IsPlaying is false,
    /// so nothing moves, and the HUD, minimap and BYTE's normal dialogue are hidden.
    /// Restarting the level (game over) does not play it again.
    /// Level 2+ (StartLevelCard): no lab, only the title card over the orbiting city with BYTE's story lines
    /// for that level (GDD story events), then the same swoop down to Pac-Man.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        // FPS: no GUILayout here, so skip IMGUI's extra Layout pass
        void Awake() => useGUILayout = false;

        public static bool Active { get; private set; }
        /// <summary>Set by GameManager.Restart so retrying a level skips its title card.</summary>
        public static bool SkipNextLevelCard;
        static bool shownThisSession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = false;
            shownThisSession = false;
            SkipNextLevelCard = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (shownThisSession) return;
            var lb = FindFirstObjectByType<LevelBuilder>();
            if (lb == null || lb.levelNumber != 1) return;   // other levels use StartLevelCard
            shownThisSession = true;
            Active = true;
            new GameObject("IntroSequence").AddComponent<IntroSequence>();
        }

        /// <summary>Title card + BYTE's story lines at the start of Level 2+ (called by LevelBuilder).</summary>
        public static void StartLevelCard(LevelBuilder lb)
        {
            if (SkipNextLevelCard) { SkipNextLevelCard = false; return; }
            Active = true;
            var card = new GameObject("LevelCard").AddComponent<IntroSequence>();
            card.cardMode = true;
            card.cardLevel = lb;
        }

        static string[] CardLinesFor(int levelNumber)
        {
            switch (levelNumber)
            {
                case 2: return new[]
                {
                    "Wait... I remember this place. Skyline Zone ran the city's transport and communication systems.",
                    "The connections between the regions were cut on purpose. This was no accident.",
                    "But the central system was built to protect the city... so why would it do this?",
                    "Careful: purple Sentinels guard the Nexus Cores here. Get too close and they attack.",
                    "Collect the 10 Cores and bring the Skyline Generator back online!"
                };
                default: return new[] { "A new region. Find the 10 Nexus Cores and restart the Generator!" };
            }
        }

        bool cardMode;
        LevelBuilder cardLevel;

        static readonly Vector3 LabOrigin = new Vector3(-70f, 0f, 60f);
        static Vector3 Lab(float x, float y, float z) => LabOrigin + new Vector3(x, y, z);

        static readonly string[] StoryLines =
        {
            "THE CITY OF NEXUS WENT DARK.",
            "ITS AI SHUT DOWN EVERY DISTRICT, ONE BY ONE.",
            "DEEP BELOW THE NEON DISTRICT, SOMETHING WAKES UP..."
        };

        static readonly string[] TerminalLines =
        {
            "> EMERGENCY POWER ............ OK",
            "> ENERGY UNIT P-4C ........... REBOOTING",
            "> MEMORY ..................... CORRUPTED",
            "> PAC-MAN .................... ONLINE"
        };

        static readonly string[] ByteLines =
        {
            "Whoa... you're awake! I'm BYTE, the city's maintenance assistant. At least... I think I am.",
            "The city AI shut everything down. Five regions, five Generators, all offline.",
            "The Neon District is right above us. Collect the 10 Nexus Cores and bring them to the Generator.",
            "And watch out for the Bugs. They used to fix the city... now they hunt anything that moves.",
            "Come on, Pac-Man. Let's bring the light back!"
        };

        Camera cam;
        CameraFollow follow;
        Transform player;
        GameObject byteGo;
        LevelBuilder level;
        Transform lab, podGlass, podRing;
        Light[] labLights;
        Light alarm, podLight;
        Material podRingMat;
        Vector3 startPos, startScale;
        Quaternion startRot;
        float savedFog;

        // What OnGUI draws
        float black = 1f;
        string story; float storyStart;
        int terminalCount; float terminalStart;
        string dialog; float dialogStart;
        float titleAlpha;
        bool showPrompt;
        float flash;

        bool skip, advance;
        GUIStyle storyStyle, termStyle, nameStyle, lineStyle, titleBig, titleNexus, titleSub, promptStyle, hintStyle;

        void Start() => StartCoroutine(Run());

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) skip = true;
            if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                advance = true;
            flash = Mathf.Max(0f, flash - Time.deltaTime * 2.5f);
        }

        IEnumerator Run()
        {
            yield return null;
            while (PlayerMovement.Instance == null || Camera.main == null) yield return null;

            level = LevelBuilder.Instance;
            cam = Camera.main;
            follow = cam.GetComponent<CameraFollow>();
            if (follow != null) follow.enabled = false;
            player = PlayerMovement.Instance.transform;
            var byteComp = FindFirstObjectByType<ByteCompanion>();
            if (byteComp != null) { byteGo = byteComp.gameObject; byteGo.SetActive(false); }

            startPos = player.position; startRot = player.rotation; startScale = player.localScale;
            savedFog = RenderSettings.fogDensity;
            RenderSettings.fogDensity = 0.012f;

            if (cardMode)
            {
                if (byteGo != null) byteGo.SetActive(true);
                yield return Title();
                yield return Swoop();
                Finish();
                yield break;
            }

            BuildLab();
            player.position = Lab(0f, 0.75f, 0f);
            player.rotation = Quaternion.LookRotation(Vector3.back);
            player.localScale = startScale * 0.7f;
            SetLabLights(0f);
            SetCamera(Lab(0f, 1.7f, -4.6f), Lab(0f, 0.9f, 0f), 45f);

            if (!skip) yield return Story();
            if (!skip) yield return Awakening();
            if (!skip) yield return MeetByte();
            yield return Title();
            yield return Swoop();
            Finish();
        }

        // ---------------------------------------------------------------- phases

        IEnumerator Story()
        {
            black = 1f;
            foreach (var l in StoryLines)
            {
                story = l; storyStart = Time.time;
                yield return Wait(2.6f, true);
                if (skip) yield break;
            }
            story = null;
        }

        IEnumerator Awakening()
        {
            // Fade in while the lights flicker on and the camera slowly moves in
            terminalStart = Time.time;
            float t0 = Time.time;
            var camFrom = Lab(0f, 1.7f, -4.6f);
            var camTo = Lab(0.25f, 1.35f, -2.9f);
            while (Time.time - t0 < 4.2f && !skip)
            {
                float t = (Time.time - t0) / 4.2f;
                black = 1f - Mathf.Clamp01(t * 2.5f);
                float flicker = t < 0.55f ? (Random.value < 0.35f ? 1f : 0.1f) : 1f;
                SetLabLights(Mathf.Clamp01(t * 1.6f) * flicker);
                terminalCount = Mathf.Min(TerminalLines.Length, Mathf.FloorToInt((Time.time - terminalStart) / 0.9f) + 1);
                SetCamera(Vector3.Lerp(camFrom, camTo, Smooth(t)), Lab(0f, 0.9f, 0f), 45f);
                PulsePod(t);
                yield return null;
            }
            black = 0f;
            SetLabLights(1f);
            terminalCount = TerminalLines.Length;
            if (skip) yield break;

            // Wake up: flash, the glass lifts, Pac-Man grows to full size and floats out of the pod
            flash = 1f;
            if (podLight != null) { podLight.color = Visuals.NeonYellow; podLight.intensity = 6f; }
            t0 = Time.time;
            var glassFrom = podGlass.position;
            var pFrom = player.position;
            var pTo = Lab(0f, 0.8f, -1.2f);
            while (Time.time - t0 < 2.4f && !skip)
            {
                float t = (Time.time - t0) / 2.4f;
                podGlass.position = glassFrom + Vector3.up * (2.2f * Smooth(Mathf.Clamp01(t * 1.6f)));
                float k = Smooth(Mathf.Clamp01((t - 0.3f) / 0.7f));
                player.position = Vector3.Lerp(pFrom, pTo, k) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f;
                player.localScale = Vector3.Lerp(startScale * 0.7f, startScale, Smooth(Mathf.Clamp01(t * 2f)));
                if (podLight != null) podLight.intensity = Mathf.Lerp(6f, 2f, t);
                yield return null;
            }
            terminalCount = 0;
            yield return Wait(0.5f, false);
        }

        IEnumerator MeetByte()
        {
            // BYTE materialises next to Pac-Man (ByteCompanion places itself beside him)
            if (byteGo != null) byteGo.SetActive(true);
            flash = 0.8f;
            SetCamera(Lab(1.7f, 1.7f, -4.6f), Lab(-0.2f, 0.85f, -0.9f), 40f);
            yield return Wait(0.8f, false);

            for (int i = 0; i < ByteLines.Length && !skip; i++)
            {
                dialog = ByteLines[i]; dialogStart = Time.time;
                // slow orbit so the shot is not static
                float t0 = Time.time, dur = 3.2f + ByteLines[i].Length * 0.035f;
                advance = false;
                while (Time.time - t0 < dur && !skip)
                {
                    if (advance)
                    {
                        advance = false;
                        // first press shows the whole line, second goes to the next one
                        if (VisibleChars(dialog, dialogStart) < dialog.Length) dialogStart = -100f;
                        else break;
                    }
                    float a = (Time.time - t0) * 3f;
                    var p = Lab(1.7f, 1.7f, -4.6f) + new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad) * 1.4f, 0f, 0f);
                    SetCamera(p, Lab(-0.2f, 0.85f, -0.9f), 40f);
                    player.position = Lab(0f, 0.8f, -1.2f) + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.06f;
                    yield return null;
                }
            }
            dialog = null;
            // Pac-Man shoots up through the lab ceiling
            flash = 1f;
            yield return Fade(1f, 0.5f);
        }

        IEnumerator Title()
        {
            dialog = null; story = null; terminalCount = 0;
            // Pac-Man back on his start street; BYTE jumps along (it follows him)
            player.position = startPos; player.rotation = startRot; player.localScale = startScale;
            if (byteGo != null && !byteGo.activeSelf) byteGo.SetActive(true);
            if (lab != null) lab.gameObject.SetActive(false);
            RenderSettings.fogDensity = 0.009f;

            var center = CityCenter();
            float size = level != null ? level.Width * level.cellSize : 33f;
            float angle = 200f;
            OrbitCamera(center, size, angle);
            yield return Fade(0f, 0.8f);

            float t0 = Time.time;
            advance = false;
            // Level card: BYTE's story lines first (ENTER = whole line / next line, auto-advance, ESC = skip)
            string[] lines = cardMode ? CardLinesFor(cardLevel != null ? cardLevel.levelNumber : 2) : null;
            int lineIndex = 0;
            float lineShownAt = t0 + 1.2f;
            if (lines != null && lines.Length > 0) { dialog = lines[0]; dialogStart = lineShownAt; }
            while (true)
            {
                angle += Time.deltaTime * 5f;
                OrbitCamera(center, size, angle);
                titleAlpha = Mathf.Clamp01((Time.time - t0) / 1.2f);
                if (dialog != null)
                {
                    showPrompt = false;
                    bool timeUp = Time.time - lineShownAt > 3.4f + dialog.Length * 0.035f;
                    if (skip) dialog = null;
                    else if ((advance && Time.time > lineShownAt) || timeUp)
                    {
                        if (!timeUp && VisibleChars(dialog, dialogStart) < dialog.Length) dialogStart = -100f;
                        else
                        {
                            lineIndex++;
                            dialog = lineIndex < lines.Length ? lines[lineIndex] : null;
                            dialogStart = lineShownAt = Time.time;
                        }
                    }
                    advance = false;
                    if (dialog == null) t0 = Time.time - 1.2f;   // prompt appears shortly after the last line
                    yield return null;
                    continue;
                }
                showPrompt = Time.time - t0 > 1.5f;
                if (showPrompt && advance) break;
                if (!showPrompt) advance = false;
                yield return null;
            }
            advance = false;
        }

        IEnumerator Swoop()
        {
            showPrompt = false;
            var fromPos = cam.transform.position;
            var fromRot = cam.transform.rotation;
            float fromFov = cam.fieldOfView;
            float pitch = follow != null ? follow.viewPitch : 66f;
            var offset = follow != null ? follow.Offset : Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -9.5f);
            float toFov = follow != null ? follow.viewFov : 50f;
            float t0 = Time.time, dur = 2.2f;
            while (Time.time - t0 < dur)
            {
                float t = Smooth((Time.time - t0) / dur);
                titleAlpha = 1f - Mathf.Clamp01((Time.time - t0) / 0.6f);
                cam.transform.position = Vector3.Lerp(fromPos, player.position + offset, t);
                cam.transform.rotation = Quaternion.Slerp(fromRot, Quaternion.Euler(pitch, 0f, 0f), t);
                cam.fieldOfView = Mathf.Lerp(fromFov, toFov, t);
                RenderSettings.fogDensity = Mathf.Lerp(0.009f, savedFog, t);
                yield return null;
            }
            titleAlpha = 0f;
        }

        void Finish()
        {
            RenderSettings.fogDensity = savedFog;
            if (follow != null)
            {
                cam.transform.position = player.position + follow.Offset;
                cam.transform.rotation = Quaternion.Euler(follow.viewPitch, 0f, 0f);
                follow.enabled = true;
            }
            if (byteGo != null) byteGo.SetActive(true);
            if (lab != null) Destroy(lab.gameObject);
            Active = false;
            var byteComp = byteGo != null ? byteGo.GetComponent<ByteCompanion>() : null;
            if (byteComp != null)
                byteComp.Say(cardMode ? "Find the 10 Nexus Cores. Watch the Sentinels!" : "Arrow keys to move. Find the 10 Nexus Cores!", 6f);
            Destroy(gameObject, 0.1f);
        }

        // ---------------------------------------------------------------- helpers

        IEnumerator Wait(float seconds, bool canAdvance)
        {
            float end = Time.time + seconds;
            advance = false;
            while (Time.time < end && !skip)
            {
                if (canAdvance && advance) { advance = false; break; }
                yield return null;
            }
        }

        IEnumerator Fade(float to, float seconds)
        {
            float from = black, t0 = Time.time;
            while (Time.time - t0 < seconds)
            {
                black = Mathf.Lerp(from, to, (Time.time - t0) / seconds);
                yield return null;
            }
            black = to;
        }

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        static int VisibleChars(string text, float start) =>
            text == null ? 0 : Mathf.Clamp(Mathf.FloorToInt((Time.time - start) * 45f), 0, text.Length);

        void SetCamera(Vector3 pos, Vector3 lookAt, float fov)
        {
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - pos);
            cam.fieldOfView = fov;
        }

        Vector3 CityCenter()
        {
            if (level == null) return Vector3.zero;
            return level.CellToWorld(new Vector2Int(level.Width / 2, level.Height / 2));
        }

        void OrbitCamera(Vector3 center, float size, float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad, r = size * 0.85f;
            var pos = center + new Vector3(Mathf.Sin(a) * r, size * 0.62f, Mathf.Cos(a) * r);
            SetCamera(pos, center + Vector3.up * 1.5f, 50f);
        }

        void SetLabLights(float k)
        {
            if (labLights == null) return;
            foreach (var l in labLights) if (l != null) l.intensity = l.range * 0.55f * k;
            if (alarm != null) alarm.intensity = 4f * Mathf.Max(0.25f, k);
        }

        void PulsePod(float t)
        {
            if (podLight != null) podLight.intensity = 1.5f + Mathf.Sin(Time.time * 6f) * 0.8f;
            if (podRing != null) podRing.localRotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
            if (alarm != null) alarm.transform.rotation = Quaternion.Euler(35f, Time.time * 220f, 0f);
        }

        void LateUpdate()
        {
            if (podRing != null) podRing.localRotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
            if (alarm != null && alarm.isActiveAndEnabled) alarm.transform.rotation = Quaternion.Euler(35f, Time.time * 220f, 0f);
        }

        // ---------------------------------------------------------------- the lab

        void BuildLab()
        {
            lab = new GameObject("IntroLab").transform;
            lab.position = LabOrigin;

            var floorMat = Visuals.Lit(Visuals.Hex("#0B1020"), 0.7f, 0.75f);
            var wallMat = Visuals.Lit(Visuals.Hex("#0E1428"), 0.5f, 0.45f);
            var panelMat = Visuals.Lit(Visuals.Hex("#151D38"), 0.6f, 0.6f);
            var cyanGlow = Visuals.GetMaterial(Visuals.Cyan, Visuals.Cyan, 3f);
            var yellowGlow = Visuals.GetMaterial(Visuals.NeonYellow * 0.5f, Visuals.NeonYellow, 0.9f);
            var redGlow = Visuals.GetMaterial(Visuals.CorruptedRed, Visuals.CorruptedRed, 3f);
            var metal = Visuals.Lit(Visuals.Hex("#232B45"), 0.9f, 0.7f);

            // Room: floor, back + side walls, ceiling (the camera side stays open)
            Visuals.Shape(PrimitiveType.Cube, "Floor", lab, new Vector3(0f, -0.05f, -1f), new Vector3(12f, 0.1f, 10f), floorMat);
            Visuals.Shape(PrimitiveType.Cube, "Ceiling", lab, new Vector3(0f, 4f, -1f), new Vector3(12f, 0.1f, 10f), wallMat);
            Visuals.Shape(PrimitiveType.Cube, "BackWall", lab, new Vector3(0f, 2f, 3f), new Vector3(12f, 4f, 0.2f), wallMat);
            Visuals.Shape(PrimitiveType.Cube, "LeftWall", lab, new Vector3(-5f, 2f, -1f), new Vector3(0.2f, 4f, 10f), wallMat);
            Visuals.Shape(PrimitiveType.Cube, "RightWall", lab, new Vector3(5f, 2f, -1f), new Vector3(0.2f, 4f, 10f), wallMat);

            // Floor guide lines leading to the pod
            for (int i = 0; i < 6; i++)
            {
                Visuals.Shape(PrimitiveType.Cube, "FloorLineL", lab, new Vector3(-0.9f, 0.01f, -1.2f - i * 0.8f), new Vector3(0.05f, 0.02f, 0.5f), cyanGlow);
                Visuals.Shape(PrimitiveType.Cube, "FloorLineR", lab, new Vector3(0.9f, 0.01f, -1.2f - i * 0.8f), new Vector3(0.05f, 0.02f, 0.5f), cyanGlow);
            }

            // Wall panels with glowing screens and cables
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                float x = i * 1.9f;
                Visuals.Shape(PrimitiveType.Cube, "Panel", lab, new Vector3(x, 1.6f, 2.85f), new Vector3(1.4f, 2.2f, 0.1f), panelMat);
                // wall screens dimmer than the light bars, so the reboot terminal stays readable over them
                var screenMat = (i % 2 == 0) ? Visuals.GetMaterial(Visuals.Cyan * 0.6f, Visuals.Cyan, 1.1f) : Visuals.GetMaterial(Visuals.Purple * 0.6f, Visuals.Purple, 1.0f);
                Visuals.Shape(PrimitiveType.Cube, "Screen", lab, new Vector3(x, 2.0f, 2.78f), new Vector3(1.1f, 0.6f, 0.02f), screenMat);
                for (int k = 0; k < 3; k++)
                    Visuals.Shape(PrimitiveType.Cube, "Bar", lab, new Vector3(x - 0.35f + k * 0.35f, 1.1f, 2.78f),
                        new Vector3(0.18f, 0.08f + k * 0.12f, 0.02f), k == 2 ? redGlow : cyanGlow);
                Visuals.Shape(PrimitiveType.Cylinder, "Cable", lab, new Vector3(x + 0.6f, 2f, 2.7f), new Vector3(0.06f, 2f, 0.06f), metal);
            }
            // Side wall strips
            for (int s = -1; s <= 1; s += 2)
            {
                Visuals.Shape(PrimitiveType.Cube, "WallStrip", lab, new Vector3(s * 4.88f, 3.2f, -1f), new Vector3(0.03f, 0.05f, 9f), cyanGlow);
                Visuals.Shape(PrimitiveType.Cube, "WallStripLow", lab, new Vector3(s * 4.88f, 0.4f, -1f), new Vector3(0.03f, 0.05f, 9f),
                    Visuals.GetMaterial(Visuals.Purple, Visuals.Purple, 2.5f));
                for (int k = 0; k < 3; k++)
                    Visuals.Shape(PrimitiveType.Cube, "Locker", lab, new Vector3(s * 4.6f, 1.2f, 1.5f - k * 1.6f), new Vector3(0.5f, 2.4f, 1.2f), panelMat);
            }
            // Ceiling light bars
            var lights = new System.Collections.Generic.List<Light>();
            for (int i = 0; i < 3; i++)
            {
                var pos = new Vector3(0f, 3.92f, 1.5f - i * 2.5f);
                Visuals.Shape(PrimitiveType.Cube, "CeilingBar", lab, pos, new Vector3(3f, 0.06f, 0.2f), cyanGlow);
                var l = new GameObject("LabLight").AddComponent<Light>();
                l.transform.SetParent(lab, false);
                l.transform.localPosition = pos + Vector3.down * 0.4f;
                l.type = LightType.Point; l.color = Visuals.Hex("#7FE9FF"); l.range = 7f; l.shadows = LightShadows.None;
                lights.Add(l);
            }
            labLights = lights.ToArray();

            // Rotating red alarm light in the corner
            Visuals.Shape(PrimitiveType.Sphere, "AlarmBulb", lab, new Vector3(-4.6f, 3.6f, 2.6f), Vector3.one * 0.25f, redGlow);
            alarm = new GameObject("Alarm").AddComponent<Light>();
            alarm.transform.SetParent(lab, false);
            alarm.transform.localPosition = new Vector3(-4.4f, 3.5f, 2.4f);
            alarm.type = LightType.Spot; alarm.spotAngle = 70f; alarm.range = 12f; alarm.color = Visuals.CorruptedRed; alarm.shadows = LightShadows.None;

            // The pod: metal base, glowing ring, glass tube, top cap
            Visuals.Shape(PrimitiveType.Cylinder, "PodBase", lab, new Vector3(0f, 0.12f, 0f), new Vector3(1.6f, 0.12f, 1.6f), metal);
            var ring = Visuals.Shape(PrimitiveType.Cylinder, "PodRing", lab, new Vector3(0f, 0.26f, 0f), new Vector3(1.45f, 0.01f, 1.45f), yellowGlow);
            podRing = ring.transform;
            podRingMat = yellowGlow;
            var glass = Visuals.Shape(PrimitiveType.Cylinder, "PodGlass", lab, new Vector3(0f, 1.2f, 0f), new Vector3(1.25f, 0.95f, 1.25f),
                Visuals.Glass(Visuals.Cyan, 0.18f, Visuals.Cyan * 0.4f));
            podGlass = glass.transform;
            var cap = Visuals.Shape(PrimitiveType.Cylinder, "PodCap", podGlass, new Vector3(0f, 1.05f, 0f), new Vector3(1.15f, 0.08f, 1.15f), metal);
            Visuals.Shape(PrimitiveType.Cylinder, "PodCapGlow", cap.transform, new Vector3(0f, -0.6f, 0f), new Vector3(0.8f, 0.3f, 0.8f), cyanGlow);
            Visuals.Shape(PrimitiveType.Cylinder, "PodTop", lab, new Vector3(0f, 3.75f, 0f), new Vector3(1.8f, 0.25f, 1.8f), metal);
            // Pipes from the pod into the wall
            Visuals.Shape(PrimitiveType.Cube, "PipeL", lab, new Vector3(-0.9f, 0.18f, 1.5f), new Vector3(0.15f, 0.15f, 3f), metal);
            Visuals.Shape(PrimitiveType.Cube, "PipeR", lab, new Vector3(0.9f, 0.18f, 1.5f), new Vector3(0.15f, 0.15f, 3f), metal);

            podLight = new GameObject("PodLight").AddComponent<Light>();
            podLight.transform.SetParent(lab, false);
            podLight.transform.localPosition = new Vector3(0f, 1.0f, -0.6f);
            podLight.type = LightType.Point; podLight.range = 4f; podLight.color = Visuals.Cyan; podLight.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- drawing

        void CreateStyles()
        {
            int h = Screen.height;
            storyStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, h / 26), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            storyStyle.normal.textColor = Visuals.Cyan;
            termStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(13, h / 46), alignment = TextAnchor.UpperLeft, font = Font.CreateDynamicFontFromOSFont("Consolas", 16) };
            termStyle.normal.textColor = Visuals.Hex("#7FE9FF");
            nameStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(15, h / 40), fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
            nameStyle.normal.textColor = Visuals.Cyan;
            lineStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(16, h / 34), alignment = TextAnchor.UpperLeft, wordWrap = true };
            lineStyle.normal.textColor = Color.white;
            titleBig = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(40, h / 7), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleBig.normal.textColor = Visuals.NeonYellow;
            titleNexus = new GUIStyle(titleBig) { fontSize = Mathf.Max(34, h / 9) };
            titleNexus.normal.textColor = Visuals.Cyan;
            titleSub = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(16, h / 30), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleSub.normal.textColor = Color.white;
            promptStyle = new GUIStyle(titleSub) { fontSize = Mathf.Max(15, h / 34) };
            promptStyle.normal.textColor = Visuals.NeonYellow;
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(11, h / 60), alignment = TextAnchor.LowerRight };
            hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
        }

        void OnGUI()
        {
            if (!Active) return;
            if (storyStyle == null) CreateStyles();
            float W = Screen.width, H = Screen.height;

            // Cinematic bars (not on the title screen)
            if (titleAlpha <= 0f)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, W, H * 0.08f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, H * 0.92f, W, H * 0.08f), Texture2D.whiteTexture);
            }

            // Reboot terminal (top left): dark panel so it stays readable over the bright lab screens,
            // lines typed letter by letter, coloured status words, blinking cursor
            if (terminalCount > 0) DrawTerminal(W, H);

            // BYTE dialogue box, with a small hologram portrait
            if (dialog != null)
            {
                var r = new Rect(W * 0.12f, H * 0.70f, W * 0.76f, H * 0.17f);
                GameHUD.DrawPanel(r, 1f);
                float icon = Mathf.Min(r.height - 20f, 56f);
                var ir = new Rect(r.x + 14f, r.y + (r.height - icon) / 2f, icon, icon);
                DrawBytePortrait(ir);
                float tx = ir.xMax + 14f;
                GUI.color = Color.white;
                GUI.Label(new Rect(tx, r.y + 10f, 200f, nameStyle.fontSize * 1.5f), "BYTE", nameStyle);
                GUI.Label(new Rect(tx, r.y + 14f + nameStyle.fontSize * 1.4f, r.xMax - tx - 18f, r.height - nameStyle.fontSize * 2f),
                    dialog.Substring(0, VisibleChars(dialog, dialogStart)), lineStyle);
            }

            // Black fade
            if (black > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, black);
                GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            }

            // Story text on black
            if (story != null)
            {
                float a = Mathf.Clamp01((Time.time - storyStart) / 0.6f) * Mathf.Clamp01((storyStart + 2.6f - Time.time) / 0.5f);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(0, H * 0.42f, W, H * 0.16f), story, storyStyle);
            }

            // Title screen
            if (titleAlpha > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.35f * titleAlpha);
                GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
                if (cardMode && cardLevel != null)
                {
                    GUI.color = new Color(1f, 1f, 1f, titleAlpha);
                    GUI.Label(new Rect(0, H * 0.12f, W, H * 0.06f), "PAC-MAN: NEXUS", promptStyle);
                    var accent = GameHUD.Accent;
                    var keep = titleNexus.normal.textColor;
                    titleNexus.normal.textColor = accent;
                    GlowLabel(new Rect(0, H * 0.2f, W, H * 0.2f), cardLevel.levelName.ToUpper(), titleNexus, accent, titleAlpha);
                    titleNexus.normal.textColor = keep;
                    GUI.color = new Color(1f, 1f, 1f, titleAlpha);
                    GUI.Label(new Rect(0, H * 0.42f, W, H * 0.06f), "L E V E L   " + cardLevel.levelNumber, titleSub);
                }
                else
                {
                    GlowLabel(new Rect(0, H * 0.14f, W, H * 0.2f), "PAC-MAN", titleBig, Visuals.NeonYellow, titleAlpha);
                    GlowLabel(new Rect(0, H * 0.31f, W, H * 0.16f), "NEXUS", titleNexus, Visuals.Cyan, titleAlpha);
                    GUI.color = new Color(1f, 1f, 1f, titleAlpha);
                    GUI.Label(new Rect(0, H * 0.47f, W, H * 0.06f), "T H E   L O S T   C I T Y", titleSub);
                    var lc = titleSub.normal.textColor;
                    titleSub.normal.textColor = Visuals.Hex("#FF2BD6");
                    GUI.Label(new Rect(0, H * 0.58f, W, H * 0.05f), "LEVEL 1  -  NEON DISTRICT", titleSub);
                    titleSub.normal.textColor = lc;
                }
                if (showPrompt)
                {
                    float blink = 0.55f + Mathf.Sin(Time.time * 4f) * 0.45f;
                    // Dark band behind the prompt so it stays readable on bright skies (Level 2 sunset)
                    GUI.color = new Color(0.02f, 0.02f, 0.06f, 0.6f * titleAlpha);
                    GUI.DrawTexture(new Rect(W * 0.3f, H * 0.715f, W * 0.4f, H * 0.07f), Texture2D.whiteTexture);
                    GUI.color = new Color(1f, 1f, 1f, blink * titleAlpha);
                    GUI.Label(new Rect(0, H * 0.72f, W, H * 0.06f), "PRESS ENTER TO START", promptStyle);
                }
            }

            // White flash
            if (flash > 0f)
            {
                GUI.color = new Color(0.8f, 1f, 1f, flash * 0.8f);
                GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            }

            // Controls hint
            if (titleAlpha <= 0f)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(0, 0, W - 16f, H - 8f), "ENTER: next   ESC: skip", hintStyle);
            }
            GUI.color = Color.white;
        }

        static Texture2D softDot;

        /// <summary>BYTE's hologram face: cyan glow, round head, dark visor with a blinking eye.</summary>
        static void DrawBytePortrait(Rect r)
        {
            if (softDot == null) softDot = Visuals.GradientTexture(true);
            float t = Time.time;
            var old = GUI.color;
            var cyan = Visuals.Cyan;
            GUI.color = new Color(cyan.r, cyan.g, cyan.b, 0.45f + 0.1f * Mathf.Sin(t * 3f));
            GUI.DrawTexture(new Rect(r.x - r.width * 0.25f, r.y - r.height * 0.25f, r.width * 1.5f, r.height * 1.5f), softDot);
            GUI.color = new Color(0.55f, 0.95f, 1f, 0.95f);
            GUI.DrawTexture(new Rect(r.x + r.width * 0.12f, r.y + r.height * 0.12f, r.width * 0.76f, r.height * 0.76f), softDot);
            GUI.DrawTexture(new Rect(r.x + r.width * 0.22f, r.y + r.height * 0.22f, r.width * 0.56f, r.height * 0.56f), softDot);
            // visor + eye
            GUI.color = new Color(0.02f, 0.08f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(r.x + r.width * 0.24f, r.y + r.height * 0.42f, r.width * 0.52f, r.height * 0.16f), Texture2D.whiteTexture);
            bool blink = Mathf.Repeat(t, 3.2f) < 0.12f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(r.x + r.width * 0.44f, r.y + r.height * (blink ? 0.49f : 0.45f), r.width * 0.12f, r.height * (blink ? 0.02f : 0.1f)), Texture2D.whiteTexture);
            // antenna
            GUI.color = new Color(0.55f, 0.95f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(r.center.x - 1f, r.y - r.height * 0.05f, 2f, r.height * 0.2f), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawTerminal(float W, float H)
        {
            int fs = termStyle.fontSize;
            float lineH = fs * 1.55f;
            var panel = new Rect(W * 0.04f, H * 0.105f, Mathf.Max(W * 0.46f, fs * 30f), lineH * (TerminalLines.Length + 1) + 22f);
            float appear = Mathf.Clamp01((Time.time - terminalStart) / 0.3f);
            GameHUD.DrawPanel(panel, appear);
            var old = GUI.color;
            termStyle.richText = true;

            GUI.color = new Color(1f, 1f, 1f, 0.7f * appear);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 8f, panel.width - 28f, lineH), "<color=#5FA9C0>NEXUS LAB // EMERGENCY REBOOT</color>", termStyle);
            GUI.color = new Color(1f, 1f, 1f, appear);
            GUI.DrawTexture(new Rect(panel.x + 14f, panel.y + 8f + lineH, panel.width - 28f, 1f), Texture2D.whiteTexture);

            for (int i = 0; i < terminalCount && i < TerminalLines.Length; i++)
            {
                string line = TerminalLines[i];
                int split = line.LastIndexOf(' ');
                string label = line.Substring(0, split + 1), status = line.Substring(split + 1);
                int shown = Mathf.Clamp(Mathf.FloorToInt((Time.time - terminalStart - i * 0.9f) * 55f), 0, line.Length);
                string l = label.Substring(0, Mathf.Min(shown, label.Length));
                string st = shown > label.Length ? status.Substring(0, shown - label.Length) : "";
                string statusColor = status == "OK" ? "#5CFF8A" : status == "REBOOTING" ? "#FFB020" : status == "CORRUPTED" ? "#FF4D6A" : "#FFD23F";
                bool typing = shown < line.Length || i == terminalCount - 1;
                string cursor = typing && Mathf.Repeat(Time.time, 0.8f) < 0.4f ? "<color=#BFF6FF>_</color>" : "";
                string text = "<color=#9FEFFF>" + l + "</color>" + (st.Length > 0 ? "<color=" + statusColor + "><b>" + st + "</b></color>" : "") + cursor;
                GUI.Label(new Rect(panel.x + 14f, panel.y + 14f + lineH * (i + 1), panel.width - 28f, lineH), text, termStyle);
            }
            GUI.color = old;
        }

        static void GlowLabel(Rect r, string text, GUIStyle style, Color glow, float alpha)
        {
            var baseColor = style.normal.textColor;
            style.normal.textColor = glow;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                GUI.color = new Color(1f, 1f, 1f, 0.12f * alpha);
                GUI.Label(new Rect(r.x + Mathf.Cos(a) * 4f, r.y + Mathf.Sin(a) * 4f, r.width, r.height), text, style);
            }
            style.normal.textColor = Color.white;
            GUI.color = new Color(1f, 1f, 1f, 0.35f * alpha);
            GUI.Label(new Rect(r.x, r.y - 1f, r.width, r.height), text, style);
            style.normal.textColor = baseColor;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(r, text, style);
        }
    }
}
