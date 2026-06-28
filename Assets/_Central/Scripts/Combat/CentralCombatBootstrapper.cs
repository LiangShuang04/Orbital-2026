using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Akila.FPSFramework;
using DontDiePlease.Central;
using DontDiePlease.Systems;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

using FrameworkInventory = Akila.FPSFramework.Inventory;
using FrameworkInventoryItem = Akila.FPSFramework.InventoryItem;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralCombatBootstrapper : MonoBehaviour
    {
        private static readonly HashSet<string> SceneNames = new HashSet<string>
        {
            "Central_Combat",
            "Demo_Combat"
        };

        private const string PlayerPrefabPath = "Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab";
        private const string PistolPath = "Assets/Akila/FPS Framework/Prefabs/Weapons/Pistol_1.prefab";
        private const string AssaultRiflePath = "Assets/Akila/FPS Framework/Prefabs/Weapons/Assault Rifle_1.prefab";
        private const string FrameworkGameManagerPath = "Assets/Akila/FPS Framework/Prefabs/World/Game Manager.prefab";
        private const string FrameworkHudPath = "Assets/Akila/FPS Framework/Prefabs/HUD/HUD.prefab";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForCombatScene()
        {
            var scene = SceneManager.GetActiveScene();

            if (!SceneNames.Contains(scene.name))
                return;

            var alreadyThere = FindObjectsByType<CentralCombatBootstrapper>(FindObjectsInactive.Include)
                .Any(x => x != null && x.gameObject.scene == scene);

            if (alreadyThere)
                return;

            var obj = new GameObject($"{scene.name}CombatBootstrapper");
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.AddComponent<CentralCombatBootstrapper>();
        }

        private IEnumerator Start()
        {
            if (!SceneNames.Contains(SceneManager.GetActiveScene().name))
                yield break;

            FPSFrameworkCore.IsActive = true;
            FPSFrameworkCore.IsInputActive = true;
            FPSFrameworkCore.IsPaused = false;

            EnsureSeedManager();
            DisableCompetingControllers();
            EnsureFrameworkManagers();
            BuildRuntimeNavMesh();

            var player = EnsureFrameworkPlayer();

            yield return null;

            EnsureSingleCamera(player);

            if (player == null)
                yield break;

            if (SceneManager.GetActiveScene().name == "Demo_Combat")
            {
                DisableExistingSpawners();
                yield break;
            }

            var waves = EnsureSpawner();
            waves.Configure(player.transform, LoadPickupPrefabs());
            CentralCombatHud.Create(waves);
        }

        private void EnsureSeedManager()
        {
            if (GameSeedManager.Instance != null)
                return;

            var go = new GameObject("GameSeedManager");
            SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            go.AddComponent<GameSeedManager>();
        }

        private void DisableCompetingControllers()
        {
            var controllers = FindObjectsByType<FirstPersonController>(FindObjectsInactive.Include);

            foreach (var ctrl in controllers)
            {
                if (ctrl == null || ctrl.gameObject.scene != gameObject.scene)
                    continue;

                ctrl.SetActiveControl(false);
                ctrl.gameObject.SetActive(false);
            }

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

            foreach (var mb in behaviours)
            {
                if (mb == null || mb.gameObject.scene != gameObject.scene)
                    continue;

                var typeName = mb.GetType().Name;

                if (typeName == "FreeCamera" || typeName == "CameraController" || typeName == "FirstPersonSceneModeSwitcher")
                    mb.enabled = false;
            }
        }

        private void EnsureFrameworkManagers()
        {
            if (FindAnyObjectByType<Akila.FPSFramework.GameManager>(FindObjectsInactive.Include) == null)
            {
                var managerPrefab = LoadEditorAsset<GameObject>(FrameworkGameManagerPath);

                if (managerPrefab != null)
                    Instantiate(managerPrefab);
            }

            if (FindAnyObjectByType<UIManager>(FindObjectsInactive.Include) == null)
            {
                var hudPrefab = LoadEditorAsset<GameObject>(FrameworkHudPath);

                if (hudPrefab != null)
                    Instantiate(hudPrefab);
            }

            if (SpawnManager.Instance == null && FindAnyObjectByType<SpawnManager>(FindObjectsInactive.Include) == null)
            {
                var go = new GameObject("CombatSpawnManager");
                SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                var spawnManager = go.AddComponent<SpawnManager>();
                var point = new GameObject("CombatRespawnPoint").transform;
                point.SetParent(go.transform, false);
                point.position = PickPlayerSpawn();
                spawnManager.sides = new List<SpawnManager.SpwanSide>
                {
                    new SpawnManager.SpwanSide
                    {
                        points = new[] { point }
                    }
                };
            }
        }

        private void BuildRuntimeNavMesh()
        {
            var surfaceName = $"{gameObject.scene.name}CombatNavMesh";
            var oldSurface = FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include)
                .FirstOrDefault(surface => surface != null && surface.gameObject.scene == gameObject.scene && surface.gameObject.name == surfaceName);

            var surface = oldSurface;

            if (surface == null)
            {
                var go = new GameObject(surfaceName);
                SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                surface = go.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.layerMask = Physics.DefaultRaycastLayers;
            surface.defaultArea = 0;
            surface.BuildNavMesh();
        }

        private GameObject EnsureFrameworkPlayer()
        {
            var players = FindObjectsByType<CharacterManager>(FindObjectsInactive.Include)
                .Where(x => x != null && x.gameObject.scene == gameObject.scene)
                .ToArray();

            foreach (var existing in players)
            {
                if (existing.gameObject.activeInHierarchy && IsReadyMadeFrameworkPlayer(existing.gameObject))
                {
                    ConfigurePlayer(existing.gameObject);
                    return existing.gameObject;
                }
            }

            if (gameObject.scene.name == "Demo_Combat")
            {
                foreach (var existing in players)
                {
                    if (existing != null)
                        existing.gameObject.SetActive(false);
                }
            }

            var playerPrefab = LoadEditorAsset<GameObject>(PlayerPrefabPath);

            if (playerPrefab == null)
            {
                Debug.LogError($"Combat scene could not load Akila player prefab at {PlayerPrefabPath}.");
                return null;
            }

            var pos = PickPlayerSpawn();
            var player = Instantiate(playerPrefab, pos, Quaternion.Euler(0f, 15f, 0f));
            player.name = gameObject.scene.name == "Demo_Combat" ? "AkilaFPSFrameworkPlayer" : "AkilaCombatPlayer";
            SceneManager.MoveGameObjectToScene(player, gameObject.scene);
            ConfigurePlayer(player);
            return player;
        }

        private void ConfigurePlayer(GameObject player)
        {
            var damageable = player.GetComponentInChildren<Damageable>(true);

            if (damageable != null)
            {
                damageable.type = DamagableType.Player;
                damageable.health = Mathf.Max(damageable.health, 100f);
                damageable.maxHealth = Mathf.Max(damageable.maxHealth, damageable.health);
            }

            var actor = player.GetComponentInChildren<Actor>(true);

            if (actor != null)
            {
                actor.actorName = "Survivor";
                actor.type = "Player";
                actor.teamId = 0;
                actor.respawnable = true;
                actor.playerCardActive = true;
                actor.playerUIEnabled = true;
            }

            var inv = player.GetComponentInChildren<FrameworkInventory>(true);

            if (inv == null)
                return;

            inv.startItems.Clear();
            AddStarterWeapon(inv, PistolPath);
            AddStarterWeapon(inv, AssaultRiflePath);
            inv.maxSlots = Mathf.Max(3, inv.startItems.Count);
        }

        private void AddStarterWeapon(FrameworkInventory inventory, string path)
        {
            var prefab = LoadEditorAsset<GameObject>(path);

            if (prefab == null)
                return;

            var item = prefab.GetComponent<FrameworkInventoryItem>() ?? prefab.GetComponentInChildren<FrameworkInventoryItem>(true);

            if (item != null && !inventory.startItems.Contains(item))
                inventory.startItems.Add(item);
        }

        private Vector3 PickPlayerSpawn()
        {
            var candidates = new[]
            {
                new Vector3(14f, 2f, -31f),
                new Vector3(-22f, 2f, -20f),
                new Vector3(26f, 2f, 16f),
                new Vector3(-34f, 2f, 26f),
                new Vector3(0f, 2f, -18f),
                new Vector3(-10f, 2f, -14f),
                new Vector3(10f, 2f, -14f),
                new Vector3(0f, 2f, 0f),
                new Vector3(-18f, 2f, 0f)
            };

            foreach (var spot in candidates)
            {
                if (NavMesh.SamplePosition(spot, out var hit, 16f, NavMesh.AllAreas))
                    return hit.position + Vector3.up * 0.08f;
            }

            foreach (var spot in candidates)
            {
                if (Physics.Raycast(spot + Vector3.up * 60f, Vector3.down, out var hit, 140f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return hit.point + Vector3.up * 0.08f;
            }

            return Vector3.up * 1.2f;
        }

        private void EnsureSingleCamera(GameObject player)
        {
            if (player == null)
                return;

            var mainCam = FindPlayerMainCamera(player);

            if (mainCam == null)
                return;

            mainCam.enabled = true;
            mainCam.gameObject.SetActive(true);
            mainCam.tag = "MainCamera";

            foreach (var camera in player.GetComponentsInChildren<Camera>(true))
            {
                if (camera == null)
                    continue;

                camera.enabled = camera == mainCam || camera.name == "Overlay Camera";
            }

            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);

            foreach (var camera in cameras)
            {
                if (camera == null || camera == mainCam)
                    continue;

                if (camera.transform.IsChildOf(player.transform))
                    continue;

                if (camera.GetComponentInParent<Canvas>() != null)
                    continue;

                camera.enabled = false;
            }

            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            var playerListener = player.GetComponentInChildren<AudioListener>(true);

            foreach (var listener in listeners)
            {
                if (listener == null)
                    continue;

                listener.enabled = listener == playerListener;
            }
        }

        private Camera FindPlayerMainCamera(GameObject player)
        {
            var cameras = player.GetComponentsInChildren<Camera>(true);
            return cameras.FirstOrDefault(camera => camera != null && camera.name == "Main Camera")
                   ?? cameras.FirstOrDefault(camera => camera != null && camera.CompareTag("MainCamera"))
                   ?? cameras.FirstOrDefault(camera => camera != null && camera.GetComponent<AudioListener>() != null)
                   ?? cameras.FirstOrDefault(camera => camera != null);
        }

        private bool IsReadyMadeFrameworkPlayer(GameObject player)
        {
            if (player == null)
                return false;

            if (player.GetComponent<Akila.FPSFramework.FirstPersonController>() == null)
                return false;

            if (FindPlayerMainCamera(player) == null)
                return false;

#if UNITY_EDITOR
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(player);
            var playerPrefab = LoadEditorAsset<GameObject>(PlayerPrefabPath);

            if (source == playerPrefab)
                return true;
#endif

            return player.name == "AkilaFPSFrameworkPlayer" || player.name == "AkilaCombatPlayer" || player.CompareTag("Player");
        }

        private void DisableExistingSpawners()
        {
            var spawners = FindObjectsByType<CentralCombatSpawner>(FindObjectsInactive.Include);

            foreach (var spawner in spawners)
            {
                if (spawner != null && spawner.gameObject.scene == gameObject.scene)
                    spawner.gameObject.SetActive(false);
            }
        }

        private CentralCombatSpawner EnsureSpawner()
        {
            var spawner = FindObjectsByType<CentralCombatSpawner>(FindObjectsInactive.Include)
                .FirstOrDefault(x => x != null && x.gameObject.scene == gameObject.scene);

            if (spawner != null)
                return spawner;

            var go = new GameObject("CentralCombatSpawner");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            return go.AddComponent<CentralCombatSpawner>();
        }

        private GameObject[] LoadPickupPrefabs()
        {
            var paths = new[]
            {
                "Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/9mm Ammo.prefab",
                "Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/5.56mm Ammo.prefab",
                "Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/7.62mm Ammo.prefab"
            };

            return paths.Select(LoadEditorAsset<GameObject>).Where(prefab => prefab != null).ToArray();
        }

        private static T LoadEditorAsset<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }
    }
}
