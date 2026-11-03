using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.EditorTools
{
    /// <summary>
    /// Automatic Play mode check of Energy Blast and the lives rules (GDD 3.1, 6), for Unity in batch mode:
    ///   Unity.exe -batchmode -projectPath . -executeMethod Nexus.EditorTools.NexusPlayTests.RunLivesTest
    ///             [-shotDir Docs/Screenshots]
    /// (no -quit: the test exits Unity itself, exit code 0 = all checks passed).
    /// Energy Blast: costs 30 energy, pushes a Bug up to 2 cells away and freezes it, has a cooldown.
    /// Then puts the Bug on top of Pac-Man and checks: life lost, respawn at 'P', Bug back at 'H',
    /// 2 s invulnerability, cores kept, game over at 0 lives, restart gives 3 lives again.
    /// It also saves a game camera screenshot (play.png) after one second of play.
    /// </summary>
    public static class NexusPlayTests
    {
        const string ScenePath = "Assets/Scenes/Level1_NeonDistrict.unity";

        static readonly List<string> failures = new List<string>();
        static IEnumerator<float> steps;
        static double waitUntil;
        static string shotDir;

        public static void RunLivesTest()
        {
            shotDir = Arg("-shotDir", "Docs/Screenshots");
            EditorSceneManager.OpenScene(ScenePath);
            // Keep this static state alive when entering Play mode
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            steps = Steps();
            waitUntil = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            bool more;
            try { more = steps.MoveNext(); }
            catch (Exception e)
            {
                failures.Add("exception: " + e);
                more = false;
            }
            if (more)
            {
                waitUntil = EditorApplication.timeSinceStartup + steps.Current;
                return;
            }
            EditorApplication.update -= Tick;
            foreach (var f in failures) Debug.LogError("NEXUS TEST FAIL: " + f);
            Debug.Log(failures.Count == 0 ? "NEXUS TEST: ALL PASSED" : $"NEXUS TEST: {failures.Count} FAILED");
            EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
        }

        static void Check(bool ok, string what)
        {
            Debug.Log((ok ? "NEXUS TEST ok:   " : "NEXUS TEST FAIL: ") + what);
            if (!ok) failures.Add(what);
        }

        /// <summary>Test script; each yield is the number of seconds to wait before the next step.</summary>
        static IEnumerator<float> Steps()
        {
            yield return 1f;
            var gm = GameManager.Instance;
            var level = LevelBuilder.Instance;
            var player = PlayerMovement.Instance;
            Check(gm != null && level != null && player != null, "scene has GameManager, LevelBuilder, player");
            Check(BugAI.All.Count > 0, "at least one Bug spawned");
            Check(BugAI.All.Count > 0 && BugAI.All[0].type == BugType.Chaser, "first Bug is the Chaser");
            if (failures.Count > 0) yield break;
            var lives = player.GetComponent<PlayerLives>();
            var bug = BugAI.All[0];
            Check(lives != null, "player has PlayerLives");
            Check(gm.Lives == 3, $"starts with 3 lives (has {gm.Lives})");
            RenderCamera("play.png");

            // ---- Energy Blast ----
            var blast = player.GetComponent<EnergyBlast>();
            Check(blast != null, "player has EnergyBlast");
            Check(!blast.TryBlast(), "no blast without energy");
            for (int i = 0; i < 40; i++) gm.CollectOrb();      // 40 energy (+1 each)
            float energyBefore = gm.Energy;
            Vector2Int near = level.PlayerStart;
            foreach (var c in GridPath.CellsWithin(level, player.CurrentCell, 2))
                if (Mathf.Abs(c.x - player.CurrentCell.x) + Mathf.Abs(c.y - player.CurrentCell.y) == 2) { near = c; break; }
            bug.PlaceAt(near);
            float distBefore = Vector3.Distance(bug.transform.position, player.transform.position);
            Check(blast.TryBlast(), "blast fires with 40 energy");
            Check(Mathf.Approximately(gm.Energy, energyBefore - 30f), $"blast costs 30 energy ({energyBefore} -> {gm.Energy})");
            Check(bug.IsFrozen, "Bug within 3 cells is frozen");
            Check(!blast.TryBlast(), "second blast blocked by cooldown");
            yield return 0.4f;
            RenderCamera("blast.png");
            float distAfter = Vector3.Distance(bug.transform.position, player.transform.position);
            Check(distAfter > distBefore + 0.5f, $"Bug pushed away ({distBefore:0.0} -> {distAfter:0.0})");
            var frozenAt = bug.transform.position;
            yield return 1f;
            Check(bug.transform.position == frozenAt && bug.IsFrozen, "Bug stays frozen");
            yield return 0.8f;
            Check(!bug.IsFrozen, "Bug unfreezes after 2 seconds");
            bug.ResetToSpawn(); // far from Pac-Man again before the lives checks
            yield return 0.9f;
            Check(blast.CooldownLeft <= 0f, "cooldown over after 3 seconds");
            bug.ResetToSpawn();
            yield return 0.1f;

            // First hit: cores must stay collected
            gm.CollectCore();
            int cores = gm.CoresCollected;
            bug.transform.position = player.transform.position;
            yield return 0.05f;
            Check(gm.Lives == 2, $"hit removes one life (has {gm.Lives})");
            Check(player.CurrentCell == level.PlayerStart, $"Pac-Man back at P {level.PlayerStart} (is {player.CurrentCell})");
            Check(bug.CurrentCell == bug.SpawnCell, $"Bug back at spawn {bug.SpawnCell} (is {bug.CurrentCell})");
            Check(lives.IsInvulnerable, "Pac-Man is invulnerable after the hit");
            Check(gm.CoresCollected == cores, $"collected cores stay ({gm.CoresCollected}/{cores})");

            // Blinking and invulnerability
            yield return 0.5f;
            bug.transform.position = player.transform.position;
            yield return 0.05f;
            Check(gm.Lives == 2, "no life lost while invulnerable");
            yield return 1.8f;
            Check(!lives.IsInvulnerable, "invulnerability ends after 2 seconds");
            bool allVisible = true;
            foreach (var r in player.GetComponentsInChildren<Renderer>()) allVisible &= r.enabled;
            Check(allVisible, "Pac-Man visible again after blinking");

            // Two more hits -> game over
            bug.transform.position = player.transform.position;
            yield return 2.3f;
            bug.transform.position = player.transform.position;
            yield return 0.1f;
            Check(gm.Lives == 0 && gm.GameOver, $"game over at 0 lives (lives {gm.Lives}, over {gm.GameOver})");
            Check(!gm.IsPlaying, "game stops at game over");
            var frozen = bug.transform.position;
            yield return 0.5f;
            Check(bug.transform.position == frozen, "Bug stops moving at game over");

            // Restart (what Space does)
            gm.Restart();
            yield return 2f;
            var gm2 = GameManager.Instance;
            Check(gm2 != null && gm2 != gm, "restart loads a fresh scene");
            Check(gm2 != null && gm2.Lives == 3 && gm2.CoresCollected == 0 && !gm2.GameOver,
                "after restart: 3 lives, 0 cores, playing");
        }

        static void RenderCamera(string file)
        {
            var cam = Camera.main;
            if (cam == null) return;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory(shotDir);
            File.WriteAllBytes(Path.Combine(shotDir, file), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
        }

        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
    }
}
