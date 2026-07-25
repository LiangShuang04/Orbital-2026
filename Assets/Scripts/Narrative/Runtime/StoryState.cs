using System;
using System.Collections.Generic;

namespace DontDiePlease.Narrative.Runtime
{
    [Serializable]
    public sealed class StoryState
    {
        public int schemaVersion = 1;
        public long revision;
        public long savedAtUnixMs;
        public long startedAtUnixMs;
        public string playthroughId = string.Empty;
        public string ownerUserId = string.Empty;
        public string currentObjectiveId = "ACT1_WAKE";
        public string playerTone = "practical";
        public int worldSeed;
        public float signalGeneratorProgress;
        public bool signalDefenseActive;
        public float signalDefenseRemainingSeconds;
        public bool signalDefensePaused;
        public List<string> completedSequenceIds = new List<string>();
        public List<string> completedObjectiveIds = new List<string>();
        public List<string> flags = new List<string>();

        public bool HasCompletedSequence(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && completedSequenceIds.Contains(id);
        }

        public bool HasFlag(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && flags.Contains(id);
        }

        public void CompleteSequence(string id)
        {
            AddUnique(completedSequenceIds, id);
        }

        public void CompleteObjective(string id)
        {
            AddUnique(completedObjectiveIds, id);
        }

        public void SetFlag(string id)
        {
            AddUnique(flags, id);
        }

        public void Normalize()
        {
            completedSequenceIds ??= new List<string>();
            completedObjectiveIds ??= new List<string>();
            flags ??= new List<string>();
            currentObjectiveId ??= string.Empty;
            playerTone ??= "practical";
            ownerUserId ??= string.Empty;
            playthroughId = string.IsNullOrWhiteSpace(playthroughId) ? "legacy" : playthroughId;
            schemaVersion = Math.Max(1, schemaVersion);
            signalGeneratorProgress = Math.Max(0f, Math.Min(100f, signalGeneratorProgress));
            signalDefenseRemainingSeconds = Math.Max(0f, signalDefenseRemainingSeconds);
        }

        public void MarkChanged()
        {
            revision++;
            savedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public StoryState Copy()
        {
            var copy = new StoryState
            {
                schemaVersion = schemaVersion,
                revision = revision,
                savedAtUnixMs = savedAtUnixMs,
                startedAtUnixMs = startedAtUnixMs,
                playthroughId = playthroughId,
                ownerUserId = ownerUserId,
                currentObjectiveId = currentObjectiveId,
                playerTone = playerTone,
                worldSeed = worldSeed,
                signalGeneratorProgress = signalGeneratorProgress,
                signalDefenseActive = signalDefenseActive,
                signalDefenseRemainingSeconds = signalDefenseRemainingSeconds,
                signalDefensePaused = signalDefensePaused,
                completedSequenceIds = new List<string>(completedSequenceIds),
                completedObjectiveIds = new List<string>(completedObjectiveIds),
                flags = new List<string>(flags)
            };
            copy.Normalize();
            return copy;
        }

        public void ReplaceWith(StoryState source)
        {
            if (source == null)
            {
                return;
            }

            schemaVersion = source.schemaVersion;
            revision = source.revision;
            savedAtUnixMs = source.savedAtUnixMs;
            startedAtUnixMs = source.startedAtUnixMs;
            playthroughId = source.playthroughId;
            ownerUserId = source.ownerUserId;
            currentObjectiveId = source.currentObjectiveId;
            playerTone = source.playerTone;
            worldSeed = source.worldSeed;
            signalGeneratorProgress = source.signalGeneratorProgress;
            signalDefenseActive = source.signalDefenseActive;
            signalDefenseRemainingSeconds = source.signalDefenseRemainingSeconds;
            signalDefensePaused = source.signalDefensePaused;
            completedSequenceIds = new List<string>(source.completedSequenceIds);
            completedObjectiveIds = new List<string>(source.completedObjectiveIds);
            flags = new List<string>(source.flags);
            Normalize();
        }

        private static void AddUnique(List<string> values, string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && !values.Contains(id))
            {
                values.Add(id);
            }
        }
    }
}
