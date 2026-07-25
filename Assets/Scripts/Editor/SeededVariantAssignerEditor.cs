using System.Linq;
using DontDiePlease.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DontDiePlease.EditorTools
{
    public static class SeededVariantAssignerEditor
    {
        private const string UndoName = "Batch Assign Seeded Variant";

        [MenuItem("Tools/Don't Die Please/Map/Batch Assign Seeded Variant")]
        private static void BatchAssignSeededVariant()
        {
            var selectedObjects = Selection.gameObjects;
            var picked = new System.Collections.Generic.List<GameObject>();

            foreach (var obj in selectedObjects)
            {
                if (IsSceneObject(obj) && !picked.Contains(obj))
                    picked.Add(obj);
            }

            if (picked.Count == 0)
            {
                Debug.LogWarning("Select one or more scene GameObjects before assigning seeded variants.");
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            foreach (var obj in picked)
            {
                var variant = obj.GetComponent<SeededDecorationVariant>();

                if (variant == null)
                {
                    variant = Undo.AddComponent<SeededDecorationVariant>(obj);
                }
                else
                {
                    Undo.RecordObject(variant, UndoName);
                }

                ApplyDefaults(variant);
                EditorUtility.SetDirty(variant);
                PrefabUtility.RecordPrefabInstancePropertyModifications(variant);
                EditorSceneManager.MarkSceneDirty(obj.scene);
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"Seeded variant defaults applied to {picked.Count} scene object{(picked.Count == 1 ? string.Empty : "s")}.");
        }

        [MenuItem("Tools/Don't Die Please/Map/Batch Assign Seeded Variant", true)]
        private static bool CanBatchAssignSeededVariant()
        {
            return Selection.gameObjects.Any(IsSceneObject);
        }

        private static bool IsSceneObject(GameObject obj)
        {
            return obj != null && !EditorUtility.IsPersistent(obj);
        }

        private static void ApplyDefaults(SeededDecorationVariant variant)
        {
            var so = new SerializedObject(variant);
            SetBool(so, "chooseSingleVariant", true);
            SetBool(so, "randomiseYRotation", true);
            SetVector2(so, "yRotationRange", new Vector2(0f, 360f));
            SetBool(so, "randomiseUniformScale", true);
            SetVector2(so, "uniformScaleRange", new Vector2(0.85f, 1.15f));
            so.ApplyModifiedProperties();
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            var property = serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
        {
            var property = serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.vector2Value = value;
            }
        }
    }
}
