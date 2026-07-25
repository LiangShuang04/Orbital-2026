using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DontDiePlease.Networking;
using DontDiePlease.Systems;
using UnityEngine;

namespace DontDiePlease.Auth
{
    public sealed class AuthenticatedGameFlow : MonoBehaviour
    {
        public const string AircraftScene = "MainGameplayScene";
        public const string FinalScene = "Demo_Combat";

        private static readonly HashSet<string> FinalObjectives = new HashSet<string>(StringComparer.Ordinal)
        {
            "ACT4_ARCHIVE",
            "ACT5_COMPONENTS",
            "FINALE_SIGNAL",
            "STORY_COMPLETE"
        };

        private static readonly string[] FinalProgressEntries =
        {
            "flag:first_robot_seen",
            "flag:unknown_transmission_heard",
            "flag:ruins_entered",
            "story:TRG_FIRST_ROBOT",
            "story:TRG_UNKNOWN_TRANSMISSION",
            "story:TRG_RUINS_ENTERED"
        };

        [SerializeField] private SaveProfileService saveService;
        [SerializeField] private GameSeedManager seedManager;

        public async Task<GameFlowResult> PrepareNextScene()
        {
            ResolveDependencies();
            var save = await saveService.LoadSave();

            if (!save.Success && save.StatusCode == 404)
            {
                save = await CreateNewProfile();
            }

            if (!save.Success || save.Data == null)
            {
                return GameFlowResult.Fail(save.Error);
            }

            seedManager.SetSeed(save.Data.worldSeed);
            var sceneName = ResolveScene(save.Data.objectiveState);

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return GameFlowResult.Fail($"Scene '{sceneName}' is not in Build Settings.");
            }

            return GameFlowResult.Ok(sceneName);
        }

        public static string ResolveScene(ObjectiveStateData state)
        {
            if (state == null)
            {
                return AircraftScene;
            }

            if (!string.IsNullOrWhiteSpace(state.currentQuest) && FinalObjectives.Contains(state.currentQuest))
            {
                return FinalScene;
            }

            if (state.completedObjectives == null)
            {
                return AircraftScene;
            }

            foreach (var entry in state.completedObjectives)
            {
                foreach (var finalEntry in FinalProgressEntries)
                {
                    if (string.Equals(entry, finalEntry, StringComparison.Ordinal))
                    {
                        return FinalScene;
                    }
                }
            }

            return AircraftScene;
        }

        private async Task<ApiResult<SaveProfileData>> CreateNewProfile()
        {
            var seed = seedManager.InitialiseRun();
            var objective = new ObjectiveStateData
            {
                currentQuest = "ACT1_WAKE",
                signalGeneratorProgress = 0f,
                completedObjectives = Array.Empty<string>(),
                activeTimers = Array.Empty<ObjectiveTimerData>()
            };
            return await saveService.SaveNewGame(seed, objective);
        }

        private void ResolveDependencies()
        {
            if (seedManager == null)
            {
                seedManager = GameSeedManager.Instance != null
                    ? GameSeedManager.Instance
                    : FindAnyObjectByType<GameSeedManager>();
            }

            if (seedManager == null)
            {
                seedManager = new GameObject("GameSeedManager").AddComponent<GameSeedManager>();
            }

            if (saveService == null)
            {
                saveService = FindAnyObjectByType<SaveProfileService>();
            }

            if (saveService == null)
            {
                saveService = gameObject.AddComponent<SaveProfileService>();
            }
        }
    }

    public sealed class GameFlowResult
    {
        public bool Success { get; private set; }
        public string SceneName { get; private set; }
        public string Error { get; private set; }

        public static GameFlowResult Ok(string sceneName)
        {
            return new GameFlowResult
            {
                Success = true,
                SceneName = sceneName,
                Error = string.Empty
            };
        }

        public static GameFlowResult Fail(string error)
        {
            return new GameFlowResult
            {
                Success = false,
                SceneName = string.Empty,
                Error = string.IsNullOrWhiteSpace(error) ? "Save profile could not be loaded." : error
            };
        }
    }
}
