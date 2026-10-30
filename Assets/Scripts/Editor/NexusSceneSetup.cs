using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nexus.EditorTools
{
    /// <summary>
    /// Menu: Nexus > Create Level 1 Scene
    /// Builds a ready-to-play scene so you only have to press Play.
    /// </summary>
    public static class NexusSceneSetup
    {
        const string LayoutPath = "Assets/Levels/Level1_NeonDistrict.txt";
        const string ScenePath = "Assets/Scenes/Level1_NeonDistrict.unity";
        const string VolumePath = "Assets/Settings/NexusVolumeProfile.asset";

        [MenuItem("Nexus/Create Level 1 Scene")]
        public static void CreateLevel1()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (layout == null)
            {
                EditorUtility.DisplayDialog("Nexus", "Layout not found: " + LayoutPath, "OK");
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

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = level.gameObject;
            Debug.Log("Nexus: Level 1 scene created at " + ScenePath + ". Press Play!");
        }
    }
}
