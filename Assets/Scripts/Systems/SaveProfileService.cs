using System;
using System.Threading;
using System.Threading.Tasks;
using DontDiePlease.Networking;
using UnityEngine;

namespace DontDiePlease.Systems
{
    public sealed class SaveProfileService : MonoBehaviour
    {
        private static readonly SemaphoreSlim ObjectiveSaveLock = new SemaphoreSlim(1, 1);
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private GameSeedManager seedManager;
        [SerializeField] private bool createSaveIfMissing = true;
        [SerializeField] private bool loadSeedOnStart;

        private async void Start()
        {
            if (!loadSeedOnStart)
            {
                return;
            }

            try
            {
                await LoadSeedIntoManager();
            }
            catch (Exception err)
            {
                Debug.LogWarning($"Failed to load world seed: {err.Message}");
            }
        }

        public async Task<ApiResult<SaveProfileData>> CreateSaveWithCurrentSeed()
        {
            var req = new SaveCreateRequest
            {
                worldSeed = CurrentWorldSeed()
            };

            var result = await networkManager.PostAuthenticatedJson<SaveCreateRequest, SaveProfileResponse>("/save", req);
            return ToProfileResult(result);
        }

        public async Task<ApiResult<SaveProfileData>> LoadSave()
        {
            ResolveDependencies();
            var result = await networkManager.GetAuthenticatedJson<SaveProfileResponse>("/save");
            return ToProfileResult(result);
        }

        public async Task<ApiResult<SaveProfileData>> SaveCurrentSeed()
        {
            var req = new SaveSeedUpdateRequest
            {
                worldSeed = CurrentWorldSeed()
            };

            var result = await networkManager.PutAuthenticatedJson<SaveSeedUpdateRequest, SaveProfileResponse>("/save", req);

            if (!result.Success && result.StatusCode == 404 && createSaveIfMissing)
            {
                return await CreateSaveWithCurrentSeed();
            }

            return ToProfileResult(result);
        }

        public async Task<ApiResult<SaveProfileData>> SaveObjectiveState(ObjectiveStateData objectiveState)
        {
            ResolveDependencies();
            await ObjectiveSaveLock.WaitAsync();

            try
            {
                var req = new SaveObjectiveUpdateRequest
                {
                    objectiveState = objectiveState
                };

                var result = await networkManager.PutAuthenticatedJson<SaveObjectiveUpdateRequest, SaveProfileResponse>("/save", req);

                if (!result.Success && result.StatusCode == 404 && createSaveIfMissing)
                {
                    var created = await CreateSaveWithCurrentSeed();

                    if (!created.Success)
                    {
                        return created;
                    }

                    result = await networkManager.PutAuthenticatedJson<SaveObjectiveUpdateRequest, SaveProfileResponse>("/save", req);
                }

                return ToProfileResult(result);
            }
            finally
            {
                ObjectiveSaveLock.Release();
            }
        }

        public async Task<ApiResult<SaveProfileData>> SaveNewGame(int worldSeed, ObjectiveStateData objectiveState)
        {
            ResolveDependencies();
            await ObjectiveSaveLock.WaitAsync();

            try
            {
                var req = new SaveNewGameRequest
                {
                    worldSeed = worldSeed,
                    objectiveState = objectiveState
                };
                var result = await networkManager.PutAuthenticatedJson<SaveNewGameRequest, SaveProfileResponse>("/save", req);

                if (!result.Success && result.StatusCode == 404 && createSaveIfMissing)
                {
                    var createReq = new SaveCreateRequest
                    {
                        worldSeed = worldSeed
                    };
                    var created = await networkManager.PostAuthenticatedJson<SaveCreateRequest, SaveProfileResponse>("/save", createReq);

                    if (!created.Success)
                    {
                        return ToProfileResult(created);
                    }

                    result = await networkManager.PutAuthenticatedJson<SaveNewGameRequest, SaveProfileResponse>("/save", req);
                }

                return ToProfileResult(result);
            }
            finally
            {
                ObjectiveSaveLock.Release();
            }
        }

        public async Task<ApiResult<SaveProfileData>> LoadSeedIntoManager()
        {
            ResolveDependencies();

            var result = await LoadSave();

            if (result.Success && result.Data != null)
            {
                seedManager.SetSeed(result.Data.worldSeed);
            }

            return result;
        }

        private ApiResult<SaveProfileData> ToProfileResult(ApiResult<SaveProfileResponse> result)
        {
            if (!result.Success)
            {
                return ApiResult<SaveProfileData>.Fail(result.StatusCode, result.Error);
            }

            if (result.Data == null || !result.Data.success || result.Data.saveProfile == null)
            {
                return ApiResult<SaveProfileData>.Fail(result.StatusCode, "Save response was invalid");
            }

            return ApiResult<SaveProfileData>.Ok(result.StatusCode, result.Data.saveProfile);
        }

        private int CurrentWorldSeed()
        {
            ResolveDependencies();

            if (!seedManager.HasSeed)
            {
                seedManager.InitialiseRun();
            }

            return seedManager.CurrentSeed;
        }

        private void ResolveDependencies()
        {
            if (networkManager == null)
            {
                networkManager = NetworkManager.Instance != null ? NetworkManager.Instance : FindObjectOfType<NetworkManager>();
            }

            if (seedManager == null)
            {
                seedManager = GameSeedManager.Instance != null ? GameSeedManager.Instance : FindObjectOfType<GameSeedManager>();
            }

            if (networkManager == null)
            {
                var go = new GameObject("NetworkManager");
                networkManager = go.AddComponent<NetworkManager>();
            }

            if (seedManager == null)
            {
                var go = new GameObject("GameSeedManager");
                seedManager = go.AddComponent<GameSeedManager>();
            }
        }
    }
}
