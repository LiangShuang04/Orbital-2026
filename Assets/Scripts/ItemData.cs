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
}

public enum ItemType
{
    Resource, //includes materials and resources
    Consumable, // includes things that can restore health or saturation like medkit or food
    Tool, // weapons and tools
    Key // key items used to unlock the next act
}


