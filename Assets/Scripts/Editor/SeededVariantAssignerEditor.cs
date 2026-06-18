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

        [MenuItem("Tools/Don't Die Please/Batch Assign Seeded Variant")]
        private static void BatchAssignSeededVariant()
        {
            var selectedObjects = Selection.gameObjects
                .Where(IsSceneObject)
                .Distinct()
                .ToArray();

            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("Select one or more scene GameObjects before assigning seeded variants.");
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            foreach (var selectedObject in selectedObjects)
            {
                var variant = selectedObject.GetComponent<SeededDecorationVariant>();

                if (variant == null)
                {
                    variant = Undo.AddComponent<SeededDecorationVariant>(selectedObject);
                }
                else
                {
                    Undo.RecordObject(variant, UndoName);
                }

                ApplyDefaults(variant);
                EditorUtility.SetDirty(variant);
                PrefabUtility.RecordPrefabInstancePropertyModifications(variant);
                EditorSceneManager.MarkSceneDirty(selectedObject.scene);
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"Seeded variant defaults applied to {selectedObjects.Length} scene object{(selectedObjects.Length == 1 ? string.Empty : "s")}.");
        }

        [MenuItem("Tools/Don't Die Please/Batch Assign Seeded Variant", true)]
        private static bool CanBatchAssignSeededVariant()
        {
            return Selection.gameObjects.Any(IsSceneObject);
        }

        private static bool IsSceneObject(GameObject selectedObject)
        {
            return selectedObject != null && !EditorUtility.IsPersistent(selectedObject);
        }

        private static void ApplyDefaults(SeededDecorationVariant variant)
        {
            var serializedVariant = new SerializedObject(variant);
            SetBool(serializedVariant, "chooseSingleVariant", true);
            SetBool(serializedVariant, "randomiseYRotation", true);
            SetVector2(serializedVariant, "yRotationRange", new Vector2(0f, 360f));
            SetBool(serializedVariant, "randomiseUniformScale", true);
            SetVector2(serializedVariant, "uniformScaleRange", new Vector2(0.85f, 1.15f));
            serializedVariant.ApplyModifiedProperties();
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
