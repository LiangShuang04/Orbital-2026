using UnityEngine;

namespace DontDiePlease.Systems
{
    public sealed class SeededDecorationVariant : MonoBehaviour
    {
        [SerializeField] private GameSeedManager seedManager;
        [SerializeField] private string stableKey;
        [SerializeField] private string streamName = "map-decoration";
        [SerializeField] private GameObject[] variants;
        [SerializeField] private bool chooseSingleVariant = true;
        [SerializeField] private bool randomiseYRotation = true;
        [SerializeField] private Vector2 yRotationRange = new Vector2(0f, 360f);
        [SerializeField] private bool randomiseUniformScale;
        [SerializeField] private Vector2 uniformScaleRange = new Vector2(0.95f, 1.05f);

        private void Awake()
        {
            ApplyVariation();
        }

        public void ApplyVariation()
        {
            var mgr = ResolveSeedManager();
            var rng = mgr.CreateRandomStream($"{streamName}:{ResolveStableKey()}");

            if (chooseSingleVariant && variants != null && variants.Length > 0)
            {
                var picked = rng.Next(0, variants.Length);

                for (var idx = 0; idx < variants.Length; idx++)
                {
                    if (variants[idx] != null)
                    {
                        variants[idx].SetActive(idx == picked);
                    }
                }
            }

            if (randomiseYRotation)
            {
                var rotation = transform.localEulerAngles;
                rotation.y = Mathf.Lerp(yRotationRange.x, yRotationRange.y, (float)rng.NextDouble());
                transform.localEulerAngles = rotation;
            }

            if (randomiseUniformScale)
            {
                var scale = Mathf.Lerp(uniformScaleRange.x, uniformScaleRange.y, (float)rng.NextDouble());
                transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        private string ResolveStableKey()
        {
            if (!string.IsNullOrWhiteSpace(stableKey))
            {
                return stableKey.Trim();
            }

            return $"{BuildPath(transform)}:{transform.localPosition.x:0.###},{transform.localPosition.y:0.###},{transform.localPosition.z:0.###}";
        }

        private string BuildPath(Transform target)
        {
            if (target == null)
            {
                return gameObject.name;
            }

            return target.parent == null ? target.name : $"{BuildPath(target.parent)}/{target.name}";
        }

        private GameSeedManager ResolveSeedManager()
        {
            if (seedManager != null)
            {
                return seedManager;
            }

            if (GameSeedManager.Instance != null)
            {
                return GameSeedManager.Instance;
            }

            var existing = FindObjectOfType<GameSeedManager>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("GameSeedManager");
            return go.AddComponent<GameSeedManager>();
        }
    }
}
