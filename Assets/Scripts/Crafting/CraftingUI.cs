using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Keyboard-driven crafting panel, in the same minimal text style as InventoryUI.
/// A CraftingStation opens it; while open, number keys 1-9 craft the matching
/// recipe and the interact key closes it. Subscribes to Inventory.OnInventoryChanged
/// so availability markers refresh automatically after every craft or pickup.
///
/// Attach to the Canvas (NOT to the panel itself — a disabled panel stops
/// receiving Update, so the close key would never work).
/// </summary>
public class CraftingUI : MonoBehaviour
{
    [Tooltip("Panel shown while a crafting station is in use.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Text element that lists the recipes.")]
    [SerializeField] private TMP_Text contentsText;
    [Tooltip("Key that closes the panel (same as interact feels natural).")]
    [SerializeField] private KeyCode closeKey = KeyCode.E;

    private CraftingStation station;
    private Inventory inventory;
    private int openedFrame; // ignore the close key on the frame we opened (same E press)

    public bool IsOpen => panel != null && panel.activeSelf;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
    }

    /// <summary>Called by a CraftingStation when the player interacts with it.</summary>
    public void Open(CraftingStation newStation, Inventory playerInventory)
    {
        if (panel == null || newStation == null || playerInventory == null) return;

        station = newStation;
        inventory = playerInventory;
        inventory.OnInventoryChanged += Refresh;

        openedFrame = Time.frameCount;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        if (panel != null) panel.SetActive(false);
        station = null;
        inventory = null;
    }

    void Update()
    {
        if (!IsOpen || station == null) return;

        // Same key opens and closes; skip the frame it was opened on so the
        // opening E press doesn't immediately close the panel again.
        if (Input.GetKeyDown(closeKey) && Time.frameCount != openedFrame)
        {
            Close();
            return;
        }

        int count = Mathf.Min(station.Recipes.Count, 9);
        for (int i = 0; i < count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                station.Recipes[i].TryCraft(inventory); // Refresh fires via OnInventoryChanged
        }
    }

    /// <summary>Rebuild the recipe list with per-ingredient have/need counts.</summary>
    private void Refresh()
    {
        if (contentsText == null || station == null || inventory == null) return;

        var sb = new StringBuilder();
        sb.AppendLine(station.GetDisplayName().ToUpper());
        sb.AppendLine();

        if (station.Recipes.Count == 0)
        {
            sb.AppendLine("No recipes available.");
        }

        int count = Mathf.Min(station.Recipes.Count, 9);
        for (int i = 0; i < count; i++)
        {
            var recipe = station.Recipes[i];
            if (recipe == null) continue;

            string status = recipe.CanCraft(inventory) ? "READY" : "MISSING MATERIALS";
            sb.AppendLine($"[{i + 1}] {recipe.DisplayName}  -  {status}");

            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null) continue;
                int have = inventory.GetCount(ing.item);
                sb.AppendLine($"      {ing.item.itemName} {have}/{ing.quantity}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Press 1-9 to craft   |   E to close");
        contentsText.text = sb.ToString();
    }
}
