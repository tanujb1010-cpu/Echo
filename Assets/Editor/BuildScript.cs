using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.EditorTools
{
    /// <summary>
    /// Ship pipeline: generates the single Main scene (the whole game runs out of <see cref="Echo.Unity.GameFlow"/>,
    /// so one empty scene with three components IS the game), then batch-builds players.
    ///
    /// CI usage:
    ///   Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod Echo.EditorTools.BuildScript.BuildMainScene -quit
    ///   Unity.exe -batchmode -projectPath &lt;path&gt; -executeMethod Echo.EditorTools.BuildScript.BuildWindows -quit
    ///   Unity.exe -batchmode -projectPath &lt;path&gt; -executeMethod Echo.EditorTools.BuildScript.BuildWebGL -quit
    /// </summary>
    public static class BuildScript
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Echo/Build/Generate Main Scene")]
        public static void BuildMainScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("GameRoot");
            root.AddComponent<Echo.Unity.GameBootstrap>();
            root.AddComponent<Echo.Unity.GameFlow>();

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 13f;                       // frames the 48x24 tile room
            cam.transform.position = new Vector3(24f, 12f, -10f);
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            EditorSceneManager.SaveScene(scene, MainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildScript] Main scene written to {MainScenePath}");
        }

        [MenuItem("Echo/Build/Windows x64")]
        public static void BuildWindows() => BuildPlayer(BuildTarget.StandaloneWindows64, "Builds/Windows/Echo.exe");

        [MenuItem("Echo/Build/WebGL")]
        public static void BuildWebGL() => BuildPlayer(BuildTarget.WebGL, "Builds/WebGL");

        private static void BuildPlayer(BuildTarget target, string outputPath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null) BuildMainScene();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[BuildScript] {target} build OK → {outputPath} ({summary.totalSize / (1024 * 1024)} MB)");
            else
                Debug.LogError($"[BuildScript] {target} build FAILED: {summary.totalErrors} errors");
        }
    }
}
