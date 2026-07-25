using System;
using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    [CreateAssetMenu(menuName = "Don't Die Please/Combat/Enemy Visual Catalog")]
    public sealed class CentralEnemyVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public CentralEnemyArchetype archetype;
            public GameObject prefab;
            public string idleState;
            public string moveState;
            public string attackState;
            public string deathState;
            public float attackLock = 0.55f;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public bool TryGet(CentralEnemyArchetype archetype, out Entry entry)
        {
            for (var idx = 0; idx < entries.Length; idx++)
            {
                var current = entries[idx];

                if (current != null && current.archetype == archetype && current.prefab != null)
                {
                    entry = current;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }
}
