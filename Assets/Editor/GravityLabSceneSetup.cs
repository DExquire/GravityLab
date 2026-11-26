using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GravityLab.EditorTools
{
    /// <summary>Creates Assets/Scenes/Main.unity (a single "Bootstrap" object) on first import and applies PC player settings.</summary>
    [InitializeOnLoad]
    public static class GravityLabSceneSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static GravityLabSceneSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath)) return;
            BuildScene();
        }

        [MenuItem("GravityLab/Rebuild Main Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject boot = new GameObject("Bootstrap");
            boot.AddComponent<GravityLab.Core.Bootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("GravityLab: Main scene created at " + ScenePath + ". Press Play (Game view 16:9 recommended).");
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "Gravity Lab";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
        }
    }
}
