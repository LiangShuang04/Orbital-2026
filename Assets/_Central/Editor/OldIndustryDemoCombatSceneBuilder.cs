using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DontDiePlease.Central.EditorTools
{
    public static class OldIndustryDemoCombatSceneBuilder
    {
        private static readonly string[] SourceScenePaths =
        {
            "Assets/OldIndustry/Scenes/Scene_OldIndustry/OldIndustry.unity",
            "Assets/Scenes/demoMainScene.unity"
        };

        private const string CombatScenePath = "Assets/Scenes/Demo_Combat.unity";

        [MenuItem("Tools/Don't Die Please/Combat/Demo/Rebuild Demo Combat Scene")]
        public static void RebuildDemoCombatScene()
        {
            var sourcePath = SourceScenePaths.FirstOrDefault(path => File.Exists(FullPath(path)));

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                EditorUtility.DisplayDialog("OldIndustry Combat", "No OldIndustry demo scene was found.", "OK");
                return;
            }

            if (File.Exists(FullPath(CombatScenePath)))
            {
                var overwrite = EditorUtility.DisplayDialog("OldIndustry Combat", $"Rebuild {CombatScenePath} from {sourcePath}?", "Rebuild", "Cancel");

                if (!overwrite)
                    return;

                AssetDatabase.DeleteAsset(CombatScenePath);
            }

            if (!AssetDatabase.CopyAsset(sourcePath, CombatScenePath))
            {
                EditorUtility.DisplayDialog("OldIndustry Combat", "Unity could not copy the demo scene.", "OK");
                return;
            }

            AssetDatabase.ImportAsset(CombatScenePath, ImportAssetOptions.ForceUpdate);
            AddSceneToBuildSettings();
            var scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("OldIndustry Combat", "Demo_Combat is ready. Open it and press Play.", "OK");
        }

        [MenuItem("Tools/Don't Die Please/Combat/Demo/Open Demo Combat Scene")]
        public static void OpenDemoCombatScene()
        {
            if (!File.Exists(FullPath(CombatScenePath)))
            {
                RebuildDemoCombatScene();
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
