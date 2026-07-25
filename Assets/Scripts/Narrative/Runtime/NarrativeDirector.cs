using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DontDiePlease.Narrative.Data;
using DontDiePlease.Narrative.Persistence;
using DontDiePlease.Narrative.UI;
using UnityEngine;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeDirector : MonoBehaviour
    {
        private const string DatabaseResourcePath = "Narrative/narrative_mvp";
        private readonly List<NarrativeDatabase.Sequence> queuedSequences = new List<NarrativeDatabase.Sequence>();
        private readonly List<PendingRequest> pendingRequests = new List<PendingRequest>();
        private readonly Dictionary<string, float> lastPlayedAt = new Dictionary<string, float>();
        private NarrativeDatabase database;
        private DialoguePresenter presenter;
        private NarrativeInputLock inputLock;
        private NarrativeSaveAdapter saveAdapter;
        private NarrativeDatabase.Sequence activeSequence;
        private int activeLineIndex;
        private bool initialized;
        private bool ready;
        private bool remoteSaveRunning;
        private bool remoteSavePending;
        private string objectiveStatus = string.Empty;
        private Task initializationTask = Task.CompletedTask;

        public StoryState State { get; private set; }
        public bool IsReady => ready;
        public bool IsPlaying => activeSequence != null;
        public string ActiveSequenceId => activeSequence?.id ?? string.Empty;
        public NarrativeDatabase Database => database;

        public event Action<string> SequenceStarted;
        public event Action<string> SequenceCompleted;
        public event Action<string> ObjectiveChanged;

        public void Configure(DialoguePresenter dialoguePresenter, NarrativeInputLock controls, NarrativeSaveAdapter persistence)
        {
            presenter = dialoguePresenter;
            inputLock = controls;
            saveAdapter = persistence;
            Initialize();
        }

        private void OnDisable()
        {
            presenter?.HideDialogue();
            inputLock?.SetLocked(false);
        }

        public bool RaiseStoryEvent(string eventId)
        {
            return RequestSequence(eventId);
        }

        public bool RequestSequence(string sequenceId, bool force = false)
        {
            Initialize();

            if (!ready)
            {
                pendingRequests.Add(new PendingRequest(sequenceId, force));
                return true;
            }

            var sequence = database?.FindSequence(sequenceId);

            if (sequence == null || (!force && !CanPlay(sequence)))
            {
                return false;
            }

            if (activeSequence == null)
            {
                StartSequence(sequence);
                return true;
            }

            if (sequence.priority > activeSequence.priority)
            {
                QueueSequence(activeSequence);
                StopActiveSequence(false, false);
                StartSequence(sequence);
                return true;
            }

            QueueSequence(sequence);
            return true;
        }

        public void SkipActiveSequence()
        {
            if (activeSequence == null || !activeSequence.skippable)
            {
                return;
            }

            StopActiveSequence(true);
        }

        public void CancelQueuedSequence(string sequenceId)
        {
            for (var index = queuedSequences.Count - 1; index >= 0; index--)
            {
                if (string.Equals(queuedSequences[index].id, sequenceId, StringComparison.Ordinal))
                {
                    queuedSequences.RemoveAt(index);
                }
            }
        }

        public void ResetNarrativeProgress()
        {
            StopActiveSequence(false);
            queuedSequences.Clear();
            State = new StoryState();
            PersistProgress(true);
            RefreshObjective();
        }

        public Task WaitUntilReady()
        {
            Initialize();
            return initializationTask;
        }

        public void PersistProgress(bool syncRemote)
        {
            if (State == null)
            {
                return;
            }

            State.MarkChanged();
            saveAdapter?.SaveLocal(State);

            if (syncRemote)
            {
                QueueRemoteSave();
            }

            RefreshObjective();
        }

        public void RefreshObjectiveDisplay()
        {
            RefreshObjective();
        }

        public void SetObjectiveStatus(string status)
        {
            objectiveStatus = status ?? string.Empty;
            RefreshObjective();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            var asset = Resources.Load<TextAsset>(DatabaseResourcePath);

            if (asset == null)
            {
                Debug.LogError($"Narrative database missing at Resources/{DatabaseResourcePath}.json", this);
                database = new NarrativeDatabase();
            }
            else
            {
                database = JsonUtility.FromJson<NarrativeDatabase>(asset.text) ?? new NarrativeDatabase();
            }

            State = saveAdapter != null ? saveAdapter.LoadLocal() : new StoryState();
            State.Normalize();
            RefreshObjective();
            initializationTask = CompleteInitialization();
        }

        private async Task CompleteInitialization()
        {
            if (saveAdapter != null)
            {
                try
                {
                    await saveAdapter.LoadRemoteInto(State);
                }
                catch (Exception err)
                {
                    Debug.LogWarning($"Narrative progress could not be loaded from the server: {err.Message}", this);
                }
            }

            ready = true;
            ApplyWorldSeed();
            RefreshObjective();
            QueueRemoteSave();
            var requests = pendingRequests.ToArray();
            pendingRequests.Clear();

            foreach (var request in requests)
            {
                RequestSequence(request.SequenceId, request.Force);
            }
        }

        private bool CanPlay(NarrativeDatabase.Sequence sequence)
        {
            if (sequence.oneShot && State.HasCompletedSequence(sequence.id))
            {
                return false;
            }

            if (sequence.cooldownSeconds > 0f &&
                lastPlayedAt.TryGetValue(sequence.id, out var lastPlayed) &&
                Time.unscaledTime < lastPlayed + sequence.cooldownSeconds)
            {
                return false;
            }

            if (!HasAllFlags(sequence.requiredFlags))
            {
                return false;
            }

            return !HasAnyFlag(sequence.blockedFlags);
        }

        private bool HasAllFlags(string[] flags)
        {
            if (flags == null)
            {
                return true;
            }

            foreach (var flag in flags)
            {
                if (!State.HasFlag(flag))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasAnyFlag(string[] flags)
        {
            if (flags == null)
            {
                return false;
            }

            foreach (var flag in flags)
            {
                if (State.HasFlag(flag))
                {
                    return true;
                }
            }

            return false;
        }

        private void StartSequence(NarrativeDatabase.Sequence sequence)
        {
            activeSequence = sequence;
            activeLineIndex = 0;
            lastPlayedAt[sequence.id] = Time.unscaledTime;
            inputLock?.SetLocked(sequence.lockInput);
            SequenceStarted?.Invoke(sequence.id);

            if (sequence.lines == null || sequence.lines.Length == 0)
            {
                StopActiveSequence(true);
                return;
            }

            PresentCurrentLine();
        }

        private void PresentCurrentLine()
        {
            if (activeSequence == null)
            {
                return;
            }

            if (activeLineIndex < 0 || activeLineIndex >= activeSequence.lines.Length)
            {
                StopActiveSequence(true);
                return;
            }

            presenter.Present(
                activeSequence.lines[activeLineIndex],
                activeSequence.mode,
                activeSequence.skippable,
                AdvanceLine,
                SelectChoice,
                SkipActiveSequence);
        }

        private void AdvanceLine()
        {
            var line = activeSequence.lines[activeLineIndex];

            if (string.Equals(line.nextLineId, "END", StringComparison.Ordinal))
            {
                StopActiveSequence(true);
                return;
            }

            activeLineIndex = string.IsNullOrWhiteSpace(line.nextLineId)
                ? activeLineIndex + 1
                : FindLineIndex(line.nextLineId, activeLineIndex + 1);
            PresentCurrentLine();
        }

        private void SelectChoice(int choiceIndex)
        {
            if (activeSequence == null ||
                activeLineIndex < 0 ||
                activeLineIndex >= activeSequence.lines.Length)
            {
                return;
            }

            var line = activeSequence.lines[activeLineIndex];

            if (line.choices == null || choiceIndex < 0 || choiceIndex >= line.choices.Length)
            {
                return;
            }

            var choice = line.choices[choiceIndex];

            if (!string.IsNullOrWhiteSpace(choice.setFlag))
            {
                State.SetFlag(choice.setFlag);
            }

            if (!string.IsNullOrWhiteSpace(choice.tone))
            {
                State.playerTone = choice.tone;
            }

            if (string.IsNullOrWhiteSpace(choice.nextLineId))
            {
                activeLineIndex++;
            }
            else
            {
                activeLineIndex = FindLineIndex(choice.nextLineId, activeLineIndex + 1);
            }

            PresentCurrentLine();
        }

        private int FindLineIndex(string lineId, int fallbackIndex)
        {
            for (var index = 0; index < activeSequence.lines.Length; index++)
            {
                if (string.Equals(activeSequence.lines[index].id, lineId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return fallbackIndex;
        }

        private void StopActiveSequence(bool completed, bool playNext = true)
        {
            if (activeSequence == null)
            {
                return;
            }

            var sequence = activeSequence;
            activeSequence = null;
            presenter?.HideDialogue();
            inputLock?.SetLocked(false);

            if (completed)
            {
                ApplySequenceOutcome(sequence);
                SequenceCompleted?.Invoke(sequence.id);
            }

            if (playNext)
            {
                PlayNextQueuedSequence();
            }
        }

        private void ApplySequenceOutcome(NarrativeDatabase.Sequence sequence)
        {
            State.CompleteSequence(sequence.id);

            if (!string.IsNullOrWhiteSpace(sequence.setFlag))
            {
                State.SetFlag(sequence.setFlag);
            }

            if (!string.IsNullOrWhiteSpace(sequence.completeObjective))
            {
                State.CompleteObjective(sequence.completeObjective);
            }

            if (!string.IsNullOrWhiteSpace(sequence.nextObjective))
            {
                State.currentObjectiveId = sequence.nextObjective;
                ObjectiveChanged?.Invoke(sequence.nextObjective);
            }

            if (sequence.signalProgress >= 0f)
            {
                State.signalGeneratorProgress = Mathf.Clamp(sequence.signalProgress, 0f, 100f);
            }

            PersistProgress(true);
        }

        private void QueueRemoteSave()
        {
            if (saveAdapter == null)
            {
                return;
            }

            remoteSavePending = true;

            if (!remoteSaveRunning)
            {
                _ = FlushRemoteSaves();
            }
        }

        private async Task FlushRemoteSaves()
        {
            remoteSaveRunning = true;

            while (remoteSavePending && this != null)
            {
                remoteSavePending = false;
                var snapshot = State.Copy();

                try
                {
                    await saveAdapter.SaveRemote(snapshot);
                }
                catch (Exception err)
                {
                    Debug.LogWarning($"Narrative progress could not be saved to the server: {err.Message}", this);
                }
            }

            remoteSaveRunning = false;
        }

        private void RefreshObjective()
        {
            if (presenter == null || database == null)
            {
                return;
            }

            var objective = database.FindObjective(State.currentObjectiveId);
            var description = objective?.description ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(objectiveStatus))
            {
                description = string.IsNullOrWhiteSpace(description)
                    ? objectiveStatus
                    : $"{description}\n{objectiveStatus}";
            }

            presenter.SetObjective(objective?.title, description);
        }

        private void ApplyWorldSeed()
        {
            if (State.worldSeed == 0)
            {
                return;
            }

            var seedManager = DontDiePlease.Systems.GameSeedManager.Instance ??
                              FindAnyObjectByType<DontDiePlease.Systems.GameSeedManager>();
            seedManager?.SetSeed(State.worldSeed);
        }

        private void QueueSequence(NarrativeDatabase.Sequence sequence)
        {
            if (sequence == null)
            {
                return;
            }

            foreach (var queued in queuedSequences)
            {
                if (string.Equals(queued.id, sequence.id, StringComparison.Ordinal))
                {
                    return;
                }
            }

            queuedSequences.Add(sequence);
            queuedSequences.Sort((left, right) => right.priority.CompareTo(left.priority));
        }

        private void PlayNextQueuedSequence()
        {
            while (queuedSequences.Count > 0)
            {
                var sequence = queuedSequences[0];
                queuedSequences.RemoveAt(0);

                if (!CanPlay(sequence))
                {
                    continue;
                }

                StartSequence(sequence);
                return;
            }
        }

        private readonly struct PendingRequest
        {
            public PendingRequest(string sequenceId, bool force)
            {
                SequenceId = sequenceId;
                Force = force;
            }

            public string SequenceId { get; }
            public bool Force { get; }
        }
    }
}
