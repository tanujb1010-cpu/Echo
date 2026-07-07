using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.EditorTools
{
    /// <summary>
    /// One-off tooling to build playable scenes for specific <see cref="Echo.Unity.SampleLevels"/> entries
    /// so a human can press Play and feel-test a level directly, instead of only reasoning about it via
    /// code. Not part of the shipped game — lives under Assets/Editor so it's excluded from player builds.
    /// Run via: Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// Echo.EditorTools.EchoSceneSetup.BuildPlaytestScenes -quit
    /// </summary>
    public static class EchoSceneSetup
    {
        [MenuItem("Echo/Rebuild Playtest Scenes")]
        public static void BuildPlaytestScenes()
        {
            BuildScene("W1_L6_Springboard", Echo.Unity.SampleLevels.World1Level6());
            BuildScene("W2_L6_Freight", Echo.Unity.SampleLevels.World2Level6());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildScene(string name, Echo.Unity.LevelDefinition def)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Levels")) AssetDatabase.CreateFolder("Assets", "Levels");
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

            // Re-runnable: overwrite a previously generated level asset instead of erroring on it existing.
            string levelAssetPath = $"Assets/Levels/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Echo.Unity.LevelDefinition>(levelAssetPath) != null)
                AssetDatabase.DeleteAsset(levelAssetPath);
            AssetDatabase.CreateAsset(def, levelAssetPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var runnerGo = new GameObject("GameRoot");
            runnerGo.AddComponent<Echo.Unity.GameBootstrap>();
            var runner = runnerGo.AddComponent<Echo.Unity.SimRunner>();
            var hud = runnerGo.AddComponent<Echo.Unity.HudV0>();

            var so = new SerializedObject(runner);
            so.FindProperty("_level").objectReferenceValue = def;
            so.ApplyModifiedProperties();

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_runner").objectReferenceValue = runner;
            hudSo.FindProperty("_maxEchoes").intValue = def.MaxEchoes;
            hudSo.ApplyModifiedProperties();

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(def.Spawn.x, def.Spawn.y, -10f);
            camGo.tag = "MainCamera";
            var follow = camGo.AddComponent<Echo.Unity.CameraFollow>();
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("_runner").objectReferenceValue = runner;
            followSo.ApplyModifiedProperties();

            string scenePath = $"Assets/Scenes/{name}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EchoSceneSetup] Built playtest scene: {scenePath} (level asset: {levelAssetPath})");
        }
    }
}
