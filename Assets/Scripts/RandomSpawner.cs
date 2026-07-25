using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [Tooltip("A random one of these is picked each spawn")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Where")]
    [Tooltip("Radius around this object that spawns can appear in")]
    [SerializeField] private float spawnRadius = 20f;
    [Tooltip("Lift the spawn point up a little, useful for items so they sit on the floor")]
    [SerializeField] private float yOffset = 0f;

    [Header("How many / how often")]
    [Tooltip("Max things alive from this spawner at once")]
    [SerializeField] private int maxAlive = 8;
    [Tooltip("Seconds between spawns")]
    [SerializeField] private float spawnInterval = 3f;
    [Tooltip("Spawn a batch immediately on start instead of waiting")]
    [SerializeField] private bool spawnAtStart = true;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private float nextSpawnTime;

    void Start()
    {
        if (spawnAtStart)
            for (var i = 0; i < maxAlive; i++) TrySpawnOne();
    }

    void Update()
    {
        spawned.RemoveAll(o => o == null);

        if (spawned.Count >= maxAlive) return;
        if (Time.time < nextSpawnTime) return;

        nextSpawnTime = Time.time + spawnInterval;
        TrySpawnOne();
    }

    void TrySpawnOne()
    {
        if (prefabs == null || prefabs.Length == 0) return;

        var prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        if (!TryGetSpawnPoint(out var pos)) return;

        var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        var go = Instantiate(prefab, pos + Vector3.up * yOffset, rot);
        spawned.Add(go);
    }

    bool TryGetSpawnPoint(out Vector3 result)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var circle = Random.insideUnitCircle * spawnRadius;
            var candidate = transform.position + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out var hit, 5f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = transform.position;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
