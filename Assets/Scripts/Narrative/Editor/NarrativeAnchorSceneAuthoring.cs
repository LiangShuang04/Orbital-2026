using System.Linq;
using DontDiePlease.Narrative.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Narrative.Editor
{
    public static class NarrativeAnchorSceneAuthoring
    {
        private const string MainScenePath = "Assets/Scenes/MainGameplayScene.unity";
        private const string DemoScenePath = "Assets/Scenes/Demo_Combat.unity";

        public static void ApplyFromCommandLine()
        {
            ClearMainGameplayCombatAnchors();
            ApplyDemoCombat();
            AssetDatabase.SaveAssets();
        }

        private static void ClearMainGameplayCombatAnchors()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var root = SceneObjects(scene).FirstOrDefault(item => item.name == "NarrativeEncounterAnchors");

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            foreach (var anchor in SceneObjects(scene)
                         .Select(item => item.GetComponent<NarrativeSpawnAnchor>())
                         .Where(item => item != null)
                         .ToArray())
            {
                Object.DestroyImmediate(anchor);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyDemoCombat()
        {
            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            var root = ResetRoot(scene);
            var player = SceneObjects(scene).FirstOrDefault(item => item.name == "AkilaFPSFrameworkPlayer");
            var origin = player != null ? player.transform.position : new Vector3(14f, 2f, -31f);
            var forward = player != null ? Flatten(player.transform.forward) : Vector3.forward;
            var right = new Vector3(forward.z, 0f, -forward.x);
            var firstRobot = Snap(origin + forward * 14f, 18f);
            var warden = Snap(origin + forward * 40f, 20f);
            var assembly = Snap(origin + forward * 27f - right * 8f, 16f);
            var center = Snap(origin + forward * 8f - right * 3f, 10f);

            Create(
                root.transform,
                "First Robot Spawn",
                firstRobot,
                Quaternion.LookRotation(-forward, Vector3.up),
                "FIRST_ROBOT",
                "first-robot-spawn",
                NarrativeAnchorKind.FirstRobotSpawn);
            Create(
                root.transform,
                "Warden-K Spawn",
                warden,
                Quaternion.LookRotation(-forward, Vector3.up),
                "WARDEN_K",
                "warden-k-spawn",
                NarrativeAnchorKind.WardenSpawn);
            Create(
                root.transform,
                "Signal Generator Assembly",
                assembly,
                Quaternion.LookRotation(forward, Vector3.up),
                "SIGNAL_GENERATOR",
                "signal-generator-assembly",
                NarrativeAnchorKind.SignalGeneratorAssembly);
            Create(
                root.transform,
                "Signal Generator Placement",
                center,
                Quaternion.LookRotation(forward, Vector3.up),
                "SIGNAL_GENERATOR",
                "signal-generator-placement",
                NarrativeAnchorKind.SignalGeneratorPlacement);
            Create(
                root.transform,
                "Signal Defense Center",
                center,
                Quaternion.identity,
                "SIGNAL_DEFENSE",
                "signal-defense-center",
                NarrativeAnchorKind.DefenseCenter);
            CreateDefenseAnchor(root.transform, "North", center, new Vector3(0f, 0f, 20f));
            CreateDefenseAnchor(root.transform, "East", center, new Vector3(20f, 0f, 0f));
            CreateDefenseAnchor(root.transform, "South", center, new Vector3(0f, 0f, -20f));
            CreateDefenseAnchor(root.transform, "West", center, new Vector3(-20f, 0f, 0f));
            RemoveMissingScripts(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject ResetRoot(Scene scene)
        {
            var existing = SceneObjects(scene).FirstOrDefault(item => item.name == "NarrativeEncounterAnchors");

            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject("NarrativeEncounterAnchors");
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static void CreateDefenseAnchor(
            Transform parent,
            string direction,
            Vector3 center,
            Vector3 offset)
        {
            var position = Snap(center + offset, 12f);
            var facing = center - position;
            facing.y = 0f;
            var rotation = facing.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(facing.normalized, Vector3.up)
                : Quaternion.identity;
            Create(
                parent,
                $"Signal Defense {direction}",
                position,
                rotation,
                "SIGNAL_DEFENSE",
                $"signal-defense-{direction.ToLowerInvariant()}",
                NarrativeAnchorKind.DefenseEnemySpawn);
        }

        private static void Create(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            string encounterId,
            string anchorId,
            NarrativeAnchorKind kind)
        {
            var target = new GameObject(name);
            target.transform.SetParent(parent, false);
            target.transform.SetPositionAndRotation(position, rotation);
            Configure(target, encounterId, anchorId, kind);
        }

        private static void Configure(
            GameObject target,
            string encounterId,
            string anchorId,
            NarrativeAnchorKind kind)
        {
            var anchor = target.GetComponent<NarrativeSpawnAnchor>() ??
                         target.AddComponent<NarrativeSpawnAnchor>();
            anchor.Configure(encounterId, anchorId, kind);
            EditorUtility.SetDirty(anchor);
        }

        private static Vector3 Snap(Vector3 position, float radius)
        {
            return NavMesh.SamplePosition(position, out var hit, radius, NavMesh.AllAreas)
                ? hit.position
                : position;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
        }

        private static void RemoveMissingScripts(Scene scene)
        {
            foreach (var item in SceneObjects(scene))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item) > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item);
                }
            }
        }

        private static GameObject[] SceneObjects(Scene scene)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item != null && item.scene == scene)
                .ToArray();
        }
    }
}
