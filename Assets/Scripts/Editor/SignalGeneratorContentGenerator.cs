#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click content for the win loop. Menu: Tools ▸ Don't Die Please ▸ Generate Signal Generator Loop.
/// Creates the missing Omphalite input, the crafted Signal Generator Core output,
/// and the CraftingRecipe (Omphalite + Aetherite + Ionvein + Corelith -> Core),
/// wired to the crystal ItemData already in Assets/ItemData/Crystals/.
/// Safe to re-run: refreshes fields, keeps any icon/worldPrefab you've assigned.
/// </summary>
public static class SignalGeneratorContentGenerator
{
    const string CrystalsFolder = "Assets/ItemData/Crystals";
    const string CraftedFolder = "Assets/ItemData/Crafted";
    const string RecipesFolder = "Assets/ItemData/Recipes";

    [MenuItem("Tools/Don't Die Please/Generate Signal Generator Loop")]
    static void Generate()
    {
        EnsureFolder(CraftedFolder);
        EnsureFolder(RecipesFolder);

        // 1) Omphalite — the rare input the recipe needs (was missing)
        var omphalite = LoadOrCreateItem($"{CrystalsFolder}/Omphalite.asset", i =>
        {
            i.itemName = "Omphalite";
            i.description = "An iridescent alloy-crystal left behind by the vanished Omphalos. Yggdrasil's rarest mineral, and the key to its highest technology.";
            i.type = ItemType.Resource;
            i.isStackable = true;
            i.maxStackSize = 20;
        });

        // 2) Signal Generator Core — the crafted output / win part
        var core = LoadOrCreateItem($"{CraftedFolder}/SignalGeneratorCore.asset", i =>
        {
            i.itemName = "Signal Generator Core";
            i.description = "A working distress-beacon core assembled from Omphalos crystal-tech. Install it in the signal generator to call for rescue.";
            i.type = ItemType.Tool;
            i.isStackable = false;
            i.maxStackSize = 1;
        });

        // 3) The recipe
        var aetherite = LoadItem($"{CrystalsFolder}/Aetherite.asset");
        var ionvein = LoadItem($"{CrystalsFolder}/Ionvein.asset");
        var corelith = LoadItem($"{CrystalsFolder}/Corelith.asset");

        string recipePath = $"{RecipesFolder}/SignalGeneratorCore.asset";
        var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(recipePath);
        bool newRecipe = recipe == null;
        if (newRecipe) recipe = ScriptableObject.CreateInstance<CraftingRecipe>();

        recipe.recipeName = "Signal Generator Core";
        recipe.ingredients = new List<ItemStack>
        {
            Stack(omphalite, 1), Stack(aetherite, 1), Stack(ionvein, 1), Stack(corelith, 1)
        };
        recipe.results = new List<ItemStack> { Stack(core, 1) };

        if (newRecipe) AssetDatabase.CreateAsset(recipe, recipePath);
        else EditorUtility.SetDirty(recipe);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SignalGeneratorContentGenerator] Ready — Omphalite, Signal Generator Core, and the recipe in {RecipesFolder}. " +
                  "Add the recipe to a CraftingStation, and set the SignalGenerator's Required Parts to 1x Signal Generator Core.");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = recipe;
    }

    static ItemStack Stack(ItemData item, int qty) => new ItemStack { item = item, quantity = qty };

    static ItemData LoadItem(string path)
    {
        var a = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (a == null)
            Debug.LogWarning($"[SignalGeneratorContentGenerator] Missing input ItemData at {path} — that ingredient will be blank in the recipe.");
        return a;
    }

    static ItemData LoadOrCreateItem(string path, Action<ItemData> fill)
    {
        EnsureFolder(Path.GetDirectoryName(path).Replace("\\", "/"));
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        bool isNew = item == null;
        if (isNew) item = ScriptableObject.CreateInstance<ItemData>();
        fill(item);
        if (isNew) AssetDatabase.CreateAsset(item, path);
        else EditorUtility.SetDirty(item);
        return item;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
