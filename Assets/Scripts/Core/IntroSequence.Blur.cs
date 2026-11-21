using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Blur-inspired opening (21 Nov), used with the realistic look (LevelBuilder.realisticLook).
    /// Same story as before, told like a racing-game intro: fast cinematic cuts, clean white typography,
    /// letterbox, glitch transitions, synthesised sound (IntroAudio).
    ///  1. Cold open: shots of the real Neon District (aerial, street dolly, crane), the city blacks out,
    ///     a Hunter in the dark, SYSTEM FAILURE.
    ///  2. Lab: macro shots of Pac-Man in his pod while the terminal reboots him, ONLINE hit, pod opens,
    ///     low hero shot, BYTE's three lines, Pac-Man launches up.
    ///  3. Title slam: the city's lights switch on in a wave from Pac-Man, PAC-MAN / NEXUS logo hit.
    ///  4. Know your enemy: Blur "rival" cards, one Bug at a time (live 3D model, class, powers, stats, weakness),
    ///     then the classified Bugs of later districts.
    ///  5. Select district: five city cards (playable / locked / signal lost). ENTER starts, camera swoops down.
    /// ENTER = next / skip ahead, ESC = jump to the district select.
    /// </summary>
    public partial class IntroSequence
    {
        bool blurMode;
        float glitch;
        float bars = 1f;
        string capTag, capLine; float capStart = -10f;
        string centerText; float centerStart; Color centerColor = Color.white;
        float titleStart = -1f, titleFade = 1f;
        string titleMain = "PAC-MAN", titleSecond = "NEXUS", titleThird = "T H E     L O S T     C I T Y";
        int rosterIndex = -1; float rosterStart;
        float menuAlpha, menuStart, shakeAt = -10f, selectAt = -10f;
        int selected;
        string bDialog; float bDialogStart;
        Transform showroom;
        readonly List<Transform> showBugs = new List<Transform>();
        Texture2D noiseTex, gradTex;
        Texture2D[] cardImages;
        GUIStyle bTag, bLine, bCenter, bTitle, bNexus, bSub, bSmall, bSmallR, bSmallC, bCardNum, bCardName, bCardText,
                 bChip, bBugName, bBugText, bStat, bPowName, bBigNum, bHeaderBig, bName, bDialogText, bClassified;

        static readonly Vector3 ShowOrigin = new Vector3(-70f, 0f, 170f);

        static readonly string[] BlurByteLines =
        {
            "Whoa... you're awake! I'm BYTE, the city's maintenance assistant.",
            "The city AI shut everything down. Five districts, five Generators - all offline.",
            "Find the Nexus Cores, restart the Generators... and bring the light back!"
        };

        struct BugInfo
        {
            public BugType type;
            public string name, cls, first, desc, p1, p1d, p2, p2d;
            public int speed, aggression, detection;
        }

        static readonly BugInfo[] Bugs =
        {
            new BugInfo { type = BugType.Chaser, name = "HUNTER", cls = "CHASER", first = "NEON DISTRICT",
                desc = "Locks on when you come within 10 blocks and takes the shortest route to you.",
                speed = 7, aggression = 9, detection = 8,
                p1 = "TRACKING", p1d = "Always finds the shortest path to you", p2 = "RELENTLESS", p2d = "Never drops a chase while you are in range" },
            new BugInfo { type = BugType.Patroller, name = "PATROLLER", cls = "ROUTE GUARD", first = "NEON DISTRICT",
                desc = "Sweeps the same long avenue back and forth. Learn its timing and slip past.",
                speed = 7, aggression = 4, detection = 3,
                p1 = "FIXED ROUTE", p1d = "Moves between the same two points", p2 = "AVENUE LOCK", p2d = "Cuts off the long main streets" },
            new BugInfo { type = BugType.Blocker, name = "BLOCKER", cls = "AREA GUARD", first = "NEON DISTRICT",
                desc = "Holds a corner of the district. Step into its zone and it charges.",
                speed = 6, aggression = 6, detection = 4,
                p1 = "TERRITORY", p1d = "Charges when you get within 3 blocks", p2 = "CHARGE", p2d = "Speeds up while it chases you" },
            new BugInfo { type = BugType.Sentinel, name = "SENTINEL", cls = "CORE GUARD", first = "SKYLINE ZONE",
                desc = "Circles a Nexus Core and strikes anyone who comes for it.",
                speed = 6, aggression = 7, detection = 5,
                p1 = "CORE WATCH", p1d = "Patrols in a ring around a Nexus Core", p2 = "AMBUSH", p2d = "Attacks within 3 blocks" },
        };

        static readonly string[] DistrictNames = { "NEON DISTRICT", "SKYLINE ZONE", "", "", "" };
        static readonly string[] DistrictDesc = { "Rain-soaked streets under a dead skyline.", "Sky bridges high above the clouds.", "", "", "" };
        static readonly string[] DistrictThreats = { "HUNTER · PATROLLER · BLOCKER", "HUNTER · PATROLLER · SENTINEL", "", "", "" };

        // ================================================================ flow

        IEnumerator BlurRun()
        {
            IntroAudio.Init();
            StartCoroutine(DroneTo(0.5f, 4f));
            black = 1f; bars = 1f;
            if (!skip) yield return ColdOpen();
            if (!skip) yield return LabWake();
            if (!skip) yield return TitleSlam();
            if (!skip) yield return Roster();
            skip = false;
            yield return Districts();
            yield return BlurSwoop();
        }

        IEnumerator DroneTo(float v, float seconds)
        {
            float from = IntroAudio.DroneLevel, t0 = Time.time;
            while (Time.time - t0 < seconds)
            {
                IntroAudio.DroneVolume(Mathf.Lerp(from, v, (Time.time - t0) / seconds));
                yield return null;
            }
            IntroAudio.DroneVolume(v);
        }

        struct Cam { public Vector3 p, l; public float fov, roll; }
        static Cam Cm(Vector3 p, Vector3 l, float fov, float roll = 0f) => new Cam { p = p, l = l, fov = fov, roll = roll };

        IEnumerator Shot(float dur, System.Func<float, Cam> f, System.Action<float> each = null, bool whoosh = true)
        {
            if (whoosh) { IntroAudio.Whoosh(); glitch = Mathf.Max(glitch, 0.35f); }
            float t0 = Time.time;
            while (!skip)
            {
                float t = Mathf.Clamp01((Time.time - t0) / dur);
                var c = f(t);
                cam.transform.position = c.p;
                var dir = c.l - c.p;
                if (dir.sqrMagnitude > 1e-6f) cam.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, c.roll);
                cam.fieldOfView = c.fov;
                each?.Invoke(t);
                if (t >= 1f) break;
                if (advance) { advance = false; break; }   // ENTER: next shot
                yield return null;
            }
        }

        /// <summary>Grid position (fractional cells, y = grid row) to world, at height h.</summary>
        Vector3 G(float x, float y, float h) => new Vector3(x * level.cellSize, h, (level.Height - 1 - y) * level.cellSize);

        void Caption(string tag, string line) { capTag = tag; capLine = line; capStart = Time.time; }

        // ---------------------------------------------------------------- level 2+ card (21 Nov)

        /// <summary>
        /// Start of Level 2+ with the realistic look: a short Blur-style cinematic of the new district with BYTE's story
        /// lines as captions (aerial approach, bridge dolly, the offline Generator, the new Bug), the district name slam,
        /// then the district cards (previous districts RESTORED) and the swoop into play.
        /// </summary>
        IEnumerator BlurLevelCard()
        {
            IntroAudio.Init();
            StartCoroutine(DroneTo(0.45f, 3f));
            black = 1f; bars = 1f;
            var lines = CardLinesFor(level.levelNumber);
            System.Func<int, string> L = i => i < lines.Length ? lines[i] : null;
            var center = CityCenter();
            float size = level.Width * level.cellSize;
            int n = level.levelNumber;

            // 1. aerial approach over the clouds
            Caption($"DISTRICT {n:00}  ·  {level.levelName.ToUpper()}", L(0));
            yield return Shot(4.4f, t => Cm(Vector3.Lerp(center + new Vector3(-30f, 8f, -36f), center + new Vector3(-13f, 6.5f, -19f), Smooth(t)),
                center + Vector3.up * 1.2f, 42f), t => black = Mathf.Max(0f, 1f - t * 3f), false);
            black = 0f;
            if (skip) goto Menu;

            // 2. low dolly along a sky bridge
            Caption("SKY BRIDGES", L(1));
            yield return Shot(4f, t => { float x = 2f + 10f * t; return Cm(G(x, 13.5f, 1.0f), G(x + 5f, 13.5f, 0.45f), 54f, 1.5f); });
            if (skip) goto Menu;

            // 3. the offline Generator
            Caption("CENTRAL GENERATOR  //  OFFLINE", L(2));
            var gp = level.CellToWorld(level.GeneratorCell);
            yield return Shot(4f, t => { float a = (120f + 50f * t) * Mathf.Deg2Rad; return Cm(gp + new Vector3(Mathf.Sin(a) * 6f, 3.4f, Mathf.Cos(a) * 6f), gp + Vector3.up * 0.6f, 40f); });
            if (skip) goto Menu;

            // 4. the new Bug
            BugAI sentinel = null;
            foreach (var b in BugAI.All) if (b != null && b.type == BugType.Sentinel) { sentinel = b; break; }
            Caption("NEW THREAT  ·  SENTINEL", L(3));
            if (sentinel != null)
            {
                var sp = sentinel.transform.position;
                yield return Shot(4f, t =>
                {
                    float a = (200f + 50f * t) * Mathf.Deg2Rad;
                    return Cm(sp + new Vector3(Mathf.Sin(a) * 2.8f, 0.8f + 0.3f * t, Mathf.Cos(a) * 2.8f), sp + Vector3.up * 0.1f, 34f);
                });
            }
            if (skip) goto Menu;

            // district name slam
            capTag = capLine = null;
            titleMain = level.levelName.ToUpper();
            titleSecond = $"DISTRICT {n:00}";
            titleThird = Sp("RESTORE THE GENERATOR");
            {
                float angle = 230f;
                IntroAudio.Riser(0.5f);
                float t0 = Time.time;
                while (Time.time - t0 < 1.6f && !skip) { angle += Time.deltaTime * 8f; OrbitCam(center, size, angle, 0.85f); yield return null; }
                IntroAudio.Impact(0.95f);
                flash = 1f; glitch = 0.3f;
                titleStart = Time.time; titleFade = 1f;
                t0 = Time.time; advance = false;
                while (Time.time - t0 < 3f && !skip)
                {
                    angle += Time.deltaTime * 5f;
                    OrbitCam(center, size, angle, 0.78f);
                    if (advance && Time.time - t0 > 0.8f) break;
                    yield return null;
                }
                advance = false;
                float f0 = Time.time;
                while (Time.time - f0 < 0.35f) { titleFade = 1f - (Time.time - f0) / 0.35f; yield return null; }
                titleStart = -1f;
            }

        Menu:
            skip = false;
            capTag = capLine = null;
            yield return Districts();
            yield return BlurSwoop();
        }

        // ---------------------------------------------------------------- 1. cold open

        IEnumerator ColdOpen()
        {
            RenderSettings.fogDensity = 0.022f;
            RealisticCity.SetCityPower(1f);

            // 1. aerial drift over the lit city
            Caption("NEXUS CITY  ·  23:47", null);
            yield return Shot(2.8f, t => Cm(Vector3.Lerp(G(2f, 36f, 13f), G(9f, 29f, 10.5f), Smooth(t)), Vector3.Lerp(G(16f, 14f, 0f), G(18f, 12f, 0f), t), 40f),
                t => black = Mathf.Max(0f, 1f - t * 2.5f), false);
            black = 0f;

            // 2. street-level dolly along the Gran Via
            Caption("GRAN VIA", null);
            yield return Shot(1.7f, t => { float x = 3f + 7f * t; return Cm(G(x, 12.5f, 0.95f), G(x + 5f, 12f, 0.4f), 56f, 2f); });

            // 3. crane up a long street toward the towers; the lights start to stutter
            Caption("NEON DISTRICT", null);
            yield return Shot(1.9f, t => { float s = Smooth(t); return Cm(G(16f, 10.5f, 0.35f + 2.6f * s), G(16f, 0f, 1.2f + 1.8f * s), 44f); },
                t => { if (t > 0.6f) RealisticCity.SetCityPower(Random.value < 0.35f ? 0.25f : 1f); });

            // 4. blackout from above
            Caption("SECTOR 01  //  POWER FAILURE", "THE CITY OF NEXUS WENT DARK.");
            bool g1 = false, g2 = false;
            yield return Shot(2.9f, t =>
            {
                var c = G(16f, 16f, 0f);
                float a = (20f + 40f * t) * Mathf.Deg2Rad;
                return Cm(c + new Vector3(Mathf.Sin(a) * 5f, 15f - 2f * t, -Mathf.Cos(a) * 5f), c, 48f);
            }, t =>
            {
                float k = t < 0.75f ? (Random.value < 0.45f ? Mathf.Lerp(1f, 0.2f, t) : 0.08f) : 0.06f;
                RealisticCity.SetCityPower(k);
                if (!g1 && t > 0.25f) { g1 = true; IntroAudio.Glitch(); glitch = 0.7f; }
                if (!g2 && t > 0.6f) { g2 = true; IntroAudio.Glitch(); glitch = 0.9f; }
            });
            RealisticCity.SetCityPower(0.06f);

            // 5. a Hunter in the dark
            BugAI hunter = null;
            foreach (var b in BugAI.All) if (b != null && b.type == BugType.Chaser) { hunter = b; break; }
            if (hunter == null && BugAI.All.Count > 0) hunter = BugAI.All[0];
            Caption("THE BUGS TOOK THE STREETS", "ITS AI SHUT DOWN EVERY DISTRICT, ONE BY ONE.");
            if (hunter != null)
            {
                var hp = hunter.transform.position;
                yield return Shot(2.7f, t =>
                {
                    float a = (200f + 50f * t) * Mathf.Deg2Rad;
                    return Cm(hp + new Vector3(Mathf.Sin(a) * 2.8f, 0.7f + 0.35f * t, Mathf.Cos(a) * 2.8f), hp + Vector3.up * 0.1f, 34f);
                });
            }

            // 6. fast tilted run down the dark avenue
            Caption(null, null);
            yield return Shot(1.4f, t => Cm(G(21.5f, 2f + 9f * t, 1.6f - 0.8f * t), G(21.5f, 12f + 9f * t, 0.3f), 62f, Mathf.Lerp(10f, -4f, t)));

            // SYSTEM FAILURE
            if (!skip)
            {
                centerText = "S Y S T E M   F A I L U R E"; centerColor = Visuals.CorruptedRed; centerStart = Time.time;
                IntroAudio.Impact(0.8f); IntroAudio.Glitch(0.5f); glitch = 1f;
                float t0 = Time.time;
                while (Time.time - t0 < 1.5f && !skip) { black = Mathf.Clamp01((Time.time - t0) / 0.5f) * 0.85f; if (Random.value < 0.08f) glitch = 0.8f; yield return null; }
            }
            black = 1f;
            capTag = capLine = null;

            // story line on black
            if (!skip)
            {
                centerText = "DEEP BELOW THE NEON DISTRICT, SOMETHING WAKES UP..."; centerColor = Color.white; centerStart = Time.time;
                float t0 = Time.time, beat = 0f;
                advance = false;
                while (Time.time - t0 < 2.8f && !skip && !advance)
                {
                    if (Time.time > beat) { IntroAudio.Thump(0.55f); beat = Time.time + 0.9f; }
                    yield return null;
                }
                advance = false;
            }
            centerText = null;
        }

        // ---------------------------------------------------------------- 2. the lab

        IEnumerator LabWake()
        {
            BuildLab();
            player.position = Lab(0f, 0.75f, 0f);
            player.rotation = Quaternion.LookRotation(Vector3.back);
            player.localScale = startScale * 0.7f;
            RenderSettings.fogDensity = 0.012f;
            SetLabLights(0f);

            // macro slide across the pod while the terminal reboots him; heartbeat
            terminalStart = Time.time;
            float beat = 0f;
            yield return Shot(4.8f, t => Cm(Vector3.Lerp(Lab(1.15f, 1.05f, -1.3f), Lab(-1.0f, 0.95f, -1.35f), Smooth(t)), Lab(0f, 0.8f, 0f), 24f), t =>
            {
                black = 1f - Mathf.Clamp01(t * 3f);
                float flick = t < 0.5f ? (Random.value < 0.3f ? 1f : 0.1f) : 1f;
                SetLabLights(Mathf.Clamp01(t * 1.5f) * flick);
                terminalCount = Mathf.Min(TerminalLines.Length, Mathf.FloorToInt((Time.time - terminalStart) / 1.0f) + 1);
                PulsePod(t);
                if (Time.time > beat) { IntroAudio.Thump(0.6f); beat = Time.time + 0.95f; }
            }, false);
            black = 0f; SetLabLights(1f);
            if (skip) yield break;

            // ONLINE: extreme close-up, hit
            flash = 1f; glitch = 0.5f;
            IntroAudio.Impact(0.75f);
            if (podLight != null) { podLight.color = Visuals.NeonYellow; podLight.intensity = 6f; }
            yield return Shot(1.5f, t => Cm(Vector3.Lerp(Lab(0.2f, 0.86f, -0.98f), Lab(0.06f, 0.82f, -0.8f), t), Lab(0f, 0.78f, 0f), 19f, Mathf.Lerp(-3f, 0f, t)), null, false);
            terminalCount = 0;

            // the pod opens, he floats out: low hero angle
            var glassFrom = podGlass.position;
            var pFrom = player.position;
            var pTo = Lab(0f, 0.85f, -1.2f);
            IntroAudio.Whoosh(0.4f);
            yield return Shot(2.6f, t => Cm(Vector3.Lerp(Lab(0.75f, 0.22f, -3.1f), Lab(0.45f, 0.3f, -2.5f), t), player.position + Vector3.up * 0.1f, 36f), t =>
            {
                podGlass.position = glassFrom + Vector3.up * (2.2f * Smooth(Mathf.Clamp01(t * 1.6f)));
                float k = Smooth(Mathf.Clamp01((t - 0.3f) / 0.7f));
                player.position = Vector3.Lerp(pFrom, pTo, k) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.3f;
                player.localScale = Vector3.Lerp(startScale * 0.7f, startScale, Smooth(Mathf.Clamp01(t * 2f)));
                if (podLight != null) podLight.intensity = Mathf.Lerp(6f, 2f, t);
            }, false);
            if (skip) yield break;

            // BYTE
            if (byteGo != null) byteGo.SetActive(true);
            flash = 0.5f; IntroAudio.Glitch(0.25f);
            for (int i = 0; i < BlurByteLines.Length && !skip; i++)
            {
                bDialog = BlurByteLines[i]; bDialogStart = Time.time;
                float t0 = Time.time, dur = 2.6f + bDialog.Length * 0.035f;
                advance = false;
                while (Time.time - t0 < dur && !skip)
                {
                    if (advance)
                    {
                        advance = false;
                        if (VisibleChars(bDialog, bDialogStart) < bDialog.Length) bDialogStart = -100f;
                        else break;
                    }
                    float a = (Time.time - t0) * 4f + i * 12f;
                    var p = Lab(1.6f, 1.45f, -4.2f) + new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad) * 1.6f, 0f, 0f);
                    SetCamera(p, Lab(-0.15f, 0.9f, -1.0f), 34f);
                    player.position = pTo + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.06f;
                    yield return null;
                }
            }
            bDialog = null;
            if (skip) yield break;

            // launch: he shoots up through the lab ceiling
            IntroAudio.Whoosh(0.7f);
            yield return Shot(0.85f, t => Cm(Lab(0.25f, 0.3f, -2.1f), player.position, 40f + 22f * t), t => player.position = pTo + Vector3.up * (t * t * 4.5f), false);
            flash = 1f;
        }

        // ---------------------------------------------------------------- 3. title slam

        void OrbitCam(Vector3 center, float size, float angleDeg, float dist)
        {
            float a = angleDeg * Mathf.Deg2Rad, r = size * 0.85f * dist;
            var pos = center + new Vector3(Mathf.Sin(a) * r, size * 0.62f * dist, Mathf.Cos(a) * r);
            SetCamera(pos, center + Vector3.up * 1.5f, 50f);
        }

        void PrepareCityView()
        {
            player.position = startPos; player.rotation = startRot; player.localScale = startScale;
            if (lab != null) lab.gameObject.SetActive(false);
            if (byteGo != null) byteGo.SetActive(true);
            DestroyShowroom();
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.009f;
            terminalCount = 0; bDialog = null; capTag = capLine = null; centerText = null; rosterIndex = -1;
        }

        IEnumerator TitleSlam()
        {
            PrepareCityView();
            RealisticCity.SetCityPower(0.05f, startPos, 0f);
            var center = CityCenter();
            float size = level.Width * level.cellSize, angle = 210f;
            IntroAudio.Riser(0.6f);
            float t0 = Time.time;
            while (Time.time - t0 < 2.4f && !skip)
            {
                float t = (Time.time - t0) / 2.4f;
                black = Mathf.Max(0f, 1f - t * 4f);
                angle += Time.deltaTime * 8f;
                OrbitCam(center, size, angle, Mathf.Lerp(0.95f, 0.78f, t));
                RealisticCity.SetCityPower(Mathf.Lerp(0.05f, 1f, t * t), startPos, t * t * size * 1.4f);
                yield return null;
            }
            black = 0f;
            RealisticCity.SetCityPower(1f);
            if (skip) yield break;

            IntroAudio.Impact(1f);
            flash = 1f; glitch = 0.35f;
            titleStart = Time.time; titleFade = 1f;
            t0 = Time.time; advance = false;
            while (Time.time - t0 < 3.6f && !skip)
            {
                angle += Time.deltaTime * 5f;
                OrbitCam(center, size, angle, 0.78f);
                if (advance && Time.time - t0 > 0.8f) break;
                yield return null;
            }
            advance = false;
            float f0 = Time.time;
            while (Time.time - f0 < 0.35f)
            {
                titleFade = 1f - (Time.time - f0) / 0.35f;
                angle += Time.deltaTime * 5f;
                OrbitCam(center, size, angle, 0.78f);
                yield return null;
            }
            titleStart = -1f;
        }

        // ---------------------------------------------------------------- 4. know your enemy

        void BuildShowroom()
        {
            DestroyShowroom();
            showroom = new GameObject("BugShowroom").transform;
            showroom.position = ShowOrigin;
            var floorMat = Visuals.Lit(Visuals.Hex("#050608"), 0.3f, 0.45f);
            Visuals.Shape(PrimitiveType.Cube, "Floor", showroom, new Vector3(9f, -0.05f, 0f), new Vector3(60f, 0.1f, 30f), floorMat);
            Visuals.Shape(PrimitiveType.Cube, "Backdrop", showroom, new Vector3(9f, 4f, 4.5f), new Vector3(60f, 9f, 0.2f), Visuals.Lit(Color.black, 0f, 0f));
            showBugs.Clear();
            for (int i = 0; i < Bugs.Length; i++)
            {
                var c = BugAI.ColorFor(Bugs[i].type);
                var root = new GameObject("Bug_" + Bugs[i].name).transform;
                root.SetParent(showroom, false);
                root.localPosition = new Vector3(i * 6f, 0.62f, 0f);
                var body = new GameObject("Body").transform;
                body.SetParent(root, false);
                BugModel.Create(body, 0.42f, c);
                var rim = new GameObject("Rim").AddComponent<Light>();
                rim.transform.SetParent(root, false);
                rim.transform.localPosition = new Vector3(-0.7f, 1.1f, 1.2f);
                rim.type = LightType.Point; rim.color = c; rim.range = 4f; rim.intensity = 7f; rim.shadows = LightShadows.None;
                var key = new GameObject("Key").AddComponent<Light>();
                key.transform.SetParent(root, false);
                key.transform.localPosition = new Vector3(1.6f, 1.3f, -1.7f);
                key.type = LightType.Point; key.color = Visuals.Hex("#DCE6FF"); key.range = 5f; key.intensity = 2.2f; key.shadows = LightShadows.None;
                showBugs.Add(root);
            }
        }

        void DestroyShowroom()
        {
            if (showroom != null) Destroy(showroom.gameObject);
            showroom = null;
            showBugs.Clear();
        }

        IEnumerator Roster()
        {
            BuildShowroom();
            RenderSettings.fog = false;
            var bg = cam.backgroundColor;
            cam.backgroundColor = Color.black;
            black = 0f;
            bars = 0.45f;
            for (int i = 0; i < Bugs.Length && !skip; i++)
            {
                rosterIndex = i; rosterStart = Time.time;
                IntroAudio.Whoosh(0.45f); glitch = Mathf.Max(glitch, 0.3f);
                advance = false;
                var target = showBugs[i];
                while (!skip)
                {
                    float t = Time.time - rosterStart;
                    target.Rotate(0f, 35f * Time.deltaTime, 0f);
                    var P = target.position;
                    float push = Smooth(Mathf.Clamp01(t / 4f));
                    var pos = P + Vector3.Lerp(new Vector3(1.6f, 0.65f, -3.5f), new Vector3(1.35f, 0.5f, -3.0f), push);
                    SetCamera(pos, P + new Vector3(0.85f, 0.05f, 0f), 30f);
                    if (t > 4.4f || (advance && t > 0.4f)) break;
                    yield return null;
                }
                advance = false;
            }
            if (!skip)
            {
                rosterIndex = Bugs.Length; rosterStart = Time.time;
                IntroAudio.Glitch(0.45f); glitch = 0.8f;
                SetCamera(ShowOrigin + new Vector3(9f, 1.2f, -7f), ShowOrigin + new Vector3(9f, 0.4f, 0f), 40f);
                float t0 = Time.time;
                advance = false;
                while (Time.time - t0 < 3f && !skip && !(advance && Time.time - t0 > 0.4f))
                {
                    if (Random.value < 0.03f) glitch = 0.5f;
                    yield return null;
                }
                advance = false;
            }
            rosterIndex = -1;
            DestroyShowroom();
            RenderSettings.fog = true;
            cam.backgroundColor = bg;
        }

        // ---------------------------------------------------------------- 5. select district

        /// <summary>0 = playable now, 1 = locked, 2 = signal lost (not built yet), 3 = restored.</summary>
        int DistrictState(int i)
        {
            int current = level != null ? level.levelNumber - 1 : 0;
            if (i < current) return 3;
            if (i == current) return 0;
            if (i == 1) return 1;
            return 2;
        }

        void LoadCardImages()
        {
            if (cardImages != null) return;
            cardImages = new[]
            {
                Resources.Load<Texture2D>("IntroCards/district1"),
                Resources.Load<Texture2D>("IntroCards/district2")
            };
        }

        IEnumerator Districts()
        {
            PrepareCityView();
            RealisticCity.SetCityPower(1f);
            LoadCardImages();
            var center = CityCenter();
            float size = level.Width * level.cellSize, angle = 150f;
            selected = Mathf.Clamp(level.levelNumber - 1, 0, 4);
            menuStart = Time.time;
            float b0 = bars;
            advance = false;
            IntroAudio.Whoosh(0.4f);
            while (true)
            {
                float t = Time.time - menuStart;
                menuAlpha = Mathf.Clamp01(t / 0.4f);
                bars = Mathf.Lerp(b0, 0.4f, Mathf.Clamp01(t / 0.6f));
                black = Mathf.Max(0f, black - Time.deltaTime * 3f);
                angle += Time.deltaTime * 3f;
                OrbitCam(center, size, angle, 0.9f);

                var kb = Keyboard.current;
                if (kb != null && t > 0.3f)
                {
                    int dir = 0;
                    if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dir = 1;
                    if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dir = -1;
                    if (dir != 0)
                    {
                        int ns = Mathf.Clamp(selected + dir, 0, 4);
                        if (ns != selected) { selected = ns; selectAt = Time.time; IntroAudio.Tick(); }
                    }
                }
                if (advance && t > 0.5f)
                {
                    advance = false;
                    if (DistrictState(selected) == 0) break;
                    shakeAt = Time.time; IntroAudio.Deny(); glitch = 0.35f;
                }
                advance = false;
                skip = false;
                yield return null;
            }
            IntroAudio.Impact(0.8f);
            flash = 0.7f;
        }

        IEnumerator BlurSwoop()
        {
            var fromPos = cam.transform.position;
            var fromRot = cam.transform.rotation;
            float fromFov = cam.fieldOfView;
            float pitch = follow != null ? follow.viewPitch : 66f;
            var offset = follow != null ? follow.Offset : Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -9.5f);
            float toFov = follow != null ? follow.viewFov : 50f;
            float v0 = IntroAudio.DroneLevel, b0 = bars;
            float t0 = Time.time, dur = 2.0f;
            IntroAudio.Whoosh(0.6f);
            while (Time.time - t0 < dur)
            {
                float u = (Time.time - t0) / dur, t = Smooth(u);
                menuAlpha = 1f - Mathf.Clamp01(u * 3f);
                bars = Mathf.Lerp(b0, 0f, t);
                cam.transform.position = Vector3.Lerp(fromPos, player.position + offset, t);
                cam.transform.rotation = Quaternion.Slerp(fromRot, Quaternion.Euler(pitch, 0f, 0f), t);
                cam.fieldOfView = Mathf.Lerp(fromFov, toFov, t);
                RenderSettings.fogDensity = Mathf.Lerp(0.009f, savedFog, t);
                IntroAudio.DroneVolume(Mathf.Lerp(v0, 0f, t));
                yield return null;
            }
            menuAlpha = 0f; bars = 0f;
        }

        void BlurCleanup()
        {
            RealisticCity.SetCityPower(1f);
            RenderSettings.fog = true;
            DestroyShowroom();
            IntroAudio.DroneVolume(0f);
            IntroAudio.Dispose(3f);
        }

        // ================================================================ drawing

        void CreateBlurStyles()
        {
            var f = GameHUD.HudFont;
            GUIStyle S(int size, Color c, bool bold = false, TextAnchor a = TextAnchor.MiddleLeft, bool wrap = false)
            {
                var st = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, alignment = a, wordWrap = wrap, richText = true, clipping = TextClipping.Overflow };
                if (f != null) st.font = f;
                st.normal.textColor = c;
                return st;
            }
            var white = new Color(0.95f, 0.96f, 0.97f);
            var dim = new Color(0.63f, 0.67f, 0.73f);
            bTag = S(15, dim, true);
            bLine = S(30, white, true);
            bCenter = S(40, white, true, TextAnchor.MiddleCenter);
            bTitle = S(150, white, true, TextAnchor.MiddleCenter);
            bNexus = S(64, Visuals.NeonYellow, true, TextAnchor.MiddleCenter);
            bSub = S(18, white, true, TextAnchor.MiddleCenter);
            bSmall = S(14, dim, true);
            bSmallR = S(14, dim, true, TextAnchor.MiddleRight);
            bSmallC = S(14, dim, true, TextAnchor.MiddleCenter);
            bCardNum = S(30, Visuals.NeonYellow, true);
            bCardName = S(21, white, true);
            bCardText = S(13, dim, false, TextAnchor.UpperLeft, true);
            bChip = S(11, Color.black, true, TextAnchor.MiddleCenter);
            bBugName = S(70, white, true);
            bBugText = S(18, new Color(0.82f, 0.85f, 0.9f), false, TextAnchor.UpperLeft, true);
            bStat = S(13, dim, true);
            bPowName = S(16, white, true);
            bBigNum = S(300, new Color(1f, 1f, 1f, 0.05f), true);
            bHeaderBig = S(48, white, true);
            bName = S(17, Visuals.Cyan, true);
            bDialogText = S(20, white, false, TextAnchor.UpperLeft, true);
            bClassified = S(22, Visuals.CorruptedRed, true, TextAnchor.MiddleCenter);

            noiseTex = new Texture2D(128, 128, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var r = new System.Random(9);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float v = (float)r.NextDouble();
                    v = v * v;
                    if (y % 3 == 0) v *= 0.6f;   // scan lines
                    noiseTex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            noiseTex.Apply();
            gradTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) { float a = 1f - y / 63f; gradTex.SetPixel(0, y, new Color(0f, 0f, 0f, a * a)); }
            gradTex.Apply();
        }

        static float Ease(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3f); }
        static string HexOf(Color c) => ColorUtility.ToHtmlStringRGB(c);
        static string Sp(string s) => string.Join(" ", s.ToCharArray()).Replace("   ", "     ");

        void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); }

        void DrawBlur()
        {
            if (bTag == null) CreateBlurStyles();
            // designed for 1080p and scaled to any window (not clamped like the HUD, so nothing is cut off)
            float W = Screen.width, H = Screen.height, k = Mathf.Max(0.35f, H / 1080f);
            var saved = GUI.matrix;
            var scaled = Matrix4x4.Scale(new Vector3(k, k, 1f));
            GUI.matrix = scaled;
            float VW = W / k, VH = H / k;

            if (menuAlpha > 0f) DrawDistricts(VW, VH);
            if (rosterIndex >= 0) DrawRoster(VW, VH);
            if (titleStart >= 0f) DrawTitleSlam(VW, VH);
            DrawCaption(VW, VH);
            if (bDialog != null) DrawBlurDialog(VW, VH);

            GUI.matrix = saved;
            if (terminalCount > 0) DrawTerminal(W, H);
            GUI.matrix = scaled;

            if (bars > 0f)
            {
                float bh = VH * 0.11f * bars;
                Box(new Rect(0, 0, VW, bh), Color.black);
                Box(new Rect(0, VH - bh, VW, bh), Color.black);
            }
            if (glitch > 0.01f) DrawGlitch(VW, VH);
            if (black > 0f) Box(new Rect(0, 0, VW, VH), new Color(0f, 0f, 0f, black));
            if (centerText != null) DrawCenter(VW, VH);
            if (flash > 0f) Box(new Rect(0, 0, VW, VH), new Color(1f, 1f, 1f, flash * 0.85f));

            if (menuAlpha <= 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
                GUI.Label(new Rect(VW - 360f, VH - 40f, 320f, 24f), "ENTER  NEXT        ESC  SKIP", bSmallR);
            }
            GUI.color = Color.white;
            GUI.matrix = saved;
        }

        void DrawCaption(float VW, float VH)
        {
            if (capTag == null && capLine == null) return;
            float t = Time.time - capStart;
            float a = Mathf.Clamp01(t / 0.25f);
            float x = 80f - (1f - Ease(t / 0.4f)) * 30f, y = VH * 0.7f;
            if (capTag != null)
            {
                Box(new Rect(x, y + 3f, 3f, 16f), new Color(Visuals.NeonYellow.r, Visuals.NeonYellow.g, Visuals.NeonYellow.b, a));
                GUI.color = new Color(0f, 0f, 0f, 0.6f * a);
                GUI.Label(new Rect(x + 15f, y + 1f, 900f, 22f), Sp(capTag), bTag);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(x + 14f, y, 900f, 22f), Sp(capTag), bTag);
            }
            if (capLine != null)
            {
                int n = Mathf.Clamp(Mathf.FloorToInt((t - 0.15f) * 50f), 0, capLine.Length);
                bLine.fontSize = capLine.Length > 60 ? 24 : 30;
                GUI.color = new Color(0f, 0f, 0f, 0.6f * a);
                GUI.Label(new Rect(x + 2f, y + 32f, 1400f, 44f), capLine.Substring(0, n), bLine);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(x, y + 30f, 1400f, 44f), capLine.Substring(0, n), bLine);
            }
            GUI.color = Color.white;
        }

        void DrawCenter(float VW, float VH)
        {
            float t = Time.time - centerStart;
            float a = Mathf.Clamp01(t / 0.3f);
            var r = new Rect(0f, VH * 0.45f, VW, 60f);
            if (glitch > 0.2f)
            {
                float j = glitch * 8f;
                GUI.color = new Color(0f, 0.85f, 1f, 0.5f * a);
                GUI.Label(new Rect(r.x - j, r.y, r.width, r.height), centerText, bCenter);
                GUI.color = new Color(1f, 0.15f, 0.25f, 0.5f * a);
                GUI.Label(new Rect(r.x + j, r.y, r.width, r.height), centerText, bCenter);
            }
            bCenter.normal.textColor = centerColor;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(r, centerText, bCenter);
            bCenter.normal.textColor = Color.white;
            GUI.color = Color.white;
        }

        void DrawGlitch(float VW, float VH)
        {
            int n = Mathf.CeilToInt(glitch * 14f);
            for (int i = 0; i < n; i++)
            {
                float y = Random.value * VH, h = 2f + Random.value * 22f * glitch;
                float x = (Random.value - 0.5f) * 80f * glitch;
                float pick = Random.value;
                Color c = pick < 0.5f ? new Color(0f, 0f, 0f, 0.55f * glitch)
                        : pick < 0.75f ? new Color(0f, 0.85f, 1f, 0.22f * glitch)
                        : new Color(1f, 0.15f, 0.3f, 0.22f * glitch);
                Box(new Rect(x, y, VW, h), c);
            }
            GUI.color = Color.white;
        }

        void DrawBlurDialog(float VW, float VH)
        {
            var r = new Rect(VW / 2f - 400f, VH * 0.72f, 800f, 92f);
            float a = Mathf.Clamp01((Time.time - bDialogStart) / 0.2f);
            if (bDialogStart < 0f) a = 1f;
            GameHUD.DrawPanel(r, a);
            DrawBytePortrait(new Rect(r.x + 18f, r.y + 18f, 56f, 56f));
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(r.x + 92f, r.y + 10f, 200f, 24f), "B Y T E", bName);
            GUI.Label(new Rect(r.x + 92f, r.y + 36f, r.width - 110f, 52f), bDialog.Substring(0, VisibleChars(bDialog, bDialogStart)), bDialogText);
            GUI.color = Color.white;
        }

        /// <summary>Text drawn letter by letter with an animated gap (logo "tracking" in).</summary>
        void DrawTracked(string text, GUIStyle st, float cx, float cy, float gap, Color c)
        {
            float total = 0f;
            var sizes = new float[text.Length];
            for (int i = 0; i < text.Length; i++) { sizes[i] = st.CalcSize(new GUIContent(text[i].ToString())).x; total += sizes[i]; }
            total += gap * (text.Length - 1);
            float x = cx - total / 2f, h = st.fontSize * 1.4f;
            var keep = st.normal.textColor;
            st.normal.textColor = Color.white;
            for (int i = 0; i < text.Length; i++)
            {
                GUI.color = c;
                var a = st.alignment; st.alignment = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(x, cy - h / 2f, sizes[i] + 10f, h), text[i].ToString(), st);
                st.alignment = a;
                x += sizes[i] + gap;
            }
            st.normal.textColor = keep;
            GUI.color = Color.white;
        }

        void DrawTitleSlam(float VW, float VH)
        {
            float t = Time.time - titleStart, a = titleFade;
            Box(new Rect(0, VH * 0.2f, VW, VH * 0.6f), new Color(0f, 0f, 0f, 0.35f * a));
            float cy = VH * 0.4f;
            DrawTracked(titleMain, bTitle, VW / 2f, cy, Mathf.Lerp(70f, titleMain.Length > 8 ? 4f : 8f, Ease(t / 0.7f)), new Color(1f, 1f, 1f, a));
            float a2 = Mathf.Clamp01((t - 0.15f) / 0.3f) * a;
            var y = Visuals.NeonYellow;
            DrawTracked(titleSecond, bNexus, VW / 2f, cy + 118f, Mathf.Lerp(70f, 30f, Ease((t - 0.15f) / 0.7f)), new Color(y.r, y.g, y.b, a2));
            float lw = 560f * Ease((t - 0.35f) / 0.5f);
            Box(new Rect(VW / 2f - lw / 2f, cy + 172f, lw, 2f), new Color(y.r, y.g, y.b, a));
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((t - 0.6f) / 0.4f) * a);
            GUI.Label(new Rect(0, cy + 186f, VW, 30f), titleThird, bSub);
            GUI.color = Color.white;
        }

        void DrawStatBar(float x, float y, string label, int value, float t, Color c)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, 140f, 18f), Sp(label), bStat);
            int shown = Mathf.Clamp(Mathf.FloorToInt((t - 0.3f) * 28f), 0, value);
            for (int i = 0; i < 10; i++)
                Box(new Rect(x + 150f + i * 22f, y + 5f, 18f, 8f), i < shown ? c : new Color(1f, 1f, 1f, 0.12f));
            GUI.color = Color.white;
        }

        void DrawRoster(float VW, float VH)
        {
            float t = Time.time - rosterStart;
            if (rosterIndex >= Bugs.Length) { DrawClassified(VW, VH, t); return; }
            var b = Bugs[rosterIndex];
            var c = BugAI.ColorFor(b.type);
            float e = Ease(t / 0.35f);
            float x = VW * 0.5f + (1f - e) * 120f, y = VH * 0.17f;
            float a = e;

            // huge faint index behind the Bug (left)
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(60f, VH * 0.42f, 600f, 320f), (rosterIndex + 1).ToString("00"), bBigNum);

            GUI.Label(new Rect(x, y, 600f, 20f), Sp("KNOW YOUR ENEMY"), bSmall);
            GUI.Label(new Rect(x, y, VW * 0.42f, 20f), $"{rosterIndex + 1:00} / {Bugs.Length + 3:00}", bSmallR);
            Box(new Rect(x, y + 26f, VW * 0.42f, 1f), new Color(1f, 1f, 1f, 0.2f * a));

            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(x - 4f, y + 36f, 900f, 90f), Sp(b.name), bBugName);
            Box(new Rect(x, y + 128f, 70f * e, 4f), c);

            // class chip + first seen
            var chip = new Rect(x, y + 146f, bChip.CalcSize(new GUIContent(b.cls)).x + 22f, 22f);
            Box(chip, new Color(c.r, c.g, c.b, 0.9f * a));
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(chip, b.cls, bChip);
            GUI.Label(new Rect(chip.xMax + 14f, chip.y, 400f, 22f), "FIRST SEEN  ·  " + b.first, bSmall);

            GUI.Label(new Rect(x, y + 182f, VW * 0.4f, 60f), b.desc, bBugText);

            float sy = y + 252f;
            DrawStatBar(x, sy, "SPEED", b.speed, t, c);
            DrawStatBar(x, sy + 26f, "AGGRESSION", b.aggression, t, c);
            DrawStatBar(x, sy + 52f, "DETECTION", b.detection, t, c);

            float py = sy + 96f;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(x, py, 300f, 18f), Sp("POWERS"), bSmall);
            DrawPower(x, py + 24f, b.p1, b.p1d, c, a);
            DrawPower(x, py + 66f, b.p2, b.p2d, c, a);

            // weakness
            float wy = py + 118f;
            Box(new Rect(x, wy, VW * 0.42f, 1f), new Color(1f, 1f, 1f, 0.15f * a));
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(x, wy + 12f, 26f, 26f), Minimap.FruitIcon(PowerUpType.Devour));
            GUI.Label(new Rect(x + 36f, wy + 10f, 600f, 18f), Sp("WEAKNESS"), bSmall);
            GUI.Label(new Rect(x + 36f, wy + 28f, 700f, 22f), "APPLE (Devour Mode) eats it  ·  ENERGY BLAST stuns it", bPowName);

            // auto-advance bar + tip
            Box(new Rect(x, VH * 0.86f, VW * 0.42f, 2f), new Color(1f, 1f, 1f, 0.12f));
            Box(new Rect(x, VH * 0.86f, VW * 0.42f * Mathf.Clamp01(t / 4.4f), 2f), new Color(c.r, c.g, c.b, 0.9f));
            GUI.color = new Color(1f, 1f, 1f, 0.7f * a);
            GUI.Label(new Rect(x, VH * 0.86f + 8f, 600f, 20f), "TIP  ·  every Bug is slower than you", bSmall);
            GUI.color = Color.white;
        }

        void DrawPower(float x, float y, string name, string desc, Color c, float a)
        {
            Box(new Rect(x, y + 5f, 8f, 8f), new Color(c.r, c.g, c.b, a));
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(x + 20f, y - 2f, 400f, 22f), name, bPowName);
            GUI.Label(new Rect(x + 20f, y + 18f, 600f, 20f), desc, bSmall);
            GUI.color = Color.white;
        }

        void DrawClassified(float VW, float VH, float t)
        {
            float a = Ease(t / 0.3f);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(0, VH * 0.17f, VW, 20f), Sp("MORE BUGS DETECTED"), bSmallC);
            GUI.Label(new Rect(0, VH * 0.2f, VW, 40f), "They show themselves in other districts.", new GUIStyle(bBugText) { alignment = TextAnchor.MiddleCenter });
            float cw = 280f, ch = 380f, gap = 30f, x0 = VW / 2f - (cw * 3f + gap * 2f) / 2f, y = VH * 0.3f;
            string[] where = { "DISTRICT 03", "DISTRICT 04", "DISTRICT 05  ·  BOSS" };
            for (int i = 0; i < 3; i++)
            {
                float e = Ease((t - i * 0.1f) / 0.35f);
                var r = new Rect(x0 + i * (cw + gap), y + (1f - e) * 50f, cw, ch);
                Box(r, new Color(0.03f, 0.03f, 0.04f, 0.92f * e));
                GUI.color = new Color(0.35f, 0.35f, 0.38f, e);
                GUI.DrawTextureWithTexCoords(new Rect(r.x + 10f, r.y + 10f, r.width - 20f, r.height * 0.62f), noiseTex,
                    new Rect(Random.value, Random.value, 0.6f, 0.8f));
                GUI.color = new Color(1f, 1f, 1f, e);
                GUI.Label(new Rect(r.x, r.y + r.height * 0.27f, r.width, 50f), "?", new GUIStyle(bHeaderBig) { alignment = TextAnchor.MiddleCenter, fontSize = 80 });
                var stamp = new Rect(r.x + 30f, r.y + r.height * 0.5f, r.width - 60f, 34f);
                Box(stamp, new Color(0f, 0f, 0f, 0.6f * e));
                GUI.color = new Color(1f, 1f, 1f, e);
                GUI.Label(stamp, Sp("CLASSIFIED"), bClassified);
                GUI.Label(new Rect(r.x + 16f, r.yMax - 92f, r.width - 32f, 30f), "? ? ? ? ?", bCardName);
                GUI.Label(new Rect(r.x + 16f, r.yMax - 58f, r.width - 32f, 20f), "SIGNAL DETECTED  ·  " + where[i], bSmall);
                Box(new Rect(r.x, r.yMax - 3f, r.width, 3f), new Color(Visuals.CorruptedRed.r, Visuals.CorruptedRed.g, Visuals.CorruptedRed.b, 0.7f * e));
            }
            GUI.color = Color.white;
        }

        void DrawDistricts(float VW, float VH)
        {
            float a = menuAlpha;
            Box(new Rect(0, 0, VW, VH), new Color(0f, 0f, 0f, 0.5f * a));
            float t = Time.time - menuStart;
            float hx = 80f, hy = VH * 0.13f;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(hx, hy, 600f, 20f), Sp("PAC-MAN NEXUS  ·  NEXUS CITY"), bSmall);
            GUI.Label(new Rect(hx - 2f, hy + 18f, 900f, 60f), "SELECT DISTRICT", bHeaderBig);
            int online = 0; for (int i = 0; i < 5; i++) if (DistrictState(i) == 3) online++;
            GUI.Label(new Rect(VW - 480f, hy + 30f, 400f, 24f), $"<color=#{HexOf(Visuals.NeonYellow)}>{online}</color> / 5   GENERATORS ONLINE", bSmallR);

            float cw = 236f, ch = 360f, gap = 26f;
            float total = cw * 5f + gap * 4f, x0 = VW / 2f - total / 2f, y0 = VH * 0.3f;
            for (int i = 0; i < 5; i++)
            {
                float e = Ease((t - 0.15f - i * 0.07f) / 0.4f);
                bool sel = i == selected;
                float lift = sel ? -14f * Ease((Time.time - selectAt) / 0.2f + (selectAt < menuStart ? 1f : 0f)) : 0f;
                float shake = sel && Time.time - shakeAt < 0.35f ? Mathf.Sin((Time.time - shakeAt) * 60f) * 9f * (1f - (Time.time - shakeAt) / 0.35f) : 0f;
                var r = new Rect(x0 + i * (cw + gap) + shake, y0 + (1f - e) * 60f + lift, cw, ch);
                DrawCard(i, r, sel, a * e);
            }

            // info under the selected card + controls
            int s = DistrictState(selected);
            string info = s == 0 ? (level != null && level.levelNumber == 2 ? "10 NEXUS CORES   ·   SENTINELS GUARD THE CORES   ·   NO TIME LIMIT" : "10 NEXUS CORES   ·   1 GENERATOR   ·   NO TIME LIMIT")
                        : s == 1 ? "LOCKED  ·  RESTORE THE NEON DISTRICT FIRST"
                        : s == 3 ? "RESTORED  ·  GENERATOR ONLINE" : "NO SIGNAL FROM THIS DISTRICT YET";
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(0, y0 + ch + 34f, VW, 24f), info, bSmallC);
            float blink = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f);
            GUI.Label(new Rect(0, VH - VH * 0.11f * bars - 52f, VW, 24f),
                $"<color=#FFFFFF>←  →</color>   BROWSE            <color=#{HexOf(Visuals.NeonYellow)}>ENTER</color>   START", bSmallC);
            GUI.color = Color.white;
        }

        void DrawCard(int i, Rect r, bool sel, float a)
        {
            if (a <= 0.01f) return;
            int st = DistrictState(i);
            var y = Visuals.NeonYellow;
            Box(r, new Color(0.02f, 0.022f, 0.03f, 0.9f * a));
            var img = new Rect(r.x + 8f, r.y + 8f, r.width - 16f, r.height * 0.56f);

            Texture2D tex = i < 2 && cardImages != null ? cardImages[i] : null;
            if (st == 2 || tex == null)
            {
                GUI.color = new Color(0.32f, 0.33f, 0.36f, a);
                GUI.DrawTextureWithTexCoords(img, noiseTex, new Rect(Random.value, Random.value, 0.5f, 0.6f));
            }
            else
            {
                float imgAspect = img.width / img.height, texAspect = tex.width / (float)tex.height;
                float zoom = sel ? 0.78f : 0.85f;
                float uvH = zoom, uvW = Mathf.Min(1f, uvH * imgAspect / texAspect);
                float pan = 0.5f + 0.5f * Mathf.Sin(Time.time * 0.12f + i * 1.7f);
                var uv = new Rect((1f - uvW) * pan, (1f - uvH) * 0.5f, uvW, uvH);
                GUI.color = st == 1 ? new Color(0.32f, 0.32f, 0.35f, a) : new Color(1f, 1f, 1f, a);
                GUI.DrawTextureWithTexCoords(img, tex, uv);
            }
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(img.x, img.y + img.height * 0.45f, img.width, img.height * 0.55f), gradTex);

            // number + status chip on the image
            bCardNum.normal.textColor = st == 0 ? y : new Color(0.75f, 0.78f, 0.83f);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(img.x + 12f, img.y + 8f, 100f, 40f), (i + 1).ToString("00"), bCardNum);
            string chipText = st == 0 ? "AVAILABLE" : st == 1 ? "LOCKED" : st == 3 ? "RESTORED" : "SIGNAL LOST";
            Color chipCol = st == 0 ? y : st == 1 ? new Color(0.7f, 0.72f, 0.76f) : st == 3 ? Visuals.Hex("#5CFF8A") : Visuals.CorruptedRed;
            var chip = new Rect(img.xMax - bChip.CalcSize(new GUIContent(chipText)).x - 30f, img.y + 14f, bChip.CalcSize(new GUIContent(chipText)).x + 18f, 20f);
            Box(chip, new Color(chipCol.r, chipCol.g, chipCol.b, 0.92f * a));
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(chip, chipText, bChip);
            if (st == 2)
                GUI.Label(new Rect(img.x, img.center.y - 14f, img.width, 28f), Sp("NO SIGNAL"), new GUIStyle(bSmallC) { fontSize = 16 });

            // text block
            float tx = r.x + 16f, ty = img.yMax + 14f, tw = r.width - 32f;
            string name = st == 2 ? "— — — — —" : DistrictNames[i];
            GUI.Label(new Rect(tx, ty, tw, 28f), name, bCardName);
            if (st != 2)
            {
                GUI.Label(new Rect(tx, ty + 30f, tw, 36f), DistrictDesc[i], bCardText);
                Box(new Rect(tx, ty + 72f, tw, 1f), new Color(1f, 1f, 1f, 0.12f * a));
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(tx, ty + 80f, tw, 16f), Sp("THREATS"), new GUIStyle(bSmall) { fontSize = 10 });
                GUI.Label(new Rect(tx, ty + 96f, tw, 18f), DistrictThreats[i], new GUIStyle(bCardText) { fontSize = 12, wordWrap = false });
                if (st == 0)
                    GUI.Label(new Rect(tx, ty + 116f, tw, 18f), "<color=#FF3158>●</color>  GENERATOR OFFLINE", new GUIStyle(bSmall) { fontSize = 11 });
                else if (st == 3)
                    GUI.Label(new Rect(tx, ty + 116f, tw, 18f), "<color=#5CFF8A>●</color>  GENERATOR ONLINE", new GUIStyle(bSmall) { fontSize = 11 });
            }
            else
                GUI.Label(new Rect(tx, ty + 30f, tw, 36f), "District data corrupted.", bCardText);

            // frame
            if (sel)
            {
                var c = st == 0 ? y : new Color(0.85f, 0.87f, 0.9f);
                Box(new Rect(r.x, r.y, r.width, 2f), new Color(c.r, c.g, c.b, a));
                Box(new Rect(r.x, r.yMax - 4f, r.width, 4f), new Color(c.r, c.g, c.b, a));
                Box(new Rect(r.x, r.y, 2f, r.height), new Color(c.r, c.g, c.b, a));
                Box(new Rect(r.xMax - 2f, r.y, 2f, r.height), new Color(c.r, c.g, c.b, a));
            }
            else
            {
                Box(new Rect(r.x, r.y, r.width, 1f), new Color(1f, 1f, 1f, 0.12f * a));
                Box(new Rect(r.x, r.yMax - 1f, r.width, 1f), new Color(1f, 1f, 1f, 0.12f * a));
            }
            GUI.color = Color.white;
        }
    }
}
