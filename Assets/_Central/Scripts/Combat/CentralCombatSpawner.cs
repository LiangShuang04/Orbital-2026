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

        public int CurrentWave => wave;
        public int ActiveEnemyCount => activeEnemies.Count(enemy => enemy != null && !enemy.IsDead);
        public int Seed { get; private set; }
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
            encounterActive = true;
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

        private List<CentralCombatEnemyConfig> BuildWave(int waveIdx)
        {
            var configs = new List<CentralCombatEnemyConfig>();

            if (waveIdx <= 1)
            {
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Shooter());
                return Shuffle(configs);
            }

            if (waveIdx == 2)
            {
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Rusher());
                configs.Add(CentralCombatEnemyConfig.Shooter());
                configs.Add(CentralCombatEnemyConfig.Shooter());
                configs.Add(CentralCombatEnemyConfig.Heavy());
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

        private void SpawnEnemy(CentralCombatEnemyConfig config, Vector3 position)
        {
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

            agent.Warp(position);

            var ai = go.AddComponent<CentralCombatEnemyAI>();
            ai.Configure(enemy, player, muzzle);
            activeEnemies.Add(enemy);
        }

        private Vector3 PickSpawnPosition(int offset)
        {
            if (spawnZones.Count == 0)
                BuildSpawnZones();

            for (var attempt = 0; attempt < 24; attempt++)
            {
                var zone = spawnZones[(rng.Next(0, spawnZones.Count) + offset) % spawnZones.Count];
                var scatter = RandomCircle(4f, 14f);
                var pos = zone + new Vector3(scatter.x, 0f, scatter.y);

                if (player != null && Vector3.Distance(pos, player.position) < minPlayerSpawnDistance)
                    continue;

                if (NavMesh.SamplePosition(pos, out var hit, 8f, NavMesh.AllAreas))
                    return hit.position;
            }

            var fallback = player != null ? player.position + player.forward * 18f : transform.position + Vector3.forward * 18f;

            if (NavMesh.SamplePosition(fallback, out var fallbackHit, 20f, NavMesh.AllAreas))
                return fallbackHit.position;

            return fallback;
        }

        private void BuildSpawnZones()
        {
            spawnZones.Clear();

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
            if (NavMesh.SamplePosition(candidate, out var hit, 18f, NavMesh.AllAreas))
                spawnZones.Add(hit.position);
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
