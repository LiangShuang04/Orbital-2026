using System.Collections;
using Akila.FPSFramework;
using DontDiePlease.Narrative.Runtime;
using UnityEngine;
using UnityEngine.Events;

using FrameworkInventory = Akila.FPSFramework.Inventory;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralPlayerRecovery : MonoBehaviour
    {
        private const float RecoveryTimeout = 40f;

        private CentralCombatBootstrapper bootstrapper;
        private Damageable damageable;
        private Actor actor;
        private NarrativeDirector director;
        private Coroutine timeoutRoutine;
        private string recoverySequence;
        private bool recovering;

        public void Configure(CentralCombatBootstrapper combatBootstrapper)
        {
            bootstrapper = combatBootstrapper;
            damageable = GetComponentInChildren<Damageable>(true);
            actor = GetComponentInChildren<Actor>(true);

            if (actor != null)
                actor.respawnable = false;

            if (damageable == null)
                return;

            damageable.destroyOnDeath = false;
            damageable.destroyRoot = false;
            damageable.onDeath ??= new UnityEvent();
            damageable.OnDeath.RemoveListener(HandleDeath);
            damageable.OnDeath.AddListener(HandleDeath);
        }

        private void OnDestroy()
        {
            if (damageable != null)
                damageable.OnDeath.RemoveListener(HandleDeath);

            if (director != null)
                director.SequenceCompleted -= HandleSequenceCompleted;
        }

        private void HandleDeath()
        {
            if (recovering)
                return;

            recovering = true;
            LockPlayer();
            director = FindAnyObjectByType<NarrativeDirector>();
            recoverySequence = director != null && director.State != null && director.State.HasFlag("first_death_seen")
                ? "REACT_REPEAT_DEATH"
                : "REACT_FIRST_DEATH";

            if (director == null)
            {
                timeoutRoutine = StartCoroutine(RecoverAfterDelay(3f));
                return;
            }

            director.SequenceCompleted -= HandleSequenceCompleted;
            director.SequenceCompleted += HandleSequenceCompleted;

            if (!director.RequestSequence(recoverySequence, true))
            {
                timeoutRoutine = StartCoroutine(RecoverAfterDelay(3f));
                return;
            }

            timeoutRoutine = StartCoroutine(RecoverAfterDelay(RecoveryTimeout));
        }

        private void HandleSequenceCompleted(string sequenceId)
        {
            if (!recovering || sequenceId != recoverySequence)
                return;

            Recover();
        }

        private IEnumerator RecoverAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Recover();
        }

        private void Recover()
        {
            if (!recovering)
                return;

            recovering = false;

            if (timeoutRoutine != null)
            {
                StopCoroutine(timeoutRoutine);
                timeoutRoutine = null;
            }

            if (director != null)
                director.SequenceCompleted -= HandleSequenceCompleted;

            bootstrapper?.RecoverPlayer(gameObject);
        }

        private void LockPlayer()
        {
            FPSFrameworkCore.IsInputActive = false;

            var input = GetComponentInChildren<CharacterInput>(true);
            if (input != null)
                input.enabled = false;

            var controller = GetComponent<Akila.FPSFramework.FirstPersonController>();
            controller?.SetActive(false);

            var inventory = GetComponentInChildren<FrameworkInventory>(true);
            if (inventory == null)
                return;

            inventory.isActive = false;
            inventory.isInputActive = false;
        }
    }
}
