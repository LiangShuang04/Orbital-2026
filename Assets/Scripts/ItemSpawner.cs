using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Scatters item pickups around this object for the player to collect.
/// Unlike RandomSpawner (which needs a finished prefab per item), this builds
/// each pickup at runtime straight from an ItemData asset: it uses the item's
/// worldPrefab for the look if there is one, otherwise a small fallback cube,
/// then attaches an ItemPickup so SelectionManager (look + E) can grab it.
///
/// Spawn points are snapped onto the NavMesh (walkable ground); if no NavMesh is
/// baked it falls back to a downward ground raycast. Drop a few of these around
/// the map, fill in the loot table, and press Play.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Tooltip("Random quantity per spawn is picked between these (inclusive).")]
        [Min(1)] public int minQuantity = 1;
        [Min(1)] public int maxQuantity = 1;
        [Tooltip("Relative chance of being chosen. Higher = more common.")]
        [Min(0f)] public float weight = 1f;
    }

    [Header("What to spawn")]
    [Tooltip("Loot table — a weighted-random entry is picked for each spawn.")]
    [SerializeField] private List<LootEntry> lootTable = new List<LootEntry>();

    [Header("Where")]
    [Tooltip("Radius around this object that pickups can appear in.")]
    [SerializeField] private float spawnRadius = 20f;
    [Tooltip("Lift pickups slightly so they rest on the surface instead of sinking in.")]
    [SerializeField] private float yOffset = 0.25f;
    [Tooltip("Layers treated as ground for the raycast fallback when no NavMesh is baked.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("How many / how often")]
    [Tooltip("Max pickups alive from this spawner at once (picked-up ones free a slot).")]
    [SerializeField] private int maxAlive = 12;
    [Tooltip("Seconds between top-up spawns once below the cap.")]
    [SerializeField] private float spawnInterval = 4f;
    [Tooltip("Fill up to the cap immediately on start instead of trickling in.")]
    [SerializeField] private bool spawnAtStart = true;

    [Header("Fallback visual")]
    [Tooltip("Edge size of the cube used when an item has no worldPrefab.")]
    [SerializeField] private float fallbackCubeSize = 0.5f;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private float nextSpawnTime;

    void Start()
    {
        if (spawnAtStart)
            for (int i = 0; i < maxAlive; i++) TrySpawnOne();
    }

    void Update()
    {
        // picked-up (destroyed) pickups become null, freeing a slot
        spawned.RemoveAll(o => o == null);

        if (spawned.Count >= maxAlive) return;
        if (Time.time < nextSpawnTime) return;

        nextSpawnTime = Time.time + spawnInterval;
        TrySpawnOne();
    }

    void TrySpawnOne()
    {
        var entry = PickWeightedEntry();
        if (entry == null) return;
        if (!TryGetSpawnPoint(out var pos)) return;

        int max = Mathf.Max(entry.minQuantity, entry.maxQuantity);
        int qty = Random.Range(entry.minQuantity, max + 1);

        var pickup = BuildPickup(entry.item, qty);
        pickup.transform.SetPositionAndRotation(
            pos + Vector3.up * yOffset,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        spawned.Add(pickup);
    }

    // Weighted random over the loot table. Total is recomputed each call so it
    // stays correct even if the table is tweaked at runtime.
    LootEntry PickWeightedEntry()
    {
        float total = 0f;
        foreach (var e in lootTable)
            if (e != null && e.item != null) total += Mathf.Max(0f, e.weight);

        if (total <= 0f) return null;

        float roll = Random.value * total;
        foreach (var e in lootTable)
        {
            if (e == null || e.item == null) continue;
            roll -= Mathf.Max(0f, e.weight);
            if (roll <= 0f) return e;
        }
        return null;
    }

    // Builds a world pickup the player can grab (look + E via SelectionManager).
    GameObject BuildPickup(ItemData item, int quantity)
    {
        GameObject go;
        if (item.worldPrefab != null)
        {
            go = Instantiate(item.worldPrefab);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = Vector3.one * fallbackCubeSize;
        }
        go.name = "Pickup_" + item.itemName;

        // SelectionManager raycasts against solid (non-trigger) colliders, so make
        // sure at least one exists — otherwise the pickup can't be highlighted/grabbed.
        if (!HasSolidCollider(go))
            go.AddComponent<BoxCollider>();

        var pickup = go.GetComponent<ItemPickup>() ?? go.AddComponent<ItemPickup>();
        pickup.itemData = item;
        pickup.quantity = Mathf.Max(1, quantity);
        return go;
    }

    static bool HasSolidCollider(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>())
            if (!c.isTrigger) return true;
        return false;
    }

    // Random point in the radius, placed on walkable ground.
    bool TryGetSpawnPoint(out Vector3 result)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(circle.x, 0f, circle.y);

            // Preferred: snap to the nearest walkable NavMesh position.
            if (NavMesh.SamplePosition(candidate, out var navHit, 6f, NavMesh.AllAreas))
            {
                result = navHit.position;
                return true;
            }

            // Fallback (no NavMesh baked): cast down from above to find ground.
            Vector3 top = candidate + Vector3.up * 50f;
            if (Physics.Raycast(top, Vector3.down, out var groundHit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            {
                result = groundHit.point;
                return true;
            }
        }
        result = transform.position;
        return false;
    }

    // shows the spawn radius in the editor when the spawner is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
