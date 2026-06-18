using System;
using System.Threading.Tasks;
using DontDiePlease.Networking;
using UnityEngine;

namespace DontDiePlease.Systems
{
    public sealed class SaveProfileService : MonoBehaviour
    {
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
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load world seed: {exception.Message}");
            }
        }

        public async Task<ApiResult<SaveProfileData>> CreateSaveWithCurrentSeed()
        {
            var request = new SaveCreateRequest
            {
                worldSeed = CurrentWorldSeed()
            };

            var result = await networkManager.PostAuthenticatedJson<SaveCreateRequest, SaveProfileResponse>("/save", request);
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
            var request = new SaveSeedUpdateRequest
            {
                worldSeed = CurrentWorldSeed()
            };

            var result = await networkManager.PutAuthenticatedJson<SaveSeedUpdateRequest, SaveProfileResponse>("/save", request);

            if (!result.Success && result.StatusCode == 404 && createSaveIfMissing)
            {
                return await CreateSaveWithCurrentSeed();
            }

            return ToProfileResult(result);
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
                var networkObject = new GameObject("NetworkManager");
                networkManager = networkObject.AddComponent<NetworkManager>();
            }

            if (seedManager == null)
            {
                var seedObject = new GameObject("GameSeedManager");
                seedManager = seedObject.AddComponent<GameSeedManager>();
            }
        }
    }
}
