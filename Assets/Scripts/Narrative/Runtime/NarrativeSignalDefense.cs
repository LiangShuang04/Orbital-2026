using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeSignalDefense : MonoBehaviour
    {
        private const float LocalSaveIntervalSeconds = 5f;
        private const float RemoteSaveIntervalSeconds = 15f;
        private readonly NarrativeDefenseTimeline timeline = new NarrativeDefenseTimeline();
        private NarrativeDirector director;
        private PlayerStats playerStats;
        private float localSaveAt;
        private float remoteSaveAt;
        private bool timelineReady;

        public bool IsActive => director != null && director.State.signalDefenseActive;
        public float RemainingSeconds => IsActive ? director.State.signalDefenseRemainingSeconds : 0f;
        public float Progress => IsActive ? timeline.Progress : 0f;

        public void Configure(NarrativeDirector narrativeDirector)
        {
            director = narrativeDirector;
            director.SequenceCompleted += HandleSequenceCompleted;
            StartCoroutine(RestoreDefense());
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.SequenceCompleted -= HandleSequenceCompleted;
                director.SetObjectiveStatus(string.Empty);
            }
        }

        private void Update()
        {
            if (!timelineReady || !IsActive || SceneManager.GetActiveScene().name != "Demo_Combat")
            {
                return;
            }

            playerStats ??= FindAnyObjectByType<PlayerStats>();
            var playerDead = playerStats != null && playerStats.currentHealth <= 0f;
            var paused = playerDead || Time.timeScale <= 0f;
            director.State.signalDefensePaused = paused;
            var tick = timeline.Advance(Time.deltaTime, paused);
            director.State.signalDefenseRemainingSeconds = timeline.RemainingSeconds;
            SendMilestones(tick);
            UpdateStatus();

            if (paused)
            {
                return;
            }

            if (Time.unscaledTime >= localSaveAt)
            {
                director.PersistProgress(false);
                localSaveAt = Time.unscaledTime + LocalSaveIntervalSeconds;
            }

            if (Time.unscaledTime >= remoteSaveAt)
            {
                director.PersistProgress(true);
                remoteSaveAt = Time.unscaledTime + RemoteSaveIntervalSeconds;
            }

            if (!tick.Completed)
            {
                return;
            }

            director.State.signalDefenseActive = false;
            director.State.signalDefensePaused = false;
            director.SetObjectiveStatus(string.Empty);
            director.PersistProgress(true);
            director.RaiseStoryEvent("TRG_SIGNAL_CHARGE_100");
        }

        private IEnumerator RestoreDefense()
        {
            while (director != null && !director.IsReady)
            {
                yield return null;
            }

            if (director == null)
            {
                yield break;
            }

            if (!director.State.signalDefenseActive &&
                director.State.HasFlag("signal_defense_started") &&
                !director.State.HasCompletedSequence("TRG_SIGNAL_CHARGE_100"))
            {
                director.State.signalDefenseActive = true;
                director.State.signalDefenseRemainingSeconds = NarrativeDefenseTimeline.DurationSeconds;
            }

            timeline.Restore(
                director.State.signalDefenseRemainingSeconds,
                director.State.HasCompletedSequence("TRG_SIGNAL_CHARGE_25"),
                director.State.HasCompletedSequence("TRG_SIGNAL_CHARGE_60"),
                director.State.HasCompletedSequence("TRG_SIGNAL_CHARGE_90"));
            timelineReady = true;
            ResetSaveSchedule();
            UpdateStatus();
        }

        private void HandleSequenceCompleted(string sequenceId)
        {
            if (sequenceId == "TRG_SIGNAL_DEFENSE")
            {
                BeginDefense();
            }
        }

        private void BeginDefense()
        {
            timeline.Start();
            timelineReady = true;
            director.State.signalDefenseActive = true;
            director.State.signalDefenseRemainingSeconds = timeline.RemainingSeconds;
            director.State.signalDefensePaused = false;
            ResetSaveSchedule();
            UpdateStatus();
            director.PersistProgress(true);
        }

        private void SendMilestones(NarrativeDefenseTick tick)
        {
            if (tick.Reached25)
            {
                director.RaiseStoryEvent("TRG_SIGNAL_CHARGE_25");
            }

            if (tick.Reached60)
            {
                director.RaiseStoryEvent("TRG_SIGNAL_CHARGE_60");
            }

            if (tick.Reached90)
            {
                director.RaiseStoryEvent("TRG_SIGNAL_CHARGE_90");
            }
        }

        private void ResetSaveSchedule()
        {
            localSaveAt = Time.unscaledTime + LocalSaveIntervalSeconds;
            remoteSaveAt = Time.unscaledTime + RemoteSaveIntervalSeconds;
        }

        private void UpdateStatus()
        {
            if (!IsActive)
            {
                director.SetObjectiveStatus(string.Empty);
                return;
            }

            var seconds = Mathf.CeilToInt(RemainingSeconds);
            var minutes = seconds / 60;
            var remainder = seconds % 60;
            var charge = Mathf.RoundToInt(Progress * 100f);
            var paused = director.State.signalDefensePaused ? "   PAUSED" : string.Empty;
            director.SetObjectiveStatus($"SIGNAL {charge}%   {minutes:00}:{remainder:00}{paused}");
        }
    }
}
