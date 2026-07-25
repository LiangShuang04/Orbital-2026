using System;
using System.Collections;
using DontDiePlease.Central.Combat;
using DontDiePlease.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeSceneBridge : MonoBehaviour, IRandomEventListener
    {
        private NarrativeDirector director;
        private NarrativeWorldBinder worldBinder;
        private RandomEventManager eventManager;
        private string sceneName;
        private Coroutine openingRoutine;
        private Coroutine stormRoutine;
        private bool storyStormActive;

        public void Configure(NarrativeDirector narrativeDirector, NarrativeWorldBinder binder, RandomEventManager randomEvents)
        {
            director = narrativeDirector;
            worldBinder = binder;
            eventManager = randomEvents;
            sceneName = SceneManager.GetActiveScene().name;
            director.SequenceCompleted += HandleSequenceCompleted;

            if (openingRoutine != null)
                StopCoroutine(openingRoutine);

            openingRoutine = StartCoroutine(BeginSceneNarrative());
        }

        private void OnEnable()
        {
            InteractableObject.Interacted += HandleInteraction;
            ItemPickup.PickedUp += HandlePickup;
            ItemPickup.PickupRejected += HandlePickupRejected;
            InventoryUI.InventoryVisibilityChanged += HandleInventoryVisibility;
            InsideShip.SafetyChanged += HandleSafetyChanged;
            EnemyHealth.AnyEnemyDied += HandleEnemyDied;
            EnemyController.AnyEnemyDetected += HandleEnemyDetected;
            if (director != null)
                openingRoutine = StartCoroutine(BeginSceneNarrative());
        }

        private void OnDisable()
        {
            InteractableObject.Interacted -= HandleInteraction;
            ItemPickup.PickedUp -= HandlePickup;
            ItemPickup.PickupRejected -= HandlePickupRejected;
            InventoryUI.InventoryVisibilityChanged -= HandleInventoryVisibility;
            InsideShip.SafetyChanged -= HandleSafetyChanged;
            EnemyHealth.AnyEnemyDied -= HandleEnemyDied;
            EnemyController.AnyEnemyDetected -= HandleEnemyDetected;

            if (director != null)
            {
                director.SequenceCompleted -= HandleSequenceCompleted;
            }

            if (openingRoutine != null)
            {
                StopCoroutine(openingRoutine);
                openingRoutine = null;
            }

            if (stormRoutine != null)
            {
                StopCoroutine(stormRoutine);
                stormRoutine = null;
            }
        }

        public void OnRandomEventStarted(RandomEventContext context)
        {
            if (context == null)
            {
                return;
            }

            switch (context.EventType)
            {
                case RandomEventType.ToxicStorm:
                    storyStormActive = true;
                    director.RaiseStoryEvent("EVT_TOXIC_STORM_STORY");
                    break;
                case RandomEventType.RobotPatrol:
                    director.RaiseStoryEvent("REACT_ROBOT_PATROL");
                    break;
                case RandomEventType.ResourceDrop:
                    director.RaiseStoryEvent("REACT_RESOURCE_DROP");
                    break;
            }
        }

        public void OnRandomEventEnded(RandomEventContext context)
        {
            if (context?.EventType != RandomEventType.ToxicStorm)
            {
                return;
            }

            storyStormActive = false;
            director.RaiseStoryEvent("REACT_STORM_CLEARED");

            if (director.State.HasCompletedSequence("TRG_STORY_STORM_SHELTER"))
            {
                director.RaiseStoryEvent("TRG_UNKNOWN_TRANSMISSION");
            }
        }

        private IEnumerator BeginSceneNarrative()
        {
            while (director != null && !director.IsReady)
            {
                yield return null;
            }

            if (director == null)
            {
                yield break;
            }

            if (sceneName == "MainGameplayScene")
            {
                director.RaiseStoryEvent("TRG_COCKPIT_WAKE");

                if (director.State.HasFlag("unknown_transmission_heard"))
                {
                    worldBinder?.EnsureCentralRuinRoute();
                }
            }
            else if (sceneName == "Demo_Combat")
            {
                for (var frame = 0; frame < 300 && FenrisFrigatePrologue.Instance == null; frame++)
                {
                    yield return null;
                }

                var frigatePrologue = FenrisFrigatePrologue.Instance;

                if (frigatePrologue != null)
                {
                    yield return BeginFenrisPrologue(frigatePrologue);
                }

                director.RaiseStoryEvent("TRG_RUINS_ENTERED");

                if (director.State.HasCompletedSequence("TRG_RUINS_ENTERED") &&
                    !director.State.HasFlag("first_robot_seen"))
                {
                    director.RaiseStoryEvent("TRG_FIRST_ROBOT");
                }

                if (director.State.HasFlag("component_core") &&
                    !director.State.HasFlag("signal_generator_crafted"))
                {
                    worldBinder?.EnsureSignalAssemblySite();
                }

                if (director.State.HasFlag("signal_generator_crafted"))
                {
                    worldBinder?.EnsureSignalInstallationSite();
                }
            }
        }

        private IEnumerator BeginFenrisPrologue(FenrisFrigatePrologue prologue)
        {
            director.RaiseStoryEvent("TRG_FENRIS_WAKE_V2");

            while (prologue != null && !prologue.HasExited)
            {
                yield return null;
            }

            if (prologue == null)
                yield break;

            director.RaiseStoryEvent("TRG_EXIT_SHIP_FIRST");

            while (!director.State.HasCompletedSequence("TRG_EXIT_SHIP_FIRST"))
            {
                yield return null;
            }

            prologue.ReleaseCombat(ShouldRunAutomaticWaves());
        }

        private bool ShouldRunAutomaticWaves()
        {
            return !director.State.signalDefenseActive &&
                   !(director.State.HasFlag("warden_k_engaged") &&
                     !director.State.HasFlag("component_core"));
        }

        private void HandleInteraction(InteractableObject source, GameObject interactor)
        {
            if (source == null)
            {
                return;
            }

            var objectName = source.gameObject.name;
            var itemName = source.ItemName ?? string.Empty;

            if (itemName.Equals("Dead Crewmate", StringComparison.OrdinalIgnoreCase))
            {
                if (objectName.Contains("Left", StringComparison.OrdinalIgnoreCase))
                {
                    director.RaiseStoryEvent("TRG_CAPTAIN_BADGE");
                    director.RaiseStoryEvent("TRG_CAPTAIN_LOG");
                }
                else
                {
                    director.RaiseStoryEvent("TRG_CREW_DISCOVERY");
                }

                return;
            }

            if (itemName.Contains("O2", StringComparison.OrdinalIgnoreCase) ||
                itemName.Contains("Oxygen", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_PICKUP_OXYGEN_FIRST");
                return;
            }

            if (itemName.Contains("Generator", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_GENERATOR_ONLINE");
                return;
            }

            if (itemName.Contains("Dematerial", StringComparison.OrdinalIgnoreCase))
            {
                if (director.State.HasFlag("component_lens") &&
                    director.State.HasFlag("component_coil") &&
                    director.State.HasFlag("component_core"))
                {
                    director.RaiseStoryEvent("TRG_SIGNAL_GENERATOR_CRAFTED");
                }
                else
                {
                    director.RaiseStoryEvent("TRG_DEMATERIALISER_FIRST_USE");
                }

                return;
            }

            if (itemName.Contains("supply", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_INVENTORY_FIRST_OPEN");
            }
        }

        private void HandlePickup(ItemData item, int quantity)
        {
            if (item == null)
            {
                return;
            }

            var itemName = item.itemName ?? string.Empty;

            if (itemName.Contains("oxygen", StringComparison.OrdinalIgnoreCase) ||
                itemName.Contains("O2", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_PICKUP_OXYGEN_FIRST");
            }
            else if (itemName.Contains("filter", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_FIELD_FILTER_CRAFTED");
            }
            else if (itemName.Contains("lens", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_COMPONENT_LENS");
            }
            else if (itemName.Contains("coil", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_COMPONENT_COIL");
            }
            else if (itemName.Contains("core", StringComparison.OrdinalIgnoreCase))
            {
                director.RaiseStoryEvent("TRG_COMPONENT_CORE");
            }
        }

        private void HandlePickupRejected(ItemData item, int quantity)
        {
            director.RaiseStoryEvent("REACT_INVENTORY_FULL");
        }

        private void HandleInventoryVisibility(bool visible)
        {
            if (visible)
            {
                director.RaiseStoryEvent("TRG_INVENTORY_FIRST_OPEN");
            }
        }

        private void HandleSafetyChanged(PlayerStats stats, bool inside)
        {
            if (inside)
            {
                director.RaiseStoryEvent("REACT_SAFE_AREA");

                if (storyStormActive)
                {
                    director.RaiseStoryEvent("TRG_STORY_STORM_SHELTER");
                }

                return;
            }

            director.RaiseStoryEvent("TRG_EXIT_SHIP_FIRST");

            if (eventManager != null && !director.State.HasCompletedSequence("EVT_TOXIC_STORM_STORY") && stormRoutine == null)
            {
                stormRoutine = StartCoroutine(BeginFirstStoryStorm());
            }
            else
            {
                eventManager?.StartEventLoop();
            }

            if (director.State.HasFlag("signal_generator_crafted"))
            {
                worldBinder?.EnsureSignalInstallationSite(stats.transform.position, stats.transform.forward);
            }
        }

        private IEnumerator BeginFirstStoryStorm()
        {
            yield return new WaitForSeconds(5f);

            if (eventManager != null)
            {
                eventManager.TriggerEvent(RandomEventType.ToxicStorm);
                eventManager.StartEventLoop();
            }

            stormRoutine = null;
        }

        private void HandleEnemyDied(EnemyHealth enemy)
        {
            director.RaiseStoryEvent("REACT_ENEMY_DEFEATED");
        }

        private void HandleEnemyDetected(EnemyController enemy)
        {
            director.RaiseStoryEvent("REACT_ENEMY_DETECTED");
        }

        private void HandleSequenceCompleted(string sequenceId)
        {
            switch (sequenceId)
            {
                case "TRG_CAPTAIN_BADGE":
                    director.RaiseStoryEvent("TRG_CAPTAIN_LOG");
                    break;
                case "TRG_RUIN_NODE_THREE":
                    director.RaiseStoryEvent("TRG_ARCHIVE_BOOT");
                    break;
                case "TRG_STORY_STORM_SHELTER":
                    director.RaiseStoryEvent("TRG_UNKNOWN_TRANSMISSION");
                    break;
                case "TRG_UNKNOWN_TRANSMISSION":
                    worldBinder?.EnsureCentralRuinRoute();
                    break;
                case "TRG_RUINS_ENTERED":
                    director.RaiseStoryEvent("TRG_FIRST_ROBOT");
                    break;
                case "TRG_COMPONENT_CORE":
                    worldBinder?.EnsureSignalAssemblySite();
                    break;
                case "TRG_SIGNAL_GENERATOR_CRAFTED":
                    worldBinder?.EnsureSignalInstallationSite();
                    break;
                case "TRG_SIGNAL_GENERATOR_INSTALLED":
                    director.RaiseStoryEvent("TRG_SIGNAL_DEFENSE");
                    break;
                case "TRG_SIGNAL_CHARGE_100":
                    director.RaiseStoryEvent("TRG_RESCUE_RESPONSE");
                    break;
                case "TRG_RESCUE_RESPONSE":
                    director.RaiseStoryEvent("TRG_EPILOGUE");
                    break;
            }
        }
    }
}
