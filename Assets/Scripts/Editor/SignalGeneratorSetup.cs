#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click: builds a fully-configured Signal Generator prefab into
/// Resources/SurvivalWorld/ so the combat bootstrapper spawns it automatically.
/// The generator requires 2 Ashenite + 1 Blightbloom; interacting with it while
/// carrying those consumes them, powers it on, and triggers the WinScreen.
/// Menu: Tools ▸ Don't Die Please ▸ Build Signal Generator (2 Ashenite + 1 Blightbloom).
/// </summary>
public static class SignalGeneratorSetup
{
    const string CrystalsFolder = "Assets/ItemData/Crystals";
    const string PrefabFolder = "Assets/Resources/SurvivalWorld";
    const string PrefabPath = "Assets/Resources/SurvivalWorld/SignalGenerator.prefab";

    [MenuItem("Tools/Don't Die Please/Build Signal Generator (2 Ashenite + 1 Blightbloom)")]
    static void Build()
    {
        var ashenite = AssetDatabase.LoadAssetAtPath<ItemData>($"{CrystalsFolder}/Ashenite.asset");
        var blightbloom = AssetDatabase.LoadAssetAtPath<ItemData>($"{CrystalsFolder}/Blightbloom.asset");
        if (ashenite == null || blightbloom == null)
        {
            Debug.LogError($"[SignalGeneratorSetup] Missing Ashenite/Blightbloom in {CrystalsFolder}. Aborting.");
            return;
        }

        EnsureFolder(PrefabFolder);

        // Placeholder body: a cylinder with a non-trigger CapsuleCollider (so the
        // crosshair raycast in SelectionManager can hit it). Swap the mesh/material later.
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Signal Generator";
        go.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        var sg = go.AddComponent<SignalGenerator>();

        // requiredParts is a private [SerializeField] list, so set it via SerializedObject.
        var so = new SerializedObject(sg);
        var nameProp = so.FindProperty("displayName");
        if (nameProp != null) nameProp.stringValue = "Signal Generator";

        var parts = so.FindProperty("requiredParts");
        parts.arraySize = 2;
        SetStack(parts.GetArrayElementAtIndex(0), ashenite, 2);
        SetStack(parts.GetArrayElementAtIndex(1), blightbloom, 1);

        var consume = so.FindProperty("consumeParts");
        if (consume != null) consume.boolValue = true;

        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SignalGeneratorSetup] Built {PrefabPath} — needs 2 Ashenite + 1 Blightbloom. " +
                  "The bootstrapper spawns it from Resources/SurvivalWorld/SignalGenerator on Play.");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;
    }

    static void SetStack(SerializedProperty element, ItemData item, int quantity)
    {
        element.FindPropertyRelative("item").objectReferenceValue = item;
        element.FindPropertyRelative("quantity").intValue = quantity;
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
