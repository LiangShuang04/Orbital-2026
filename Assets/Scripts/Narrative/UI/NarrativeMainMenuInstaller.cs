using DontDiePlease.Narrative.Persistence;
using DontDiePlease.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Narrative.UI
{
    public static class NarrativeMainMenuInstaller
    {
        private const string MainMenuSceneName = "MainMenuScene";
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (!subscribed)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                subscribed = true;
            }

            Install(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Install(scene);
        }

        private static void Install(Scene scene)
        {
            if (!scene.IsValid() || scene.name != MainMenuSceneName)
            {
                return;
            }

            var menus = Object.FindObjectsByType<NarrativeMainMenuController>(FindObjectsInactive.Include);

            foreach (var menu in menus)
            {
                if (menu != null && menu.gameObject.scene == scene)
                {
                    return;
                }
            }

            var root = new GameObject("NarrativeMainMenu");
            SceneManager.MoveGameObjectToScene(root, scene);
            var saveService = Object.FindAnyObjectByType<SaveProfileService>();

            if (saveService == null)
            {
                saveService = root.AddComponent<SaveProfileService>();
            }

            var seedManager = GameSeedManager.Instance ?? Object.FindAnyObjectByType<GameSeedManager>();

            if (seedManager == null)
            {
                var seedObject = new GameObject("GameSeedManager");
                seedManager = seedObject.AddComponent<GameSeedManager>();
            }

            var saveAdapter = root.AddComponent<NarrativeSaveAdapter>();
            saveAdapter.Configure(saveService);
            var controller = root.AddComponent<NarrativeMainMenuController>();
            controller.Configure(saveAdapter, saveService, seedManager);
        }
    }
}
