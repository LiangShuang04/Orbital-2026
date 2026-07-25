using System;
using System.Collections.Generic;
using UnityEngine;
// Alias the two clashing "Inventory" types so this file stays readable and the
// bare name "Inventory" always means OUR survival inventory.
using AkilaInventory = Akila.FPSFramework.Inventory;
using AkilaInventoryItem = Akila.FPSFramework.InventoryItem;

/// <summary>
/// Reflects the weapons held in Akila's Inventory into our survival Inventory so
/// they appear in the Tab UI next to crystals and consumables.
///
/// Akila stays the authority for guns (viewmodel, aim, fire, switching) — this
/// only mirrors them for display. It works no matter how a weapon entered Akila
/// (Pickable pickup, start item, script), because it just diffs Akila's live
/// item list against what we've already shown.
///
/// Put this on the player root (the object that has our survival Inventory).
/// </summary>
public class AkilaInventoryBridge : MonoBehaviour
{
    [Serializable]
    public class WeaponMapping
    {
        [Tooltip("Must match the Akila InventoryItem's 'Name' field (on the gun prefab).")]
        public string akilaItemName;
        [Tooltip("ItemData shown in the survival inventory for that weapon — gives it an icon/description.")]
        public ItemData itemData;
    }

    [Header("References (auto-found if left empty)")]
    [Tooltip("Our survival Inventory (the one Tab opens).")]
    [SerializeField] private Inventory survivalInventory;
    [Tooltip("Akila's Inventory (usually on a child of the player).")]
    [SerializeField] private AkilaInventory akilaInventory;

    [Header("Display")]
    [Tooltip("Optional: give specific guns a proper icon/name. Unmapped guns fall back to their Akila Name as text.")]
    [SerializeField] private List<WeaponMapping> weaponMappings = new List<WeaponMapping>();

    [Tooltip("Seconds between re-syncs. 0 = every frame.")]
    [SerializeField] private float syncInterval = 0.25f;

    // Which Akila weapon instance is currently represented by which ItemData in our inventory.
    private readonly Dictionary<AkilaInventoryItem, ItemData> mirrored = new Dictionary<AkilaInventoryItem, ItemData>();
    // ItemData we created on the fly (unmapped guns) — kept so we can destroy them on drop.
    private readonly Dictionary<AkilaInventoryItem, ItemData> runtimeData = new Dictionary<AkilaInventoryItem, ItemData>();
    private readonly List<AkilaInventoryItem> toRemove = new List<AkilaInventoryItem>();
    private float nextSync;

    private void Awake()
    {
        if (survivalInventory == null)
            survivalInventory = GetComponentInParent<Inventory>() ?? GetComponentInChildren<Inventory>();
        if (akilaInventory == null)
            akilaInventory = GetComponentInParent<AkilaInventory>() ?? GetComponentInChildren<AkilaInventory>();
    }

    private void Update()
    {
        if (survivalInventory == null || akilaInventory == null) return;
        if (Time.time < nextSync) return;
        nextSync = Time.time + Mathf.Max(0f, syncInterval);
        Sync();
    }

    private void Sync()
    {
        var current = akilaInventory.items; // live list, rebuilt by Akila each frame

        // Additions: weapons Akila now holds that we haven't shown yet.
        if (current != null)
        {
            foreach (var weapon in current)
            {
                if (weapon == null || mirrored.ContainsKey(weapon)) continue;

                var data = ResolveItemData(weapon);
                if (data != null && survivalInventory.AddItem(data, 1))
                    mirrored.Add(weapon, data);
            }
        }

        // Removals: weapons we showed that Akila no longer holds (dropped / destroyed).
        toRemove.Clear();
        foreach (var kv in mirrored)
            if (kv.Key == null || current == null || !current.Contains(kv.Key))
                toRemove.Add(kv.Key);

        foreach (var weapon in toRemove)
        {
            survivalInventory.RemoveItem(mirrored[weapon], 1);
            mirrored.Remove(weapon);

            if (runtimeData.TryGetValue(weapon, out var rd))
            {
                Destroy(rd);
                runtimeData.Remove(weapon);
            }
        }
    }

    private ItemData ResolveItemData(AkilaInventoryItem weapon)
    {
        // 1) An explicit mapping wins (lets you assign a real icon).
        foreach (var m in weaponMappings)
            if (m != null && m.itemData != null && m.akilaItemName == weapon.Name)
                return m.itemData;

        // 2) Otherwise build a throwaway ItemData carrying just the weapon's name.
        if (!runtimeData.TryGetValue(weapon, out var data))
        {
            data = ScriptableObject.CreateInstance<ItemData>();
            data.itemName = string.IsNullOrEmpty(weapon.Name) ? weapon.name : weapon.Name;
            data.description = "A weapon carried in your hands (handled by the firearm system).";
            data.type = ItemType.Tool;
            data.isStackable = false;
            data.maxStackSize = 1;
            runtimeData[weapon] = data;
        }
        return data;
    }
}
