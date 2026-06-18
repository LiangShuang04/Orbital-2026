using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Minimal inventory display: a panel listing the current stacks as text, toggled
/// with a key. Subscribes to Inventory.OnInventoryChanged so it only rebuilds when
/// the contents actually change. Replace with a slot/icon grid later.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [Tooltip("Panel shown/hidden when the toggle key is pressed.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Text element that lists the inventory contents.")]
    [SerializeField] private TMP_Text contentsText;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        if (inventory != null) inventory.OnInventoryChanged += Refresh;

        if (panel != null) panel.SetActive(false);
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
    }

    void Update()
    {
        if (panel != null && Input.GetKeyDown(toggleKey))
            panel.SetActive(!panel.activeSelf);
    }

    private void Refresh()
    {
        if (contentsText == null || inventory == null) return;

        if (inventory.Slots.Count == 0)
        {
            contentsText.text = "Inventory empty";
            return;
        }

        var sb = new StringBuilder();
        foreach (var slot in inventory.Slots)
        {
            string itemName = slot.item != null ? slot.item.itemName : "Unknown";
            sb.AppendLine($"{itemName}  x{slot.quantity}");
        }
        contentsText.text = sb.ToString();
    }
}
