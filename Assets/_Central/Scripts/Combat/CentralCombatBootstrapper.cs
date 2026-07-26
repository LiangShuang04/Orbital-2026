using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Akila.FPSFramework;
using DontDiePlease.Central;
using DontDiePlease.Systems;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
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
        private const string FenrisFrigatePath = "Assets/VattalusAssets/FenrisFrigate/_URP/Prefabs/DEMO/FenrisFrigate_DemoPrefab.prefab";
        private const string AssetCatalogPath = "Combat/CentralCombatAssetCatalog";
        private const float PlayerSpawnHeight = 1.08f;

        private CentralCombatAssetCatalog assetCatalog;
        private FenrisFrigatePrologue fenrisPrologue;
        private Vector3 mapSpawn;
        private bool mapSpawnReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHandler()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForActiveScene()
        {
            CreateForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CreateForScene(scene);
        }

        private static void CreateForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || !SceneNames.Contains(scene.name))
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
            assetCatalog = Resources.Load<CentralCombatAssetCatalog>(AssetCatalogPath);

            EnsureSeedManager();
            DisableCompetingControllers();
            DisableActiveEventSystems();
            EnsureNavMesh();
            mapSpawn = PickMapSpawn();
            mapSpawnReady = true;

            if (gameObject.scene.name == "Demo_Combat")
            {
                var frigatePrefab = LoadAsset(assetCatalog?.FenrisFrigatePrefab, FenrisFrigatePath);
                fenrisPrologue = FenrisFrigatePrologue.Create(frigatePrefab, gameObject.scene, mapSpawn);
            }

            EnsureFrameworkManagers();
            NormalizeEventSystems();

            var player = EnsureFrameworkPlayer();

            yield return null;

            EnsureSingleCamera(player);

            if (player == null)
                yield break;

            yield return FinishPlayerSetup(player);

            SpawnSurvivalWorldObjects(player);

            if (SceneManager.GetActiveScene().name == "Demo_Combat")
            {
                var demoSpawner = EnsureSpawner();
                demoSpawner.gameObject.SetActive(true);
                demoSpawner.Configure(player.transform, LoadPickupPrefabs());
                demoSpawner.SetAutomaticWaves(fenrisPrologue == null, true);
                fenrisPrologue?.AttachPlayer(player.transform, demoSpawner);
                player.GetComponent<CentralPlayerGrounding>()?.RefreshSafePosition(player.transform.position);
                CentralCombatHud.Create(demoSpawner);
                yield break;
            }

            var waves = EnsureSpawner();
            waves.Configure(player.transform, LoadPickupPrefabs());
            CentralCombatHud.Create(waves);
        }

        private void DisableActiveEventSystems()
        {
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include);

            foreach (var eventSystem in eventSystems)
            {
                if (eventSystem != null)
                    eventSystem.gameObject.SetActive(false);
            }
        }

        private void NormalizeEventSystems()
        {
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            var activeSystem = eventSystems.FirstOrDefault(eventSystem =>
                eventSystem != null &&
                eventSystem.GetComponentInParent<Akila.FPSFramework.GameManager>(true) != null);

            if (activeSystem == null)
                activeSystem = eventSystems.FirstOrDefault(eventSystem => eventSystem != null);

            foreach (var eventSystem in eventSystems)
            {
                if (eventSystem != null)
                    eventSystem.gameObject.SetActive(eventSystem == activeSystem);
            }
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

        // Bolts our survival systems onto whatever player the bootstrapper spawns.
        // NOTE: a bare "Inventory" here would resolve to Akila's (via 'using Akila.FPSFramework'),
        // so the survival inventory must be referenced as global::Inventory.
        private void AttachSurvivalSystems(GameObject player)
        {
            if (player.GetComponent<PlayerStats>() == null) player.AddComponent<PlayerStats>();                   // survival stats -> SurvivalHUD
            if (player.GetComponent<global::Inventory>() == null) player.AddComponent<global::Inventory>();       // crystals -> GraphicalInventoryUI, crafting
            if (player.GetComponent<SelectionManager>() == null) player.AddComponent<SelectionManager>();         // look + interact: pickups, station, generator
            if (player.GetComponent<AkilaInventoryBridge>() == null) player.AddComponent<AkilaInventoryBridge>(); // guns appear in the Tab inventory
            if (player.GetComponent<CombatHealthBridge>() == null) player.AddComponent<CombatHealthBridge>();     // enemy damage -> survival HP bar

            player.tag = "Player"; // enemies find the player by this tag
        }

        // Spawns the survival world objects (item spawners, crafting station, signal
        // generator) from configured prefabs in Resources/SurvivalWorld/. Each prefab bakes
        // in its own content (loot table / recipe / required parts), so this stays build-safe
        // with no runtime asset wiring. Missing prefabs are logged and skipped.
        private void SpawnSurvivalWorldObjects(GameObject player)
        {
            Vector3 origin = player != null ? player.transform.position : PickPlayerSpawn();

            SpawnFromResources("SurvivalWorld/ItemSpawner", origin + new Vector3(0f, 0f, 6f));
            SpawnFromResources("SurvivalWorld/CraftingStation", origin + new Vector3(4f, 0f, 6f));
            SpawnFromResources("SurvivalWorld/SignalGenerator", origin + new Vector3(-4f, 0f, 8f));
        }

        private void SpawnFromResources(string resourcePath, Vector3 position)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Survival] No prefab at Resources/{resourcePath} — skipped. Create + configure it and put it there to spawn it.");
                return;
            }

            var obj = Instantiate(prefab, position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
        }

        // Disables Akila HUD elements we don't use: the PlayerCard (Akila's health/name
        // bar — our SurvivalHUD shows health instead) and the Minimap (non-functional).
        // Component-based, so it works no matter which prefab (GameManager or HUD) spawned them.
        private void StripUnwantedHudElements()
        {
            foreach (var card in FindObjectsByType<PlayerCard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                card.gameObject.SetActive(false);

            foreach (var minimap in FindObjectsByType<Minimap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                minimap.gameObject.SetActive(false);

            // Move the Akila FPS counter to the top-right so it stops overlapping the
            // survival HUD and the wave counter in the top-left.
            foreach (var fps in FindObjectsByType<Akila.FPSFramework.UI.FPSCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (fps.transform is RectTransform rt)
                {
                    // top-right, but well to the LEFT of the 380-wide objective panel so they don't overlap
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-430f, -20f);
                }
            }
        }

        private void EnsureFrameworkManagers()
        {
            if (FindAnyObjectByType<Akila.FPSFramework.GameManager>(FindObjectsInactive.Include) == null)
            {
                var managerPrefab = LoadAsset(assetCatalog?.GameManagerPrefab, FrameworkGameManagerPath);

                if (managerPrefab != null)
                    Instantiate(managerPrefab);
            }

            if (FindAnyObjectByType<UIManager>(FindObjectsInactive.Include) == null)
            {
                var hudPrefab = LoadAsset(assetCatalog?.HudPrefab, FrameworkHudPath);

                if (hudPrefab != null)
                    Instantiate(hudPrefab);
            }

            // strip the Akila player-card health bar + broken minimap from the spawned HUD
            StripUnwantedHudElements();
            Invoke(nameof(StripUnwantedHudElements), 0.25f); // re-run in case the HUD spawns a frame later

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

        private void EnsureNavMesh()
        {
            var bakedSurface = FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include)
                .FirstOrDefault(surface =>
                    surface != null &&
                    surface.gameObject.scene == gameObject.scene &&
                    surface.navMeshData != null);

            if (bakedSurface != null)
            {
                bakedSurface.enabled = true;
                bakedSurface.AddData();
                return;
            }

            BuildRuntimeNavMesh();
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
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = Physics.DefaultRaycastLayers;
            surface.defaultArea = 0;

            var addedModifiers = new List<NavMeshModifier>();
            var changedModifiers = new List<(NavMeshModifier modifier, bool ignored)>();
            var meshColliders = FindObjectsByType<MeshCollider>(FindObjectsInactive.Include);

            foreach (var meshCollider in meshColliders)
            {
                if (meshCollider == null || meshCollider.gameObject.scene != gameObject.scene)
                    continue;

                var modifier = meshCollider.GetComponent<NavMeshModifier>();

                if (modifier == null)
                {
                    modifier = meshCollider.gameObject.AddComponent<NavMeshModifier>();
                    addedModifiers.Add(modifier);
                }
                else
                {
                    changedModifiers.Add((modifier, modifier.ignoreFromBuild));
                }

                modifier.ignoreFromBuild = true;
            }

            surface.BuildNavMesh();

            foreach (var state in changedModifiers)
            {
                if (state.modifier != null)
                    state.modifier.ignoreFromBuild = state.ignored;
            }

            foreach (var modifier in addedModifiers)
            {
                if (modifier != null)
                    Destroy(modifier);
            }
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
                    MovePlayerToSpawn(existing.gameObject);
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

            var playerPrefab = LoadAsset(assetCatalog?.PlayerPrefab, PlayerPrefabPath);

            if (playerPrefab == null)
            {
                Debug.LogError($"Combat scene could not load Akila player prefab at {PlayerPrefabPath}.");
                return null;
            }

            var pos = PickPlayerSpawn();
            var player = Instantiate(playerPrefab, pos, PickPlayerRotation());
            player.name = gameObject.scene.name == "Demo_Combat" ? "AkilaFPSFrameworkPlayer" : "AkilaCombatPlayer";
            SceneManager.MoveGameObjectToScene(player, gameObject.scene);
            ConfigurePlayer(player);
            return player;
        }

        private void ConfigurePlayer(GameObject player)
        {
            var controller = player.GetComponent<Akila.FPSFramework.FirstPersonController>();

            if (controller != null)
            {
                controller.enabled = true;
                controller.lockCursor = true;
                controller.SetActive(true);
            }

            var characterInput = player.GetComponent<CharacterInput>();

            if (characterInput != null)
                characterInput.enabled = true;

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
                actor.respawnable = false;
                actor.playerCardActive = true;
                actor.playerUIEnabled = true;
            }

            var recovery = player.GetComponent<CentralPlayerRecovery>();

            if (recovery == null)
                recovery = player.AddComponent<CentralPlayerRecovery>();

            recovery.Configure(this);

            var grounding = player.GetComponent<CentralPlayerGrounding>();

            if (grounding == null)
                grounding = player.AddComponent<CentralPlayerGrounding>();

            grounding.Configure(player.transform.position);

            // Attach all our survival systems (stats, inventory, interaction, gun mirror,
            // combat -> survival health bridge, and the Player tag) onto the spawned player.
            AttachSurvivalSystems(player);

            var inv = player.GetComponentInChildren<FrameworkInventory>(true);

            if (inv == null)
                return;

            inv.enabled = true;
            inv.isActive = true;
            inv.isInputActive = true;
            inv.startItems.Clear();
            AddStarterWeapon(inv, LoadAsset(assetCatalog?.PistolPrefab, PistolPath));
            AddStarterWeapon(inv, LoadAsset(assetCatalog?.AssaultRiflePrefab, AssaultRiflePath));
            inv.maxSlots = Mathf.Max(3, inv.startItems.Count);
        }

        private void EnsureEquippedWeapons(GameObject player)
        {
            FPSFrameworkCore.IsActive = true;
            FPSFrameworkCore.IsInputActive = true;
            FPSFrameworkCore.IsPaused = false;

            var inventory = player.GetComponentInChildren<FrameworkInventory>(true);

            if (inventory == null)
                return;

            inventory.enabled = true;
            inventory.isActive = true;
            inventory.isInputActive = true;
            EnsureWeaponInstance(inventory, LoadAsset(assetCatalog?.PistolPrefab, PistolPath), "Pistol_1");
            EnsureWeaponInstance(inventory, LoadAsset(assetCatalog?.AssaultRiflePrefab, AssaultRiflePath), "Assault Rifle_1");
            inventory.items = inventory.GetComponentsInChildren<FrameworkInventoryItem>(true).ToList();
            inventory.currentItemIndex = 0;
            inventory.Switch(0);
        }

        private IEnumerator FinishPlayerSetup(GameObject player)
        {
            if (player == null)
                yield break;

            EnsureEquippedWeapons(player);

            for (var frame = 0; frame < 45; frame++)
            {
                if (HasReadyWeaponInput(player))
                    break;

                yield return null;
            }

            EnsureEquippedWeapons(player);
            PrepareFirearms(player);

            var controller = player.GetComponent<Akila.FPSFramework.FirstPersonController>();

            if (controller != null)
            {
                controller.enabled = true;
                controller.lockCursor = true;
                controller.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            FPSFrameworkCore.IsActive = true;
            FPSFrameworkCore.IsPaused = false;
            FPSFrameworkCore.IsInputActive = true;
        }

        private static void PrepareFirearms(GameObject player)
        {
            foreach (var firearm in player.GetComponentsInChildren<Firearm>(true))
            {
                firearm.enabled = true;
                firearm.isInputActive = true;
                firearm.firePrevented = false;
                firearm.isReloading = false;

                var itemInput = firearm.GetComponent<ItemInput>();

                if (itemInput != null)
                {
                    itemInput.enabled = true;
                    itemInput.allowInputWhilePaused = false;
                }

                if (!firearm.gameObject.activeInHierarchy)
                    continue;

                foreach (var animator in firearm.GetComponentsInChildren<Animator>(true))
                {
                    animator.speed = 1f;
                    var state = animator.GetCurrentAnimatorStateInfo(0);

                    if (state.IsName("Take") || state.IsName("Pickup"))
                    {
                        animator.Play("Idle", 0, 0f);
                        animator.Update(0f);
                    }
                }
            }
        }

        private bool HasReadyWeaponInput(GameObject player)
        {
            var inventory = player.GetComponentInChildren<FrameworkInventory>(true);

            if (inventory == null)
                return false;

            var activeInput = inventory.GetComponentsInChildren<ItemInput>(true)
                .FirstOrDefault(input => input != null && input.gameObject.activeInHierarchy);

            return activeInput != null &&
                   activeInput.enabled &&
                   activeInput.Controls != null &&
                   activeInput.Inventory != null &&
                   activeInput.CharacterInput != null;
        }

        public void RecoverPlayer(GameObject previousPlayer)
        {
            StartCoroutine(RecoverPlayerRoutine(previousPlayer));
        }

        private IEnumerator RecoverPlayerRoutine(GameObject previousPlayer)
        {
            var playerPrefab = LoadAsset(assetCatalog?.PlayerPrefab, PlayerPrefabPath);

            if (playerPrefab == null)
            {
                Debug.LogError($"Combat scene could not reload Akila player prefab at {PlayerPrefabPath}.");
                yield break;
            }

            var player = Instantiate(playerPrefab, PickPlayerSpawn(), PickPlayerRotation());
            player.name = gameObject.scene.name == "Demo_Combat" ? "AkilaFPSFrameworkPlayer" : "AkilaCombatPlayer";
            SceneManager.MoveGameObjectToScene(player, gameObject.scene);
            ConfigurePlayer(player);
            fenrisPrologue?.SetPlayer(player.transform);

            if (previousPlayer != null)
                previousPlayer.SetActive(false);

            yield return null;

            yield return FinishPlayerSetup(player);
            EnsureSingleCamera(player);
            DeathCamera.Instance?.Disable();

            var spawner = FindObjectsByType<CentralCombatSpawner>(FindObjectsInactive.Include)
                .FirstOrDefault(x => x != null && x.gameObject.scene == gameObject.scene);
            spawner?.SetPlayerTarget(player.transform);

            if (previousPlayer != null)
                Destroy(previousPlayer);

            FPSFrameworkCore.IsActive = true;
            FPSFrameworkCore.IsPaused = false;
            FPSFrameworkCore.IsInputActive = true;
        }

        private void EnsureWeaponInstance(FrameworkInventory inventory, GameObject prefab, string weaponName)
        {
            if (prefab == null ||
                inventory.GetComponentsInChildren<FrameworkInventoryItem>(true)
                    .Any(item => item != null && item.name.Contains(weaponName)))
            {
                return;
            }

            var item = prefab.GetComponent<FrameworkInventoryItem>() ??
                       prefab.GetComponentInChildren<FrameworkInventoryItem>(true);

            if (item != null)
                Instantiate(item, inventory.transform);
        }

        private void AddStarterWeapon(FrameworkInventory inventory, GameObject prefab)
        {
            if (prefab == null)
                return;

            var item = prefab.GetComponent<FrameworkInventoryItem>() ??
                       prefab.GetComponentInChildren<FrameworkInventoryItem>(true);

            if (item != null && !inventory.startItems.Contains(item))
                inventory.startItems.Add(item);
        }

        private Vector3 PickPlayerSpawn()
        {
            if (fenrisPrologue != null)
                return fenrisPrologue.CurrentRespawnPosition;

            return mapSpawnReady ? mapSpawn : PickMapSpawn();
        }

        private Quaternion PickPlayerRotation()
        {
            return fenrisPrologue != null
                ? fenrisPrologue.CurrentRespawnRotation
                : Quaternion.Euler(0f, 15f, 0f);
        }

        private void MovePlayerToSpawn(GameObject player)
        {
            if (player == null)
                return;

            var controller = player.GetComponent<CharacterController>();
            var wasEnabled = controller != null && controller.enabled;

            if (wasEnabled)
                controller.enabled = false;

            player.transform.SetPositionAndRotation(PickPlayerSpawn(), PickPlayerRotation());
            Physics.SyncTransforms();

            if (wasEnabled)
                controller.enabled = true;
        }

        private Vector3 PickMapSpawn()
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
                    return hit.position + Vector3.up * PlayerSpawnHeight;
            }

            foreach (var spot in candidates)
            {
                if (Physics.Raycast(spot + Vector3.up * 60f, Vector3.down, out var hit, 140f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return hit.point + Vector3.up * PlayerSpawnHeight;
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
            if (assetCatalog != null && assetCatalog.PickupPrefabs.Length > 0)
                return assetCatalog.PickupPrefabs.Where(prefab => prefab != null).ToArray();

            var paths = new[]
            {
                "Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/9mm Ammo.prefab",
                "Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/5.56mm Ammo.prefab",
                "Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/7.62mm Ammo.prefab"
            };

            return paths.Select(LoadEditorAsset<GameObject>).Where(prefab => prefab != null).ToArray();
        }

        private static GameObject LoadAsset(GameObject runtimeAsset, string editorPath)
        {
            return runtimeAsset != null ? runtimeAsset : LoadEditorAsset<GameObject>(editorPath);
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
