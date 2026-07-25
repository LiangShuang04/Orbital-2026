using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Display")]
    public string itemName = "New Item";
    [TextArea] public string description;
    public Sprite icon;

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 50;

    [Header("Category")]
    public ItemType type;

    [Header("Dropping")]
    [Tooltip("Object spawned when this item is dropped. Leave empty for a simple cube")]
    public GameObject worldPrefab;
}

public enum ItemType
{
    Resource,
    Consumable,
    Tool,
    Key
}


