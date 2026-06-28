using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DontDiePlease.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DontDiePlease.EditorTools
{
    [CustomEditor(typeof(SeededMapGenerator))]
    [CanEditMultipleObjects]
    public sealed class SeededMapGeneratorEditor : Editor
    {
        private const string DefaultExternalPath = @"C:\baidunetdiskdownload";
        private const string ImportedAssetFolder = "Assets/ImportedSciFiAssets";
        private const string ExternalPathPrefKey = "DontDiePlease.SeededMapGenerator.ExternalAssetPath";

        private static readonly HashSet<string> PrimaryAssetExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".prefab",
            ".fbx",
            ".obj"
        };

        private static readonly HashSet<string> CopyAssetExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".prefab",
            ".fbx",
            ".obj",
            ".mat",
            ".png",
            ".jpg",
            ".jpeg",
            ".tga",
            ".tif",
            ".tiff",
            ".psd",
            ".exr",
            ".hdr",
            ".asset",
            ".anim",
            ".controller",
            ".shader",
            ".shadergraph"
        };

        private string externalAssetPath;

        private void OnEnable()
        {
            externalAssetPath = EditorPrefs.GetString(ExternalPathPrefKey, DefaultExternalPath);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(12f);
            DrawImportPanel();
            GUILayout.Space(12f);
            DrawGenerationButtons();
        }

        private void DrawImportPanel()
        {
            EditorGUILayout.LabelField("Sci-Fi Asset Import", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                externalAssetPath = EditorGUILayout.TextField("External Path", externalAssetPath);

                if (GUILayout.Button("Browse", GUILayout.Width(90f)))
                {
                    var pickedPath = EditorUtility.OpenFolderPanel("Select External Sci-Fi Asset Folder", externalAssetPath, string.Empty);

                    if (!string.IsNullOrWhiteSpace(pickedPath))
                    {
                        externalAssetPath = pickedPath;
                        EditorPrefs.SetString(ExternalPathPrefKey, externalAssetPath);
                    }
                }
            }

            if (GUILayout.Button("Import and Auto-Assign Assets", GUILayout.Height(34f)))
            {
                ImportAndAutoAssignAssets();
            }
        }

        private void DrawGenerationButtons()
        {
            if (GUILayout.Button("Generate"))
            {
                foreach (var targetObj in targets)
                {
                    var generator = targetObj as SeededMapGenerator;

                    if (generator == null)
                    {
                        continue;
                    }

                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Seeded Map");
                    generator.Generate();
                    PersistGenerator(generator);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                foreach (var targetObj in targets)
                {
                    var generator = targetObj as SeededMapGenerator;

                    if (generator == null)
                    {
                        continue;
                    }

                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Seeded Map");
                    generator.ClearGeneratedMap();
                    PersistGenerator(generator);
                }
            }
        }

        private void ImportAndAutoAssignAssets()
        {
            var generators = targets.OfType<SeededMapGenerator>().Where(generator => generator != null).ToArray();

            if (generators.Length == 0)
            {
                EditorUtility.DisplayDialog("Import Failed", "No SeededMapGenerator target was found.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(externalAssetPath))
            {
                EditorUtility.DisplayDialog("Import Failed", "Please choose a valid external asset path first.", "OK");
                return;
            }

            EditorPrefs.SetString(ExternalPathPrefKey, externalAssetPath);

            var importPlan = BuildImportPlan(externalAssetPath);

            if (importPlan == null)
            {
                return;
            }

            var importedAssetPaths = new List<string>();

            try
            {
                if (importPlan.UnityPackages.Count > 0)
                {
                    importedAssetPaths.AddRange(ImportUnityPackages(importPlan.UnityPackages));
                }

                if (importPlan.CopyFiles.Count > 0)
                {
                    importedAssetPaths.AddRange(CopyExternalAssets(importPlan.CopyFiles, importPlan.RootPath));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }
                else if (importPlan.UnityPackages.Count > 0)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }
            }
            catch (Exception err)
            {
                EditorUtility.DisplayDialog("Import Failed", err.Message, "OK");
                return;
            }

            var assetObjects = LoadImportedGameObjects(importedAssetPaths);

            if (assetObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Import Failed", "Unity imported the files, but no prefab or model GameObject assets were found.", "OK");
                return;
            }

            var groups = CategorizeAssets(assetObjects);

            foreach (var generator in generators)
            {
                Undo.RecordObject(generator, "Auto-Assign Seeded Map Assets");
                AssignGroups(generator, groups);
                PersistGenerator(generator);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Import Complete", BuildResultMessage(assetObjects.Count, groups), "OK");
        }

        private static ImportPlan BuildImportPlan(string inputPath)
        {
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(inputPath);
            }
            catch (Exception err) when (err is ArgumentException || err is NotSupportedException || err is PathTooLongException)
            {
                EditorUtility.DisplayDialog("Import Failed", err.Message, "OK");
                return null;
            }

            if (File.Exists(fullPath))
            {
                return BuildFileImportPlan(fullPath);
            }

            if (!Directory.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("Import Failed", $"The external path does not exist:\n{fullPath}", "OK");
                return null;
            }

            List<string> allFiles;

            try
            {
                allFiles = Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories).ToList();
            }
            catch (Exception err) when (err is IOException || err is UnauthorizedAccessException)
            {
                EditorUtility.DisplayDialog("Import Failed", err.Message, "OK");
                return null;
            }

            var primaryAssets = allFiles.Where(path => PrimaryAssetExts.Contains(Path.GetExtension(path))).ToList();
            var unityPackages = allFiles.Where(path => string.Equals(Path.GetExtension(path), ".unitypackage", StringComparison.OrdinalIgnoreCase)).ToList();

            if (primaryAssets.Count == 0 && unityPackages.Count == 0)
            {
                EditorUtility.DisplayDialog("Import Failed", "No .prefab, .fbx, .obj, or .unitypackage assets were found in that folder.", "OK");
                return null;
            }

            var copyFiles = allFiles.Where(path => CopyAssetExts.Contains(Path.GetExtension(path))).ToList();
            return new ImportPlan(fullPath, copyFiles, unityPackages);
        }

        private static ImportPlan BuildFileImportPlan(string filePath)
        {
            var ext = Path.GetExtension(filePath);

            if (string.Equals(ext, ".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                return new ImportPlan(ResolveFileRoot(filePath), new List<string>(), new List<string> { filePath });
            }

            if (!PrimaryAssetExts.Contains(ext))
            {
                EditorUtility.DisplayDialog("Import Failed", "The selected file must be a .prefab, .fbx, .obj, or .unitypackage file.", "OK");
                return null;
            }

            return new ImportPlan(ResolveFileRoot(filePath), new List<string> { filePath }, new List<string>());
        }

        private static List<string> ImportUnityPackages(IEnumerable<string> packagePaths)
        {
            var before = new HashSet<string>(AssetDatabase.GetAllAssetPaths(), StringComparer.OrdinalIgnoreCase);

            foreach (var packagePath in packagePaths)
            {
                AssetDatabase.ImportPackage(packagePath, false);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return AssetDatabase.GetAllAssetPaths()
                .Where(path => !before.Contains(path))
                .Where(IsGameObjectAssetPath)
                .ToList();
        }

        private static List<string> CopyExternalAssets(IEnumerable<string> files, string rootPath)
        {
            Directory.CreateDirectory(ProjectImportAbsolutePath());

            var copiedPaths = new List<string>();
            var projectImportRoot = Path.GetFullPath(ProjectImportAbsolutePath());

            foreach (var sourcePath in files)
            {
                var fullSourcePath = Path.GetFullPath(sourcePath);

                if (fullSourcePath.StartsWith(projectImportRoot, StringComparison.OrdinalIgnoreCase))
                {
                    copiedPaths.Add(ToAssetPath(fullSourcePath));
                    continue;
                }

                var relativePath = GetRelativePath(rootPath, fullSourcePath);
                var targetPath = Path.Combine(projectImportRoot, relativePath);
                var targetDir = Path.GetDirectoryName(targetPath);

                if (!string.IsNullOrWhiteSpace(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(fullSourcePath, targetPath, true);
                copiedPaths.Add(ToAssetPath(targetPath));
            }

            return copiedPaths.Where(IsGameObjectAssetPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<GameObject> LoadImportedGameObjects(IEnumerable<string> assetPaths)
        {
            var paths = assetPaths
                .Where(IsGameObjectAssetPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (paths.Count == 0 && AssetDatabase.IsValidFolder(ImportedAssetFolder))
            {
                paths = AssetDatabase.FindAssets(string.Empty, new[] { ImportedAssetFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(IsGameObjectAssetPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return paths
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(obj => obj != null)
                .Distinct()
                .OrderBy(obj => obj.name)
                .ToList();
        }

        private static PrefabGroups CategorizeAssets(IEnumerable<GameObject> assets)
        {
            var groups = new PrefabGroups();

            foreach (var asset in assets)
            {
                var name = asset.name.ToLowerInvariant();

                if (HasAny(name, "floor", "ground"))
                {
                    groups.Floors.Add(asset);
                }

                if (HasAny(name, "wall", "panel", "corner"))
                {
                    groups.Walls.Add(asset);
                }

                if (HasAny(name, "door", "gate"))
                {
                    groups.Doors.Add(asset);
                }

                if (HasAny(name, "ceiling", "roof"))
                {
                    groups.Ceilings.Add(asset);
                }

                if (HasAny(name, "storage", "crate", "barrel", "box"))
                {
                    groups.Storage.Add(asset);
                }

                if (HasAny(name, "maintenance", "pipe", "valve", "vent"))
                {
                    groups.Maintenance.Add(asset);
                }

                if (HasAny(name, "robot", "checkpoint", "turret", "security"))
                {
                    groups.RobotCheckpoints.Add(asset);
                }

                if (HasAny(name, "toxic", "gas", "poison", "hazard"))
                {
                    groups.ToxicPockets.Add(asset);
                }

                if (HasAny(name, "resource", "crystal", "ore", "loot"))
                {
                    groups.Resources.Add(asset);
                }
            }

            return groups;
        }

        private static void AssignGroups(SeededMapGenerator generator, PrefabGroups groups)
        {
            var so = new SerializedObject(generator);
            AssignArray(so, "floorPrefabs", groups.Floors);
            AssignArray(so, "wallPrefabs", groups.Walls);
            AssignArray(so, "doorPrefabs", groups.Doors);
            AssignArray(so, "ceilingPrefabs", groups.Ceilings);
            AssignArray(so, "storagePrefabs", groups.Storage);
            AssignArray(so, "maintenancePrefabs", groups.Maintenance);
            AssignArray(so, "robotCheckpointPrefabs", groups.RobotCheckpoints);
            AssignArray(so, "toxicPocketPrefabs", groups.ToxicPockets);
            AssignArray(so, "resourcePrefabs", groups.Resources);
            so.ApplyModifiedProperties();
        }

        private static void AssignArray(SerializedObject so, string propertyName, IEnumerable<GameObject> values)
        {
            var prop = so.FindProperty(propertyName);

            if (prop == null || !prop.isArray)
            {
                return;
            }

            var list = values.Where(obj => obj != null).Distinct().ToList();
            prop.ClearArray();

            for (var idx = 0; idx < list.Count; idx++)
            {
                prop.InsertArrayElementAtIndex(idx);
                prop.GetArrayElementAtIndex(idx).objectReferenceValue = list[idx];
            }
        }

        private static string BuildResultMessage(int importedCount, PrefabGroups groups)
        {
            return string.Join("\n", new[]
            {
                $"Loaded GameObject assets: {importedCount}",
                $"Floors: {groups.Floors.Count}",
                $"Walls: {groups.Walls.Count}",
                $"Doors: {groups.Doors.Count}",
                $"Ceilings: {groups.Ceilings.Count}",
                $"Storage: {groups.Storage.Count}",
                $"Maintenance: {groups.Maintenance.Count}",
                $"Robot checkpoints: {groups.RobotCheckpoints.Count}",
                $"Toxic pockets: {groups.ToxicPockets.Count}",
                $"Resources: {groups.Resources.Count}"
            });
        }

        private static void PersistGenerator(SeededMapGenerator generator)
        {
            EditorUtility.SetDirty(generator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(generator);

            var scene = generator.gameObject.scene;

            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static bool HasAny(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }

        private static string ResolveFileRoot(string filePath)
        {
            return Path.GetDirectoryName(filePath) ?? Path.GetPathRoot(filePath) ?? string.Empty;
        }

        private static bool IsGameObjectAssetPath(string path)
        {
            return PrimaryAssetExts.Contains(Path.GetExtension(path));
        }

        private static string ProjectImportAbsolutePath()
        {
            return Path.Combine(ProjectRootPath(), ImportedAssetFolder.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private static string ProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string ToAssetPath(string absolutePath)
        {
            var root = ProjectRootPath().Replace("\\", "/");
            var cleanPath = Path.GetFullPath(absolutePath).Replace("\\", "/");
            return cleanPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? cleanPath.Substring(root.Length + 1) : cleanPath;
        }

        private static string GetRelativePath(string rootPath, string filePath)
        {
            var rootUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(rootPath)));
            var fileUri = new Uri(Path.GetFullPath(filePath));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + Path.DirectorySeparatorChar;
        }

        private sealed class ImportPlan
        {
            public readonly string RootPath;
            public readonly List<string> CopyFiles;
            public readonly List<string> UnityPackages;

            public ImportPlan(string rootPath, List<string> copyFiles, List<string> unityPackages)
            {
                RootPath = rootPath;
                CopyFiles = copyFiles;
                UnityPackages = unityPackages;
            }
        }

        private sealed class PrefabGroups
        {
            public readonly List<GameObject> Floors = new List<GameObject>();
            public readonly List<GameObject> Walls = new List<GameObject>();
            public readonly List<GameObject> Doors = new List<GameObject>();
            public readonly List<GameObject> Ceilings = new List<GameObject>();
            public readonly List<GameObject> Storage = new List<GameObject>();
            public readonly List<GameObject> Maintenance = new List<GameObject>();
            public readonly List<GameObject> RobotCheckpoints = new List<GameObject>();
            public readonly List<GameObject> ToxicPockets = new List<GameObject>();
            public readonly List<GameObject> Resources = new List<GameObject>();
        }
    }
}
