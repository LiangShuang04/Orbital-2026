using UnityEngine;

namespace DontDiePlease.Systems
{
    public sealed class RandomEventSpawnPoint : MonoBehaviour
    {
        [SerializeField] private RandomEventType eventType;
        [SerializeField] private float radius = 0.5f;

        public RandomEventType EventType => eventType;
        public float Radius => Mathf.Max(0f, radius);

        public void SetEventType(RandomEventType value)
        {
            eventType = value;
        }

        public void SetRadius(float value)
        {
            radius = Mathf.Max(0f, value);
        }

        public Vector3 GetSpawnPosition(System.Random random)
        {
            if (random == null || radius <= 0f)
            {
                return transform.position;
            }

            var angle = random.NextDouble() * Mathf.PI * 2f;
            var distance = System.Math.Sqrt(random.NextDouble()) * radius;
            var offset = new Vector3((float)(System.Math.Cos(angle) * distance), 0f, (float)(System.Math.Sin(angle) * distance));
            return transform.position + offset;
        }
    }
}
