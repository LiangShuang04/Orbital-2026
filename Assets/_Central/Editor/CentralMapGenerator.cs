using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Central.EditorTools
{
    public sealed class CentralMapGenerator : EditorWindow
    {
        private const string OldIndustryRoot = "Assets/OldIndustry";
        private const string CentralRoot = "Assets/_Central";
        private const string ManifestPath = "Assets/_Central/PrefabManifest.json";
        private const string ReadmePath = "Assets/_Central/README.md";
        private const string PuddleMeshPath = "Assets/_Central/CentralPuddleQuad.asset";
        private const string ScenePath = "Assets/Scenes/Central.unity";
        private const string GeneratedRootName = "Central_Generated";
        private const float CellSize = 6f;
        private const int DefaultSeed = 27062026;

        private int seed = DefaultSeed;
        private bool randomizeSeedOnGenerate;
        private Vector2 scroll;
        private CentralPrefabManifest manifest;
        private BuildSummary lastSummary;
        private static Mesh puddleMesh;

        [MenuItem("Tools/Don't Die Please/Map/Central/Open Generator")]
        public static void Open()
        {
            var window = GetWindow<CentralMapGenerator>("Central Map");
            window.minSize = new Vector2(440f, 520f);
            window.Show();
        }

        [MenuItem("Tools/Don't Die Please/Map/Central/Generate Map")]
        public static void BuildDefaultCentralMap()
        {
            var gen = CreateInstance<CentralMapGenerator>();
            gen.seed = DefaultSeed;
            gen.randomizeSeedOnGenerate = false;
            gen.GenerateCentral(true);
            DestroyImmediate(gen);
        }

        [MenuItem("Tools/Don't Die Please/Map/Central/Rebuild Manifest")]
        public static void RebuildManifestFromMenu()
        {
            EnsureFolders();
            var fresh = BuildManifest();
            SaveManifest(fresh);
            ShowMessage("Central Manifest", BuildManifestMessage(fresh));
        }

        private void OnEnable()
        {
            seed = EditorPrefs.GetInt("DontDiePlease.Central.Seed", DefaultSeed);
            randomizeSeedOnGenerate = EditorPrefs.GetBool("DontDiePlease.Central.RandomizeSeed", false);
            manifest = LoadOrBuildManifest();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Central Map Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);
            seed = EditorGUILayout.IntField("Seed", seed);
            randomizeSeedOnGenerate = EditorGUILayout.Toggle("Randomize Seed On Generate", randomizeSeedOnGenerate);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Seed", GUILayout.Height(28f)))
                {
                    seed = new System.Random().Next(1, int.MaxValue);
                    EditorPrefs.SetInt("DontDiePlease.Central.Seed", seed);
                }

                if (GUILayout.Button("Rebuild Manifest", GUILayout.Height(28f)))
                {
                    manifest = BuildManifest();
                    SaveManifest(manifest);
                }
            }

            if (GUILayout.Button("Generate Central", GUILayout.Height(36f)))
            {
                GenerateCentral(false);
            }

            if (GUILayout.Button("Clear Central Scene", GUILayout.Height(28f)))
            {
                ClearCentralSceneWithPrompt();
            }

            EditorGUILayout.Space(10f);
            DrawManifestStatus();
            DrawSummary();
            EditorGUILayout.EndScrollView();
        }

        private void GenerateCentral(bool allowOverwrite)
        {
            EnsureFolders();

            if (!Directory.Exists(AbsolutePath(OldIndustryRoot)))
            {
                ShowMessage("Central Map", "Assets/OldIndustry was not found.");
                return;
            }

            if (File.Exists(AbsolutePath(ScenePath)) && !allowOverwrite)
            {
                var overwrite = ConfirmAction("Central Map", "Assets/Scenes/Central.unity already exists. Rebuild it?", "Rebuild", "Cancel");

                if (!overwrite)
                {
                    return;
                }
            }

            if (randomizeSeedOnGenerate)
            {
                seed = new System.Random().Next(1, int.MaxValue);
            }

            EditorPrefs.SetInt("DontDiePlease.Central.Seed", seed);
            EditorPrefs.SetBool("DontDiePlease.Central.RandomizeSeed", randomizeSeedOnGenerate);

            manifest = LoadOrBuildManifest();

            if (manifest == null || manifest.Buckets.Length == 0)
            {
                ShowMessage("Central Map", "Prefab manifest is empty.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var rng = new System.Random(seed);
            var assets = new ManifestAssets(manifest);
            lastSummary = new BuildSummary(seed, DetectPipeline());
            var context = new BuildContext(scene, rng, assets, manifest, lastSummary, seed);

            SetupSceneAtmosphere(context);
            BuildHierarchy(context);
            BuildCoreHall(context);
            BuildRailSpur(context);
            BuildSatellites(context);
            BuildPipeRuns(context);
            BuildPerimeter(context);
            BuildDressing(context);
            BuildLighting(context);
            PlaceNavigationTools(context);
            FinalizeScene(context);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            WriteReadme(lastSummary);
            SaveManifest(manifest);
            ShowMessage("Central Map", BuildFinalMessage(lastSummary));
        }

        private void ClearCentralSceneWithPrompt()
        {
            if (!File.Exists(AbsolutePath(ScenePath)))
            {
                ShowMessage("Central Map", "Assets/Scenes/Central.unity does not exist yet.");
                return;
            }

            var clear = ConfirmAction("Central Map", "Open Central.unity and clear Central_Generated?", "Clear", "Cancel");

            if (!clear)
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = FindRoot(scene, GeneratedRootName);

            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void SetupSceneAtmosphere(BuildContext context)
        {
            var skybox = LoadFirstMaterial(context.Assets.SkyboxMaterials);

            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.18f, 0.2f, 0.21f, 1f);
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.33f, 0.33f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.19f, 0.19f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.075f, 1f);
        }

        private static void BuildHierarchy(BuildContext context)
        {
            context.Root = CreateRoot(GeneratedRootName);
            context.Ground = CreateChild(context.Root, "Ground");
            context.Buildings = CreateChild(context.Root, "Buildings");
            context.RailAndPipes = CreateChild(context.Root, "RailAndPipes");
            context.Machinery = CreateChild(context.Root, "Machinery");
            context.Dressing = CreateChild(context.Root, "Dressing");
            context.Lighting = CreateChild(context.Root, "Lighting");
            context.Markers = CreateChild(context.Root, "Markers");
            CreateChild(context.Root, $"__MAP_SEED__={context.Seed}");
        }

        private static void BuildCoreHall(BuildContext context)
        {
            var width = context.Rng.Next(7, 10);
            var depth = context.Rng.Next(6, 9);
            var halfW = width / 2;
            var halfD = depth / 2;
            context.CoreBounds = new Bounds(Vector3.zero, new Vector3(width * CellSize, 8f, depth * CellSize));
            context.Summary.CoreDimensions = $"{width * CellSize:0}m x {depth * CellSize:0}m";

            for (var x = -halfW; x <= halfW; x++)
            {
                for (var z = -halfD; z <= halfD; z++)
                {
                    var pos = new Vector3(x * CellSize, 0f, z * CellSize);
                    Place(context, "floor", pos, Quaternion.identity, context.Ground);

                    var roofRoll = context.Rng.NextDouble();

                    if (roofRoll > 0.22f)
                    {
                        Place(context, "roof", pos + Vector3.up * 6f, Quaternion.identity, context.Buildings);
                    }
                }
            }

            for (var x = -halfW; x <= halfW; x++)
            {
                var northGap = x == 0 || x == 1;
                var southGap = x == -1 || x == 0;

                if (!northGap)
                {
                    PlaceWall(context, new Vector3(x * CellSize, 0f, (halfD + 0.5f) * CellSize), 0f);
                }

                if (!southGap)
                {
                    PlaceWall(context, new Vector3(x * CellSize, 0f, (-halfD - 0.5f) * CellSize), 180f);
                }
            }

            for (var z = -halfD; z <= halfD; z++)
            {
                var westGap = z == 0;
                var eastGap = z == -1 || z == 0;

                if (!westGap)
                {
                    PlaceWall(context, new Vector3((-halfW - 0.5f) * CellSize, 0f, z * CellSize), 270f);
                }

                if (!eastGap)
                {
                    PlaceWall(context, new Vector3((halfW + 0.5f) * CellSize, 0f, z * CellSize), 90f);
                }
            }

            Place(context, "door", new Vector3(0f, 0f, (-halfD - 0.5f) * CellSize), Quaternion.Euler(0f, 180f, 0f), context.Buildings);
            Place(context, "machinery", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, PickCardinal(context.Rng), 0f), context.Machinery, 1.1f);
            Place(context, "machinery", new Vector3(-CellSize * 1.7f, 0f, CellSize * 1.2f), Quaternion.Euler(0f, PickCardinal(context.Rng), 0f), context.Machinery, 0.85f);
            Place(context, "machinery", new Vector3(CellSize * 1.8f, 0f, CellSize * 1.4f), Quaternion.Euler(0f, PickCardinal(context.Rng), 0f), context.Machinery, 0.8f);

            for (var x = -halfW + 1; x <= halfW - 1; x++)
            {
                var pos = new Vector3(x * CellSize, 4.2f, -CellSize * 0.35f);
                Place(context, "bridge", pos, Quaternion.identity, context.Buildings);
                Place(context, "railing", pos + new Vector3(0f, 0f, -CellSize * 0.45f), Quaternion.identity, context.Buildings);
                Place(context, "railing", pos + new Vector3(0f, 0f, CellSize * 0.45f), Quaternion.Euler(0f, 180f, 0f), context.Buildings);
            }

            Place(context, "stairs", new Vector3((-halfW + 1) * CellSize, 0f, -CellSize * 1.2f), Quaternion.Euler(0f, 90f, 0f), context.Buildings);
            Place(context, "stairs", new Vector3((halfW - 1) * CellSize, 0f, CellSize * 1.2f), Quaternion.Euler(0f, 270f, 0f), context.Buildings);
            context.Summary.Buildings += 1;
        }

        private static void BuildRailSpur(BuildContext context)
        {
            var railZ = -context.CoreBounds.extents.z - CellSize * 0.9f;
            var start = -8;
            var end = 8;

            for (var x = start; x <= end; x++)
            {
                Place(context, "track", new Vector3(x * CellSize, 0.04f, railZ), Quaternion.identity, context.RailAndPipes);
            }

            Place(context, "train", new Vector3(-CellSize * 2f, 0.08f, railZ), Quaternion.identity, context.RailAndPipes, 1f);
            Place(context, "train", new Vector3(CellSize * 2f, 0.08f, railZ), Quaternion.identity, context.RailAndPipes, 1f);
            context.Summary.RailSegments += end - start + 1;
        }

        private static void BuildSatellites(BuildContext context)
        {
            var count = context.Rng.Next(2, 4);
            var anchors = new[]
            {
                new Vector3(-CellSize * 7f, 0f, CellSize * 3f),
                new Vector3(CellSize * 7f, 0f, CellSize * 3f),
                new Vector3(CellSize * 5f, 0f, -CellSize * 5f),
                new Vector3(-CellSize * 5f, 0f, -CellSize * 5f)
            }
            .OrderBy(_ => context.Rng.Next())
            .Take(count)
            .ToArray();

            foreach (var anchor in anchors)
            {
                BuildSatellite(context, anchor);
            }
        }

        private static void BuildSatellite(BuildContext context, Vector3 center)
        {
            var width = context.Rng.Next(3, 5);
            var depth = context.Rng.Next(3, 5);
            var halfW = width / 2;
            var halfD = depth / 2;

            for (var x = -halfW; x <= halfW; x++)
            {
                for (var z = -halfD; z <= halfD; z++)
                {
                    var pos = center + new Vector3(x * CellSize, 0f, z * CellSize);
                    Place(context, "floor", pos, Quaternion.identity, context.Ground);

                    if (context.Rng.NextDouble() > 0.3f)
                    {
                        Place(context, "roof", pos + Vector3.up * 5.2f, Quaternion.identity, context.Buildings);
                    }
                }
            }

            for (var x = -halfW; x <= halfW; x++)
            {
                PlaceWall(context, center + new Vector3(x * CellSize, 0f, (halfD + 0.5f) * CellSize), 0f);
                PlaceWall(context, center + new Vector3(x * CellSize, 0f, (-halfD - 0.5f) * CellSize), 180f);
            }

            for (var z = -halfD; z <= halfD; z++)
            {
                PlaceWall(context, center + new Vector3((-halfW - 0.5f) * CellSize, 0f, z * CellSize), 270f);
                PlaceWall(context, center + new Vector3((halfW + 0.5f) * CellSize, 0f, z * CellSize), 90f);
            }

            var interiorCount = context.Rng.Next(3, 6);

            for (var idx = 0; idx < interiorCount; idx++)
            {
                var offset = new Vector3(context.Rng.Next(-halfW, halfW + 1) * CellSize * 0.7f, 0f, context.Rng.Next(-halfD, halfD + 1) * CellSize * 0.7f);
                var category = context.Rng.NextDouble() > 0.45f ? "electrical" : "storage";
                Place(context, category, center + offset, Quaternion.Euler(0f, PickCardinal(context.Rng), 0f), context.Machinery);
            }

            context.Summary.Buildings += 1;
            context.SatelliteCenters.Add(center);
        }

        private static void BuildPipeRuns(BuildContext context)
        {
            foreach (var target in context.SatelliteCenters)
            {
                var xStep = target.x >= 0f ? 1 : -1;
                var zStep = target.z >= 0f ? 1 : -1;

                for (var x = 0; Mathf.Abs(x * CellSize) < Mathf.Abs(target.x); x += xStep)
                {
                    var pos = new Vector3(x * CellSize, 3.4f, 0f);
                    Place(context, "pipe", pos, Quaternion.Euler(0f, 90f, 0f), context.RailAndPipes, 1f);
                }

                for (var z = 0; Mathf.Abs(z * CellSize) < Mathf.Abs(target.z); z += zStep)
                {
                    var pos = new Vector3(target.x, 3.4f, z * CellSize);
                    Place(context, "pipe", pos, Quaternion.identity, context.RailAndPipes, 1f);
                }
            }
        }

        private static void BuildPerimeter(BuildContext context)
        {
            var halfX = CellSize * 9f;
            var halfZ = CellSize * 7f;

            for (var idx = -9; idx <= 9; idx++)
            {
                if (idx > -2 && idx < 2)
                {
                    continue;
                }

                Place(context, "railing", new Vector3(idx * CellSize, 0f, -halfZ), Quaternion.identity, context.Buildings);
                Place(context, "railing", new Vector3(idx * CellSize, 0f, halfZ), Quaternion.Euler(0f, 180f, 0f), context.Buildings);
            }

            for (var idx = -7; idx <= 7; idx++)
            {
                Place(context, "railing", new Vector3(-halfX, 0f, idx * CellSize), Quaternion.Euler(0f, 90f, 0f), context.Buildings);
                Place(context, "railing", new Vector3(halfX, 0f, idx * CellSize), Quaternion.Euler(0f, 270f, 0f), context.Buildings);
            }
        }

        private static void BuildDressing(BuildContext context)
        {
            Scatter(context, "vegetation", context.Dressing, 58, new Vector2(-52f, 52f), new Vector2(-40f, 40f), 0.45f, true);
            Scatter(context, "scrap", context.Dressing, 42, new Vector2(-45f, 45f), new Vector2(-35f, 35f), 0.55f, true);
            ScatterPuddles(context, 20, new Vector2(-44f, 44f), new Vector2(-34f, 34f));
            Scatter(context, "pebble", context.Dressing, 70, new Vector2(-54f, 54f), new Vector2(-42f, 42f), 0.4f, true);
            ScatterWallDetails(context);
        }

        private static void ScatterPuddles(BuildContext context, int count, Vector2 xRange, Vector2 zRange)
        {
            var mat = LoadFirstMaterial(context.Assets.PuddleMaterials);

            if (mat == null)
            {
                return;
            }

            for (var idx = 0; idx < count; idx++)
            {
                var pos = new Vector3(NextFloat(context.Rng, xRange.x, xRange.y), 0.035f, NextFloat(context.Rng, zRange.x, zRange.y));

                if (context.CoreBounds.Contains(pos))
                {
                    idx--;
                    continue;
                }

                var obj = new GameObject("Puddle");
                obj.transform.SetParent(context.Dressing, false);
                obj.transform.position = pos;
                obj.transform.rotation = Quaternion.Euler(0f, NextFloat(context.Rng, 0f, 360f), 0f);
                obj.transform.localScale = new Vector3(NextFloat(context.Rng, 1.8f, 4.4f), 1f, NextFloat(context.Rng, 1.2f, 3.3f));
                var meshFilter = obj.AddComponent<MeshFilter>();
                var renderer = obj.AddComponent<MeshRenderer>();
                meshFilter.sharedMesh = CreateQuadMesh();
                renderer.sharedMaterial = mat;
                Undo.RegisterCreatedObjectUndo(obj, "Place Central Puddle");
                context.Summary.VisiblePrefabs += 1;
            }
        }

        private static void Scatter(BuildContext context, string category, Transform parent, int count, Vector2 xRange, Vector2 zRange, float scale, bool avoidCore)
        {
            for (var idx = 0; idx < count; idx++)
            {
                var pos = new Vector3(NextFloat(context.Rng, xRange.x, xRange.y), 0f, NextFloat(context.Rng, zRange.x, zRange.y));

                if (avoidCore && context.CoreBounds.Contains(pos))
                {
                    idx--;
                    continue;
                }

                var rot = Quaternion.Euler(0f, NextCardinalOrFree(context.Rng), 0f);
                var obj = Place(context, category, pos, rot, parent, NextFloat(context.Rng, scale * 0.75f, scale * 1.25f));

                if (obj == null)
                {
                    return;
                }
            }
        }

        private static void ScatterWallDetails(BuildContext context)
        {
            for (var idx = 0; idx < 14; idx++)
            {
                var side = context.Rng.Next(0, 4);
                var pos = Vector3.zero;
                var rot = Quaternion.identity;

                if (side == 0)
                {
                    pos = new Vector3(NextFloat(context.Rng, -24f, 24f), 1.8f, context.CoreBounds.extents.z + CellSize * 0.55f);
                    rot = Quaternion.identity;
                }
                else if (side == 1)
                {
                    pos = new Vector3(NextFloat(context.Rng, -24f, 24f), 1.8f, -context.CoreBounds.extents.z - CellSize * 0.55f);
                    rot = Quaternion.Euler(0f, 180f, 0f);
                }
                else if (side == 2)
                {
                    pos = new Vector3(context.CoreBounds.extents.x + CellSize * 0.55f, 1.8f, NextFloat(context.Rng, -18f, 18f));
                    rot = Quaternion.Euler(0f, 90f, 0f);
                }
                else
                {
                    pos = new Vector3(-context.CoreBounds.extents.x - CellSize * 0.55f, 1.8f, NextFloat(context.Rng, -18f, 18f));
                    rot = Quaternion.Euler(0f, 270f, 0f);
                }

                Place(context, "graffiti", pos, rot, context.Dressing, NextFloat(context.Rng, 0.85f, 1.25f));
            }
        }

        private static void BuildLighting(BuildContext context)
        {
            var sunObj = new GameObject("Industrial Sun");
            sunObj.transform.SetParent(context.Lighting, false);
            sunObj.transform.rotation = Quaternion.Euler(44f, -35f, 0f);
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.86f, 0.82f, 0.72f, 1f);
            sun.intensity = 1.25f;
            Undo.RegisterCreatedObjectUndo(sunObj, "Create Central Sun");

            var lampPositions = new[]
            {
                new Vector3(-18f, 3.3f, -12f),
                new Vector3(18f, 3.3f, -12f),
                new Vector3(-18f, 3.3f, 12f),
                new Vector3(18f, 3.3f, 12f),
                new Vector3(0f, 4.6f, 0f)
            };

            foreach (var pos in lampPositions)
            {
                var lamp = Place(context, "lamp", pos, Quaternion.Euler(0f, PickCardinal(context.Rng), 0f), context.Lighting, 1f);
                var lightObj = new GameObject("Warm Pool Light");
                lightObj.transform.SetParent(lamp != null ? lamp.transform : context.Lighting, false);
                lightObj.transform.position = pos + Vector3.up * 0.4f;
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.66f, 0.38f, 1f);
                light.intensity = 3.2f;
                light.range = 11f;
                Undo.RegisterCreatedObjectUndo(lightObj, "Create Central Lamp Light");
            }
        }

        private static void PlaceNavigationTools(BuildContext context)
        {
            var cameraObj = new GameObject("Central Preview Camera");
            cameraObj.transform.SetParent(context.Markers, false);
            cameraObj.transform.position = new Vector3(0f, 30f, -54f);
            cameraObj.transform.rotation = Quaternion.Euler(57f, 0f, 0f);
            var camera = cameraObj.AddComponent<Camera>();
            camera.farClipPlane = 420f;
            camera.fieldOfView = 56f;
            AddScriptByName(cameraObj, "FreeCamera");
            AddScriptByName(cameraObj, "Flashlight");
            Undo.RegisterCreatedObjectUndo(cameraObj, "Create Central Preview Camera");
        }

        private static void FinalizeScene(BuildContext context)
        {
            var renderers = context.Root.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                var obj = renderer.gameObject;
                GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null || mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        context.Summary.PinkOrMissingMaterials += 1;
                    }
                }
            }

            context.Summary.PrefabInstances = context.Root.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject)
                .Count(PrefabUtility.IsPartOfPrefabInstance);
            context.Summary.TotalObjects = context.Root.GetComponentsInChildren<Transform>(true).Length;
            context.Summary.ManifestCounts = BuildManifestCountText(context.Manifest);
            EditorSceneManager.MarkSceneDirty(context.Scene);
        }

        private static void PlaceWall(BuildContext context, Vector3 pos, float yRot)
        {
            var category = context.Rng.NextDouble() > 0.18f ? "wall" : "wallBroken";
            Place(context, category, pos, Quaternion.Euler(0f, yRot, 0f), context.Buildings);
        }

        private static GameObject Place(BuildContext context, string category, Vector3 pos, Quaternion rot, Transform parent, float scale = 1f)
        {
            var prefab = context.Assets.Pick(category, context.Rng);

            if (prefab == null)
            {
                context.Summary.MissingCategories.Add(category);
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, context.Scene) as GameObject;

            if (instance == null)
            {
                return null;
            }

            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(pos, rot);

            if (!Mathf.Approximately(scale, 1f))
            {
                instance.transform.localScale *= scale;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Place Central Prefab");
            context.Summary.VisiblePrefabs += 1;
            return instance;
        }

        private static GameObject CreateRoot(string name)
        {
            var root = GameObject.Find(name);

            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }

            root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create Central Root");
            return root;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(obj, "Create Central Group");
            return obj.transform;
        }

        private static Transform CreateChild(GameObject parent, string name)
        {
            return CreateChild(parent.transform, name);
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            return scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == rootName);
        }

        private static void AddScriptByName(GameObject target, string scriptName)
        {
            var guids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript", new[] { OldIndustryRoot });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script != null ? script.GetClass() : null;

                if (type != null && typeof(MonoBehaviour).IsAssignableFrom(type) && target.GetComponent(type) == null)
                {
                    target.AddComponent(type);
                    return;
                }
            }
        }

        private static Material LoadFirstMaterial(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat != null)
                {
                    return mat;
                }
            }

            return null;
        }

        private static Mesh CreateQuadMesh()
        {
            if (puddleMesh != null)
            {
                return puddleMesh;
            }

            puddleMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PuddleMeshPath);

            if (puddleMesh != null)
            {
                return puddleMesh;
            }

            puddleMesh = new Mesh
            {
                name = "Central_Puddle_Quad",
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0.5f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[]
                {
                    0,
                    2,
                    1,
                    2,
                    3,
                    1
                }
            };
            puddleMesh.RecalculateBounds();
            AssetDatabase.CreateAsset(puddleMesh, PuddleMeshPath);
            AssetDatabase.ImportAsset(PuddleMeshPath, ImportAssetOptions.ForceUpdate);
            return puddleMesh;
        }

        private static CentralPrefabManifest LoadOrBuildManifest()
        {
            EnsureFolders();

            if (File.Exists(AbsolutePath(ManifestPath)))
            {
                var json = File.ReadAllText(AbsolutePath(ManifestPath));
                var loaded = JsonUtility.FromJson<CentralPrefabManifest>(json);

                if (loaded != null && loaded.Buckets != null && loaded.Buckets.Length > 0)
                {
                    return loaded;
                }
            }

            var manifest = BuildManifest();
            SaveManifest(manifest);
            return manifest;
        }

        private static CentralPrefabManifest BuildManifest()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            var pipeline = DetectPipeline();
            var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { OldIndustryRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var buckets = CreateEmptyBuckets();

            foreach (var path in prefabPaths)
            {
                var assigned = false;
                assigned |= AddIf(path, buckets, "floor", "floor", "ground", "block");
                assigned |= AddIf(path, buckets, "door", "door", "gate");
                assigned |= AddIf(path, buckets, "roof", "roof", "ceiling");
                assigned |= AddIf(path, buckets, "wallBroken", "broken", "collapsed");
                assigned |= AddIf(path, buckets, "wall", "wall", "window", "pillar", "concretewall", "brickwall", "metalwall");
                assigned |= AddIf(path, buckets, "stairs", "stair");
                assigned |= AddIf(path, buckets, "bridge", "bridge", "catwalk");
                assigned |= AddIf(path, buckets, "railing", "rail", "fence");
                assigned |= AddIf(path, buckets, "track", "track");
                assigned |= AddIf(path, buckets, "train", "train", "torpedo", "laddlecar");
                assigned |= AddIf(path, buckets, "pipe", "pipe", "vent", "cable", "funnel");
                assigned |= AddIf(path, buckets, "machinery", "furnace", "bof", "casting", "crane", "laddle", "eaf", "runner", "machine", "turret");
                assigned |= AddIf(path, buckets, "electrical", "electrical", "cabin", "switch", "control");
                assigned |= AddIf(path, buckets, "storage", "box", "barrel", "tank", "tarp");
                assigned |= AddIf(path, buckets, "vegetation", "grass", "ivy", "plant");
                assigned |= AddIf(path, buckets, "lamp", "lamp", "light");
                assigned |= AddIf(path, buckets, "graffiti", "graffiti", "tag");
                assigned |= AddIf(path, buckets, "puddle", "puddle", "water");
                assigned |= AddIf(path, buckets, "pebble", "pebble", "stone", "rock");
                assigned |= AddIf(path, buckets, "scrap", "scrap", "pile", "debris", "broken");

                if (!assigned)
                {
                    buckets["misc"].Add(path);
                }
            }

            var materialPaths = AssetDatabase.FindAssets("t:Material", new[] { OldIndustryRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new CentralPrefabManifest
            {
                PackageRoot = OldIndustryRoot,
                Pipeline = pipeline,
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Buckets = buckets.Select(pair => new CentralPrefabBucket
                {
                    Category = pair.Key,
                    Items = pair.Value.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Select(ToManifestItem).ToArray()
                }).ToArray(),
                SkyboxMaterials = materialPaths.Where(path => HasAny(path, "skybox", "industrial")).ToArray(),
                GroundMaterials = materialPaths.Where(path => HasAny(path, "ground", "terrain", "concrete", "asphalt")).ToArray(),
                PuddleMaterials = materialPaths.Where(path => HasAny(path, "puddle", "wetzone", "wet")).ToArray(),
                Scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { OldIndustryRoot }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
            };
        }

        private static Dictionary<string, List<string>> CreateEmptyBuckets()
        {
            var names = new[]
            {
                "floor",
                "wall",
                "wallBroken",
                "door",
                "roof",
                "stairs",
                "bridge",
                "railing",
                "track",
                "train",
                "pipe",
                "machinery",
                "electrical",
                "storage",
                "vegetation",
                "lamp",
                "graffiti",
                "puddle",
                "pebble",
                "scrap",
                "misc"
            };

            return names.ToDictionary(name => name, _ => new List<string>());
        }

        private static bool AddIf(string path, Dictionary<string, List<string>> buckets, string category, params string[] terms)
        {
            if (!HasAny(path, terms))
            {
                return false;
            }

            buckets[category].Add(path);
            return true;
        }

        private static CentralPrefabItem ToManifestItem(string path)
        {
            return new CentralPrefabItem
            {
                Guid = AssetDatabase.AssetPathToGUID(path),
                Path = path
            };
        }

        private static void SaveManifest(CentralPrefabManifest manifest)
        {
            EnsureFolders();
            File.WriteAllText(AbsolutePath(ManifestPath), JsonUtility.ToJson(manifest, true));
            AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceUpdate);
        }

        private static void WriteReadme(BuildSummary summary)
        {
            EnsureFolders();
            var builder = new StringBuilder();
            builder.AppendLine("# Central");
            builder.AppendLine();
            builder.AppendLine($"Seed: `{summary.Seed}`");
            builder.AppendLine($"Scene: `{ScenePath}`");
            builder.AppendLine($"Generated root: `{GeneratedRootName}`");
            builder.AppendLine($"Pipeline detected: `{summary.Pipeline}`");
            builder.AppendLine();
            builder.AppendLine("## Regenerate");
            builder.AppendLine();
            builder.AppendLine("Open Unity, then run `Tools/Don't Die Please/Map/Central/Open Generator`, keep the same seed, and press `Generate Central`.");
            builder.AppendLine();
            builder.AppendLine("## Layout");
            builder.AppendLine();
            builder.AppendLine($"Central foundry footprint: `{summary.CoreDimensions}`");
            builder.AppendLine($"Buildings: `{summary.Buildings}`");
            builder.AppendLine($"Rail segments: `{summary.RailSegments}`");
            builder.AppendLine($"Visible prefab placements: `{summary.VisiblePrefabs}`");
            builder.AppendLine($"Prefab instances: `{summary.PrefabInstances}`");
            builder.AppendLine($"Generated objects: `{summary.TotalObjects}`");
            builder.AppendLine($"Pink or missing material slots found: `{summary.PinkOrMissingMaterials}`");
            builder.AppendLine();
            builder.AppendLine("## Manifest Counts");
            builder.AppendLine();
            builder.AppendLine(summary.ManifestCounts);
            builder.AppendLine();
            builder.AppendLine("## Notes");
            builder.AppendLine();
            builder.AppendLine("Central is built as a seed-driven abandoned industrial foundry district using only OldIndustry visible assets. The main gameplay scene is not modified.");
            File.WriteAllText(AbsolutePath(ReadmePath), builder.ToString());
            AssetDatabase.ImportAsset(ReadmePath, ImportAssetOptions.ForceUpdate);
        }

        private static string BuildManifestMessage(CentralPrefabManifest manifest)
        {
            return $"Pipeline: {manifest.Pipeline}\n{BuildManifestCountText(manifest)}";
        }

        private static string BuildFinalMessage(BuildSummary summary)
        {
            return $"Central built with seed {summary.Seed}\nObjects: {summary.TotalObjects}\nPrefab instances: {summary.PrefabInstances}\nPink/missing material slots: {summary.PinkOrMissingMaterials}";
        }

        private static string BuildManifestCountText(CentralPrefabManifest manifest)
        {
            if (manifest == null || manifest.Buckets == null)
            {
                return string.Empty;
            }

            return string.Join("\n", manifest.Buckets.Select(bucket => $"{bucket.Category}: {bucket.Items.Length}"));
        }

        private void DrawManifestStatus()
        {
            if (manifest == null)
            {
                manifest = LoadOrBuildManifest();
            }

            EditorGUILayout.LabelField("Manifest", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Package Root", manifest.PackageRoot);
            EditorGUILayout.LabelField("Pipeline", manifest.Pipeline);
            EditorGUILayout.TextArea(BuildManifestCountText(manifest), GUILayout.MinHeight(150f));
        }

        private void DrawSummary()
        {
            if (lastSummary == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Last Build", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Seed", lastSummary.Seed.ToString());
            EditorGUILayout.LabelField("Core", lastSummary.CoreDimensions);
            EditorGUILayout.LabelField("Objects", lastSummary.TotalObjects.ToString());
            EditorGUILayout.LabelField("Prefab Instances", lastSummary.PrefabInstances.ToString());
            EditorGUILayout.LabelField("Pink/Missing Material Slots", lastSummary.PinkOrMissingMaterials.ToString());
        }

        private static void ShowMessage(string title, string message)
        {
            if (Application.isBatchMode)
            {
                Debug.Log($"{title}: {message}");
                return;
            }

            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static bool ConfirmAction(string title, string message, string ok, string cancel)
        {
            return Application.isBatchMode || EditorUtility.DisplayDialog(title, message, ok, cancel);
        }

        private static string DetectPipeline()
        {
            var asset = GraphicsSettings.currentRenderPipeline;

            if (asset == null)
            {
                return "Built-in";
            }

            var typeName = asset.GetType().Name;

            if (typeName.Contains("HDRenderPipeline"))
            {
                return "HDRP";
            }

            if (typeName.Contains("UniversalRenderPipeline"))
            {
                return "URP";
            }

            return typeName;
        }

        private static bool HasAny(string text, params string[] terms)
        {
            var value = text.Replace("\\", "/").ToLowerInvariant();
            return terms.Any(term => value.Contains(term.ToLowerInvariant()));
        }

        private static int PickCardinal(System.Random rng)
        {
            return rng.Next(0, 4) * 90;
        }

        private static float NextCardinalOrFree(System.Random rng)
        {
            return rng.NextDouble() > 0.7 ? PickCardinal(rng) : NextFloat(rng, 0f, 360f);
        }

        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Central");
            EnsureFolder(CentralRoot, "Editor");
            EnsureFolder("Assets", "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string AbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        [Serializable]
        private sealed class CentralPrefabManifest
        {
            public string PackageRoot;
            public string Pipeline;
            public string GeneratedAt;
            public CentralPrefabBucket[] Buckets;
            public string[] SkyboxMaterials;
            public string[] GroundMaterials;
            public string[] PuddleMaterials;
            public string[] Scripts;
        }

        [Serializable]
        private sealed class CentralPrefabBucket
        {
            public string Category;
            public CentralPrefabItem[] Items;
        }

        [Serializable]
        private sealed class CentralPrefabItem
        {
            public string Guid;
            public string Path;
        }

        private sealed class ManifestAssets
        {
            private readonly Dictionary<string, List<GameObject>> prefabs;
            public readonly string[] SkyboxMaterials;
            public readonly string[] GroundMaterials;
            public readonly string[] PuddleMaterials;

            public ManifestAssets(CentralPrefabManifest manifest)
            {
                prefabs = manifest.Buckets.ToDictionary(bucket => bucket.Category, bucket => bucket.Items.Select(item => AssetDatabase.LoadAssetAtPath<GameObject>(item.Path)).Where(obj => obj != null).OrderBy(obj => obj.name).ToList());
                SkyboxMaterials = manifest.SkyboxMaterials ?? Array.Empty<string>();
                GroundMaterials = manifest.GroundMaterials ?? Array.Empty<string>();
                PuddleMaterials = manifest.PuddleMaterials ?? Array.Empty<string>();
            }

            public GameObject Pick(string category, System.Random rng)
            {
                if (!prefabs.TryGetValue(category, out var list) || list.Count == 0)
                {
                    return null;
                }

                return list[rng.Next(0, list.Count)];
            }
        }

        private sealed class BuildContext
        {
            public readonly Scene Scene;
            public readonly System.Random Rng;
            public readonly ManifestAssets Assets;
            public readonly int Seed;
            public readonly CentralPrefabManifest Manifest;
            public readonly List<Vector3> SatelliteCenters = new List<Vector3>();
            public BuildSummary Summary;
            public GameObject Root;
            public Transform Ground;
            public Transform Buildings;
            public Transform RailAndPipes;
            public Transform Machinery;
            public Transform Dressing;
            public Transform Lighting;
            public Transform Markers;
            public Bounds CoreBounds;

            public BuildContext(Scene scene, System.Random rng, ManifestAssets assets, CentralPrefabManifest manifest, BuildSummary summary, int seed)
            {
                Scene = scene;
                Rng = rng;
                Assets = assets;
                Manifest = manifest;
                Summary = summary;
                Seed = seed;
            }
        }

        private sealed class BuildSummary
        {
            public readonly int Seed;
            public readonly string Pipeline;
            public readonly HashSet<string> MissingCategories = new HashSet<string>();
            public int VisiblePrefabs;
            public int Buildings;
            public int RailSegments;
            public int TotalObjects;
            public int PrefabInstances;
            public int PinkOrMissingMaterials;
            public string CoreDimensions;
            public string ManifestCounts;

            public BuildSummary(int seed, string pipeline)
            {
                Seed = seed;
                Pipeline = pipeline;
            }
        }
    }
}
