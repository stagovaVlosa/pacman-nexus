using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nexus.EditorTools
{
    /// <summary>
    /// Menu: Nexus > Create Level 1 Scene / Create Level 2 Scene
    /// Builds a ready-to-play scene so you only have to press Play, links the levels
    /// (Level 1 -> Level 2) and puts both scenes in the Build Settings.
    /// </summary>
    public static class NexusSceneSetup
    {
        const string Level1Layout = "Assets/Levels/Level1_NeonDistrict.txt";
        const string Level1Scene = "Assets/Scenes/Level1_NeonDistrict.unity";
        const string Level2Layout = "Assets/Levels/Level2_SkylineZone.txt";
        const string Level2Scene = "Assets/Scenes/Level2_SkylineZone.unity";
        const string VolumePath = "Assets/Settings/NexusVolumeProfile.asset";

        [MenuItem("Nexus/Create Level 1 Scene")]
        public static void CreateLevel1() =>
            CreateLevel(Level1Layout, Level1Scene, "Neon District", 1, LevelTheme.NeonDistrict, Level2Scene);

        [MenuItem("Nexus/Create Level 2 Scene")]
        public static void CreateLevel2()
        {
            CreateLevel(Level2Layout, Level2Scene, "Skyline Zone", 2, LevelTheme.SkylineZone, "");
            LinkLevel1ToLevel2();
        }

        /// <summary>Sets Level 1's next scene to Level 2 (without rebuilding Level 1) and updates the Build Settings.</summary>
        [MenuItem("Nexus/Link Level 1 -> Level 2")]
        public static void LinkLevel1ToLevel2()
        {
            var current = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(Level1Scene, OpenSceneMode.Single);
            var lb = Object.FindFirstObjectByType<LevelBuilder>();
            if (lb != null)
            {
                lb.nextScenePath = Level2Scene;
                lb.levelName = "Neon District";
                lb.levelNumber = 1;
                lb.theme = LevelTheme.NeonDistrict;
                EditorUtility.SetDirty(lb);
                EditorSceneManager.SaveScene(scene);
            }
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Level1Scene, true),
                new EditorBuildSettingsScene(Level2Scene, true)
            };
            if (!string.IsNullOrEmpty(current) && current != Level1Scene) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            Debug.Log("Nexus: Level 1 now continues to Level 2 (ENTER after the Generator). Build Settings updated.");
        }

        static void CreateLevel(string layoutPath, string scenePath, string levelName, int number, LevelTheme theme, string next)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(layoutPath);
            if (layout == null)
            {
                EditorUtility.DisplayDialog("Nexus", "Layout not found: " + layoutPath, "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Visuals.Hex("#080D1B");
            cam.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();
            if (!camGo.TryGetComponent(out UniversalAdditionalCameraData camData)) camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camGo.AddComponent<CameraFollow>();

            // Dim moonlight so the neon stands out
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.5f;
            light.color = Visuals.Hex("#8FA8FF");
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Visuals.Hex("#1A2340");

            // Bloom for the neon glow
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(1.3f);
                bloom.threshold.Override(0.9f);
                bloom.scatter.Override(0.7f);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);
                AssetDatabase.CreateAsset(profile, VolumePath);
            }
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // Game systems
            new GameObject("GameManager").AddComponent<GameManager>();
            new GameObject("HUD").AddComponent<GameHUD>();
            var level = new GameObject("Level").AddComponent<LevelBuilder>();
            level.layout = layout;
            level.levelName = levelName;
            level.levelNumber = number;
            level.theme = theme;
            level.nextScenePath = next;

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = level.gameObject;
            Debug.Log($"Nexus: Level {number} scene created at {scenePath}. Press Play!");
        }
    }
}
