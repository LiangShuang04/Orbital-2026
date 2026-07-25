using System;
using UnityEngine;

namespace DontDiePlease.Narrative.Runtime
{
    [CreateAssetMenu(fileName = "NarrativeCombatBindings", menuName = "Don't Die Please/Narrative Combat Bindings")]
    public sealed class NarrativeCombatBindings : ScriptableObject
    {
        [SerializeField] private GameObject firstRobotPrefab;
        [SerializeField] private string firstRobotAnchorName = "EnemyRoot";
        [SerializeField] private float firstRobotNavMeshRadius = 8f;
        [SerializeField] private NarrativeDefenseWavePhase[] defensePhases = Array.Empty<NarrativeDefenseWavePhase>();

        public GameObject FirstRobotPrefab => firstRobotPrefab;
        public string FirstRobotAnchorName => firstRobotAnchorName;
        public float FirstRobotNavMeshRadius => Mathf.Max(1f, firstRobotNavMeshRadius);
        public NarrativeDefenseWavePhase[] DefensePhases => defensePhases;

        public NarrativeDefenseWavePhase FindDefensePhase(float progress)
        {
            NarrativeDefenseWavePhase selected = null;

            foreach (var phase in defensePhases)
            {
                if (phase != null && progress >= phase.StartProgress)
                {
                    selected = phase;
                }
            }

            return selected;
        }
    }

    [Serializable]
    public sealed class NarrativeDefenseWavePhase
    {
        [SerializeField] private float startProgress;
        [SerializeField] private int enemiesPerWave = 1;
        [SerializeField] private int maxActiveEnemies = 2;
        [SerializeField] private float spawnIntervalSeconds = 18f;
        [SerializeField] private float minSpawnRadius = 18f;
        [SerializeField] private float maxSpawnRadius = 28f;

        public float StartProgress => Mathf.Clamp01(startProgress);
        public int EnemiesPerWave => Mathf.Max(1, enemiesPerWave);
        public int MaxActiveEnemies => Mathf.Max(1, maxActiveEnemies);
        public float SpawnIntervalSeconds => Mathf.Max(1f, spawnIntervalSeconds);
        public float MinSpawnRadius => Mathf.Max(4f, minSpawnRadius);
        public float MaxSpawnRadius => Mathf.Max(MinSpawnRadius, maxSpawnRadius);
    }
}
