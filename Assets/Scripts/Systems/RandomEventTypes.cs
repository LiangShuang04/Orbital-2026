using System;
using UnityEngine;

namespace DontDiePlease.Systems
{
    public enum RandomEventType
    {
        ToxicStorm,
        RobotPatrol,
        ResourceDrop
    }

    [Serializable]
    public sealed class RandomEventDefinition
    {
        public RandomEventType eventType;
        public string displayName;
        [Min(0)] public int weight = 1;
        [Min(0f)] public float durationSeconds = 10f;
        [Min(0f)] public float intensity = 1f;
    }

    public sealed class RandomEventContext
    {
        public RandomEventType EventType { get; set; }
        public string DisplayName { get; set; }
        public int Seed { get; set; }
        public int SequenceNumber { get; set; }
        public float DurationSeconds { get; set; }
        public float Intensity { get; set; }
        public Vector3 SpawnPosition { get; set; }
        public GameObject SpawnedObject { get; set; }
    }

    public interface IRandomEventListener
    {
        void OnRandomEventStarted(RandomEventContext context);
        void OnRandomEventEnded(RandomEventContext context);
    }
}
