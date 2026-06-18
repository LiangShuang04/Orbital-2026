using System;

namespace DontDiePlease.Systems
{
    [Serializable]
    public sealed class SaveProfileResponse
    {
        public bool success;
        public SaveProfileData saveProfile;
    }

    [Serializable]
    public sealed class SaveProfileData
    {
        public string id;
        public string userId;
        public int worldSeed;
        public PlayerTransformData playerTransform;
        public SurvivalStatsData survivalStats;
        public InventoryItemData[] inventory;
        public BaseModuleData[] baseModules;
        public ObjectiveStateData objectiveState;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public sealed class SaveCreateRequest
    {
        public int worldSeed;
    }

    [Serializable]
    public sealed class SaveSeedUpdateRequest
    {
        public int worldSeed;
    }

    [Serializable]
    public sealed class SaveProfileUpdateRequest
    {
        public int worldSeed;
        public PlayerTransformData playerTransform;
        public SurvivalStatsData survivalStats;
        public InventoryItemData[] inventory;
        public BaseModuleData[] baseModules;
        public ObjectiveStateData objectiveState;
    }

    [Serializable]
    public sealed class PlayerTransformData
    {
        public Vector3Data position;
        public Vector3Data rotation;
    }

    [Serializable]
    public sealed class Vector3Data
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public sealed class SurvivalStatsData
    {
        public float health;
        public float oxygen;
        public float hunger;
        public float toxicity;
    }

    [Serializable]
    public sealed class InventoryItemData
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class BaseModuleData
    {
        public string moduleId;
        public bool isActive;
        public Vector3Data position;
    }

    [Serializable]
    public sealed class ObjectiveStateData
    {
        public string currentQuest;
        public float signalGeneratorProgress;
        public string[] completedObjectives;
        public ObjectiveTimerData[] activeTimers;
    }

    [Serializable]
    public sealed class ObjectiveTimerData
    {
        public string timerId;
        public int remainingSeconds;
        public bool isPaused;
    }
}
