using UnityEngine;
using UnityEngine.SceneManagement;
using DontDiePlease.Narrative.Runtime;

namespace DontDiePlease.Narrative.Triggers
{
    public sealed class NarrativeScenePortal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Travel";
        [SerializeField] private string targetScene = string.Empty;
        private bool loading;
        private NarrativeDirector director;

        public void Configure(string label, string sceneName, NarrativeDirector narrativeDirector)
        {
            displayName = label;
            targetScene = sceneName;
            director = narrativeDirector;
        }

        public string GetDisplayName()
        {
            if (director != null && director.State.signalDefenseActive && targetScene != "MainGameplayScene")
            {
                return "Signal defense in progress";
            }

            return displayName;
        }

        public void Interact(GameObject interactor)
        {
            if (loading || string.IsNullOrWhiteSpace(targetScene))
            {
                return;
            }

            if (director != null && director.State.signalDefenseActive && targetScene != "MainGameplayScene")
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogError($"Scene '{targetScene}' is not available in Build Settings.", this);
                return;
            }

            loading = true;
            SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        }
    }
}
