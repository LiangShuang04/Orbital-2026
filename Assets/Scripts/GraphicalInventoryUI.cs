using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Graphical inventory panel built entirely in code, so an asset import can never
/// overwrite it. Shows a grid of slots with each item's icon and stack count
/// Tab opens/closes it and unlocks the cursor, click a slot to select it,
/// press the drop key to drop the selected item into the world
/// No editor setup needed, having this script in the project is enough
/// </summary>
public class GraphicalInventoryUI : MonoBehaviour
{
    class Cell
    {
        public Image bg;
        public Image icon;
        public TextMeshProUGUI qty;
        public TextMeshProUGUI nameLabel; // fallback when an item has no icon
    }

    const int SlotCount = 24;          // should match Inventory.maxSlots
    const int Columns = 6;
    const int CellSize = 72;
    const int CellGap = 8;
    const KeyCode ToggleKey = KeyCode.Tab;
    const KeyCode DropKey = KeyCode.Q;

    static readonly Color SlotColor = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color SelectedColor = new Color(0.30f, 0.70f, 1f, 0.55f);

    RectTransform panel;
    readonly List<Cell> cells = new List<Cell>();
    Inventory inventory;
    float nextSearchTime;
    int selectedIndex = -1;

    // builds the panel automatically on start, no scene object required
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<GraphicalInventoryUI>() != null) return;
        var go = new GameObject("GraphicalInventoryUI (auto)");
        go.AddComponent<GraphicalInventoryUI>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        panel.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
    }

    void Update()
    {
        // the player may not exist yet, so keep looking until an Inventory shows up
        if (inventory == null && Time.unscaledTime >= nextSearchTime)
        {
            nextSearchTime = Time.unscaledTime + 0.5f;
            var found = FindObjectOfType<Inventory>();
            if (found != null)
            {
                inventory = found;
                inventory.OnInventoryChanged += Refresh;
                Refresh();
            }
        }

        if (Input.GetKeyDown(ToggleKey)) SetOpen(!panel.gameObject.activeSelf);

        if (panel.gameObject.activeSelf && Input.GetKeyDown(DropKey)) DropSelected();
    }

    // opens/closes the panel and frees the cursor so slots can be clicked
    void SetOpen(bool open)
    {
        panel.gameObject.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        if (open) Refresh();
        else Select(-1);
    }

    void Refresh()
    {
        if (inventory == null) return;
        var slots = inventory.Slots;

        for (var i = 0; i < cells.Count; i++)
        {
            if (i < slots.Count && slots[i].item != null)
            {
                var item = slots[i].item;
                if (item.icon != null)
                {
                    cells[i].icon.sprite = item.icon;
                    cells[i].icon.enabled = true;
                    cells[i].nameLabel.text = "";
                }
                else
                {
                    cells[i].icon.enabled = false;
                    cells[i].nameLabel.text = item.itemName; // no icon, show the name
                }
                cells[i].qty.text = slots[i].quantity > 1 ? slots[i].quantity.ToString() : "";
            }
            else
            {
                cells[i].icon.enabled = false;
                cells[i].nameLabel.text = "";
                cells[i].qty.text = "";
            }
        }

        // deselect if the selected slot is now empty
        if (selectedIndex >= slots.Count) Select(-1);
        else UpdateSelectionVisual();
    }

    void Select(int index)
    {
        selectedIndex = index;
        UpdateSelectionVisual();
    }

    void UpdateSelectionVisual()
    {
        for (var i = 0; i < cells.Count; i++)
            cells[i].bg.color = i == selectedIndex ? SelectedColor : SlotColor;
    }

    // removes one of the selected item and spawns it in front of the player
    void DropSelected()
    {
        if (inventory == null) return;
        var slots = inventory.Slots;
        if (selectedIndex < 0 || selectedIndex >= slots.Count) return;

        var item = slots[selectedIndex].item;
        if (item == null) return;

        inventory.RemoveItem(item, 1); // fires OnInventoryChanged -> Refresh
        SpawnDropped(item);
    }

    void SpawnDropped(ItemData item)
    {
        var cam = Camera.main;
        var pos = cam != null
            ? cam.transform.position + cam.transform.forward * 1.2f
            : transform.position;

        GameObject dropped;
        if (item.worldPrefab != null)
        {
            dropped = Instantiate(item.worldPrefab, pos, Quaternion.identity);
        }
        else
        {
            // fallback so a drop always produces a re-collectable object
            dropped = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dropped.transform.position = pos;
            dropped.transform.localScale = Vector3.one * 0.3f;
        }

        // make sure the dropped object can be picked back up
        var pickup = dropped.GetComponent<ItemPickup>();
        if (pickup == null) pickup = dropped.AddComponent<ItemPickup>();
        pickup.itemData = item;
        pickup.quantity = 1;
    }

    void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600; // above the survival HUD and the FPS framework HUD

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>(); // needed so slots can be clicked

        // centred panel
        var gridWidth = Columns * CellSize + (Columns - 1) * CellGap;
        var rows = Mathf.CeilToInt(SlotCount / (float)Columns);
        var gridHeight = rows * CellSize + (rows - 1) * CellGap;
        var panelWidth = gridWidth + 48;
        var panelHeight = gridHeight + 120;

        panel = NewRect("Panel", transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(panelWidth, panelHeight);
        var panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.08f, 0.92f);
        panelImage.raycastTarget = false;

        // title
        var titleRect = NewRect("Title", panel);
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        var title = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        title.text = "INVENTORY";
        title.fontSize = 22f;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) title.font = TMP_Settings.defaultFontAsset;

        // grid container
        var gridRect = NewRect("Grid", panel);
        gridRect.anchorMin = new Vector2(0.5f, 1f);
        gridRect.anchorMax = new Vector2(0.5f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
        gridRect.anchoredPosition = new Vector2(0f, -60f);
        var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(CellSize, CellSize);
        grid.spacing = new Vector2(CellGap, CellGap);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;

        for (var i = 0; i < SlotCount; i++)
            cells.Add(CreateCell(gridRect, i));

        // hint line at the bottom
        var hintRect = NewRect("Hint", panel);
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(0f, 30f);
        hintRect.anchoredPosition = new Vector2(0f, 10f);
        var hint = hintRect.gameObject.AddComponent<TextMeshProUGUI>();
        hint.text = "Click a slot to select   |   Q to drop   |   Tab to close";
        hint.fontSize = 13f;
        hint.color = new Color(1f, 1f, 1f, 0.6f);
        hint.alignment = TextAlignmentOptions.Center;
        hint.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) hint.font = TMP_Settings.defaultFontAsset;
    }

    Cell CreateCell(RectTransform parent, int index)
    {
        var cellRect = NewRect("Slot", parent);
        var slotBg = cellRect.gameObject.AddComponent<Image>();
        slotBg.color = SlotColor;
        slotBg.raycastTarget = true; // this is what receives the click

        // clicking the slot selects it
        var button = cellRect.gameObject.AddComponent<Button>();
        button.targetGraphic = slotBg;
        var captured = index;
        button.onClick.AddListener(() => Select(captured));

        var iconRect = NewRect("Icon", cellRect);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 6f);
        iconRect.offsetMax = new Vector2(-6f, -6f);
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        var nameRect = NewRect("Name", cellRect);
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(2f, 2f);
        nameRect.offsetMax = new Vector2(-2f, -2f);
        var nameLabel = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
        nameLabel.fontSize = 11f;
        nameLabel.color = Color.white;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.enableWordWrapping = true;
        nameLabel.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) nameLabel.font = TMP_Settings.defaultFontAsset;

        var qtyRect = NewRect("Qty", cellRect);
        qtyRect.anchorMin = Vector2.zero;
        qtyRect.anchorMax = Vector2.one;
        qtyRect.offsetMin = new Vector2(0f, 2f);
        qtyRect.offsetMax = new Vector2(-4f, 0f);
        var qty = qtyRect.gameObject.AddComponent<TextMeshProUGUI>();
        qty.fontSize = 14f;
        qty.color = Color.white;
        qty.alignment = TextAlignmentOptions.BottomRight;
        qty.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) qty.font = TMP_Settings.defaultFontAsset;

        return new Cell { bg = slotBg, icon = icon, qty = qty, nameLabel = nameLabel };
    }

    // clicking UI needs an EventSystem, create one if the scene has none
    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
}
