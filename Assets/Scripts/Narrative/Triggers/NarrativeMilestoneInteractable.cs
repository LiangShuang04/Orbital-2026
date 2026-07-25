using System;
using DontDiePlease.Narrative.Runtime;
using UnityEngine;

namespace DontDiePlease.Narrative.Triggers
{
    public sealed class NarrativeMilestoneInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Story terminal";
        [SerializeField] private string[] eventIds = Array.Empty<string>();
        private NarrativeDirector director;

        public void Configure(string label, string[] sequenceIds, NarrativeDirector narrativeDirector)
        {
            displayName = label;
            eventIds = sequenceIds ?? Array.Empty<string>();
            director = narrativeDirector;
        }

        public string GetDisplayName()
        {
            return displayName;
        }

        public void Interact(GameObject interactor)
        {
            director ??= FindAnyObjectByType<NarrativeDirector>();

            if (director == null)
            {
                return;
            }

            foreach (var eventId in eventIds)
            {
                if (director.State.HasCompletedSequence(eventId))
                {
                    continue;
                }

                director.RaiseStoryEvent(eventId);
                return;
            }
        }
    }
}
