using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DontDiePlease.Central;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Central.EditorTools
{
    public static class OldIndustryFirstPersonSetup
    {
        private const string DemoScenePath = "Assets/Scenes/demoMainScene.unity";
        private const string MainGameplaySceneName = "MainGameplayScene";
        private const string PrefabFolder = "Assets/_Central/Prefabs";
        private const string PrefabPath = PrefabFolder + "/FPSPlayer.prefab";
        private const string PlayerName = "FPSPlayer";
        private const string PlayerCameraName = "FPS Camera";
        private const string FreeCameraName = "OldIndustry FreeCamera";

        [MenuItem("Tools/Don't Die Please/First Person/Setup Walker In Demo Scene")]
        public static void SetupDemoScene()
        {
            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            SetupScene(scene);
        }

        [MenuItem("Tools/Don't Die Please/First Person/Setup Walker In Open Scene")]
        public static void SetupOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            SetupScene(scene);
        }

        public static void SetupScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("No scene loaded", "Open demoMainScene or Central before setting up the first-person walker.", "OK");
                return;
            }

            if (scene.name == MainGameplaySceneName)
            {
                EditorUtility.DisplayDialog("Main scene blocked", "This setup will not modify MainGameplayScene.", "OK");
                return;
            }

            if (scene.name != "demoMainScene" && scene.name != "Central")
            {
                EditorUtility.DisplayDialog("Wrong target scene", $"Open demoMainScene or Central first. Current scene is {scene.name}.", "OK");
                return;
            }

            Debug.Log($"fps setup: scene={scene.name}, input={DetectInputBackend()}, pipeline={DetectRenderPipeline()}, cams={FindSceneComponents<Camera>(scene).Count}, listeners={FindSceneComponents<AudioListener>(scene).Count}");

            EnsureFolders();
            var prefab = CreateOrUpdatePrefab();
            var colliderReport = EnsureWalkableColliders(scene);
            Debug.Log($"fps setup colliders: terrain={colliderReport.TerrainColliders}, added={colliderReport.CollidersAdded}, skipped decor={colliderReport.SkippedDecor}");

            var player = PlaceOrUpdatePlayer(scene, prefab);
            Debug.Log($"fps setup player: prefab={PrefabPath}, player={player.name}");

            var freeCamera = EnsureFreeCamera(scene, player);
            ConfigureCamerasAndSwitcher(scene, player, freeCamera);
            Debug.Log($"fps setup camera: toggle=F, freeCam={(freeCamera != null ? freeCamera.name : "none")}");

            var spawn = SeatPlayerOnGround(player.transform.position);
            player.transform.position = spawn;
            player.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            Debug.Log($"fps setup spawn: {spawn}, mainCam={PlayerCameraName}");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"fps setup saved: {scene.path}. controls are WASD, mouse, shift, space, ctrl, esc, F");
        }

        private static GameObject CreateOrUpdatePrefab()
        {
            var temp = BuildPlayerObject();
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            UnityEngine.Object.DestroyImmediate(temp);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        private static GameObject BuildPlayerObject()
        {
            var player = new GameObject(PlayerName);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.45f;
            controller.slopeLimit = 45f;
            controller.skinWidth = 0.08f;

            var fps = player.AddComponent<FirstPersonController>();
            var switcher = player.AddComponent<FirstPersonSceneModeSwitcher>();
            var cameraObj = new GameObject(PlayerCameraName);
            cameraObj.transform.SetParent(player.transform);
            cameraObj.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraObj.transform.localRotation = Quaternion.identity;
            cameraObj.tag = "MainCamera";

            var camera = cameraObj.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 1200f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            EnsureAdditionalCameraData(camera);

            var listener = cameraObj.AddComponent<AudioListener>();
            fps.ConfigureCameraPivot(cameraObj.transform);
            switcher.Configure(fps, camera, listener, null, null, null);
            return player;
        }

        private static GameObject PlaceOrUpdatePlayer(Scene scene, GameObject prefab)
        {
            var existing = FindSceneObjects(scene)
                .FirstOrDefault(obj => obj.name == PlayerName);

            GameObject player;

            if (existing != null)
            {
                player = existing;
                EnsurePlayerComponents(player);
            }
            else
            {
                player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                player.name = PlayerName;
            }

            var spawn = FindSpawnPosition(scene);
            player.transform.position = spawn;
            player.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            EditorUtility.SetDirty(player);
            return player;
        }

        private static void EnsurePlayerComponents(GameObject player)
        {
            var controller = player.GetComponent<CharacterController>() ?? player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.45f;
            controller.slopeLimit = 45f;

            var fps = player.GetComponent<FirstPersonController>() ?? player.AddComponent<FirstPersonController>();
            var switcher = player.GetComponent<FirstPersonSceneModeSwitcher>() ?? player.AddComponent<FirstPersonSceneModeSwitcher>();
            var camera = player.GetComponentsInChildren<Camera>(true).FirstOrDefault();

            if (camera == null)
            {
                var cameraObj = new GameObject(PlayerCameraName);
                cameraObj.transform.SetParent(player.transform);
                cameraObj.transform.localPosition = new Vector3(0f, 1.62f, 0f);
                cameraObj.transform.localRotation = Quaternion.identity;
                camera = cameraObj.AddComponent<Camera>();
            }

            camera.name = PlayerCameraName;
            camera.fieldOfView = 60f;
            EnsureAdditionalCameraData(camera);

            var listener = camera.GetComponent<AudioListener>() ?? camera.gameObject.AddComponent<AudioListener>();
            fps.ConfigureCameraPivot(camera.transform);
            switcher.Configure(fps, camera, listener, null, null, null);
        }

        private static Camera EnsureFreeCamera(Scene scene, GameObject player)
        {
            var cameras = FindSceneComponents<Camera>(scene)
                .Where(camera => camera != null && !camera.transform.IsChildOf(player.transform))
                .ToList();

            var freeCamera = cameras.FirstOrDefault(camera => camera.GetComponent<global::FreeCamera>() != null) ?? cameras.FirstOrDefault();

            if (freeCamera == null)
            {
                var obj = new GameObject(FreeCameraName);
                SceneManager.MoveGameObjectToScene(obj, scene);
                freeCamera = obj.AddComponent<Camera>();
                freeCamera.transform.position = player.transform.position + new Vector3(0f, 8f, -12f);
                freeCamera.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
            }

            freeCamera.name = FreeCameraName;
            freeCamera.tag = "Untagged";
            freeCamera.enabled = false;
            freeCamera.fieldOfView = Mathf.Clamp(freeCamera.fieldOfView, 45f, 75f);
            EnsureAdditionalCameraData(freeCamera);

            var freeCameraController = freeCamera.GetComponent<global::FreeCamera>() ?? freeCamera.gameObject.AddComponent<global::FreeCamera>();
            freeCameraController.enabled = false;

            var listener = freeCamera.GetComponent<AudioListener>() ?? freeCamera.gameObject.AddComponent<AudioListener>();
            listener.enabled = false;

            EditorUtility.SetDirty(freeCamera);
            EditorUtility.SetDirty(freeCamera.gameObject);
            return freeCamera;
        }

        private static void ConfigureCamerasAndSwitcher(Scene scene, GameObject player, Camera freeCamera)
        {
            var fps = player.GetComponent<FirstPersonController>();
            var switcher = player.GetComponent<FirstPersonSceneModeSwitcher>();
            var playerCamera = player.GetComponentsInChildren<Camera>(true).First(camera => camera.name == PlayerCameraName);
            var playerAudio = playerCamera.GetComponent<AudioListener>() ?? playerCamera.gameObject.AddComponent<AudioListener>();
            var freeAudio = freeCamera != null ? freeCamera.GetComponent<AudioListener>() : null;
            var freeController = freeCamera != null ? freeCamera.GetComponent<global::FreeCamera>() : null;

            foreach (var camera in FindSceneComponents<Camera>(scene))
            {
                camera.enabled = camera == playerCamera;
                camera.tag = camera == playerCamera ? "MainCamera" : "Untagged";
                EditorUtility.SetDirty(camera);
            }

            foreach (var listener in FindSceneComponents<AudioListener>(scene))
            {
                listener.enabled = listener == playerAudio;
                EditorUtility.SetDirty(listener);
            }

            switcher.Configure(fps, playerCamera, playerAudio, freeCamera, freeAudio, freeController);
            fps.ConfigureCameraPivot(playerCamera.transform);
            EditorUtility.SetDirty(switcher);
            EditorUtility.SetDirty(fps);
            EditorUtility.SetDirty(playerCamera.gameObject);
        }

        private static ColliderReport EnsureWalkableColliders(Scene scene)
        {
            var report = new ColliderReport();

            foreach (var terrain in FindSceneComponents<Terrain>(scene))
            {
                var terrainCollider = terrain.GetComponent<TerrainCollider>() ?? terrain.gameObject.AddComponent<TerrainCollider>();
                terrainCollider.terrainData = terrain.terrainData;
                terrainCollider.enabled = true;
                report.TerrainColliders++;
                EditorUtility.SetDirty(terrainCollider);
            }

            foreach (var renderer in FindSceneComponents<MeshRenderer>(scene))
            {
                if (!ShouldReceiveCollider(renderer))
                {
                    report.SkippedDecor++;
                    continue;
                }

                if (renderer.GetComponent<Collider>() != null)
                    continue;

                var meshFilter = renderer.GetComponent<MeshFilter>();

                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                var collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.sharedMesh;
                collider.convex = false;
                report.CollidersAdded++;
                EditorUtility.SetDirty(collider);
                EditorUtility.SetDirty(renderer.gameObject);
            }

            return report;
        }

        private static bool ShouldReceiveCollider(MeshRenderer renderer)
        {
            var name = renderer.gameObject.name.ToLowerInvariant();
            var parentNames = string.Join(" ", renderer.GetComponentsInParent<Transform>(true).Select(transform => transform.name.ToLowerInvariant()));
            var searchable = name + " " + parentNames;

            if (ContainsAny(searchable, "lamp", "cable", "wire", "ivy", "grass", "pebble", "decal", "poster", "glass", "window", "particle", "smoke", "steam"))
                return false;

            if (ContainsAny(searchable, "floor", "ground", "terrain", "road", "stair", "wall", "building", "bridge", "roof", "metalwall", "brickwall", "concrete", "runner", "walkway"))
                return renderer.bounds.size.magnitude > 0.8f;

            var size = renderer.bounds.size;
            return size.y > 1.4f && Mathf.Max(size.x, size.z) > 2.2f;
        }

        private static Vector3 FindSpawnPosition(Scene scene)
        {
            Physics.SyncTransforms();

            var candidates = new[]
            {
                new Vector3(-18f, 120f, -18f),
                new Vector3(6f, 120f, -24f),
                new Vector3(22f, 120f, -12f),
                new Vector3(-36f, 120f, 4f),
                new Vector3(42f, 120f, 8f),
                new Vector3(-52f, 120f, -24f),
                new Vector3(0f, 120f, 0f),
                new Vector3(18f, 120f, 32f)
            };

            foreach (var candidate in candidates)
            {
                if (!Physics.Raycast(candidate, Vector3.down, out var hit, 240f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;

                if (!IsSpawnSurface(hit.collider))
                    continue;

                var position = hit.point + Vector3.up * 0.08f;

                if (HasPlayerClearance(position))
                    return position;
            }

            var terrain = FindSceneComponents<Terrain>(scene).FirstOrDefault();

            if (terrain != null)
            {
                var position = terrain.transform.position + new Vector3(54f, terrain.SampleHeight(terrain.transform.position + new Vector3(54f, 0f, 38f)) + 0.08f, 38f);
                return position;
            }

            return new Vector3(-18f, 2f, -18f);
        }

        private static Vector3 SeatPlayerOnGround(Vector3 current)
        {
            Physics.SyncTransforms();
            var origin = current + Vector3.up * 80f;
            return Physics.Raycast(origin, Vector3.down, out var hit, 180f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * 0.08f
                : current;
        }

        private static bool IsSpawnSurface(Collider collider)
        {
            if (collider is TerrainCollider)
                return true;

            var name = collider.gameObject.name.ToLowerInvariant();
            return ContainsAny(name, "ground", "road", "floor", "terrain", "concrete", "bridge", "walkway");
        }

        private static bool HasPlayerClearance(Vector3 position)
        {
            var bottom = position + Vector3.up * 0.38f;
            var top = position + Vector3.up * 1.72f;
            return !Physics.CheckCapsule(bottom, top, 0.32f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private static void EnsureAdditionalCameraData(Camera camera)
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

            if (type == null || camera.GetComponent(type) != null)
                return;

            camera.gameObject.AddComponent(type);
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "_Central");
            CreateFolder("Assets/_Central", "Prefabs");
            CreateFolder("Assets/_Central", "Scripts");
            CreateFolder("Assets/_Central", "Docs");
        }

        private static void CreateFolder(string parent, string child)
        {
            var path = parent + "/" + child;

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(component => component != null)
                .ToList();
        }

        private static List<GameObject> FindSceneObjects(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .ToList();
        }

        private static bool ContainsAny(string value, params string[] parts)
        {
            return parts.Any(part => value.Contains(part));
        }

        private static string DetectInputBackend()
        {
            var path = "ProjectSettings/ProjectSettings.asset";

            if (!File.Exists(path))
                return "unknown";

            var text = File.ReadAllText(path);

            if (text.Contains("activeInputHandler: 2"))
                return "Both";

            if (text.Contains("activeInputHandler: 1"))
                return "Input System";

            if (text.Contains("activeInputHandler: 0"))
                return "Legacy Input Manager";

            return "unknown";
        }

        private static string DetectRenderPipeline()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;

            if (pipeline == null)
                return "Built-in";

            var name = pipeline.GetType().Name;
            return name.Contains("Universal", StringComparison.OrdinalIgnoreCase) ? "URP" : name;
        }

        private sealed class ColliderReport
        {
            public int TerrainColliders;
            public int CollidersAdded;
            public int SkippedDecor;
        }
    }
}
