using System;
using System.Collections.Generic;
using System.Linq;
using DontDiePlease.Narrative.Runtime;
using DontDiePlease.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class FenrisFrigatePrologue : MonoBehaviour
    {
        private const float GroundClearance = 0.08f;
        private const float DoorInteractionRange = 3.8f;
        private const float StationInteractionRange = 3.2f;
        private readonly List<Vector2> candidateOffsets = new List<Vector2>();
        private readonly List<MonoBehaviour> shipDoors = new List<MonoBehaviour>();
        private readonly List<MonoBehaviour> airlockDoors = new List<MonoBehaviour>();
        private readonly List<MonoBehaviour> interiorRooms = new List<MonoBehaviour>();
        private readonly List<Collider> interiorVolumes = new List<Collider>();
        private readonly List<TutorialStation> tutorialStations = new List<TutorialStation>();
        private GameObject ship;
        private Transform player;
        private CentralCombatSpawner spawner;
        private NarrativeDirector narrativeDirector;
        private Transform airlockAnchor;
        private Transform interiorSpawnAnchor;
        private Transform tutorialView;
        private GameObject interactionPrompt;
        private GameObject tutorialPanel;
        private TextMeshProUGUI interactionPromptText;
        private TextMeshProUGUI tutorialText;
        private Bounds shipBounds;
        private Vector3 interiorSpawn;
        private Vector3 airlockApproachPosition;
        private Vector3 mapSpawn;
        private Vector3 mapEntryPosition;
        private Vector3 lastSafeInteriorPosition;
        private Quaternion interiorRotation;
        private Quaternion tutorialStartViewRotation;
        private Vector3 tutorialStartPosition;
        private Quaternion tutorialStartRotation;
        private float departureCheckAt;
        private float outsideInteriorSince = -1f;
        private float nextSafeInteriorSampleAt;
        private bool combatReleased;
        private bool moved;
        private bool looked;
        private bool switchedWeapon;
        private bool aimed;
        private bool fired;
        private bool reloaded;
        private Material skyMaterial;
        private int tutorialStep;

        public static FenrisFrigatePrologue Instance { get; private set; }
        public GameObject Ship => ship;
        public bool HasExited { get; private set; }
        public bool BlocksCombat => !combatReleased;
        public Vector3 CurrentRespawnPosition => HasExited ? mapSpawn : interiorSpawn;
        public Quaternion CurrentRespawnRotation => HasExited ? Quaternion.Euler(0f, 15f, 0f) : interiorRotation;
        public bool IsPlayerInside => player != null && !HasExited && IsInInteriorVolume(player.position);
        public bool UsedFallbackPlacement { get; private set; }
        public bool AirlockOpened { get; private set; }
        public bool TutorialComplete => tutorialStep >= 6;
        public bool WakeSequenceComplete => narrativeDirector != null &&
                                            narrativeDirector.State != null &&
                                            narrativeDirector.State.HasCompletedSequence("TRG_FENRIS_WAKE_V2");
        public bool IsAirlockInteractionAvailable => CanOpenAirlock();
        public Vector3 InteriorSpawnPosition => interiorSpawn;
        public Vector3 AirlockApproachPosition => airlockApproachPosition;
        public Vector3 MapEntryPosition => mapEntryPosition;
        public Transform AirlockAnchor => airlockAnchor;
        public Transform InteriorSpawnAnchor => interiorSpawnAnchor;
        public int InteriorRoomCount => interiorRooms.Count;
        public int VisibleInteriorRoomCount => interiorRooms.Count(IsRoomVisible);
        public int InteriorVolumeCount => interiorVolumes.Count;
        public int SolidColliderCount => ship == null
            ? 0
            : ship.GetComponentsInChildren<Collider>(true).Count(collider => !collider.isTrigger);
        public int InteractableCount => ship == null
            ? 0
            : ship.GetComponentsInChildren<MonoBehaviour>(true)
                .Count(behaviour => behaviour != null && behaviour.GetType().Name == "VattalusInteractable");
        public int TutorialStationCount => tutorialStations.Count;
        public int CompletedTutorialStationCount => tutorialStations.Count(station => station.Used);
        public bool InteriorSpawnIsClear => IsCapsuleClear(interiorSpawn);
        public bool MapEntryIsClear => IsCapsuleClear(mapEntryPosition);
        public float ShipDistanceFromMapSpawn => Vector3.Distance(
            new Vector3(shipBounds.center.x, 0f, shipBounds.center.z),
            new Vector3(mapSpawn.x, 0f, mapSpawn.z));

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (skyMaterial != null)
                Destroy(skyMaterial);

        }

        private void Update()
        {
            FindNarrativeDirector();
            EnsurePlayerCameraUsesSky();
            UpdateTutorial();
            UpdateInteraction();
            ProtectInterior();
        }

        public static FenrisFrigatePrologue Create(GameObject prefab, Scene scene, Vector3 fallbackMapSpawn)
        {
            if (prefab == null || !scene.IsValid())
                return null;

            var root = new GameObject("FenrisFrigatePrologue");
            SceneManager.MoveGameObjectToScene(root, scene);
            var prologue = root.AddComponent<FenrisFrigatePrologue>();
            prologue.mapSpawn = fallbackMapSpawn;
            prologue.BuildShip(prefab, scene);
            return prologue.ship == null ? null : prologue;
        }

        public void AttachPlayer(Transform target, CentralCombatSpawner combatSpawner)
        {
            player = target;
            spawner = combatSpawner;
            FindNarrativeDirector();
            departureCheckAt = Time.unscaledTime + 2f;

            if (player != null && !HasExited)
            {
                MovePlayer(interiorSpawn, interiorRotation);
                lastSafeInteriorPosition = interiorSpawn;
                outsideInteriorSince = -1f;
                nextSafeInteriorSampleAt = Time.unscaledTime + 0.5f;
                tutorialStartPosition = interiorSpawn;
                tutorialStartRotation = interiorRotation;
                tutorialView = player.GetComponentsInChildren<Camera>(true)
                    .Select(camera => camera.transform)
                    .FirstOrDefault() ?? player;
                tutorialStartViewRotation = tutorialView.rotation;
                EnsurePlayerCameraUsesSky();
                UpdateTutorialPanel();
            }
        }

        public void SetPlayer(Transform target)
        {
            player = target;

            if (player != null && !HasExited)
            {
                lastSafeInteriorPosition = player.position;
                departureCheckAt = Time.unscaledTime + 2f;
                outsideInteriorSince = -1f;
                tutorialView = player.GetComponentsInChildren<Camera>(true)
                    .Select(camera => camera.transform)
                    .FirstOrDefault() ?? player;
                EnsurePlayerCameraUsesSky();
            }
        }

        public void CompleteDeparture()
        {
            if (HasExited)
                return;

            HasExited = true;

            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        public void SkipToMap()
        {
            foreach (var station in tutorialStations)
                station.Used = true;

            tutorialStep = 6;
            OpenAirlock();
            Disembark();
        }

        public void OpenAirlock()
        {
            if (AirlockOpened || !TutorialComplete)
                return;

            var opened = false;

            foreach (var door in airlockDoors)
            {
                opened |= OpenDoor(door);
            }

            AirlockOpened = opened;

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            if (opened)
                UpdateTutorialPanel();
        }

        public void Disembark()
        {
            if (HasExited || !AirlockOpened || !TutorialComplete || player == null)
                return;

            MovePlayer(mapEntryPosition, Quaternion.LookRotation(MapEntryDirection(), Vector3.up));
            CompleteDeparture();
            player.GetComponent<CentralPlayerGrounding>()?.RefreshSafePosition(mapEntryPosition);
        }

        public void ReleaseCombat(bool enableAutomaticWaves)
        {
            combatReleased = true;
            spawner?.SetAutomaticWaves(enableAutomaticWaves, true);
        }

        private void BuildShip(GameObject prefab, Scene scene)
        {
            EnsureVattalusContext(scene);
            ship = Instantiate(prefab);
            ship.name = "FenrisFrigate";
            SceneManager.MoveGameObjectToScene(ship, scene);
            ship.SetActive(false);
            ship.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            shipBounds = CalculateBounds(ship);
            PlaceShip();
            ship.SetActive(true);
            Physics.SyncTransforms();
            PrepareInteriorForAkila();
            Physics.SyncTransforms();
            shipBounds = CalculateBounds(ship);

            airlockAnchor = FindChild("Position_MainDeck_Airlock") ?? FindArea("Airlock");
            interiorSpawnAnchor = FindChild("Position_Bridge") ??
                                  FindArea("Bridge") ??
                                  FindChild("Position_ReadyRoom") ??
                                  FindArea("ReadyRoom") ??
                                  airlockAnchor;
            FindDoors();
            interiorSpawn = interiorSpawnAnchor != null
                ? FindWalkablePosition(interiorSpawnAnchor)
                : shipBounds.center + Vector3.up;
            var airlockDoor = airlockDoors.FirstOrDefault();
            var airlockApproach = airlockDoor != null ? airlockDoor.transform : airlockAnchor;
            airlockApproachPosition = airlockApproach != null
                ? FindWalkablePosition(airlockApproach)
                : interiorSpawn;
            var routeAnchor = FindChild("Position_ReadyRoom") ??
                              FindArea("ReadyRoom") ??
                              airlockAnchor;
            var exitDirection = routeAnchor != null
                ? Vector3.ProjectOnPlane(routeAnchor.position - interiorSpawn, Vector3.up)
                : ship.transform.right;

            if (exitDirection.sqrMagnitude < 0.01f)
                exitDirection = ship.transform.right;

            interiorRotation = Quaternion.LookRotation(exitDirection.normalized, Vector3.up);
            mapEntryPosition = FindMapEntryPosition();
            lastSafeInteriorPosition = interiorSpawn;
            BuildTutorialStations();
            ConfigureSky();
            BuildTutorialInterface();
        }

        private void PlaceShip()
        {
            var terrains = Terrain.activeTerrains
                .Where(terrain => terrain != null && terrain.gameObject.scene == gameObject.scene)
                .ToArray();

            foreach (var terrain in terrains)
            {
                if (TryPlaceOnTerrain(terrain))
                    return;
            }

            var rootPos = mapSpawn + new Vector3(42f, 0f, 18f);

            if (Physics.Raycast(rootPos + Vector3.up * 120f, Vector3.down, out var hit, 240f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                rootPos.y = hit.point.y;

            UsedFallbackPlacement = true;
            SetShipBase(rootPos);
        }

        private bool TryPlaceOnTerrain(Terrain terrain)
        {
            var data = terrain.terrainData;

            if (data == null)
                return false;

            var terrainOrigin = terrain.transform.position;
            var terrainSize = data.size;
            var marginX = shipBounds.extents.x + 5f;
            var marginZ = shipBounds.extents.z + 5f;

            if (terrainSize.x <= marginX * 2f || terrainSize.z <= marginZ * 2f)
                return false;

            if (TryPlaceNearMapEntrance(terrain, marginX, marginZ))
                return true;

            BuildCandidateOffsets();
            ShuffleCandidates();

            foreach (var offset in candidateOffsets)
            {
                var x = Mathf.Lerp(terrainOrigin.x + marginX, terrainOrigin.x + terrainSize.x - marginX, offset.x);
                var z = Mathf.Lerp(terrainOrigin.z + marginZ, terrainOrigin.z + terrainSize.z - marginZ, offset.y);
                var center = new Vector3(x, 0f, z);

                if (!TryGetFlatTerrainHeight(terrain, center, out var groundHeight))
                    continue;

                if (!IsPlacementClear(center, groundHeight))
                    continue;

                SetShipBase(new Vector3(center.x, groundHeight, center.z));
                return true;
            }

            var fallback = new Vector3(
                terrainOrigin.x + terrainSize.x * 0.5f,
                0f,
                terrainOrigin.z + terrainSize.z * 0.18f);
            fallback.y = terrain.SampleHeight(fallback) + terrainOrigin.y;
            UsedFallbackPlacement = true;
            SetShipBase(fallback);
            return true;
        }

        private bool TryPlaceNearMapEntrance(Terrain terrain, float marginX, float marginZ)
        {
            var offsets = new List<Vector2>
            {
                new Vector2(28f, 12f),
                new Vector2(48f, 12f),
                new Vector2(8f, 12f),
                new Vector2(58f, 22f),
                new Vector2(36f, 28f)
            };
            var rng = GameSeedManager.Instance != null
                ? GameSeedManager.Instance.CreateRandomStream("fenris-landing-zone-v2")
                : new System.Random(2026);

            for (var idx = offsets.Count - 1; idx > 0; idx--)
            {
                var swap = rng.Next(0, idx + 1);
                (offsets[idx], offsets[swap]) = (offsets[swap], offsets[idx]);
            }

            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;

            foreach (var offset in offsets)
            {
                var center = new Vector3(mapSpawn.x + offset.x, 0f, mapSpawn.z + offset.y);

                if (center.x < origin.x + marginX ||
                    center.x > origin.x + size.x - marginX ||
                    center.z < origin.z + marginZ ||
                    center.z > origin.z + size.z - marginZ)
                {
                    continue;
                }

                if (!TryGetFlatTerrainHeight(terrain, center, out var groundHeight))
                    continue;

                if (!IsPlacementClear(center, groundHeight))
                    continue;

                SetShipBase(new Vector3(center.x, groundHeight, center.z));
                return true;
            }

            return false;
        }

        private void BuildCandidateOffsets()
        {
            candidateOffsets.Clear();
            var values = new[] { 0.06f, 0.17f, 0.28f, 0.39f, 0.5f, 0.61f, 0.72f, 0.83f, 0.94f };

            foreach (var x in values)
            {
                foreach (var z in values)
                {
                    if (Mathf.Abs(x - 0.5f) < 0.05f && Mathf.Abs(z - 0.5f) < 0.05f)
                        continue;

                    candidateOffsets.Add(new Vector2(x, z));
                }
            }
        }

        private void ShuffleCandidates()
        {
            var rng = GameSeedManager.Instance != null
                ? GameSeedManager.Instance.CreateRandomStream("fenris-placement-v1")
                : new System.Random(2026);

            for (var idx = candidateOffsets.Count - 1; idx > 0; idx--)
            {
                var swap = rng.Next(0, idx + 1);
                (candidateOffsets[idx], candidateOffsets[swap]) = (candidateOffsets[swap], candidateOffsets[idx]);
            }
        }

        private bool TryGetFlatTerrainHeight(Terrain terrain, Vector3 center, out float height)
        {
            var extents = shipBounds.extents;
            var samples = new[]
            {
                center,
                center + new Vector3(extents.x * 0.82f, 0f, extents.z * 0.82f),
                center + new Vector3(extents.x * 0.82f, 0f, -extents.z * 0.82f),
                center + new Vector3(-extents.x * 0.82f, 0f, extents.z * 0.82f),
                center + new Vector3(-extents.x * 0.82f, 0f, -extents.z * 0.82f)
            };
            var min = float.MaxValue;
            var max = float.MinValue;

            foreach (var sample in samples)
            {
                var sampleHeight = terrain.SampleHeight(sample) + terrain.transform.position.y;
                min = Mathf.Min(min, sampleHeight);
                max = Mathf.Max(max, sampleHeight);
            }

            height = max;
            return max - min <= 4f;
        }

        private bool IsPlacementClear(Vector3 center, float groundHeight)
        {
            var halfExtents = new Vector3(
                Mathf.Max(2f, shipBounds.extents.x * 0.82f),
                Mathf.Max(2f, shipBounds.extents.y * 0.72f),
                Mathf.Max(2f, shipBounds.extents.z * 0.82f));
            var checkCenter = new Vector3(center.x, groundHeight + halfExtents.y + 0.5f, center.z);
            var hits = Physics.OverlapBox(
                checkCenter,
                halfExtents,
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit == null || hit is TerrainCollider || hit.transform.IsChildOf(ship.transform))
                    continue;

                if (hit.bounds.max.y <= groundHeight + 0.65f)
                    continue;

                if (hit.bounds.size.sqrMagnitude < 0.35f)
                    continue;

                var size = hit.bounds.size;

                if (size.y < 0.75f || size.x < 6f && size.z < 6f && size.y < 6f)
                    continue;

                return false;
            }

            return true;
        }

        private void SetShipBase(Vector3 groundPoint)
        {
            var root = ship.transform;
            var pos = root.position;
            pos.x += groundPoint.x - shipBounds.center.x;
            pos.z += groundPoint.z - shipBounds.center.z;
            pos.y += groundPoint.y - shipBounds.min.y + GroundClearance;
            root.position = pos;
            Physics.SyncTransforms();
            shipBounds = CalculateBounds(ship);
        }

        private Transform FindChild(string childName)
        {
            return ship.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child != null && child.name == childName);
        }

        private Transform FindArea(string areaName)
        {
            return ship.GetComponentsInChildren<Transform>(true)
                .Where(child => child != null &&
                                child.name.Contains(areaName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(child => child.GetComponentsInChildren<Renderer>(true).Length)
                .FirstOrDefault();
        }

        private void PrepareInteriorForAkila()
        {
            interiorRooms.Clear();
            interiorVolumes.Clear();

            foreach (var behaviour in ship.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;

                var typeName = behaviour.GetType().Name;

                if (typeName == "VattalusInteriorEnvManager" ||
                    typeName == "VattalusPlayerDetector" ||
                    typeName == "VattalusSpaceshipController" ||
                    typeName == "VattalusThrusterController" ||
                    typeName == "VattalusEngineSoundController")
                {
                    behaviour.enabled = false;
                    continue;
                }

                if (typeName != "VattalusRoomController")
                    continue;

                behaviour.enabled = false;
                interiorRooms.Add(behaviour);
                behaviour.GetType().GetMethod("UnhideRoom")?.Invoke(behaviour, null);
            }

            interiorVolumes.AddRange(
                ship.GetComponentsInChildren<Collider>(true)
                    .Where(collider =>
                        collider != null &&
                        collider.isTrigger &&
                        collider.name.Contains("BoundsCollider", StringComparison.OrdinalIgnoreCase)));

            foreach (var light in ship.GetComponentsInChildren<Light>(true))
                light.shadows = LightShadows.None;
        }

        private bool IsRoomVisible(MonoBehaviour room)
        {
            if (room == null)
                return false;

            var meshField = room.GetType().GetField("meshParent");
            var lightsField = room.GetType().GetField("lightsParent");
            var mesh = meshField?.GetValue(room) as GameObject;
            var lights = lightsField?.GetValue(room) as GameObject;
            return (mesh == null || mesh.activeInHierarchy) &&
                   (lights == null || lights.activeInHierarchy);
        }

        private Vector3 FindWalkablePosition(Transform anchor)
        {
            var offsets = new[]
            {
                Vector3.zero,
                anchor.forward * 0.8f,
                -anchor.forward * 0.8f,
                anchor.right * 0.8f,
                -anchor.right * 0.8f,
                anchor.forward * 1.4f,
                -anchor.forward * 1.4f,
                anchor.right * 1.4f,
                -anchor.right * 1.4f,
                anchor.forward * 2.2f,
                -anchor.forward * 2.2f,
                anchor.right * 2.2f,
                -anchor.right * 2.2f,
                (anchor.forward + anchor.right).normalized * 1.6f,
                (anchor.forward - anchor.right).normalized * 1.6f,
                (-anchor.forward + anchor.right).normalized * 1.6f,
                (-anchor.forward - anchor.right).normalized * 1.6f
            };

            foreach (var offset in offsets)
            {
                var probe = anchor.position + Vector3.ProjectOnPlane(offset, Vector3.up);
                var hits = Physics.RaycastAll(
                        probe + Vector3.up * 2.5f,
                        Vector3.down,
                        5f,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore)
                    .Where(hit =>
                        hit.transform != null &&
                        hit.transform.IsChildOf(ship.transform) &&
                        hit.normal.y > 0.55f &&
                        hit.point.y <= anchor.position.y + 0.5f &&
                        hit.point.y >= anchor.position.y - 1.5f)
                    .OrderByDescending(hit => hit.point.y);

                foreach (var hit in hits)
                {
                    var candidate = hit.point + Vector3.up * 0.98f;

                    if (IsCapsuleClear(candidate))
                        return candidate;
                }
            }

            return anchor.position + Vector3.up * 1.02f;
        }

        private bool IsCapsuleClear(Vector3 position)
        {
            var bottom = position + Vector3.down * 0.58f;
            var top = position + Vector3.up * 0.58f;
            var hits = Physics.OverlapCapsule(
                bottom,
                top,
                0.27f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit == null || hit.transform.IsChildOf(transform))
                    continue;

                if (player != null && (hit.transform == player || hit.transform.IsChildOf(player)))
                    continue;

                return false;
            }

            return true;
        }

        private void FindDoors()
        {
            shipDoors.Clear();
            airlockDoors.Clear();

            foreach (var behaviour in ship.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour.GetType().Name != "VattalusDoorController")
                    continue;

                shipDoors.Add(behaviour);

                if (IsAirlockDoor(behaviour))
                    airlockDoors.Add(behaviour);
            }
        }

        private bool IsAirlockDoor(MonoBehaviour door)
        {
            if (door == null)
                return false;

            if (HasAirlockName(door.transform))
                return true;

            var type = door.GetType();

            foreach (var fieldName in new[] { "connectedRoom1", "connectedRoom2" })
            {
                var room = type.GetField(fieldName)?.GetValue(door) as Component;

                if (room != null && HasAirlockName(room.transform))
                    return true;
            }

            return false;
        }

        private bool HasAirlockName(Transform current)
        {
            while (current != null && current.IsChildOf(ship.transform))
            {
                if (current.name.Contains("Airlock", StringComparison.OrdinalIgnoreCase))
                    return true;

                current = current.parent;
            }

            return false;
        }

        private MonoBehaviour ClosestClosedDoor(Vector3 position)
        {
            return shipDoors
                .Where(door => door != null && !IsDoorOpen(door))
                .OrderBy(door => Vector3.SqrMagnitude(door.transform.position - position))
                .FirstOrDefault();
        }

        private MonoBehaviour ClosestAirlockDoor(Vector3 position)
        {
            return airlockDoors
                .Where(door => door != null)
                .OrderBy(door => Vector3.SqrMagnitude(door.transform.position - position))
                .FirstOrDefault();
        }

        private bool IsDoorOpen(MonoBehaviour door)
        {
            var method = door?.GetType().GetMethod("isDoorOpen");
            return method != null && (bool)method.Invoke(door, null);
        }

        private bool OpenDoor(MonoBehaviour door)
        {
            if (door == null)
                return false;

            var method = door.GetType().GetMethod("OpenDoor", new[] { typeof(bool) });

            if (method == null)
                return false;

            method.Invoke(door, new object[] { true });
            return true;
        }

        private void UpdateInteraction()
        {
            if (interactionPrompt == null || player == null || HasExited || Time.timeScale <= 0f)
            {
                interactionPrompt?.SetActive(false);
                return;
            }

            var station = CurrentTutorialStation();

            if (station != null &&
                Vector3.Distance(player.position, station.Position) <= StationInteractionRange)
            {
                interactionPrompt.SetActive(true);

                if (tutorialStep == 3 && !WeaponCheckComplete())
                {
                    interactionPromptText.text = "READY ROOM  |  SWITCH, AIM, FIRE AND RELOAD";
                    return;
                }

                interactionPromptText.text = $"[E]  {station.Action}";

                if (Input.GetKeyDown(KeyCode.E))
                    UseTutorialStation(station);

                return;
            }

            var airlock = ClosestAirlockDoor(player.position);

            if (airlock != null &&
                Vector3.Distance(player.position, airlock.transform.position) <= DoorInteractionRange)
            {
                interactionPrompt.SetActive(true);

                if (AirlockOpened)
                {
                    interactionPromptText.text = "[E]  EXIT SHIP  |  STEP 2 OF 2";

                    if (Input.GetKeyDown(KeyCode.E))
                        Disembark();

                    return;
                }

                if (!TutorialComplete)
                {
                    interactionPromptText.text = WakeSequenceComplete
                        ? "AIRLOCK LOCKED  |  COMPLETE SHIP RECOVERY"
                        : "AIRLOCK LOCKED  |  MIMIR PROTOCOL ACTIVE";
                    return;
                }

                interactionPromptText.text = "[E]  OPEN MAIN AIRLOCK  |  STEP 1 OF 2";

                if (Input.GetKeyDown(KeyCode.E))
                    OpenAirlock();

                return;
            }

            var focused = FindFocusedInteractable();

            if (focused != null)
            {
                var focusedDoor = FindDoorForInteractable(focused);
                var focusedAirlock = focusedDoor != null && airlockDoors.Contains(focusedDoor);
                interactionPrompt.SetActive(true);

                if (focusedAirlock && !TutorialComplete)
                {
                    interactionPromptText.text = "AIRLOCK LOCKED  |  COMPLETE SHIP RECOVERY";
                    return;
                }

                interactionPromptText.text = $"[E]  {InteractableLabel(focused, focusedDoor)}";

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (focusedDoor != null)
                        ToggleDoor(focusedDoor);
                    else
                        InvokeInteractable(focused);
                }

                return;
            }

            var door = ClosestClosedDoor(player.position);

            if (door == null || Vector3.Distance(player.position, door.transform.position) > DoorInteractionRange)
            {
                interactionPrompt.SetActive(false);
                return;
            }

            var isAirlock = airlockDoors.Contains(door);
            interactionPrompt.SetActive(true);

            if (isAirlock && !TutorialComplete)
            {
                interactionPromptText.text = "AIRLOCK LOCKED  |  COMPLETE DECK DRILL";
                return;
            }

            interactionPromptText.text = isAirlock ? "[E]  OPEN AIRLOCK" : "[E]  OPEN DOOR";

            if (!Input.GetKeyDown(KeyCode.E))
                return;

            if (isAirlock)
                OpenAirlock();
            else
                OpenDoor(door);
        }

        private MonoBehaviour FindFocusedInteractable()
        {
            if (tutorialView == null)
                return null;

            var hits = Physics.RaycastAll(
                    tutorialView.position,
                    tutorialView.forward,
                    DoorInteractionRange,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                .OrderBy(hit => hit.distance);

            foreach (var hit in hits)
            {
                if (hit.transform == null ||
                    hit.transform == player ||
                    hit.transform.IsChildOf(player))
                {
                    continue;
                }

                var interactable = hit.collider.GetComponentsInParent<MonoBehaviour>(true)
                    .FirstOrDefault(component =>
                        component != null &&
                        component.GetType().Name == "VattalusInteractable");

                if (interactable == null)
                    return null;

                var seatField = interactable.GetType().GetField("isSeat");

                if (seatField != null && (bool)seatField.GetValue(interactable))
                    return null;

                return interactable;
            }

            return null;
        }

        private MonoBehaviour FindDoorForInteractable(MonoBehaviour interactable)
        {
            return interactable.GetComponentsInParent<MonoBehaviour>(true)
                .FirstOrDefault(component =>
                    component != null &&
                    component.GetType().Name == "VattalusDoorController");
        }

        private string InteractableLabel(MonoBehaviour interactable, MonoBehaviour door)
        {
            if (door != null)
                return IsDoorOpen(door) ? "CLOSE DOOR" : "OPEN DOOR";

            var type = interactable.GetType();
            var activeProperty = type.GetProperty("isActivated");
            var active = activeProperty != null && (bool)activeProperty.GetValue(interactable);
            var fieldName = active ? "deactivateText" : "activateText";
            var text = type.GetField(fieldName)?.GetValue(interactable) as string;
            return string.IsNullOrWhiteSpace(text) ? "INTERACT" : text.ToUpperInvariant();
        }

        private void InvokeInteractable(MonoBehaviour interactable)
        {
            var method = interactable.GetType().GetMethod(
                "Interact",
                new[] { typeof(bool), typeof(bool) });
            method?.Invoke(interactable, new object[] { false, true });
        }

        private void ToggleDoor(MonoBehaviour door)
        {
            var methodName = IsDoorOpen(door) ? "CloseDoor" : "OpenDoor";
            var method = door.GetType().GetMethod(methodName, new[] { typeof(bool) });
            method?.Invoke(door, new object[] { true });
        }

        private bool CanOpenAirlock()
        {
            if (HasExited || AirlockOpened || !TutorialComplete || player == null || Time.timeScale <= 0f)
                return false;

            var door = ClosestAirlockDoor(player.position);
            return door != null &&
                   Vector3.Distance(player.position, door.transform.position) <= DoorInteractionRange;
        }

        private void UpdateTutorial()
        {
            if (player == null || HasExited)
                return;

            var flatTravel = Vector3.ProjectOnPlane(player.position - tutorialStartPosition, Vector3.up);
            moved |= flatTravel.sqrMagnitude >= 4f;
            var view = tutorialView != null ? tutorialView.rotation : player.rotation;
            looked |= Quaternion.Angle(tutorialStartViewRotation, view) >= 14f ||
                      Quaternion.Angle(tutorialStartRotation, player.rotation) >= 14f;
            switchedWeapon |= Input.GetKeyDown(KeyCode.Alpha1) ||
                              Input.GetKeyDown(KeyCode.Alpha2) ||
                              Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f;
            aimed |= Input.GetMouseButton(1);
            fired |= Input.GetMouseButtonDown(0);
            reloaded |= Input.GetKeyDown(KeyCode.R);
            var previousStep = tutorialStep;

            if (tutorialStep == 0 && moved && looked)
                tutorialStep = 1;

            if (tutorialStep == 5 && WakeSequenceComplete)
                tutorialStep = 6;

            if (tutorialStep != previousStep)
                UpdateTutorialPanel();
            else
                UpdateTutorialPanel();
        }

        private void UpdateTutorialPanel()
        {
            if (tutorialPanel == null || tutorialText == null)
                return;

            tutorialPanel.SetActive(!HasExited);
            var movement = tutorialStep > 0 ? "[x]" : "[ ]";
            var bridge = tutorialStations.Count > 0 && tutorialStations[0].Used ? "[x]" : "[ ]";
            var medbay = tutorialStations.Count > 1 && tutorialStations[1].Used ? "[x]" : "[ ]";
            var weapons = tutorialStations.Count > 2 && tutorialStations[2].Used ? "[x]" : "[ ]";
            var engineering = tutorialStations.Count > 3 && tutorialStations[3].Used ? "[x]" : "[ ]";
            var airlock = AirlockOpened
                ? "AIRLOCK OPEN: PRESS E AGAIN TO EXIT"
                : TutorialComplete
                    ? "AIRLOCK READY: PRESS E TO OPEN, THEN E AGAIN TO EXIT"
                    : "AIRLOCK LOCKED UNTIL ALL STEPS ARE COMPLETE";
            tutorialText.text =
                $"<color=#65E5F2><b>FENRIS EVACUATION</b></color>  <color=#A9BAC0>{CurrentStepLabel()}</color>\n" +
                $"<color=#FFFFFF><b>{CurrentObjectiveText()}</b></color>\n" +
                $"<color=#D8E3E6>{CurrentControlHint()}</color>\n\n" +
                $"<color=#AFC3C8>{movement} MOVE   {bridge} BRIDGE   {medbay} MEDBAY</color>\n" +
                $"<color=#AFC3C8>{weapons} WEAPONS   {engineering} POWER</color>\n" +
                $"<color=#FFD36A><b>{airlock}</b></color>";
        }

        private TutorialStation CurrentTutorialStation()
        {
            var index = tutorialStep - 1;
            return index >= 0 && index < tutorialStations.Count
                ? tutorialStations[index]
                : null;
        }

        private void UseTutorialStation(TutorialStation station)
        {
            if (station == null || station != CurrentTutorialStation())
                return;

            if (tutorialStep == 3 && !WeaponCheckComplete())
                return;

            station.Used = true;
            tutorialStep++;
            UpdateTutorialPanel();
        }

        private bool WeaponCheckComplete()
        {
            return switchedWeapon && aimed && fired && reloaded;
        }

        private string CurrentObjectiveText()
        {
            if (tutorialStep == 0)
                return "CURRENT: MOVE AT LEAST 2M AND TURN THE CAMERA";

            var station = CurrentTutorialStation();

            if (station != null)
            {
                if (tutorialStep == 3 && !WeaponCheckComplete())
                    return $"{NavigationText("READY ROOM", station.Position)}";

                return NavigationText(station.Title.ToUpperInvariant(), station.Position);
            }

            if (tutorialStep == 5)
                return WakeSequenceComplete
                    ? NavigationText("MAIN AIRLOCK", airlockApproachPosition)
                    : "CURRENT: WAIT FOR MIMIR TO FINISH THE RECOVERY MESSAGE";

            return AirlockOpened
                ? "CURRENT: STAND AT THE AIRLOCK AND PRESS E TO EXIT"
                : NavigationText("MAIN AIRLOCK", airlockApproachPosition);
        }

        private string CurrentStepLabel()
        {
            if (AirlockOpened)
                return "EXIT STEP 2/2";

            if (tutorialStep >= 6)
                return "EXIT STEP 1/2";

            return $"TUTORIAL STEP {Mathf.Clamp(tutorialStep + 1, 1, 6)}/6";
        }

        private string CurrentControlHint()
        {
            if (tutorialStep == 0)
                return "USE WASD TO MOVE  |  MOVE THE MOUSE TO LOOK";

            if (tutorialStep == 3 && !WeaponCheckComplete())
            {
                var weapon = switchedWeapon ? "[x]" : "[ ]";
                var aim = aimed ? "[x]" : "[ ]";
                var fire = fired ? "[x]" : "[ ]";
                var reload = reloaded ? "[x]" : "[ ]";
                return $"1/2 OR WHEEL {weapon}  RMB AIM {aim}  LMB FIRE {fire}  R RELOAD {reload}";
            }

            if (tutorialStep >= 1 && tutorialStep <= 4)
                return "FOLLOW THE DIRECTION ABOVE  |  PRESS E WHEN THE PROMPT APPEARS";

            if (tutorialStep == 5 && !WakeSequenceComplete)
                return "THE AIRLOCK WILL UNLOCK AFTER THE MESSAGE AND ALL CHECKS";

            return AirlockOpened
                ? "PRESS E AGAIN TO LEAVE THE FENRIS AND ENTER THE OLD INDUSTRY MAP"
                : "AT THE AIRLOCK PRESS E TO OPEN IT, THEN PRESS E AGAIN TO EXIT";
        }

        private string NavigationText(string targetName, Vector3 target)
        {
            if (player == null)
                return targetName;

            var direction = Vector3.ProjectOnPlane(target - player.position, Vector3.up);
            var distance = direction.magnitude;

            if (distance < 1.5f)
                return $"YOU ARE AT {targetName}";

            var view = tutorialView != null ? tutorialView : player;
            var local = view.InverseTransformDirection(direction.normalized);
            var heading = local.z < -0.35f
                ? "BEHIND"
                : Mathf.Abs(local.x) > 0.4f
                    ? local.x < 0f ? "LEFT" : "RIGHT"
                    : "AHEAD";
            return $"GO TO {targetName}: {heading}, {distance:0}M AWAY";
        }

        private void BuildTutorialStations()
        {
            tutorialStations.Clear();

            CreateTutorialStation(
                "BRIDGE NAVIGATION CONSOLE",
                "SYNC BRIDGE NAVIGATION LINK",
                FindArea("Bridge") ?? interiorSpawnAnchor);
            CreateTutorialStation(
                "MEDBAY DIAGNOSTIC CONSOLE",
                "RUN MEDBAY SUIT DIAGNOSTIC",
                FindArea("Medbay") ?? interiorSpawnAnchor);
            CreateTutorialStation(
                "READY ROOM WEAPON CONSOLE",
                "CONFIRM READY ROOM LOADOUT",
                FindArea("ReadyRoom") ?? interiorSpawnAnchor);
            CreateTutorialStation(
                "ENGINEERING POWER CONSOLE",
                "RESTORE AUXILIARY POWER",
                FindArea("Engineering") ?? interiorSpawnAnchor);
        }

        private void CreateTutorialStation(string title, string action, Transform area)
        {
            if (area == null)
                return;

            var floorPosition = FindWalkablePosition(area);
            tutorialStations.Add(new TutorialStation(
                title,
                action,
                floorPosition));
        }

        private void ProtectInterior()
        {
            if (player == null || HasExited || Time.unscaledTime < departureCheckAt)
                return;

            if (IsInInteriorVolume(player.position))
            {
                outsideInteriorSince = -1f;

                if (Time.unscaledTime >= nextSafeInteriorSampleAt)
                {
                    lastSafeInteriorPosition = player.position;
                    nextSafeInteriorSampleAt = Time.unscaledTime + 0.4f;
                }

                return;
            }

            if (outsideInteriorSince < 0f)
            {
                outsideInteriorSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - outsideInteriorSince < 0.8f)
                return;

            MovePlayer(lastSafeInteriorPosition, player.rotation);
            player.GetComponent<CentralPlayerGrounding>()?.RefreshSafePosition(lastSafeInteriorPosition);
            outsideInteriorSince = -1f;
        }

        private bool IsInInteriorVolume(Vector3 position)
        {
            if (TutorialComplete &&
                airlockAnchor != null &&
                Vector3.Distance(position, airlockAnchor.position) <= 8f)
            {
                return true;
            }

            if (interiorVolumes.Count == 0)
                return ExpandedShipBounds().Contains(position);

            foreach (var volume in interiorVolumes)
            {
                if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy)
                    continue;

                var bounds = volume.bounds;
                bounds.Expand(0.45f);

                if (bounds.Contains(position))
                    return true;
            }

            return false;
        }

        private Vector3 FindMapEntryPosition()
        {
            var outward = AirlockOutwardDirection();
            var start = airlockAnchor != null ? airlockAnchor.position : shipBounds.center;
            var candidate = start;

            for (var step = 0; step < 40 && ExpandedShipBounds().Contains(candidate); step++)
                candidate += outward * 2f;

            candidate += outward * 3f;
            var side = Vector3.Cross(Vector3.up, outward).normalized;
            var offsets = new[] { 0f, 3f, -3f, 6f, -6f, 9f, -9f };

            foreach (var offset in offsets)
            {
                var sample = candidate + side * offset;

                foreach (var terrain in Terrain.activeTerrains)
                {
                    if (terrain == null || terrain.terrainData == null)
                        continue;

                    var origin = terrain.transform.position;
                    var size = terrain.terrainData.size;

                    if (sample.x < origin.x || sample.x > origin.x + size.x ||
                        sample.z < origin.z || sample.z > origin.z + size.z)
                    {
                        continue;
                    }

                    sample.y = terrain.SampleHeight(sample) + origin.y + 1.02f;

                    if (IsCapsuleClear(sample))
                        return sample;
                }
            }

            return mapSpawn;
        }

        private Vector3 AirlockOutwardDirection()
        {
            var anchor = airlockAnchor != null ? airlockAnchor.position : shipBounds.center + ship.transform.right;
            var direction = Vector3.ProjectOnPlane(anchor - shipBounds.center, Vector3.up);

            if (direction.sqrMagnitude < 0.01f)
                direction = ship.transform.right;

            return direction.normalized;
        }

        private Vector3 MapEntryDirection()
        {
            var direction = Vector3.ProjectOnPlane(mapSpawn - mapEntryPosition, Vector3.up);
            return direction.sqrMagnitude < 0.01f
                ? AirlockOutwardDirection()
                : direction.normalized;
        }

        private void FindNarrativeDirector()
        {
            if (narrativeDirector == null)
                narrativeDirector = FindFirstObjectByType<NarrativeDirector>();
        }

        private void ConfigureSky()
        {
            var shader = Shader.Find("Skybox/Procedural");

            if (shader == null)
                return;

            skyMaterial = new Material(shader)
            {
                name = "YggdrasilOvercastSky"
            };
            skyMaterial.SetColor("_SkyTint", new Color(0.22f, 0.28f, 0.34f, 1f));
            skyMaterial.SetColor("_GroundColor", new Color(0.07f, 0.08f, 0.085f, 1f));
            skyMaterial.SetFloat("_AtmosphereThickness", 0.72f);
            skyMaterial.SetFloat("_Exposure", 0.72f);
            skyMaterial.SetFloat("_SunSize", 0.025f);
            RenderSettings.skybox = skyMaterial;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.68f;
            DynamicGI.UpdateEnvironment();
        }

        private void EnsurePlayerCameraUsesSky()
        {
            if (player == null)
                return;

            foreach (var camera in player.GetComponentsInChildren<Camera>(true))
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            }
        }

        private void BuildTutorialInterface()
        {
            var canvasObject = new GameObject("FenrisTutorialCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ??
                       TMP_Settings.defaultFontAsset;

            tutorialPanel = new GameObject("FenrisTutorialPanel", typeof(Image));
            tutorialPanel.transform.SetParent(canvasObject.transform, false);
            var tutorialRect = tutorialPanel.GetComponent<RectTransform>();
            tutorialRect.anchorMin = new Vector2(0f, 1f);
            tutorialRect.anchorMax = new Vector2(0f, 1f);
            tutorialRect.pivot = new Vector2(0f, 1f);
            tutorialRect.anchoredPosition = new Vector2(22f, -22f);
            tutorialRect.sizeDelta = new Vector2(680f, 248f);
            tutorialPanel.GetComponent<Image>().color = new Color(0.012f, 0.022f, 0.03f, 0.86f);
            var accentObject = new GameObject("FenrisTutorialAccent", typeof(Image));
            accentObject.transform.SetParent(tutorialPanel.transform, false);
            var accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(5f, 0f);
            accentObject.GetComponent<Image>().color = new Color(0.2f, 0.88f, 0.94f, 1f);
            var tutorialTextObject = new GameObject("TutorialText", typeof(TextMeshProUGUI));
            tutorialTextObject.transform.SetParent(tutorialPanel.transform, false);
            tutorialText = tutorialTextObject.GetComponent<TextMeshProUGUI>();
            tutorialText.font = font;
            tutorialText.fontSize = 17f;
            tutorialText.enableAutoSizing = true;
            tutorialText.fontSizeMin = 14f;
            tutorialText.fontSizeMax = 17f;
            tutorialText.fontStyle = FontStyles.Normal;
            tutorialText.alignment = TextAlignmentOptions.TopLeft;
            tutorialText.color = new Color(0.94f, 0.97f, 0.98f, 1f);
            tutorialText.lineSpacing = 5f;
            tutorialText.outlineColor = new Color(0f, 0f, 0f, 0.88f);
            tutorialText.outlineWidth = 0.055f;
            tutorialText.raycastTarget = false;
            var tutorialTextRect = tutorialText.rectTransform;
            tutorialTextRect.anchorMin = Vector2.zero;
            tutorialTextRect.anchorMax = Vector2.one;
            tutorialTextRect.offsetMin = new Vector2(18f, 14f);
            tutorialTextRect.offsetMax = new Vector2(-18f, -14f);

            interactionPrompt = new GameObject("OpenAirlockPrompt", typeof(Image));
            interactionPrompt.transform.SetParent(canvasObject.transform, false);
            var panel = interactionPrompt.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -150f);
            panel.sizeDelta = new Vector2(520f, 52f);
            interactionPrompt.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.035f, 0.9f);
            var textObject = new GameObject("PromptText", typeof(TextMeshProUGUI));
            textObject.transform.SetParent(interactionPrompt.transform, false);
            interactionPromptText = textObject.GetComponent<TextMeshProUGUI>();
            interactionPromptText.font = font;
            interactionPromptText.fontSize = 22f;
            interactionPromptText.fontStyle = FontStyles.Bold;
            interactionPromptText.alignment = TextAlignmentOptions.Center;
            interactionPromptText.color = new Color(0.84f, 0.97f, 1f, 1f);
            interactionPromptText.raycastTarget = false;
            var textRect = interactionPromptText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 5f);
            textRect.offsetMax = new Vector2(-12f, -5f);
            interactionPrompt.SetActive(false);
            tutorialPanel.SetActive(false);
        }

        private void MovePlayer(Vector3 position, Quaternion rotation)
        {
            if (player == null)
                return;

            var controller = player.GetComponent<CharacterController>();
            var wasEnabled = controller != null && controller.enabled;

            if (wasEnabled)
                controller.enabled = false;

            player.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();

            if (wasEnabled)
                controller.enabled = true;
        }

        private Bounds ExpandedShipBounds()
        {
            var bounds = shipBounds;
            bounds.Expand(new Vector3(2f, 3f, 2f));
            return bounds;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null)
                .ToArray();

            if (renderers.Length == 0)
                return new Bounds(root.transform.position, new Vector3(18f, 12f, 70f));

            var bounds = renderers[0].bounds;

            for (var idx = 1; idx < renderers.Length; idx++)
                bounds.Encapsulate(renderers[idx].bounds);

            return bounds;
        }

        private static void EnsureVattalusContext(Scene scene)
        {
            var controllerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("VattalusSceneController", false))
                .FirstOrDefault(type => type != null);

            if (controllerType == null)
                return;

            var existing = Resources.FindObjectsOfTypeAll(controllerType)
                .OfType<Component>()
                .FirstOrDefault(component => component != null && component.gameObject.scene == scene);

            if (existing != null)
                return;

            var go = new GameObject("FenrisInteractionContext");
            SceneManager.MoveGameObjectToScene(go, scene);
            var controller = go.AddComponent(controllerType) as Behaviour;

            if (controller != null)
                controller.enabled = false;
        }

        private sealed class TutorialStation
        {
            public TutorialStation(
                string title,
                string action,
                Vector3 position)
            {
                Title = title;
                Action = action;
                Position = position;
            }

            public string Title { get; }
            public string Action { get; }
            public Vector3 Position { get; }
            public bool Used { get; set; }
        }
    }
}
