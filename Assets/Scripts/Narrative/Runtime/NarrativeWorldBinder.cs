using System.Collections;
using System.Linq;
using DontDiePlease.Narrative.Triggers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeWorldBinder : MonoBehaviour
    {
        private const string CockpitSeatAnchor = "PilotSeat_Right";
        private const string CaptainAnchor = "PilotSeat_Left_DeadCrew";
        private const string DematerialiserAnchor = "Dematerialiser";
        private NarrativeDirector director;
        private Material terminalMaterial;
        private GameObject signalSite;
        private GameObject signalAssembly;
        private GameObject centralRoute;
        private GameObject shipRoute;
        private NarrativeSpawnAnchor signalGeneratorAnchor;
        private NarrativeSpawnAnchor signalAssemblyAnchor;

        public void Configure(NarrativeDirector narrativeDirector)
        {
            director = narrativeDirector;
            terminalMaterial = CreateTerminalMaterial();
            ResolveSignalAnchors();
            StartCoroutine(BuildSceneMilestones());
        }

        private void ResolveSignalAnchors()
        {
            signalGeneratorAnchor = FindObjectsByType<NarrativeSpawnAnchor>(FindObjectsInactive.Include)
                .FirstOrDefault(anchor =>
                    anchor != null &&
                    anchor.gameObject.scene == gameObject.scene &&
                    anchor.Kind == NarrativeAnchorKind.SignalGeneratorPlacement &&
                    anchor.AnchorId == "signal-generator-placement");
            signalAssemblyAnchor = FindObjectsByType<NarrativeSpawnAnchor>(FindObjectsInactive.Include)
                .FirstOrDefault(anchor =>
                    anchor != null &&
                    anchor.gameObject.scene == gameObject.scene &&
                    anchor.Kind == NarrativeAnchorKind.SignalGeneratorAssembly &&
                    anchor.AnchorId == "signal-generator-assembly");
        }

        private void OnDestroy()
        {
            if (terminalMaterial != null)
            {
                Destroy(terminalMaterial);
            }
        }

        public void EnsureSignalInstallationSite(Vector3 nearPosition, Vector3 forward)
        {
            if (signalSite != null)
            {
                return;
            }

            ResolveSignalAnchors();

            if (signalGeneratorAnchor == null)
            {
                Debug.LogError(
                    $"Narrative anchor 'signal-generator-placement' is missing from {SceneManager.GetActiveScene().name}.",
                    this);
                return;
            }

            signalSite = CreateTerminal(
                "SignalGeneratorInstallationSite",
                "Install signal generator",
                new[] { "TRG_SIGNAL_GENERATOR_INSTALLED" },
                signalGeneratorAnchor.transform.position,
                new Vector3(1.6f, 0.35f, 1.6f));
            signalSite.transform.rotation = signalGeneratorAnchor.transform.rotation;
        }

        public void EnsureSignalInstallationSite()
        {
            var player = FindPlayer();

            if (player != null)
            {
                EnsureSignalInstallationSite(player.position, player.forward);
            }
        }

        public void EnsureSignalAssemblySite()
        {
            if (signalAssembly != null)
            {
                return;
            }

            ResolveSignalAnchors();

            if (signalAssemblyAnchor == null)
            {
                Debug.LogError(
                    $"Narrative anchor 'signal-generator-assembly' is missing from {SceneManager.GetActiveScene().name}.",
                    this);
                return;
            }

            signalAssembly = CreateTerminal(
                "SignalGeneratorAssemblyConsole",
                "Assemble signal generator",
                new[] { "TRG_SIGNAL_GENERATOR_CRAFTED" },
                signalAssemblyAnchor.transform.position,
                new Vector3(0.8f, 1.2f, 0.55f));
            signalAssembly.transform.rotation = signalAssemblyAnchor.transform.rotation;
        }

        public void EnsureCentralRuinRoute()
        {
            if (centralRoute != null)
            {
                return;
            }

            var player = FindPlayer();

            if (player == null)
            {
                return;
            }

            centralRoute = CreatePortal(
                "CentralRuinsRoute",
                "Enter the Omphalos industrial ruins",
                "Demo_Combat",
                GroundPosition(player.position + Flatten(player.forward) * 6f));
        }

        public void EnsureShipReturnRoute()
        {
            if (shipRoute != null)
            {
                return;
            }

            var player = FindPlayer();

            if (player == null)
            {
                return;
            }

            shipRoute = CreatePortal(
                "FarKiteReturnRoute",
                "Return to the Far Kite",
                "MainGameplayScene",
                GroundPosition(player.position + Flatten(player.forward) * 6f));
        }

        private IEnumerator BuildSceneMilestones()
        {
            var sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == "MainGameplayScene")
            {
                BuildShipMilestones();
                yield break;
            }

            if (sceneName != "Demo_Combat")
            {
                yield break;
            }

            Transform player = null;

            for (var attempt = 0; attempt < 30 && player == null; attempt++)
            {
                player = FindPlayer();

                if (player == null)
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                }
            }

            var origin = player != null ? player.position : Vector3.zero;
            var forward = player != null ? Flatten(player.forward) : Vector3.forward;
            var right = new Vector3(forward.z, 0f, -forward.x);
            BuildCentralMilestones(origin, forward, right);
        }

        private void BuildShipMilestones()
        {
            var cockpitSeat = FindRequiredAnchor(CockpitSeatAnchor);

            if (cockpitSeat != null)
            {
                CreateTerminal(
                    "NavigationLogConsole",
                    "Inspect navigation log",
                    new[] { "TRG_COCKPIT_INSPECTION" },
                    cockpitSeat.transform.position + cockpitSeat.transform.forward * 0.8f + Vector3.up * 0.7f,
                    new Vector3(0.55f, 0.35f, 0.25f));
            }

            var captain = FindRequiredAnchor(CaptainAnchor);

            if (captain != null)
            {
                CreateTerminal(
                    "CaptainBadge",
                    "Captain Voss identification badge",
                    new[] { "TRG_CAPTAIN_BADGE" },
                    captain.transform.position + Vector3.up * 0.75f,
                    new Vector3(0.2f, 0.04f, 0.32f));
            }

            var dematerialiser = FindRequiredAnchor(DematerialiserAnchor);

            if (dematerialiser != null)
            {
                CreateTerminal(
                    "SignalGeneratorAssemblyConsole",
                    "Calibrate the dematerialiser",
                    new[] { "TRG_DEMATERIALISER_FIRST_USE", "TRG_FIELD_FILTER_CRAFTED" },
                    dematerialiser.transform.position + dematerialiser.transform.right * 1.6f + Vector3.up * 0.5f,
                    new Vector3(0.65f, 1f, 0.4f));
            }
        }

        private GameObject FindRequiredAnchor(string objectName)
        {
            var anchor = GameObject.Find(objectName);

            if (anchor == null)
            {
                Debug.LogError($"Narrative anchor '{objectName}' is missing from {SceneManager.GetActiveScene().name}.", this);
            }

            return anchor;
        }

        private void BuildCentralMilestones(Vector3 origin, Vector3 forward, Vector3 right)
        {
            CreateTerminal(
                "RuinPowerNodeOne",
                "Restore power node 1",
                new[] { "TRG_RUIN_NODE_ONE" },
                GroundPosition(origin + forward * 8f - right * 6f),
                new Vector3(0.8f, 1.8f, 0.8f));

            CreateTerminal(
                "RuinPowerNodeTwo",
                "Restore power node 2",
                new[] { "TRG_RUIN_NODE_TWO" },
                GroundPosition(origin + forward * 12f + right * 7f),
                new Vector3(0.8f, 1.8f, 0.8f));

            CreateTerminal(
                "RuinPowerNodeThree",
                "Restore power node 3",
                new[] { "TRG_RUIN_NODE_THREE" },
                GroundPosition(origin + forward * 18f - right * 3f),
                new Vector3(0.8f, 1.8f, 0.8f));

            CreateTerminal(
                "ResonanceLensStation",
                "Recover Resonance Lens",
                new[] { "TRG_COMPONENT_LENS" },
                GroundPosition(origin + forward * 24f + right * 11f),
                new Vector3(1.1f, 1.4f, 1.1f));

            CreateTerminal(
                "PhaseCoilMiningConsole",
                "Extract Complete Phase Coil",
                new[] { "TRG_COMPONENT_COIL" },
                GroundPosition(origin + forward * 27f - right * 11f),
                new Vector3(1.1f, 1.4f, 1.1f));

            CreateTerminal(
                "WardenKDefenseCore",
                "Confront Warden-K",
                new[] { "TRG_BOSS_WARDEN_K" },
                GroundPosition(origin + forward * 36f),
                new Vector3(1.6f, 2.2f, 1.6f));
        }

        private GameObject CreateTerminal(string objectName, string label, string[] eventIds, Vector3 position, Vector3 scale)
        {
            var terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            terminal.name = objectName;
            terminal.transform.SetPositionAndRotation(position + Vector3.up * scale.y * 0.5f, Quaternion.identity);
            terminal.transform.localScale = scale;

            var renderer = terminal.GetComponent<Renderer>();

            if (renderer != null && terminalMaterial != null)
            {
                renderer.sharedMaterial = terminalMaterial;
            }

            var interactable = terminal.AddComponent<NarrativeMilestoneInteractable>();
            interactable.Configure(label, eventIds, director);
            return terminal;
        }

        private GameObject CreatePortal(string objectName, string label, string targetScene, Vector3 position)
        {
            var portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = objectName;
            portal.transform.SetPositionAndRotation(position + Vector3.up * 0.15f, Quaternion.identity);
            portal.transform.localScale = new Vector3(1.4f, 0.15f, 1.4f);

            var renderer = portal.GetComponent<Renderer>();

            if (renderer != null && terminalMaterial != null)
            {
                renderer.sharedMaterial = terminalMaterial;
            }

            var interactable = portal.AddComponent<NarrativeScenePortal>();
            interactable.Configure(label, targetScene, director);
            return portal;
        }

        private static Transform FindPlayer()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");

            if (tagged != null)
            {
                return tagged.transform;
            }

            var stats = FindAnyObjectByType<PlayerStats>();
            return stats != null ? stats.transform : null;
        }

        private static Vector3 GroundPosition(Vector3 candidate)
        {
            if (NavMesh.SamplePosition(candidate, out var navHit, 12f, NavMesh.AllAreas))
            {
                return navHit.position;
            }

            if (Physics.Raycast(candidate + Vector3.up * 40f, Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return candidate;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
        }

        private static Material CreateTerminalMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "NarrativeTerminalMaterial"
            };
            var baseColor = new Color(0.06f, 0.2f, 0.23f, 1f);
            var emission = new Color(0.05f, 0.8f, 0.9f, 1f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            return material;
        }
    }
}
