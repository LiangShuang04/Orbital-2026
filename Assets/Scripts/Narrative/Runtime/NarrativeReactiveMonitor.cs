using UnityEngine;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeReactiveMonitor : MonoBehaviour
    {
        private const float PollInterval = 0.35f;
        private NarrativeDirector director;
        private PlayerStats playerStats;
        private float nextPollAt;
        private bool deathReported;

        public void Configure(NarrativeDirector narrativeDirector)
        {
            director = narrativeDirector;
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        private void Update()
        {
            if (director == null || Time.unscaledTime < nextPollAt)
            {
                return;
            }

            nextPollAt = Time.unscaledTime + PollInterval;

            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();

                if (playerStats == null)
                {
                    return;
                }
            }

            CheckHealth();
            CheckOxygen();
            CheckToxicity();
            CheckHunger();
        }

        private void CheckHealth()
        {
            var ratio = SafeRatio(playerStats.currentHealth, playerStats.maxHealth);

            if (ratio <= 0f)
            {
                if (!deathReported)
                {
                    director.RaiseStoryEvent(director.State.HasFlag("first_death_seen") ? "REACT_REPEAT_DEATH" : "REACT_FIRST_DEATH");
                    deathReported = true;
                }

                return;
            }

            deathReported = false;
            director.CancelQueuedSequence("REACT_FIRST_DEATH");
            director.CancelQueuedSequence("REACT_REPEAT_DEATH");

            if (ratio <= 0.15f)
            {
                director.CancelQueuedSequence("REACT_LOW_HEALTH");
                director.RaiseStoryEvent("REACT_CRITICAL_HEALTH");
            }
            else if (ratio <= 0.35f)
            {
                director.CancelQueuedSequence("REACT_CRITICAL_HEALTH");
                director.RaiseStoryEvent("REACT_LOW_HEALTH");
            }
            else
            {
                director.CancelQueuedSequence("REACT_CRITICAL_HEALTH");
                director.CancelQueuedSequence("REACT_LOW_HEALTH");
            }
        }

        private void CheckOxygen()
        {
            var ratio = SafeRatio(playerStats.currentOxygen, playerStats.maxOxygen);

            if (ratio <= 0.05f)
            {
                director.CancelQueuedSequence("REACT_LOW_OXYGEN");
                director.RaiseStoryEvent("REACT_CRITICAL_OXYGEN");
            }
            else if (ratio <= 0.3f)
            {
                director.CancelQueuedSequence("REACT_CRITICAL_OXYGEN");
                director.RaiseStoryEvent("REACT_LOW_OXYGEN");
            }
            else
            {
                director.CancelQueuedSequence("REACT_CRITICAL_OXYGEN");
                director.CancelQueuedSequence("REACT_LOW_OXYGEN");
            }
        }

        private void CheckToxicity()
        {
            var ratio = SafeRatio(playerStats.currentToxicity, playerStats.maxToxicity);

            if (ratio >= 0.9f)
            {
                director.CancelQueuedSequence("REACT_HIGH_TOXICITY");
                director.RaiseStoryEvent("REACT_CRITICAL_TOXICITY");
            }
            else if (ratio >= 0.4f)
            {
                director.CancelQueuedSequence("REACT_CRITICAL_TOXICITY");
                director.RaiseStoryEvent("REACT_HIGH_TOXICITY");
            }
            else
            {
                director.CancelQueuedSequence("REACT_CRITICAL_TOXICITY");
                director.CancelQueuedSequence("REACT_HIGH_TOXICITY");
            }
        }

        private void CheckHunger()
        {
            var ratio = SafeRatio(playerStats.currentSaturation, playerStats.maxSaturation);

            if (ratio <= 0.15f)
            {
                director.CancelQueuedSequence("REACT_LOW_HUNGER");
                director.RaiseStoryEvent("REACT_CRITICAL_HUNGER");
            }
            else if (ratio <= 0.35f)
            {
                director.CancelQueuedSequence("REACT_CRITICAL_HUNGER");
                director.RaiseStoryEvent("REACT_LOW_HUNGER");
            }
            else
            {
                director.CancelQueuedSequence("REACT_CRITICAL_HUNGER");
                director.CancelQueuedSequence("REACT_LOW_HUNGER");
            }
        }

        private static float SafeRatio(float value, float maximum)
        {
            return maximum > 0f ? Mathf.Clamp01(value / maximum) : 0f;
        }
    }
}
