using System;
using System.Collections.Generic;
using System.Linq;
using Akila.FPSFramework;
using DontDiePlease.Systems;
using UnityEngine;
using UnityEngine.AI;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralCombatSpawner : MonoBehaviour
    {
        [SerializeField] private int fallbackSeed = 2026;
        [SerializeField] private float firstWaveDelay = 1.4f;
        [SerializeField] private float waveDelay = 5f;
        [SerializeField] private float minPlayerSpawnDistance = 14f;
        [SerializeField] private int maxActiveEnemies = 12;

        private readonly List<CentralCombatEnemy> activeEnemies = new List<CentralCombatEnemy>();
        private readonly List<Vector3> spawnZones = new List<Vector3>();
        private readonly List<GameObject> spawnedPickups = new List<GameObject>();
        private System.Random rng;
        private Transform player;
        private GameObject[] pickupPrefabs = Array.Empty<GameObject>();
        private int wave;
        private float nextWaveTimer;
        private bool encounterActive;
        private bool automaticWavesEnabled = true;

        public int CurrentWave => wave;
        public int ActiveEnemyCount => activeEnemies.Count(enemy => enemy != null && !enemy.IsDead);
        public int Seed { get; private set; }
        public bool IsConfigured { get; private set; }
        public event Action<int, int> CombatStateChanged;

        public void Configure(Transform playerTarget, GameObject[] pickups)
        {
            player = playerTarget;
            pickupPrefabs = pickups?.Where(x => x != null).ToArray() ?? Array.Empty<GameObject>();
            Seed = ResolveSeed();
            rng = new System.Random(Seed);
            BuildSpawnZones();
            PlacePickups();
            nextWaveTimer = firstWaveDelay;
            IsConfigured = true;
            encounterActive = automaticWavesEnabled;
            CombatStateChanged?.Invoke(wave, ActiveEnemyCount);
        }

        private void Update()
        {
            if (!encounterActive || player == null)
                return;

            activeEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);

            if (activeEnemies.Count > 0)
            {
                CombatStateChanged?.Invoke(wave, ActiveEnemyCount);
                return;
            }

            nextWaveTimer -= Time.deltaTime;

            if (nextWaveTimer > 0f)
                return;

            SpawnNextWave();
            nextWaveTimer = waveDelay + Mathf.Min(wave * 0.45f, 4f);
        }

        public void SpawnNextWave()
        {
            wave++;

            var configs = BuildWave(wave);
            var spawnCount = Mathf.Min(configs.Count, maxActiveEnemies);

            for (var idx = 0; idx < spawnCount; idx++)
            {
                var position = PickSpawnPosition(idx);
                SpawnEnemy(configs[idx], position);
            }

            CombatStateChanged?.Invoke(wave, ActiveEnemyCount);
        }

        public void SetAutomaticWaves(bool enabled, bool clearExisting)
        {
            automaticWavesEnabled = enabled;
            encounterActive = enabled && IsConfigured;

            if (clearExisting)
            {
                ClearActiveEnemies();
            }

            CombatStateChanged?.Invoke(wave, ActiveEnemyCount);
        }

        public void SetPlayerTarget(Transform target)
        {
            player = target;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || enemy.IsDead)
                    continue;

                var ai = enemy.GetComponent<CentralCombatEnemyAI>();
                ai?.SetTarget(target);
            }
        }

        public CentralCombatEnemy SpawnEncounterEnemy(CentralCombatEnemyConfig config, Vector3 position)
        {
            if (!IsConfigured || config == null || player == null)
            {
                return null;
            }

            if (!NavMesh.SamplePosition(position, out var hit, 12f, NavMesh.AllAreas))
                return null;

            var enemy = SpawnEnemy(config, hit.position);
            CombatStateChanged?.Invoke(wave, ActiveEnemyCount);
            return enemy;
        }

        public void ClearActiveEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
        }

        private List<CentralCombatEnemyConfig> BuildWave(int waveIdx)
        {
            var configs = new List<CentralCombatEnemyConfig>();

            if (waveIdx <= 1)
            {
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Shooter());
                configs.Add(CentralCombatEnemyConfig.Heavy());
                configs.Add(CentralCombatEnemyConfig.Stalker());
                return Shuffle(configs);
            }

            if (waveIdx == 2)
            {
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Shooter());
                configs.Add(CentralCombatEnemyConfig.Shooter());
                configs.Add(CentralCombatEnemyConfig.Heavy());
                configs.Add(CentralCombatEnemyConfig.Stalker());
                return Shuffle(configs);
            }

            var total = Mathf.Clamp(6 + waveIdx, 7, maxActiveEnemies);

            for (var idx = 0; idx < total; idx++)
            {
                var roll = rng.NextDouble();

                if (roll < 0.36)
                    configs.Add(CentralCombatEnemyConfig.Rusher());
                else if (roll < 0.62)
                    configs.Add(CentralCombatEnemyConfig.Shooter());
                else if (roll < 0.84)
                    configs.Add(CentralCombatEnemyConfig.Heavy());
                else
                    configs.Add(CentralCombatEnemyConfig.Stalker());
            }

            if (!configs.Any(config => config.archetype == CentralEnemyArchetype.Heavy))
                configs[configs.Count - 1] = CentralCombatEnemyConfig.Heavy();

            return Shuffle(configs);
        }

        private List<CentralCombatEnemyConfig> Shuffle(List<CentralCombatEnemyConfig> values)
        {
            for (var idx = values.Count - 1; idx > 0; idx--)
            {
                var swap = rng.Next(0, idx + 1);
                (values[idx], values[swap]) = (values[swap], values[idx]);
            }

            return values;
        }

        private CentralCombatEnemy SpawnEnemy(CentralCombatEnemyConfig config, Vector3 position)
        {
            if (!NavMesh.SamplePosition(position, out var hit, 8f, NavMesh.AllAreas))
                return null;

            position = hit.position;
            var go = new GameObject($"Enemy_{config.displayName}");
            go.transform.SetParent(transform, true);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, NextFloat(0f, 360f), 0f));

            go.AddComponent<Damageable>();
            go.AddComponent<Actor>();
            var agent = go.AddComponent<NavMeshAgent>();
            go.AddComponent<CapsuleCollider>();

            CentralCombatVisuals.BuildEnemyBody(go.transform, config);
            var muzzle = CentralCombatVisuals.BuildProjectileMuzzle(go.transform, config);

            var enemy = go.AddComponent<CentralCombatEnemy>();
            enemy.Configure(config);
            enemy.enabled = true;

            agent.Warp(position);

            var ai = go.AddComponent<CentralCombatEnemyAI>();
            ai.Configure(enemy, player, muzzle);
            ai.enabled = true;
            activeEnemies.Add(enemy);
            return enemy;
        }

        private Vector3 PickSpawnPosition(int offset)
        {
            if (spawnZones.Count == 0)
                BuildSpawnZones();

            var hasPlayerPosition = TryGetPlayerNavPosition(out var playerPosition);

            for (var attempt = 0; attempt < 36; attempt++)
            {
                var zone = hasPlayerPosition && attempt < 24
                    ? playerPosition
                    : spawnZones[(rng.Next(0, spawnZones.Count) + offset) % spawnZones.Count];
                var minRadius = hasPlayerPosition && attempt < 24 ? minPlayerSpawnDistance : 4f;
                var maxRadius = hasPlayerPosition && attempt < 24 ? minPlayerSpawnDistance + 24f : 14f;
                var scatter = RandomCircle(minRadius, maxRadius);
                var pos = zone + new Vector3(scatter.x, 0f, scatter.y);

                if (player != null && Vector3.Distance(pos, player.position) < minPlayerSpawnDistance)
                    continue;

                if (NavMesh.SamplePosition(pos, out var hit, 12f, NavMesh.AllAreas) &&
                    IsReachableSpawn(hit.position) &&
                    IsSeparatedSpawn(hit.position))
                {
                    return hit.position;
                }
            }

            for (var idx = 0; idx < spawnZones.Count; idx++)
            {
                var zone = spawnZones[(idx + offset) % spawnZones.Count];

                if (IsReachableSpawn(zone) && IsSeparatedSpawn(zone))
                    return zone;
            }

            if (hasPlayerPosition)
            {
                for (var idx = 0; idx < 8; idx++)
                {
                    var direction = Quaternion.Euler(0f, idx * 45f, 0f) * Vector3.forward;
                    var candidate = playerPosition + direction * minPlayerSpawnDistance;

                    if (NavMesh.SamplePosition(candidate, out var hit, 18f, NavMesh.AllAreas) &&
                        IsSeparatedSpawn(hit.position))
                    {
                        return hit.position;
                    }
                }

                return playerPosition;
            }

            return NavMesh.SamplePosition(transform.position, out var fallback, 100f, NavMesh.AllAreas)
                ? fallback.position
                : transform.position;
        }

        private bool IsReachableSpawn(Vector3 position)
        {
            if (!TryGetPlayerNavPosition(out var playerPosition) ||
                Mathf.Abs(position.y - playerPosition.y) > 12f)
                return false;

            var path = new NavMeshPath();
            return NavMesh.CalculatePath(position, playerPosition, NavMesh.AllAreas, path) &&
                   path.status == NavMeshPathStatus.PathComplete;
        }

        private bool IsSeparatedSpawn(Vector3 position)
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && !enemy.IsDead &&
                    Vector3.SqrMagnitude(enemy.transform.position - position) < 9f)
                {
                    return false;
                }
            }

            return true;
        }

        private void BuildSpawnZones()
        {
            spawnZones.Clear();

            if (TryGetPlayerNavPosition(out var playerPosition))
            {
                for (var idx = 0; idx < 8; idx++)
                {
                    var direction = Quaternion.Euler(0f, idx * 45f, 0f) * Vector3.forward;
                    AddZone(playerPosition + direction * 24f);
                }
            }

            var bounds = CalculateSceneBounds();
            var center = bounds.center;
            var x = Mathf.Clamp(bounds.extents.x * 0.55f, 18f, 60f);
            var z = Mathf.Clamp(bounds.extents.z * 0.55f, 18f, 60f);

            AddZone(center + new Vector3(-x, 0f, -z * 0.45f));
            AddZone(center + new Vector3(x, 0f, -z * 0.35f));
            AddZone(center + new Vector3(-x * 0.7f, 0f, z * 0.55f));
            AddZone(center + new Vector3(x * 0.7f, 0f, z * 0.55f));
            AddZone(center + new Vector3(0f, 0f, z * 0.78f));
            AddZone(center + new Vector3(0f, 0f, -z * 0.78f));

            if (spawnZones.Count == 0)
            {
                spawnZones.Add(transform.position + new Vector3(18f, 0f, 0f));
                spawnZones.Add(transform.position + new Vector3(-18f, 0f, 0f));
            }
        }

        private void AddZone(Vector3 candidate)
        {
            if (NavMesh.SamplePosition(candidate, out var hit, 18f, NavMesh.AllAreas) &&
                spawnZones.All(zone => Vector3.SqrMagnitude(zone - hit.position) > 4f))
            {
                spawnZones.Add(hit.position);
            }
        }

        private bool TryGetPlayerNavPosition(out Vector3 position)
        {
            if (player != null &&
                NavMesh.SamplePosition(player.position, out var hit, 30f, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            position = default;
            return false;
        }

        private Bounds CalculateSceneBounds()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
                .Where(r => r != null && r.enabled && r.gameObject.scene == gameObject.scene)
                .ToArray();

            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, new Vector3(72f, 18f, 72f));

            var bounds = renderers[0].bounds;

            for (var idx = 1; idx < renderers.Length; idx++)
            {
                if (renderers[idx].GetComponentInParent<Canvas>() != null)
                    continue;

                bounds.Encapsulate(renderers[idx].bounds);
            }

            return bounds;
        }

        private void PlacePickups()
        {
            if (pickupPrefabs.Length == 0)
                return;

            for (var idx = 0; idx < 7; idx++)
            {
                var position = PickPickupPosition(idx);
                var prefab = pickupPrefabs[rng.Next(0, pickupPrefabs.Length)];
                var pickup = Instantiate(prefab, position + Vector3.up * 0.15f, Quaternion.Euler(0f, NextFloat(0f, 360f), 0f));
                pickup.name = $"CombatPickup_{prefab.name}";
                spawnedPickups.Add(pickup);
            }
        }

        private Vector3 PickPickupPosition(int idx)
        {
            var angle = idx * 51.428f + NextFloat(-12f, 12f);
            var radius = NextFloat(8f, 34f);
            var candidate = transform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;

            if (NavMesh.SamplePosition(candidate, out var hit, 15f, NavMesh.AllAreas))
                return hit.position;

            return transform.position + new Vector3(idx * 2f, 0f, 4f);
        }

        private int ResolveSeed()
        {
            if (GameSeedManager.Instance != null)
            {
                var stream = GameSeedManager.Instance.CreateRandomStream("central-combat-v1");
                return stream.Next(1, int.MaxValue);
            }

            return fallbackSeed;
        }

        private Vector2 RandomCircle(float minRadius, float maxRadius)
        {
            var angle = NextFloat(0f, Mathf.PI * 2f);
            var radius = NextFloat(minRadius, maxRadius);
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        private float NextFloat(float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
