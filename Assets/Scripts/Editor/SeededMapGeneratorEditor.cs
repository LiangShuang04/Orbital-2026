using DontDiePlease.Systems;
using UnityEditor;
using UnityEngine;

namespace DontDiePlease.EditorTools
{
    [CustomEditor(typeof(SeededMapGenerator))]
    [CanEditMultipleObjects]
    public sealed class SeededMapGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10f);

            if (GUILayout.Button("Generate"))
            {
                foreach (var targetObj in targets)
                {
                    var generator = (SeededMapGenerator)targetObj;
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Seeded Map");
                    generator.Generate();
                    EditorUtility.SetDirty(generator);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                foreach (var targetObj in targets)
                {
                    var generator = (SeededMapGenerator)targetObj;
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Seeded Map");
                    generator.ClearGeneratedMap();
                    EditorUtility.SetDirty(generator);
                }
            }
        }
    }
}
