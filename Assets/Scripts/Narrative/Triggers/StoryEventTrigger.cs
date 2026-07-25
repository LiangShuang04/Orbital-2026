using DontDiePlease.Narrative.Runtime;
using UnityEngine;

namespace DontDiePlease.Narrative.Triggers
{
    [RequireComponent(typeof(Collider))]
    public sealed class StoryEventTrigger : MonoBehaviour
    {
        [SerializeField] private string eventId = string.Empty;
        [SerializeField] private bool oneShot = true;
        private NarrativeDirector director;
        private bool fired;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            director = FindAnyObjectByType<NarrativeDirector>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fired && oneShot)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerStats>() == null)
            {
                return;
            }

            director ??= FindAnyObjectByType<NarrativeDirector>();

            if (director != null && director.RaiseStoryEvent(eventId))
            {
                fired = true;
            }
        }

        public void Configure(string id, bool fireOnce)
        {
            eventId = id;
            oneShot = fireOnce;
        }
    }
}
