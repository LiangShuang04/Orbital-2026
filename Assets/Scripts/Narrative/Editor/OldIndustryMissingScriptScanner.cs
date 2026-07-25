using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DontDiePlease.Narrative.Editor
{
    public static class OldIndustryMissingScriptScanner
    {
        private const string ScenePath = "Assets/Scenes/Central_Combat.unity";
        private static readonly HashSet<string> SafeUrpRemovalGuids =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "f19d9143a39eb3b46bc4563e9889cfbd",
                "7a68c43fe1f2a47cfa234b5eeaa98012"
            };

        public static void ScanFromCommandLine()
        {
            var rootPath = Directory.GetParent(Application.dataPath).FullName;
            var outputPath = Path.Combine(rootPath, "Temp", "oldindustry-missing-scripts.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var rows = new List<string>
            {
                Csv(
                    "Scene",
                    "Hierarchy",
                    "ComponentIndex",
                    "MissingScriptGuid",
                    "SourceAsset",
                    "SourceHierarchy",
                    "ActiveSelf",
                    "ActiveInHierarchy",
                    "OtherComponents")
            };
            File.WriteAllLines(outputPath, rows, new UTF8Encoding(false));
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var target in root.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject))
                {
                    var components = target.GetComponents<Component>();

                    for (var index = 0; index < components.Length; index++)
                    {
                        if (components[index] != null)
                        {
                            continue;
                        }

                        try
                        {
                            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(target) ?? target;
                            var sourcePath = AssetDatabase.GetAssetPath(source);

                            if (string.IsNullOrEmpty(sourcePath))
                            {
                                sourcePath = ScenePath;
                            }

                            var sourceId = GlobalObjectId.GetGlobalObjectIdSlow(source).targetObjectId;
                            var scriptGuid = FindScriptGuid(sourcePath, sourceId, index);
                            var otherComponents = components
                                .Where(component => component != null)
                                .Select(component => component.GetType().FullName)
                                .ToArray();
                            rows.Add(
                                Csv(
                                    ScenePath,
                                    Hierarchy(target.transform),
                                    index.ToString(),
                                    scriptGuid,
                                    sourcePath,
                                    Hierarchy(source.transform),
                                    target.activeSelf.ToString(),
                                    target.activeInHierarchy.ToString(),
                                    string.Join("|", otherComponents)));
                        }
                        catch (Exception error)
                        {
                            rows.Add(
                                Csv(
                                    ScenePath,
                                    Hierarchy(target.transform),
                                    index.ToString(),
                                    string.Empty,
                                    string.Empty,
                                    string.Empty,
                                    target.activeSelf.ToString(),
                                    target.activeInHierarchy.ToString(),
                                    error.GetType().Name));
                        }
                    }
                }
            }

            File.WriteAllLines(outputPath, rows, new UTF8Encoding(false));
        }

        public static void ScanSceneDependenciesFromCommandLine()
        {
            var rootPath = Directory.GetParent(Application.dataPath).FullName;
            var outputPath = Path.Combine(rootPath, "Temp", "central-dependency-missing-scripts.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var rows = new List<string>
            {
                Csv(
                    "Asset",
                    "Hierarchy",
                    "ComponentIndex",
                    "MissingScriptGuid",
                    "ActiveSelf",
                    "OtherComponents")
            };
            var prefabPaths = AssetDatabase.GetDependencies(ScenePath, true)
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            foreach (var prefabPath in prefabPaths)
            {
                GameObject root = null;

                try
                {
                    root = PrefabUtility.LoadPrefabContents(prefabPath);

                    foreach (var target in root.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject))
                    {
                        var components = target.GetComponents<Component>();

                        for (var index = 0; index < components.Length; index++)
                        {
                            if (components[index] != null)
                            {
                                continue;
                            }

                            var sourceId = GlobalObjectId.GetGlobalObjectIdSlow(target).targetObjectId;
                            var scriptGuid = FindScriptGuid(prefabPath, sourceId, index);
                            var otherComponents = components
                                .Where(component => component != null)
                                .Select(component => component.GetType().FullName)
                                .ToArray();
                            rows.Add(
                                Csv(
                                    prefabPath,
                                    Hierarchy(target.transform),
                                    index.ToString(),
                                    scriptGuid,
                                    target.activeSelf.ToString(),
                                    string.Join("|", otherComponents)));
                        }
                    }
                }
                catch (Exception error)
                {
                    rows.Add(Csv(prefabPath, string.Empty, string.Empty, string.Empty, string.Empty, error.GetType().Name));
                }
                finally
                {
                    if (root != null)
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            File.WriteAllLines(outputPath, rows, new UTF8Encoding(false));
        }

        public static void RepairProjectOwnedSceneFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var changed = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var target in root.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject))
                {
                    var components = target.GetComponents<Component>();
                    var missingIndexes = Enumerable.Range(0, components.Length)
                        .Where(index => components[index] == null)
                        .ToArray();

                    if (missingIndexes.Length == 0)
                    {
                        continue;
                    }

                    var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(target) ?? target;
                    var sourcePath = AssetDatabase.GetAssetPath(source);
                    var sourceId = GlobalObjectId.GetGlobalObjectIdSlow(source).targetObjectId;

                    foreach (var index in missingIndexes)
                    {
                        var scriptGuid = FindScriptGuid(sourcePath, sourceId, index);

                        if (!sourcePath.StartsWith("Assets/OldIndustry/", StringComparison.Ordinal) ||
                            !SafeUrpRemovalGuids.Contains(scriptGuid))
                        {
                            throw new InvalidOperationException(
                                $"Refusing to remove unclassified missing script '{scriptGuid}' from '{Hierarchy(target.transform)}'.");
                        }
                    }

                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        public static void BuildUrpSafeSceneInstancesFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var objects = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject)
                .ToArray();
            var decalInstances = objects
                .Where(target =>
                {
                    var sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
                    return sourcePath.StartsWith(
                        "Assets/OldIndustry/Prefabs/Decals/",
                        StringComparison.Ordinal);
                })
                .Select(PrefabUtility.GetNearestPrefabInstanceRoot)
                .Where(target => target != null)
                .Distinct()
                .ToArray();

            foreach (var decal in decalInstances)
            {
                UnityEngine.Object.DestroyImmediate(decal);
            }

            var lampRoots = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject)
                .Where(target =>
                {
                    var sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
                    return sourcePath == "Assets/OldIndustry/Prefabs/Furniture/Lamps/Lamp02_Double.prefab" ||
                           sourcePath == "Assets/OldIndustry/Prefabs/Furniture/Lamps/Lamp02_Bar.prefab" ||
                           sourcePath == "Assets/OldIndustry/Prefabs/Furniture/Lamps/Lamp02_Bar_Pink Variant.prefab";
                })
                .Select(PrefabUtility.GetOutermostPrefabInstanceRoot)
                .Where(target => target != null)
                .Distinct()
                .ToArray();

            foreach (var lamp in lampRoots)
            {
                PrefabUtility.UnpackPrefabInstance(
                    lamp,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);

                foreach (var child in lamp.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static string FindScriptGuid(string assetPath, ulong gameObjectId, int componentIndex)
        {
            var absolutePath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!File.Exists(absolutePath))
            {
                return string.Empty;
            }

            var yaml = File.ReadAllText(absolutePath);
            var gameObjectMatch = Regex.Match(
                yaml,
                $@"(?ms)^--- !u!1 &{gameObjectId}\r?\n(?<body>.*?)(?=^--- !u!|\z)");

            if (!gameObjectMatch.Success)
            {
                return string.Empty;
            }

            var componentMatches = Regex.Matches(
                gameObjectMatch.Groups["body"].Value,
                @"(?m)^  - component: \{fileID: (-?\d+)\}");

            if (componentIndex < 0 || componentIndex >= componentMatches.Count)
            {
                return string.Empty;
            }

            var componentId = componentMatches[componentIndex].Groups[1].Value;
            var componentMatch = Regex.Match(
                yaml,
                $@"(?ms)^--- !u!114 &{componentId}\r?\n(?<body>.*?)(?=^--- !u!|\z)");

            if (!componentMatch.Success)
            {
                return string.Empty;
            }

            return Regex.Match(
                componentMatch.Groups["body"].Value,
                @"m_Script: \{fileID: \d+, guid: ([a-fA-F0-9]{32}), type: \d+\}")
                .Groups[1]
                .Value;
        }

        private static string Hierarchy(Transform target)
        {
            var names = new Stack<string>();

            while (target != null)
            {
                names.Push(string.IsNullOrEmpty(target.name) ? "<unnamed>" : target.name);
                target = target.parent;
            }

            return string.Join("/", names);
        }

        private static string Csv(params string[] values)
        {
            return string.Join(",", values.Select(value => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\""));
        }
    }
}
