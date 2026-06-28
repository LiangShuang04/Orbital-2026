using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DontDiePlease.Central.EditorTools
{
    public static class CentralCombatSceneBuilder
    {
        private const string SourceScenePath = "Assets/Scenes/Central.unity";
        private const string CombatScenePath = "Assets/Scenes/Central_Combat.unity";

        [MenuItem("Tools/Don't Die Please/Combat/Central/Rebuild Central Combat Scene")]
        public static void RebuildCentralCombatScene()
        {
            if (!File.Exists(FullPath(SourceScenePath)))
            {
                EditorUtility.DisplayDialog("Central Combat", "Assets/Scenes/Central.unity was not found.", "OK");
                return;
            }

            if (File.Exists(FullPath(CombatScenePath)))
            {
                var overwrite = EditorUtility.DisplayDialog("Central Combat", "Rebuild Assets/Scenes/Central_Combat.unity from Central.unity?", "Rebuild", "Cancel");

                if (!overwrite)
                    return;

                AssetDatabase.DeleteAsset(CombatScenePath);
            }

            var copied = AssetDatabase.CopyAsset(SourceScenePath, CombatScenePath);

            if (!copied)
            {
                EditorUtility.DisplayDialog("Central Combat", "Unity could not copy the Central scene.", "OK");
                return;
            }

            AssetDatabase.ImportAsset(CombatScenePath, ImportAssetOptions.ForceUpdate);
            AddSceneToBuildSettings();
            var scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Central Combat", "Central_Combat is ready. Open it and press Play.", "OK");
        }

        [MenuItem("Tools/Don't Die Please/Combat/Central/Open Central Combat Scene")]
        public static void OpenCentralCombatScene()
        {
            if (!File.Exists(FullPath(CombatScenePath)))
            {
                RebuildCentralCombatScene();
                return;
            }

            EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            if (scenes.Any(x => x.path == CombatScenePath))
                return;

            scenes.Add(new EditorBuildSettingsScene(CombatScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static string FullPath(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }
}
