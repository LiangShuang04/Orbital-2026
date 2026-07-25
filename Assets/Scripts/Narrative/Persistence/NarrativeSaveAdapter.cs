using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DontDiePlease.Auth;
using DontDiePlease.Networking;
using DontDiePlease.Narrative.Runtime;
using DontDiePlease.Systems;
using UnityEngine;

namespace DontDiePlease.Narrative.Persistence
{
    public sealed class NarrativeSaveAdapter : MonoBehaviour
    {
        private const string LegacyLocalSaveKey = "DontDiePlease.Narrative.State";
        private const string LocalSavePrefix = "DontDiePlease.Narrative.State.";
        private const string DefenseTimerId = "signal_defense";
        private SaveProfileService saveProfileService;

        public void Configure(SaveProfileService service)
        {
            saveProfileService = service;
        }

        public StoryState LoadLocal()
        {
            var key = CurrentLocalSaveKey();
            var json = PlayerPrefs.GetString(key, string.Empty);

            if (string.IsNullOrWhiteSpace(json) && key.EndsWith(".guest", StringComparison.Ordinal))
            {
                json = PlayerPrefs.GetString(LegacyLocalSaveKey, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateOwnedState();
            }

            try
            {
                var state = JsonUtility.FromJson<StoryState>(json) ?? CreateOwnedState();
                state.Normalize();
                state.ownerUserId = CurrentOwnerId();
                return state;
            }
            catch (ArgumentException)
            {
                return CreateOwnedState();
            }
        }

        public void SaveLocal(StoryState state)
        {
            if (state == null)
            {
                return;
            }

            state.Normalize();
            state.ownerUserId = CurrentOwnerId();
            PlayerPrefs.SetString(CurrentLocalSaveKey(), JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public async Task LoadRemoteInto(StoryState state)
        {
            if (!CanUseRemote() || state == null)
            {
                return;
            }

            var result = await saveProfileService.LoadSave();

            if (!result.Success)
            {
                if (result.StatusCode == 404)
                {
                    return;
                }

                throw new InvalidOperationException(result.Error);
            }

            if (result.Data?.objectiveState == null)
            {
                return;
            }

            var remote = CreateOwnedState();
            remote.worldSeed = result.Data.worldSeed;
            ApplyObjectiveState(remote, result.Data.objectiveState);
            Merge(state, remote);
            SaveLocal(state);
        }

        public async Task SaveRemote(StoryState state)
        {
            if (!CanUseRemote() || state == null)
            {
                return;
            }

            var result = await saveProfileService.SaveObjectiveState(ToObjectiveState(state));

            if (!result.Success)
            {
                throw new InvalidOperationException(result.Error);
            }
        }

        public async Task<NarrativeResetResult> ResetForNewGame(int worldSeed)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var state = new StoryState
            {
                ownerUserId = CurrentOwnerId(),
                playthroughId = Guid.NewGuid().ToString("N"),
                startedAtUnixMs = now,
                revision = now,
                savedAtUnixMs = now,
                worldSeed = worldSeed
            };
            state.Normalize();
            SaveLocal(state);

            if (!CanUseRemote())
            {
                return new NarrativeResetResult(state, false, string.Empty);
            }

            var result = await saveProfileService.SaveNewGame(worldSeed, ToObjectiveState(state));
            return result.Success
                ? new NarrativeResetResult(state, true, string.Empty)
                : new NarrativeResetResult(state, false, result.Error);
        }

        private bool CanUseRemote()
        {
            return saveProfileService != null &&
                   NetworkManager.Instance != null &&
                   NetworkManager.Instance.IsAuthenticated;
        }

        private static ObjectiveStateData ToObjectiveState(StoryState state)
        {
            var completed = new List<string>();

            foreach (var id in state.completedSequenceIds)
            {
                completed.Add($"story:{id}");
            }

            foreach (var id in state.completedObjectiveIds)
            {
                completed.Add($"objective:{id}");
            }

            foreach (var id in state.flags)
            {
                completed.Add($"flag:{id}");
            }

            completed.Add($"tone:{state.playerTone}");
            completed.Add($"meta:schema:{state.schemaVersion}");
            completed.Add($"meta:revision:{state.revision}");
            completed.Add($"meta:savedAt:{state.savedAtUnixMs}");
            completed.Add($"meta:startedAt:{state.startedAtUnixMs}");
            completed.Add($"meta:playthrough:{state.playthroughId}");
            completed.Add($"meta:worldSeed:{state.worldSeed}");

            var timers = Array.Empty<ObjectiveTimerData>();

            if (state.signalDefenseActive)
            {
                timers = new[]
                {
                    new ObjectiveTimerData
                    {
                        timerId = DefenseTimerId,
                        remainingSeconds = Mathf.CeilToInt(state.signalDefenseRemainingSeconds),
                        isPaused = state.signalDefensePaused
                    }
                };
            }

            return new ObjectiveStateData
            {
                currentQuest = string.IsNullOrWhiteSpace(state.currentObjectiveId) ? "ACT1_WAKE" : state.currentObjectiveId,
                signalGeneratorProgress = Mathf.Clamp(state.signalGeneratorProgress, 0f, 100f),
                completedObjectives = completed.ToArray(),
                activeTimers = timers
            };
        }

        private static void ApplyObjectiveState(StoryState state, ObjectiveStateData objectiveState)
        {
            state.currentObjectiveId = string.IsNullOrWhiteSpace(objectiveState.currentQuest)
                ? state.currentObjectiveId
                : objectiveState.currentQuest;
            state.signalGeneratorProgress = Mathf.Clamp(objectiveState.signalGeneratorProgress, 0f, 100f);

            if (objectiveState.completedObjectives != null)
            {
                foreach (var entry in objectiveState.completedObjectives)
                {
                    ApplyCompletedEntry(state, entry);
                }
            }

            if (objectiveState.activeTimers != null)
            {
                foreach (var timer in objectiveState.activeTimers)
                {
                    if (timer == null || timer.timerId != DefenseTimerId)
                    {
                        continue;
                    }

                    state.signalDefenseActive = timer.remainingSeconds > 0;
                    state.signalDefenseRemainingSeconds = Mathf.Max(0f, timer.remainingSeconds);
                    state.signalDefensePaused = timer.isPaused;
                    break;
                }
            }

            state.Normalize();
        }

        private static void ApplyCompletedEntry(StoryState state, string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                return;
            }

            if (entry.StartsWith("story:", StringComparison.Ordinal))
            {
                state.CompleteSequence(entry.Substring(6));
            }
            else if (entry.StartsWith("objective:", StringComparison.Ordinal))
            {
                state.CompleteObjective(entry.Substring(10));
            }
            else if (entry.StartsWith("flag:", StringComparison.Ordinal))
            {
                state.SetFlag(entry.Substring(5));
            }
            else if (entry.StartsWith("tone:", StringComparison.Ordinal))
            {
                state.playerTone = entry.Substring(5);
            }
            else if (entry.StartsWith("meta:schema:", StringComparison.Ordinal) &&
                     int.TryParse(entry.Substring(12), out var schema))
            {
                state.schemaVersion = schema;
            }
            else if (entry.StartsWith("meta:revision:", StringComparison.Ordinal) &&
                     long.TryParse(entry.Substring(14), out var revision))
            {
                state.revision = revision;
            }
            else if (entry.StartsWith("meta:savedAt:", StringComparison.Ordinal) &&
                     long.TryParse(entry.Substring(13), out var savedAt))
            {
                state.savedAtUnixMs = savedAt;
            }
            else if (entry.StartsWith("meta:startedAt:", StringComparison.Ordinal) &&
                     long.TryParse(entry.Substring(15), out var startedAt))
            {
                state.startedAtUnixMs = startedAt;
            }
            else if (entry.StartsWith("meta:playthrough:", StringComparison.Ordinal))
            {
                state.playthroughId = entry.Substring(17);
            }
            else if (entry.StartsWith("meta:worldSeed:", StringComparison.Ordinal) &&
                     int.TryParse(entry.Substring(15), out var worldSeed))
            {
                state.worldSeed = worldSeed;
            }
        }

        private static void Merge(StoryState local, StoryState remote)
        {
            if (!string.Equals(local.playthroughId, remote.playthroughId, StringComparison.Ordinal))
            {
                var newer = remote.startedAtUnixMs > local.startedAtUnixMs ? remote : local;
                local.ReplaceWith(newer);
                return;
            }

            var remoteWins = remote.revision > local.revision ||
                             remote.revision == local.revision && remote.savedAtUnixMs > local.savedAtUnixMs ||
                             remote.revision == local.revision &&
                             remote.savedAtUnixMs == local.savedAtUnixMs &&
                             ProgressScore(remote) > ProgressScore(local);
            var preferred = remoteWins ? remote : local;

            MergeUnique(local.completedSequenceIds, remote.completedSequenceIds);
            MergeUnique(local.completedObjectiveIds, remote.completedObjectiveIds);
            MergeUnique(local.flags, remote.flags);
            local.currentObjectiveId = preferred.currentObjectiveId;
            local.playerTone = preferred.playerTone;
            local.worldSeed = preferred.worldSeed;
            local.schemaVersion = Math.Max(local.schemaVersion, remote.schemaVersion);
            local.revision = Math.Max(local.revision, remote.revision);
            local.savedAtUnixMs = Math.Max(local.savedAtUnixMs, remote.savedAtUnixMs);
            local.signalGeneratorProgress = Mathf.Max(local.signalGeneratorProgress, remote.signalGeneratorProgress);
            local.signalDefenseActive = preferred.signalDefenseActive;
            local.signalDefenseRemainingSeconds = preferred.signalDefenseRemainingSeconds;
            local.signalDefensePaused = preferred.signalDefensePaused;

            if (local.HasCompletedSequence("TRG_SIGNAL_CHARGE_100"))
            {
                local.signalDefenseActive = false;
                local.signalDefenseRemainingSeconds = 0f;
                local.signalDefensePaused = false;
            }

            local.Normalize();
        }

        private static float ProgressScore(StoryState state)
        {
            return state.completedSequenceIds.Count * 10f +
                   state.completedObjectiveIds.Count * 25f +
                   state.flags.Count * 5f +
                   state.signalGeneratorProgress;
        }

        private static void MergeUnique(List<string> target, List<string> source)
        {
            foreach (var id in source)
            {
                if (!target.Contains(id))
                {
                    target.Add(id);
                }
            }
        }

        private static StoryState CreateOwnedState()
        {
            return new StoryState
            {
                ownerUserId = CurrentOwnerId()
            };
        }

        private static string CurrentLocalSaveKey()
        {
            return LocalSavePrefix + CurrentOwnerId();
        }

        private static string CurrentOwnerId()
        {
            var network = NetworkManager.Instance;

            if (network != null && network.IsAuthenticated && !string.IsNullOrWhiteSpace(network.CurrentUserId))
            {
                return network.CurrentUserId;
            }

            var auth = AuthManager.Instance;
            return auth != null && auth.IsAuthenticated && !string.IsNullOrWhiteSpace(auth.UserId)
                ? auth.UserId
                : "guest";
        }
    }

    public sealed class NarrativeResetResult
    {
        public NarrativeResetResult(StoryState state, bool remoteSynced, string error)
        {
            State = state;
            RemoteSynced = remoteSynced;
            Error = error ?? string.Empty;
        }

        public StoryState State { get; }
        public bool RemoteSynced { get; }
        public string Error { get; }
    }
}
