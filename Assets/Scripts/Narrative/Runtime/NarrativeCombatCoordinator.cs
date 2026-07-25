using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DontDiePlease.Central.Combat;
using DontDiePlease.Systems;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeCombatCoordinator : MonoBehaviour
    {
        private const string CentralCombatSceneName = "Central_Combat";
        private const string DemoCombatSceneName = "Demo_Combat";
        private const string FirstRobotAnchorId = "first-robot-spawn";
        private const string WardenAnchorId = "warden-k-spawn";
        private const string DefenseCenterAnchorId = "signal-defense-center";
        private const string DefenseEncounterId = "SIGNAL_DEFENSE";
        private readonly List<CentralCombatEnemy> waveEnemies = new List<CentralCombatEnemy>();
        private readonly Dictionary<CentralCombatEnemy, Action<CentralCombatEnemy>> waveDeathHandlers =
            new Dictionary<CentralCombatEnemy, Action<CentralCombatEnemy>>();
        private readonly Dictionary<string, NarrativeSpawnAnchor> anchors =
            new Dictionary<string, NarrativeSpawnAnchor>(StringComparer.Ordinal);
        private readonly List<NarrativeSpawnAnchor> defenseAnchors = new List<NarrativeSpawnAnchor>();
        private NarrativeDirector director;
        private NarrativeCombatBindings bindings;
        private string sceneName;
        private Transform firstRobotAnchor;
        private EnemyHealth firstRobot;
        private CentralCombatSpawner centralSpawner;
        private CentralCombatEnemy warden;
        private System.Random defenseRandom;
        private NarrativeSpawnAnchor defenseCenter;
        private string previousDefenseAnchorId;
        private float nextWaveAt;
        private bool defenseWasActive;

        public bool IsReady { get; private set; }

        public void Configure(NarrativeDirector narrativeDirector, NarrativeCombatBindings combatBindings)
        {
            director = narrativeDirector;
            bindings = combatBindings;
            sceneName = SceneManager.GetActiveScene().name;
            IsReady = false;
            director.SequenceCompleted += HandleSequenceCompleted;
            StartCoroutine(Initialize());
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SequenceCompleted -= HandleSequenceCompleted;
            }

            UnbindFirstRobot();
            UnbindWarden();
            ClearWaveEnemies();
        }

        private IEnumerator Initialize()
        {
            while (director != null && !director.IsReady)
            {
                yield return null;
            }

            if (director == null || bindings == null)
            {
                Debug.LogError("Narrative combat bindings are unavailable.", this);
                yield break;
            }

            CacheAnchors();

            if (sceneName == DemoCombatSceneName)
            {
                yield return InitializeCentralCombat();
                InitializeFirstRobot();
                InitializeDefense();
            }
            else if (sceneName == CentralCombatSceneName)
            {
                yield return InitializeCentralCombat();
            }

            IsReady = true;
        }

        private void Update()
        {
            if (sceneName != DemoCombatSceneName || director == null || !director.IsReady || bindings == null)
            {
                return;
            }

            UpdateDefenseWaves();
        }

        private void InitializeFirstRobot()
        {
            if (bindings.FirstRobotPrefab == null)
            {
                Debug.LogError("The first robot prefab is missing from NarrativeCombatBindings.", this);
                return;
            }

            if (!TryGetAnchor(
                    FirstRobotAnchorId,
                    NarrativeAnchorKind.FirstRobotSpawn,
                    out var anchor))
            {
                return;
            }

            firstRobotAnchor = anchor.transform;

            if (NavMesh.SamplePosition(firstRobotAnchor.position, out var hit, 20f, NavMesh.AllAreas))
            {
                firstRobotAnchor.position = hit.position;
            }

            anchor.gameObject.SetActive(false);

            if (director.State.HasFlag("first_robot_seen") &&
                !director.State.HasFlag("first_robot_defeated"))
            {
                SpawnFirstRobot();
            }
        }

        private void SpawnFirstRobot()
        {
            if (firstRobot != null ||
                firstRobotAnchor == null ||
                director.State.HasFlag("first_robot_defeated"))
            {
                return;
            }

            var position = firstRobotAnchor.position;

            if (!NavMesh.SamplePosition(position, out var hit, bindings.FirstRobotNavMeshRadius, NavMesh.AllAreas))
            {
                Debug.LogError(
                    $"Narrative anchor '{FirstRobotAnchorId}' in {sceneName} is farther than {bindings.FirstRobotNavMeshRadius:0.#} metres from NavMesh.",
                    this);
                return;
            }

            var robotObject = Instantiate(bindings.FirstRobotPrefab, hit.position, firstRobotAnchor.rotation);
            robotObject.name = "NarrativeFirstRobot";
            CentralCombatVisuals.ReplaceEnemyBody(
                robotObject.transform,
                CentralEnemyArchetype.Stalker,
                1.65f);
            firstRobot = robotObject.GetComponent<EnemyHealth>();

            if (firstRobot == null)
            {
                Debug.LogError("The configured first robot prefab has no EnemyHealth component.", robotObject);
                Destroy(robotObject);
                return;
            }

            if (robotObject.GetComponent<EnemyHealthDamageAdapter>() == null)
            {
                robotObject.AddComponent<EnemyHealthDamageAdapter>();
            }

            firstRobot.OnDied += HandleFirstRobotDied;
        }

        private void HandleFirstRobotDied()
        {
            UnbindFirstRobot();

            if (!director.State.HasFlag("mechanical_component"))
            {
                director.State.SetFlag("mechanical_component");
                director.State.MarkChanged();
                director.PersistProgress(true);
            }

            director.RaiseStoryEvent("TRG_FIRST_ROBOT_DEFEATED");
        }

        private void UnbindFirstRobot()
        {
            if (firstRobot != null)
            {
                firstRobot.OnDied -= HandleFirstRobotDied;
                firstRobot = null;
            }
        }

        private IEnumerator InitializeCentralCombat()
        {
            for (var attempt = 0; attempt < 80; attempt++)
            {
                centralSpawner = FindAnyObjectByType<CentralCombatSpawner>();

                if (centralSpawner != null && centralSpawner.IsConfigured)
                {
                    break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }

            if (centralSpawner == null || !centralSpawner.IsConfigured)
            {
                Debug.LogError($"Central combat spawner is unavailable in {sceneName}.", this);
                yield break;
            }

            var wardenActive = director.State.HasFlag("warden_k_engaged") &&
                               !director.State.HasFlag("component_core");
            var runWaves = sceneName == DemoCombatSceneName &&
                           !wardenActive &&
                           !director.State.signalDefenseActive;
            centralSpawner.SetAutomaticWaves(runWaves, true);

            if (wardenActive)
            {
                SpawnWarden();
            }
        }

        private void SpawnWarden()
        {
            if (warden != null || director.State.HasFlag("component_core"))
            {
                return;
            }

            if (centralSpawner == null)
            {
                var spawners = FindObjectsByType<CentralCombatSpawner>(FindObjectsInactive.Include);

                foreach (var spawner in spawners)
                {
                    if (spawner != null && spawner.gameObject.scene.name == sceneName && spawner.IsConfigured)
                    {
                        centralSpawner = spawner;
                        break;
                    }
                }
            }

            if (centralSpawner == null || !centralSpawner.IsConfigured)
            {
                return;
            }

            centralSpawner.SetAutomaticWaves(false, true);

            if (!TryGetAnchor(WardenAnchorId, NarrativeAnchorKind.WardenSpawn, out var anchor))
            {
                return;
            }

            if (!NavMesh.SamplePosition(anchor.transform.position, out var hit, 12f, NavMesh.AllAreas))
            {
                Debug.LogError(
                    $"Narrative anchor '{WardenAnchorId}' in {sceneName} is farther than 12 metres from NavMesh.",
                    this);
                return;
            }

            var config = CentralCombatEnemyConfig.Boss();
            warden = centralSpawner.SpawnEncounterEnemy(config, hit.position);

            if (warden == null)
            {
                Debug.LogError("Warden-K could not be spawned by the Central combat system.", this);
                return;
            }

            warden.Died += HandleWardenDied;
        }

        private void HandleWardenDied(CentralCombatEnemy enemy)
        {
            UnbindWarden();
            director.RaiseStoryEvent("TRG_COMPONENT_CORE");
        }

        private void UnbindWarden()
        {
            if (warden != null)
            {
                warden.Died -= HandleWardenDied;
                warden = null;
            }
        }

        private void InitializeDefense()
        {
            var seedManager = GameSeedManager.Instance;
            defenseRandom = seedManager != null
                ? seedManager.CreateRandomStream("narrative-defense-v1")
                : new System.Random(director.State.worldSeed);
            defenseCenter = null;
            defenseAnchors.Clear();
            previousDefenseAnchorId = null;

            TryGetAnchor(
                DefenseCenterAnchorId,
                NarrativeAnchorKind.DefenseCenter,
                out defenseCenter);

            if (defenseCenter != null &&
                NavMesh.SamplePosition(defenseCenter.transform.position, out var centerHit, 20f, NavMesh.AllAreas))
            {
                defenseCenter.transform.position = centerHit.position;
            }

            var authoredDefenseAnchors = anchors.Values
                .Where(item =>
                    item.Kind == NarrativeAnchorKind.DefenseEnemySpawn &&
                    item.EncounterId == DefenseEncounterId)
                .OrderBy(item => item.AnchorId, StringComparer.Ordinal)
                .ToArray();

            for (var idx = 0; idx < authoredDefenseAnchors.Length; idx++)
            {
                var anchor = authoredDefenseAnchors[idx];

                if (TryAlignDefenseAnchor(anchor, idx))
                {
                    defenseAnchors.Add(anchor);
                }
                else
                {
                    Debug.LogWarning(
                        $"Narrative anchor '{anchor.AnchorId}' in {sceneName} could not be aligned to reachable NavMesh.",
                        anchor);
                }
            }

            if (defenseAnchors.Count == 0)
            {
                Debug.LogError(
                    $"No usable '{DefenseEncounterId}' defence enemy anchors were found in {sceneName}.",
                    this);
            }

            nextWaveAt = Time.unscaledTime + 2f;
            defenseWasActive = director.State.signalDefenseActive;

            if (defenseWasActive)
                centralSpawner?.SetAutomaticWaves(false, true);
        }

        private bool TryAlignDefenseAnchor(NarrativeSpawnAnchor anchor, int index)
        {
            if (defenseCenter == null)
            {
                return false;
            }

            if (TryFindReachablePosition(anchor.transform.position, 20f, out var authoredPosition) &&
                IsSeparateDefensePosition(authoredPosition))
            {
                anchor.transform.position = authoredPosition;
                return true;
            }

            for (var ring = 0; ring < 4; ring++)
            {
                var radius = 19f + ring * 2f;

                for (var step = 0; step < 24; step++)
                {
                    var angle = (index * 90f + step * 15f) * Mathf.Deg2Rad;
                    var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;

                    if (!TryFindReachablePosition(
                            defenseCenter.transform.position + offset,
                            5f,
                            out var position) ||
                        !IsSeparateDefensePosition(position))
                    {
                        continue;
                    }

                    anchor.transform.position = position;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindReachablePosition(Vector3 candidate, float radius, out Vector3 position)
        {
            position = default;

            if (!NavMesh.SamplePosition(candidate, out var hit, radius, NavMesh.AllAreas))
            {
                return false;
            }

            var path = new NavMeshPath();

            if (!NavMesh.CalculatePath(
                    hit.position,
                    defenseCenter.transform.position,
                    NavMesh.AllAreas,
                    path) ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            var distance = Vector3.Distance(hit.position, defenseCenter.transform.position);

            if (distance < 16f || distance > 30f)
            {
                return false;
            }

            position = hit.position;
            return true;
        }

        private bool IsSeparateDefensePosition(Vector3 position)
        {
            return defenseAnchors.All(anchor =>
                Vector3.Distance(anchor.transform.position, position) >= 6f);
        }

        private void UpdateDefenseWaves()
        {
            var active = director.State.signalDefenseActive;

            if (!active)
            {
                if (defenseWasActive)
                {
                    ClearWaveEnemies();
                }

                defenseWasActive = false;
                return;
            }

            if (!defenseWasActive)
            {
                defenseWasActive = true;
                nextWaveAt = Time.unscaledTime + 2f;
                centralSpawner?.SetAutomaticWaves(false, true);
            }

            PruneWaveEnemies();

            if (director.State.signalDefensePaused || Time.timeScale <= 0f || Time.unscaledTime < nextWaveAt)
            {
                return;
            }

            var progress = Mathf.Clamp01(
                1f - director.State.signalDefenseRemainingSeconds / NarrativeDefenseTimeline.DurationSeconds);
            var phase = bindings.FindDefensePhase(progress);

            if (phase == null || waveEnemies.Count >= phase.MaxActiveEnemies)
            {
                return;
            }

            var available = phase.MaxActiveEnemies - waveEnemies.Count;
            var count = Mathf.Min(phase.EnemiesPerWave, available);

            for (var idx = 0; idx < count; idx++)
            {
                SpawnDefenseEnemy(phase, idx);
            }

            nextWaveAt = Time.unscaledTime + phase.SpawnIntervalSeconds;
        }

        private void SpawnDefenseEnemy(NarrativeDefenseWavePhase phase, int offset)
        {
            if (centralSpawner == null || !centralSpawner.IsConfigured)
                return;

            var player = FindPlayer();

            if (player == null || !TryFindDefenseSpawn(player, phase, offset, out var position))
            {
                return;
            }

            var archetype = PickDefenseArchetype(phase, offset);
            var config = BuildDefenseConfig(archetype);
            var enemy = centralSpawner.SpawnEncounterEnemy(config, position);

            if (enemy == null)
                return;

            enemy.gameObject.name = "SignalDefenseRobot";
            Action<CentralCombatEnemy> handler = HandleWaveEnemyDied;
            waveDeathHandlers[enemy] = handler;
            enemy.Died += handler;
            waveEnemies.Add(enemy);
        }

        private CentralEnemyArchetype PickDefenseArchetype(
            NarrativeDefenseWavePhase phase,
            int offset)
        {
            if (phase.MaxActiveEnemies >= 7 && offset == 0)
                return CentralEnemyArchetype.Heavy;

            var roll = defenseRandom.NextDouble();

            if (roll < 0.42)
                return CentralEnemyArchetype.Rusher;

            if (roll < 0.74)
                return CentralEnemyArchetype.Shooter;

            return CentralEnemyArchetype.Stalker;
        }

        private static CentralCombatEnemyConfig BuildDefenseConfig(CentralEnemyArchetype archetype)
        {
            switch (archetype)
            {
                case CentralEnemyArchetype.Heavy:
                    return CentralCombatEnemyConfig.Heavy();
                case CentralEnemyArchetype.Shooter:
                    return CentralCombatEnemyConfig.Shooter();
                case CentralEnemyArchetype.Stalker:
                    return CentralCombatEnemyConfig.Stalker();
                default:
                    return CentralCombatEnemyConfig.Rusher();
            }
        }

        private bool TryFindDefenseSpawn(
            Transform player,
            NarrativeDefenseWavePhase phase,
            int offset,
            out Vector3 position)
        {
            if (defenseCenter == null || defenseAnchors.Count == 0)
            {
                position = default;
                return false;
            }

            var candidates = defenseAnchors
                .Where(anchor =>
                {
                    var distance = Vector3.Distance(anchor.transform.position, defenseCenter.transform.position);
                    return distance >= phase.MinSpawnRadius &&
                           distance <= phase.MaxSpawnRadius &&
                           Vector3.Distance(anchor.transform.position, player.position) >= 12f;
                })
                .ToList();

            if (candidates.Count == 0)
            {
                position = default;
                return false;
            }

            var selected = SelectDefenseAnchor(candidates, offset);

            if (selected == null ||
                !NavMesh.SamplePosition(selected.transform.position, out var hit, 20f, NavMesh.AllAreas))
            {
                position = default;
                return false;
            }

            var path = new NavMeshPath();

            if (!NavMesh.CalculatePath(
                    hit.position,
                    defenseCenter.transform.position,
                    NavMesh.AllAreas,
                    path) ||
                path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogError(
                    $"Narrative anchor '{selected.AnchorId}' in {sceneName} has no complete NavMesh path to '{DefenseCenterAnchorId}'.",
                    selected);
                position = default;
                return false;
            }

            previousDefenseAnchorId = selected.AnchorId;
            position = hit.position;
            return true;
        }

        private NarrativeSpawnAnchor SelectDefenseAnchor(
            List<NarrativeSpawnAnchor> candidates,
            int offset)
        {
            var available = candidates.Count > 1
                ? candidates.Where(item => item.AnchorId != previousDefenseAnchorId).ToList()
                : candidates;
            var totalWeight = available.Sum(item => item.SpawnWeight);
            var selection = (float)defenseRandom.NextDouble() * totalWeight;

            for (var idx = 0; idx < available.Count; idx++)
            {
                var candidate = available[(idx + offset) % available.Count];
                selection -= candidate.SpawnWeight;

                if (selection <= 0f)
                {
                    return candidate;
                }
            }

            return available[available.Count - 1];
        }

        private void CacheAnchors()
        {
            anchors.Clear();
            var duplicateIds = new HashSet<string>(StringComparer.Ordinal);
            var sceneAnchors = FindObjectsByType<NarrativeSpawnAnchor>(FindObjectsInactive.Include)
                .Where(anchor =>
                    anchor != null &&
                    anchor.gameObject.scene.name == sceneName)
                .ToArray();

            foreach (var anchor in sceneAnchors)
            {
                if (string.IsNullOrWhiteSpace(anchor.AnchorId))
                {
                    Debug.LogError(
                        $"A NarrativeSpawnAnchor in {sceneName} has an empty anchor ID.",
                        anchor);
                    continue;
                }

                if (duplicateIds.Contains(anchor.AnchorId))
                {
                    continue;
                }

                if (anchors.ContainsKey(anchor.AnchorId))
                {
                    anchors.Remove(anchor.AnchorId);
                    duplicateIds.Add(anchor.AnchorId);
                    Debug.LogError(
                        $"Duplicate narrative anchor ID '{anchor.AnchorId}' exists in {sceneName}.",
                        anchor);
                    continue;
                }

                anchors.Add(anchor.AnchorId, anchor);
            }
        }

        private bool TryGetAnchor(
            string anchorId,
            NarrativeAnchorKind kind,
            out NarrativeSpawnAnchor anchor)
        {
            if (!anchors.TryGetValue(anchorId, out anchor))
            {
                Debug.LogError(
                    $"Narrative anchor '{anchorId}' is missing from {sceneName}.",
                    this);
                return false;
            }

            if (anchor.Kind == kind)
            {
                return true;
            }

            Debug.LogError(
                $"Narrative anchor '{anchorId}' in {sceneName} is '{anchor.Kind}' but must be '{kind}'.",
                anchor);
            anchor = null;
            return false;
        }

        private void HandleWaveEnemyDied(CentralCombatEnemy enemy)
        {
            UnbindWaveEnemy(enemy);
        }

        private void PruneWaveEnemies()
        {
            for (var idx = waveEnemies.Count - 1; idx >= 0; idx--)
            {
                var enemy = waveEnemies[idx];

                if (enemy == null || enemy.IsDead)
                {
                    UnbindWaveEnemy(enemy);
                }
            }
        }

        private void ClearWaveEnemies()
        {
            for (var idx = waveEnemies.Count - 1; idx >= 0; idx--)
            {
                var enemy = waveEnemies[idx];
                UnbindWaveEnemy(enemy);

                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            waveEnemies.Clear();
            waveDeathHandlers.Clear();
        }

        private void UnbindWaveEnemy(CentralCombatEnemy enemy)
        {
            if (enemy != null && waveDeathHandlers.TryGetValue(enemy, out var handler))
            {
                enemy.Died -= handler;
                waveDeathHandlers.Remove(enemy);
            }

            waveEnemies.Remove(enemy);
        }

        private static Transform FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }

        private void HandleSequenceCompleted(string sequenceId)
        {
            switch (sequenceId)
            {
                case "TRG_FIRST_ROBOT":
                    SpawnFirstRobot();
                    break;
                case "TRG_BOSS_WARDEN_K":
                    SpawnWarden();
                    break;
            }
        }
    }
}
