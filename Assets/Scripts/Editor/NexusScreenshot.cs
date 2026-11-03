using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.EditorTools
{
    /// <summary>
    /// Screenshots from the game camera.
    /// - Menu Nexus > Capture Game Screenshot (in Play mode): saves Docs/Screenshots/game_<time>.png
    /// - Command line (no Play mode needed, Unity in batch mode):
    ///   Unity.exe -batchmode -projectPath . -executeMethod Nexus.EditorTools.NexusScreenshot.CaptureBatch
    ///             -shotPath out.png [-shotWidth 1600 -shotHeight 900] -quit
    ///   Opens Level 1, builds it in Edit mode, runs a few frames of the game scripts and renders the camera.
    /// </summary>
    public static class NexusScreenshot
    {
        const string ScenePath = "Assets/Scenes/Level1_NeonDistrict.unity";

        [MenuItem("Nexus/Capture Game Screenshot")]
        public static void CaptureInPlayMode()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Nexus", "Press Play first, then use this menu.", "OK");
                return;
            }
            Directory.CreateDirectory("Docs/Screenshots");
            string path = $"Docs/Screenshots/game_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Nexus: screenshot saved to " + path);
        }

        public static void CaptureBatch()
        {
            string path = Arg("-shotPath", "Docs/Screenshots/batch.png");
            int width = int.Parse(Arg("-shotWidth", "1600"));
            int height = int.Parse(Arg("-shotHeight", "900"));
            int frames = int.Parse(Arg("-shotFrames", "30"));
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                SimulateStartup(frames);
                Render(Camera.main, path, width, height);
                Debug.Log("Nexus: batch screenshot saved to " + path);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        /// <summary>Calls Awake/Start/Update/LateUpdate by hand, because Edit mode does not run them.</summary>
        static void SimulateStartup(int frames)
        {
            var done = new System.Collections.Generic.HashSet<MonoBehaviour>();
            // Managers first, then the level builder (it creates everything else)
            foreach (var name in new[] { "GameManager", "LevelBuilder" })
                foreach (var mb in Behaviours().Where(b => b.GetType().Name == name))
                {
                    Call(mb, "Awake");
                    done.Add(mb);
                }
            var all = Behaviours().ToList();
            foreach (var mb in all) if (!done.Contains(mb)) Call(mb, "Awake");
            foreach (var mb in all) Call(mb, "Start");
            for (int f = 0; f < frames; f++)
            {
                foreach (var mb in Behaviours()) Call(mb, "Update");
                foreach (var mb in Behaviours()) Call(mb, "LateUpdate");
            }
            // Time does not advance in Edit mode: finish fades (BuildingFader) in one big step
            foreach (var mb in Behaviours())
            {
                var refresh = mb.GetType().GetMethod("Refresh", new[] { typeof(float) });
                if (refresh != null) refresh.Invoke(mb, new object[] { 10f });
            }
        }

        static System.Collections.Generic.IEnumerable<MonoBehaviour> Behaviours() =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b != null && b.GetType().Namespace == "Nexus");

        static void Call(MonoBehaviour mb, string method)
        {
            var m = mb.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null || m.GetParameters().Length != 0) return;
            try { m.Invoke(mb, null); }
            catch (Exception e) { Debug.LogWarning($"Nexus screenshot: {mb.GetType().Name}.{method} failed: {e.InnerException?.Message ?? e.Message}"); }
        }

        static void Render(Camera cam, string path, int width, int height)
        {
            if (cam == null) throw new Exception("No Main Camera in the scene.");
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.Render(); // second frame: reflection probe and bloom history are ready
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            cam.targetTexture = prevTarget;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
        }
    }
}
