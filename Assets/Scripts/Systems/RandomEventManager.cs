using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DontDiePlease.Systems
{
    public sealed class RandomEventManager : MonoBehaviour
    {
        [SerializeField] private GameSeedManager seedManager;
        [SerializeField] private bool startOnAwake = true;
        [SerializeField] private bool logEvents = true;
        [SerializeField] private bool logPreviewOnStart = true;
        [SerializeField] private int previewEventCount = 5;
        [SerializeField] private float firstEventDelaySeconds = 8f;
        [SerializeField] private float minimumSecondsBetweenEvents = 25f;
        [SerializeField] private float maximumSecondsBetweenEvents = 55f;
        [SerializeField] private Text eventStatusText;
        [SerializeField] private GameObject toxicStormVisual;
        [SerializeField] private GameObject robotPatrolPrefab;
        [SerializeField] private GameObject resourceDropPrefab;
        [SerializeField] private RandomEventDefinition[] eventDefinitions;
        [SerializeField] private RandomEventSpawnPoint[] spawnPoints;

        private readonly List<IRandomEventListener> listeners = new List<IRandomEventListener>();
        private System.Random eventRandom;
        private RandomEventContext activeTimedEvent;
        private float activeEventEndsAt;
        private float nextEventAt;
        private float statusMessageEndsAt;
        private int eventSequenceNumber;
        private bool isRunning;

        private void Reset()
        {
            eventDefinitions = new[]
            {
                new RandomEventDefinition
                {
                    eventType = RandomEventType.ToxicStorm,
                    displayName = "Toxic storm",
                    weight = 40,
                    durationSeconds = 30f,
                    intensity = 1f
                },
                new RandomEventDefinition
                {
                    eventType = RandomEventType.RobotPatrol,
                    displayName = "Robot patrol",
                    weight = 30,
                    durationSeconds = 0f,
                    intensity = 1f
                },
                new RandomEventDefinition
                {
                    eventType = RandomEventType.ResourceDrop,
                    displayName = "Resource drop",
                    weight = 30,
                    durationSeconds = 0f,
                    intensity = 1f
                }
            };
        }

        private void Awake()
        {
            seedManager = ResolveSeedManager();
            eventRandom = seedManager.GetRandomStream("random-events");
            RefreshListeners();
            SetToxicStormVisual(false);
            ClearStatus();

            if (logPreviewOnStart)
            {
                Debug.Log(BuildEventPreview(Mathf.Max(1, previewEventCount)));
            }

            if (startOnAwake)
            {
                StartEventLoop();
            }
        }

        private void Update()
        {
            if (activeTimedEvent != null && Time.time >= activeEventEndsAt)
            {
                EndTimedEvent();
            }

            if (eventStatusText != null && statusMessageEndsAt > 0f && Time.time >= statusMessageEndsAt)
            {
                ClearStatus();
            }

            if (!isRunning || activeTimedEvent != null || Time.time < nextEventAt)
            {
                return;
            }

            TriggerNextEvent();
            ScheduleNextEvent(activeTimedEvent != null ? activeEventEndsAt : Time.time);
        }

        public void StartEventLoop()
        {
            isRunning = true;
            nextEventAt = Time.time + Mathf.Max(0f, firstEventDelaySeconds);
        }

        public void StopEventLoop()
        {
            isRunning = false;
        }

        public void TriggerNextEvent()
        {
            var definition = PickDefinition(eventRandom);

            if (definition == null)
            {
                return;
            }

            eventSequenceNumber++;

            var context = new RandomEventContext
            {
                EventType = definition.eventType,
                DisplayName = CleanDisplayName(definition),
                Seed = seedManager.CurrentSeed,
                SequenceNumber = eventSequenceNumber,
                DurationSeconds = Mathf.Max(0f, definition.durationSeconds),
                Intensity = Mathf.Max(0f, definition.intensity)
            };

            ApplyEvent(context);
            NotifyStarted(context);

            if (context.DurationSeconds > 0f)
            {
                activeTimedEvent = context;
                activeEventEndsAt = Time.time + context.DurationSeconds;
            }
        }

        public string BuildEventPreview(int count)
        {
            var previewRandom = seedManager.CreateRandomStream("random-events");
            var builder = new StringBuilder();
            builder.Append($"Seed {seedManager.CurrentSeed} event preview:");
            var delay = Mathf.Max(0f, firstEventDelaySeconds);

            for (var index = 0; index < count; index++)
            {
                var definition = PickDefinition(previewRandom);
                var eventName = definition != null ? CleanDisplayName(definition) : "none";
                builder.Append(index == 0 ? " " : " -> ");
                builder.Append($"{eventName} in {delay:0.0}s");
                delay = NextInterval(previewRandom);
            }

            return builder.ToString();
        }

        public void RefreshListeners()
        {
            listeners.Clear();
            var behaviours = FindObjectsOfType<MonoBehaviour>();

            foreach (var behaviour in behaviours)
            {
                if (behaviour is IRandomEventListener listener && !ReferenceEquals(listener, this))
                {
                    listeners.Add(listener);
                }
            }
        }

        private void ApplyEvent(RandomEventContext context)
        {
            switch (context.EventType)
            {
                case RandomEventType.ToxicStorm:
                    SetToxicStormVisual(true);
                    ShowStatus($"{context.DisplayName} incoming");
                    break;
                case RandomEventType.RobotPatrol:
                    context.SpawnedObject = SpawnEventObject(context, robotPatrolPrefab, "Seeded Robot Patrol", new Color(0.8f, 0.1f, 0.1f, 1f));
                    ShowStatus($"{context.DisplayName} detected");
                    break;
                case RandomEventType.ResourceDrop:
                    context.SpawnedObject = SpawnEventObject(context, resourceDropPrefab, "Seeded Resource Drop", new Color(0.1f, 0.8f, 0.45f, 1f));
                    ShowStatus($"{context.DisplayName} located");
                    break;
            }

            if (logEvents)
            {
                Debug.Log($"Random event #{context.SequenceNumber}: {context.DisplayName} seed={context.Seed}");
            }
        }

        private GameObject SpawnEventObject(RandomEventContext context, GameObject prefab, string fallbackName, Color fallbackColor)
        {
            var spawnRandom = seedManager.GetRandomStream($"spawn-{context.EventType}");
            var spawnPoint = PickSpawnPoint(context.EventType, spawnRandom);
            var position = spawnPoint != null ? spawnPoint.GetSpawnPosition(spawnRandom) : transform.position + DeterministicOffset(spawnRandom);
            var rotation = spawnPoint != null ? spawnPoint.transform.rotation : Quaternion.identity;
            context.SpawnPosition = position;

            if (prefab != null)
            {
                return Instantiate(prefab, position, rotation);
            }

            return CreateFallbackObject(fallbackName, position, rotation, fallbackColor);
        }

        private GameObject CreateFallbackObject(string objectName, Vector3 position, Quaternion rotation, Color color)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = objectName;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            return instance;
        }

        private RandomEventSpawnPoint PickSpawnPoint(RandomEventType eventType, System.Random random)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            var validPoints = new List<RandomEventSpawnPoint>();

            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null && spawnPoint.EventType == eventType)
                {
                    validPoints.Add(spawnPoint);
                }
            }

            if (validPoints.Count == 0)
            {
                return null;
            }

            return validPoints[random.Next(0, validPoints.Count)];
        }

        private Vector3 DeterministicOffset(System.Random random)
        {
            var angle = random.NextDouble() * Mathf.PI * 2f;
            var distance = 6f + random.NextDouble() * 8f;
            return new Vector3((float)(System.Math.Cos(angle) * distance), 0f, (float)(System.Math.Sin(angle) * distance));
        }

        private void ScheduleNextEvent(float baseTime)
        {
            nextEventAt = baseTime + NextInterval(eventRandom);
        }

        private float NextInterval(System.Random random)
        {
            var minimum = Mathf.Max(0f, minimumSecondsBetweenEvents);
            var maximum = Mathf.Max(minimum, maximumSecondsBetweenEvents);

            if (Mathf.Approximately(minimum, maximum))
            {
                return minimum;
            }

            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private RandomEventDefinition PickDefinition(System.Random random)
        {
            if (eventDefinitions == null || eventDefinitions.Length == 0)
            {
                Reset();
            }

            var totalWeight = 0;

            foreach (var definition in eventDefinitions)
            {
                if (definition != null)
                {
                    totalWeight += Mathf.Max(0, definition.weight);
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = random.Next(0, totalWeight);
            var cursor = 0;

            foreach (var definition in eventDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                cursor += Mathf.Max(0, definition.weight);

                if (roll < cursor)
                {
                    return definition;
                }
            }

            return eventDefinitions[eventDefinitions.Length - 1];
        }

        private void EndTimedEvent()
        {
            var endingEvent = activeTimedEvent;
            activeTimedEvent = null;

            if (endingEvent == null)
            {
                return;
            }

            if (endingEvent.EventType == RandomEventType.ToxicStorm)
            {
                SetToxicStormVisual(false);
                ShowStatus($"{endingEvent.DisplayName} cleared");
            }

            NotifyEnded(endingEvent);
        }

        private void NotifyStarted(RandomEventContext context)
        {
            RefreshListeners();

            foreach (var listener in listeners)
            {
                listener.OnRandomEventStarted(context);
            }
        }

        private void NotifyEnded(RandomEventContext context)
        {
            foreach (var listener in listeners)
            {
                listener.OnRandomEventEnded(context);
            }
        }

        private void ShowStatus(string message)
        {
            if (eventStatusText == null)
            {
                return;
            }

            eventStatusText.text = message;
            eventStatusText.gameObject.SetActive(true);
            statusMessageEndsAt = Time.time + 4f;
        }

        private void ClearStatus()
        {
            if (eventStatusText != null)
            {
                eventStatusText.text = string.Empty;
                eventStatusText.gameObject.SetActive(false);
            }

            statusMessageEndsAt = 0f;
        }

        private void SetToxicStormVisual(bool active)
        {
            if (toxicStormVisual != null)
            {
                toxicStormVisual.SetActive(active);
            }
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

            var existingSeedManager = FindObjectOfType<GameSeedManager>();
            if (existingSeedManager != null)
            {
                return existingSeedManager;
            }

            var seedObject = new GameObject("GameSeedManager");
            return seedObject.AddComponent<GameSeedManager>();
        }

        private static string CleanDisplayName(RandomEventDefinition definition)
        {
            if (definition == null)
            {
                return "Unknown event";
            }

            return string.IsNullOrWhiteSpace(definition.displayName) ? definition.eventType.ToString() : definition.displayName.Trim();
        }
    }
}
