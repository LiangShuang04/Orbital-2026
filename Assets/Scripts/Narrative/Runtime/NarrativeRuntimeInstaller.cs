using System;
using System.Linq;
using DontDiePlease.Narrative.Persistence;
using DontDiePlease.Narrative.UI;
using DontDiePlease.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Narrative.Runtime
{
    public static class NarrativeRuntimeInstaller
    {
        private static readonly string[] SupportedScenes =
        {
            "MainGameplayScene",
            "Demo_Combat"
        };

        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForLoadedScene()
        {
            if (!subscribed)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                subscribed = true;
            }

            TryInstall(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall(scene);
        }

        private static void TryInstall(Scene scene)
        {
            if (!scene.IsValid() ||
                !SupportedScenes.Contains(scene.name, StringComparer.Ordinal) ||
                UnityEngine.Object.FindObjectsByType<NarrativeDirector>(FindObjectsInactive.Include)
                    .Any(item => item != null && item.gameObject.scene == scene))
            {
                return;
            }

            var root = new GameObject("NarrativeRuntime");
            SceneManager.MoveGameObjectToScene(root, scene);

            var presenter = root.AddComponent<DialoguePresenter>();
            var inputLock = root.AddComponent<NarrativeInputLock>();
            var saveService = UnityEngine.Object.FindAnyObjectByType<SaveProfileService>();

            if (saveService == null)
            {
                saveService = root.AddComponent<SaveProfileService>();
            }

            var saveAdapter = root.AddComponent<NarrativeSaveAdapter>();
            saveAdapter.Configure(saveService);

            var director = root.AddComponent<NarrativeDirector>();
            director.Configure(presenter, inputLock, saveAdapter);

            var worldBinder = root.AddComponent<NarrativeWorldBinder>();
            worldBinder.Configure(director);

            var sceneBridge = root.AddComponent<NarrativeSceneBridge>();
            var eventManager = UnityEngine.Object.FindAnyObjectByType<RandomEventManager>();

            if (scene.name == "MainGameplayScene" && eventManager == null)
            {
                eventManager = root.AddComponent<RandomEventManager>();
                eventManager.StopEventLoop();
            }

            sceneBridge.Configure(director, worldBinder, eventManager);

            if (eventManager != null)
            {
                eventManager.RefreshListeners();
            }

            var monitor = root.AddComponent<NarrativeReactiveMonitor>();
            monitor.Configure(director);

            var combatMonitor = root.AddComponent<NarrativeCombatMonitor>();
            combatMonitor.Configure(director);

            var combatBindings = Resources.Load<NarrativeCombatBindings>("Narrative/NarrativeCombatBindings");
            var combatCoordinator = root.AddComponent<NarrativeCombatCoordinator>();
            combatCoordinator.Configure(director, combatBindings);

            var signalDefense = root.AddComponent<NarrativeSignalDefense>();
            signalDefense.Configure(director);
        }
    }
}
