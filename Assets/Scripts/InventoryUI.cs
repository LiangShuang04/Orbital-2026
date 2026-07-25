using System.Text;
using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private GameObject panel;
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

        var text = new StringBuilder();
        foreach (var slot in inventory.Slots)
        {
            var itemName = slot.item != null ? slot.item.itemName : "Unknown";
            text.AppendLine($"{itemName}  x{slot.quantity}");
        }
        contentsText.text = text.ToString();
    }
}
