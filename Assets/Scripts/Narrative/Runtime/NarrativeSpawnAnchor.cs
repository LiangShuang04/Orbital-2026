using UnityEngine;

namespace DontDiePlease.Narrative.Runtime
{
    public enum NarrativeAnchorKind
    {
        FirstRobotSpawn,
        WardenSpawn,
        SignalGeneratorPlacement,
        DefenseCenter,
        DefenseEnemySpawn,
        SignalGeneratorAssembly
    }

    [DisallowMultipleComponent]
    public sealed class NarrativeSpawnAnchor : MonoBehaviour
    {
        [SerializeField] private string encounterId;
        [SerializeField] private string anchorId;
        [SerializeField] private NarrativeAnchorKind kind;
        [SerializeField] private float spawnWeight = 1f;

        public string EncounterId => encounterId;
        public string AnchorId => anchorId;
        public NarrativeAnchorKind Kind => kind;
        public float SpawnWeight => Mathf.Max(0.01f, spawnWeight);

        public void Configure(
            string encounter,
            string id,
            NarrativeAnchorKind anchorKind,
            float weight = 1f)
        {
            encounterId = encounter;
            anchorId = id;
            kind = anchorKind;
            spawnWeight = Mathf.Max(0.01f, weight);
        }
    }
}
