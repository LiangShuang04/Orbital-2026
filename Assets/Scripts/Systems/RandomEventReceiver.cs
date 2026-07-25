using UnityEngine;

namespace DontDiePlease.Systems
{
    public sealed class RandomEventReceiver : MonoBehaviour, IRandomEventListener
    {
        [SerializeField] private bool logEvents = true;
        [SerializeField] private GameObject target;
        [SerializeField] private string toxicStormStartMethod = "BeginToxicStorm";
        [SerializeField] private string toxicStormEndMethod = "EndToxicStorm";
        [SerializeField] private string robotPatrolMethod = "OnRobotPatrolSpawned";
        [SerializeField] private string resourceDropMethod = "OnResourceDropSpawned";

        private GameObject Target => target != null ? target : gameObject;

        public void OnRandomEventStarted(RandomEventContext context)
        {
            if (context == null)
            {
                return;
            }

            if (logEvents)
            {
                Debug.Log($"Random event started: {context.DisplayName}");
            }

            switch (context.EventType)
            {
                case RandomEventType.ToxicStorm:
                    SendEventMessage(toxicStormStartMethod, context);
                    break;
                case RandomEventType.RobotPatrol:
                    SendEventMessage(robotPatrolMethod, context);
                    break;
                case RandomEventType.ResourceDrop:
                    SendEventMessage(resourceDropMethod, context);
                    break;
            }
        }

        public void OnRandomEventEnded(RandomEventContext context)
        {
            if (context == null)
            {
                return;
            }

            if (logEvents)
            {
                Debug.Log($"Random event ended: {context.DisplayName}");
            }

            if (context.EventType == RandomEventType.ToxicStorm)
            {
                SendEventMessage(toxicStormEndMethod, context);
            }
        }

        private void SendEventMessage(string methodName, RandomEventContext context)
        {
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                Target.SendMessage(methodName, context, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
