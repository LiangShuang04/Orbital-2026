using DontDiePlease.Narrative.Runtime;
using UnityEditor;
using UnityEngine;

namespace DontDiePlease.Narrative.Editor
{
    public static class NarrativeCombatBindingsBuilder
    {
        private const string AssetPath = "Assets/Resources/Narrative/NarrativeCombatBindings.asset";
        private const string RobotPrefabPath = "Assets/Enemy stats/EnemyRoot.prefab";

        public static void Build()
        {
            var robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath);

            if (robotPrefab == null)
            {
                throw new MissingReferenceException($"Robot prefab is missing at {RobotPrefabPath}.");
            }

            var bindings = AssetDatabase.LoadAssetAtPath<NarrativeCombatBindings>(AssetPath);

            if (bindings == null)
            {
                bindings = ScriptableObject.CreateInstance<NarrativeCombatBindings>();
                AssetDatabase.CreateAsset(bindings, AssetPath);
            }

            var serialized = new SerializedObject(bindings);
            serialized.FindProperty("firstRobotPrefab").objectReferenceValue = robotPrefab;
            serialized.FindProperty("firstRobotAnchorName").stringValue = "EnemyRoot";
            serialized.FindProperty("firstRobotNavMeshRadius").floatValue = 8f;
            var phases = serialized.FindProperty("defensePhases");
            phases.arraySize = 4;
            SetPhase(phases.GetArrayElementAtIndex(0), 0f, 1, 2, 18f, 18f, 26f);
            SetPhase(phases.GetArrayElementAtIndex(1), 0.25f, 2, 3, 16f, 20f, 30f);
            SetPhase(phases.GetArrayElementAtIndex(2), 0.6f, 2, 4, 13f, 22f, 32f);
            SetPhase(phases.GetArrayElementAtIndex(3), 0.9f, 3, 5, 10f, 24f, 34f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bindings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SetPhase(
            SerializedProperty phase,
            float startProgress,
            int enemiesPerWave,
            int maxActiveEnemies,
            float spawnIntervalSeconds,
            float minSpawnRadius,
            float maxSpawnRadius)
        {
            phase.FindPropertyRelative("startProgress").floatValue = startProgress;
            phase.FindPropertyRelative("enemiesPerWave").intValue = enemiesPerWave;
            phase.FindPropertyRelative("maxActiveEnemies").intValue = maxActiveEnemies;
            phase.FindPropertyRelative("spawnIntervalSeconds").floatValue = spawnIntervalSeconds;
            phase.FindPropertyRelative("minSpawnRadius").floatValue = minSpawnRadius;
            phase.FindPropertyRelative("maxSpawnRadius").floatValue = maxSpawnRadius;
        }
    }
}
