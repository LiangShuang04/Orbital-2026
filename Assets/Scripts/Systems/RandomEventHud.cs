using UnityEngine;
using UnityEngine.UI;

namespace DontDiePlease.Systems
{
    public sealed class RandomEventHud : MonoBehaviour, IRandomEventListener
    {
        [SerializeField] private GameObject alertRoot;
        [SerializeField] private Text alertText;
        [SerializeField] private float visibleSeconds = 4f;
        [SerializeField] private string toxicStormMessage = "WARNING: TOXIC STORM INCOMING";
        [SerializeField] private string robotPatrolMessage = "ROBOT PATROL DETECTED";
        [SerializeField] private string resourceDropMessage = "RESOURCE DROP LOCATED";

        private float hideAt;

        private void Awake()
        {
            HideAlert();
        }

        private void Update()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt)
            {
                HideAlert();
            }
        }

        public void OnRandomEventStarted(RandomEventContext context)
        {
            if (context == null)
            {
                return;
            }

            ShowAlert(ResolveMessage(context.EventType));
        }

        public void OnRandomEventEnded(RandomEventContext context)
        {
            if (context != null && context.EventType == RandomEventType.ToxicStorm)
            {
                ShowAlert("TOXIC STORM CLEARED");
            }
        }

        public void ShowAlert(string msg)
        {
            if (alertRoot != null)
            {
                alertRoot.SetActive(true);
            }

            if (alertText != null)
            {
                alertText.text = msg;
                alertText.gameObject.SetActive(true);
            }

            hideAt = Time.unscaledTime + Mathf.Max(0.5f, visibleSeconds);
        }

        private void HideAlert()
        {
            if (alertText != null)
            {
                alertText.text = string.Empty;
                alertText.gameObject.SetActive(false);
            }

            if (alertRoot != null)
            {
                alertRoot.SetActive(false);
            }

            hideAt = 0f;
        }

        private string ResolveMessage(RandomEventType type)
        {
            switch (type)
            {
                case RandomEventType.ToxicStorm:
                    return toxicStormMessage;
                case RandomEventType.RobotPatrol:
                    return robotPatrolMessage;
                case RandomEventType.ResourceDrop:
                    return resourceDropMessage;
                default:
                    return "UNKNOWN EVENT DETECTED";
            }
        }
    }
}
