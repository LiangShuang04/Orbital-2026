using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Central.EditorTools
{
    public static class DemoCombatNavMeshBaker
    {
        private const string ScenePath = "Assets/Scenes/Demo_Combat.unity";
        private const string DataFolder = "Assets/Scenes/Demo_Combat_NavMesh";
        private const string DataPath = DataFolder + "/DemoCombatWalkable.asset";
        private const string SurfaceName = "Demo_CombatCombatNavMesh";
        private const string RequestPath = "Temp/DemoCombatNavMeshBake.request";
        private const string ResultPath = "Temp/DemoCombatNavMeshBake.result";

        [InitializeOnLoadMethod]
        private static void BakeWhenRequested()
        {
            if (!File.Exists(RequestPath))
                return;

            EditorApplication.delayCall += RunRequestedBake;
        }

        [MenuItem("Tools/Don't Die Please/Combat/Demo/Bake Demo Combat NavMesh")]
        public static void BakeFromMenu()
        {
            try
            {
                var result = Bake();
                Debug.Log(result);
                EditorUtility.DisplayDialog("Demo Combat NavMesh", result, "OK");
            }
            catch (Exception err)
            {
                Debug.LogException(err);
                EditorUtility.DisplayDialog("Demo Combat NavMesh", err.Message, "OK");
            }
        }

        public static void BakeAndLog()
        {
            Debug.Log(Bake());
        }

        public static string Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before baking the Demo_Combat NavMesh.");

            var scene = LoadScene();
            var surface = GetOrCreateSurface(scene);
            var modifierStates = IgnoreMeshColliders(scene);

            try
            {
                surface.BuildNavMesh();
                SaveNavMeshData(surface);
            }
            finally
            {
                RestoreModifiers(modifierStates);
            }

            EditorUtility.SetDirty(surface);
            EditorUtility.SetDirty(surface.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();

            var triangulation = NavMesh.CalculateTriangulation();

            if (surface.navMeshData == null || triangulation.vertices.Length == 0)
                throw new InvalidOperationException("The bake completed without producing walkable NavMesh geometry.");

            var bounds = surface.navMeshData.sourceBounds;
            return $"Demo_Combat NavMesh baked: {triangulation.vertices.Length} vertices, {triangulation.indices.Length / 3} triangles, bounds {bounds.size}, data {DataPath}.";
        }

        private static void RunRequestedBake()
        {
            if (!File.Exists(RequestPath))
                return;

            File.Delete(RequestPath);

            try
            {
                var result = Bake();
                Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "Temp");
                File.WriteAllText(ResultPath, result);
                Debug.Log(result);
            }
            catch (Exception err)
            {
                File.WriteAllText(ResultPath, "ERROR: " + err);
                Debug.LogException(err);
            }
        }

        private static Scene LoadScene()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);

            if (scene.IsValid() && scene.isLoaded)
                return scene;

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static NavMeshSurface GetOrCreateSurface(Scene scene)
        {
            var surface = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<NavMeshSurface>(true))
                .FirstOrDefault(item => item != null && item.gameObject.name == SurfaceName);

            if (surface == null)
            {
                var obj = new GameObject(SurfaceName);
                SceneManager.MoveGameObjectToScene(obj, scene);
                surface = obj.AddComponent<NavMeshSurface>();
            }

            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = Physics.DefaultRaycastLayers;
            surface.defaultArea = 0;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;
            surface.overrideTileSize = false;
            surface.overrideVoxelSize = false;
            surface.minRegionArea = 2f;
            surface.buildHeightMesh = false;
            surface.enabled = true;
            return surface;
        }

        private static List<ModifierState> IgnoreMeshColliders(Scene scene)
        {
            var states = new List<ModifierState>();
            var colliders = Resources.FindObjectsOfTypeAll<MeshCollider>()
                .Where(collider => collider != null && collider.gameObject.scene == scene)
                .ToArray();

            foreach (var collider in colliders)
            {
                var modifier = collider.GetComponent<NavMeshModifier>();
                var added = modifier == null;

                if (added)
                    modifier = collider.gameObject.AddComponent<NavMeshModifier>();

                states.Add(new ModifierState(modifier, added, modifier.ignoreFromBuild));
                modifier.ignoreFromBuild = true;
            }

            return states;
        }

        private static void RestoreModifiers(IEnumerable<ModifierState> states)
        {
            foreach (var state in states)
            {
                if (state.Modifier == null)
                    continue;

                if (state.Added)
                    UnityEngine.Object.DestroyImmediate(state.Modifier);
                else
                    state.Modifier.ignoreFromBuild = state.Ignored;
            }
        }

        private static void SaveNavMeshData(NavMeshSurface surface)
        {
            var builtData = surface.navMeshData;

            if (builtData == null)
                throw new InvalidOperationException("Unity did not produce NavMeshData for Demo_Combat.");

            builtData.name = Path.GetFileNameWithoutExtension(DataPath);
            EnsureFolder();
            var savedData = AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);

            if (savedData == null)
            {
                AssetDatabase.CreateAsset(builtData, DataPath);
                surface.navMeshData = builtData;
                return;
            }

            surface.RemoveData();
            EditorUtility.CopySerialized(builtData, savedData);
            surface.navMeshData = savedData;
            surface.AddData();
            UnityEngine.Object.DestroyImmediate(builtData);
            EditorUtility.SetDirty(savedData);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets/Scenes", "Demo_Combat_NavMesh");
        }

        private readonly struct ModifierState
        {
            public readonly NavMeshModifier Modifier;
            public readonly bool Added;
            public readonly bool Ignored;

            public ModifierState(NavMeshModifier modifier, bool added, bool ignored)
            {
                Modifier = modifier;
                Added = added;
                Ignored = ignored;
            }
        }
    }
}
