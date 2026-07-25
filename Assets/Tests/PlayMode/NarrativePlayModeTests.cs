using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DontDiePlease.Tests.PlayMode
{
    public sealed class NarrativePlayModeTests
    {
        private const string GuestSaveKey = "DontDiePlease.Narrative.State.guest";
        private const string AuthTokenKey = "DontDiePlease.Auth.Token";
        private const string AuthUserIdKey = "DontDiePlease.Auth.UserId";
        private const string AuthUsernameKey = "DontDiePlease.Auth.Username";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            DestroyNarrativeRuntimes();
            yield return null;
            PlayerPrefs.DeleteKey(GuestSaveKey);
            PlayerPrefs.DeleteKey("DontDiePlease.Narrative.State");
            PlayerPrefs.DeleteKey(AuthTokenKey);
            PlayerPrefs.DeleteKey(AuthUserIdKey);
            PlayerPrefs.DeleteKey(AuthUsernameKey);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            DestroyDetachedProjectiles();
            DestroyNarrativeRuntimes();
            yield return null;
            PlayerPrefs.DeleteKey(GuestSaveKey);
            PlayerPrefs.DeleteKey("DontDiePlease.Narrative.State");
            PlayerPrefs.DeleteKey(AuthTokenKey);
            PlayerPrefs.DeleteKey(AuthUserIdKey);
            PlayerPrefs.DeleteKey(AuthUsernameKey);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator MainMenuCreatesOneResponsiveNewGameInterface()
        {
            yield return LoadScene("MainMenuScene");
            yield return null;
            yield return null;

            var scene = SceneManager.GetActiveScene();
            var root = FindGameObject(scene, "NarrativeMainMenu");
            var canvas = FindGameObject(scene, "MainMenuCanvas");
            var newGame = FindGameObject(scene, "NewGameButton");
            var continueGame = FindGameObject(scene, "ContinueButton");
            Assert.That(root, Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(newGame, Is.Not.Null);
            Assert.That(continueGame, Is.Not.Null);
            Assert.That(newGame.GetComponent<Button>().interactable, Is.True);
            Assert.That(continueGame.GetComponent<Button>().interactable, Is.True);
            Assert.That(canvas.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(FindSceneObjects(scene, typeof(Canvas)).Length, Is.EqualTo(1));
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator ContinueKeepsTheCurrentPlaythroughState()
        {
            var stateType = RuntimeType("DontDiePlease.Narrative.Runtime.StoryState");
            var adapterType = RuntimeType("DontDiePlease.Narrative.Persistence.NarrativeSaveAdapter");
            var state = Activator.CreateInstance(stateType);
            SetField(state, "currentObjectiveId", "ACT3_COMPONENTS");
            SetField(state, "worldSeed", 882211);
            SetField(state, "playthroughId", "continue-run");
            SetField(state, "startedAtUnixMs", 1000L);
            Invoke(state, "SetFlag", "phase_coil");
            var saveObject = new GameObject("ContinueSaveSetup");
            var adapter = saveObject.AddComponent(adapterType);
            Invoke(adapter, "SaveLocal", state);
            UnityEngine.Object.Destroy(saveObject);
            yield return null;

            yield return LoadScene("MainMenuScene");
            var continueButton = FindGameObject(SceneManager.GetActiveScene(), "ContinueButton");
            Assert.That(continueButton, Is.Not.Null);
            continueButton.GetComponent<Button>().onClick.Invoke();
            yield return WaitForActiveScene("Demo_Combat", 600);
            var scene = SceneManager.GetActiveScene();
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeDirector",
                300);
            var director = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeDirector");
            yield return WaitForReady(director, 300);
            var loadedState = GetProperty(director, "State");

            Assert.That(GetField<string>(loadedState, "currentObjectiveId"), Is.EqualTo("ACT3_COMPONENTS"));
            Assert.That(GetField<int>(loadedState, "worldSeed"), Is.EqualTo(882211));
            Assert.That(GetField<string>(loadedState, "playthroughId"), Is.EqualTo("continue-run"));
            Assert.That((bool)Invoke(loadedState, "HasFlag", "phase_coil"), Is.True);
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator NewGameClearsThePreviousPlaythroughThroughTheMenu()
        {
            var stateType = RuntimeType("DontDiePlease.Narrative.Runtime.StoryState");
            var adapterType = RuntimeType("DontDiePlease.Narrative.Persistence.NarrativeSaveAdapter");
            var state = Activator.CreateInstance(stateType);
            SetField(state, "currentObjectiveId", "ACT7_DEFEND");
            SetField(state, "worldSeed", 111);
            SetField(state, "playthroughId", "finished-run");
            SetField(state, "startedAtUnixMs", 1000L);
            SetField(state, "signalDefenseActive", true);
            SetField(state, "signalDefenseRemainingSeconds", 30f);
            Invoke(state, "SetFlag", "story_complete");
            Invoke(state, "CompleteSequence", "TRG_EPILOGUE");
            var saveObject = new GameObject("NewGameSaveSetup");
            var adapter = saveObject.AddComponent(adapterType);
            Invoke(adapter, "SaveLocal", state);
            UnityEngine.Object.Destroy(saveObject);
            yield return null;

            yield return LoadScene("MainMenuScene");
            var newGameButton = FindGameObject(SceneManager.GetActiveScene(), "NewGameButton");
            Assert.That(newGameButton, Is.Not.Null);
            newGameButton.GetComponent<Button>().onClick.Invoke();
            yield return WaitForActiveScene("Demo_Combat", 600);
            var scene = SceneManager.GetActiveScene();
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeDirector",
                300);
            var director = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeDirector");
            yield return WaitForReady(director, 300);
            var newState = GetProperty(director, "State");

            Assert.That(GetField<string>(newState, "currentObjectiveId"), Is.EqualTo("ACT1_WAKE"));
            Assert.That(GetField<int>(newState, "worldSeed"), Is.Not.Zero.And.Not.EqualTo(111));
            Assert.That(GetField<string>(newState, "playthroughId"), Is.Not.Empty.And.Not.EqualTo("finished-run"));
            Assert.That(GetField<bool>(newState, "signalDefenseActive"), Is.False);
            Assert.That(GetField<float>(newState, "signalDefenseRemainingSeconds"), Is.Zero);
            Assert.That(ListCount(newState, "completedSequenceIds"), Is.Zero);
            Assert.That(ListCount(newState, "completedObjectiveIds"), Is.Zero);
            Assert.That(ListCount(newState, "flags"), Is.Zero);
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator DemoCombatGatesTheAuthoredFirstRobotUntilItsStoryEvent()
        {
            yield return LoadScene("Demo_Combat");
            yield return WaitForConfiguredSpawner(SceneManager.GetActiveScene(), 600);
            yield return WaitForRuntimeObject(
                SceneManager.GetActiveScene(),
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator",
                300);

            var scene = SceneManager.GetActiveScene();
            var coordinator = FindRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator");
            yield return WaitForReady(coordinator, 600);
            var anchor = FindGameObject(scene, "First Robot Spawn", true);
            Assert.That(anchor, Is.Not.Null);
            Assert.That(anchor.activeSelf, Is.False);
            Assert.That(FindGameObject(scene, "NarrativeFirstRobot", true), Is.Null);
            yield return WaitForGameObject(scene, "RuinPowerNodeOne", 300);
            Assert.That(Resources.Load("Narrative/NarrativeCombatBindings"), Is.Not.Null);
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator DemoCombatAuthoredAnchorsAreUniqueAndReachable()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator",
                300);
            var coordinator = FindRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator");
            yield return WaitForReady(coordinator, 600);
            Assert.That(
                Resources.FindObjectsOfTypeAll<EventSystem>().Count(eventSystem =>
                    eventSystem != null &&
                    eventSystem.gameObject.scene.IsValid() &&
                    eventSystem.gameObject.activeInHierarchy),
                Is.EqualTo(1));
            var industry = FindGameObject(scene, "Environment");
            Assert.That(industry, Is.Not.Null);
            Assert.That(FindGameObject(scene, "Building01"), Is.Not.Null);
            Assert.That(FindGameObject(scene, "Building02"), Is.Not.Null);
            Assert.That(industry.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(10000));
            Assert.That(industry.GetComponentsInChildren<Collider>(true).Length, Is.GreaterThan(8000));
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.Not.Null);
            Assert.That(GraphicsSettings.currentRenderPipeline.GetType().Name, Does.Contain("UniversalRenderPipeline"));
            var sceneMaterials = industry.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            var invalidMaterials = sceneMaterials
                .Where(material =>
                    material.shader == null ||
                    !material.shader.isSupported ||
                    material.shader.name == "Hidden/InternalErrorShader")
                .Select(material => material.name)
                .Distinct()
                .ToArray();
            Assert.That(sceneMaterials.Length, Is.GreaterThan(50));
            Assert.That(invalidMaterials, Is.Empty, string.Join(", ", invalidMaterials));
            var anchorType = RuntimeType("DontDiePlease.Narrative.Runtime.NarrativeSpawnAnchor");
            var sceneAnchors = FindSceneObjects(scene, anchorType).OfType<Component>().ToArray();
            Assert.That(sceneAnchors.Length, Is.EqualTo(9));
            var ids = sceneAnchors.Select(anchor => GetProperty<string>(anchor, "AnchorId")).ToArray();
            Assert.That(ids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(ids.Length));

            var center = sceneAnchors.Single(anchor =>
                GetProperty<string>(anchor, "AnchorId") == "signal-defense-center");
            var defenseAnchors = sceneAnchors.Where(anchor =>
                GetProperty(anchor, "Kind").ToString() == "DefenseEnemySpawn").ToArray();
            Assert.That(defenseAnchors.Length, Is.EqualTo(4));

            foreach (var anchor in defenseAnchors)
            {
                Assert.That(
                    UnityEngine.AI.NavMesh.SamplePosition(
                        anchor.transform.position,
                        out var hit,
                        1f,
                        UnityEngine.AI.NavMesh.AllAreas),
                    Is.True);
                Assert.That(Vector3.Distance(hit.position, center.transform.position), Is.InRange(10f, 32f));
                var path = new UnityEngine.AI.NavMeshPath();
                Assert.That(
                    UnityEngine.AI.NavMesh.CalculatePath(
                        hit.position,
                        center.transform.position,
                        UnityEngine.AI.NavMesh.AllAreas,
                        path),
                    Is.True);
                Assert.That(path.status, Is.EqualTo(UnityEngine.AI.NavMeshPathStatus.PathComplete));
            }

            var duplicate = new GameObject("DuplicateNarrativeAnchor");
            var duplicateAnchor = duplicate.AddComponent(anchorType);
            var kindType = RuntimeType("DontDiePlease.Narrative.Runtime.NarrativeAnchorKind");
            Invoke(
                duplicateAnchor,
                "Configure",
                "SIGNAL_DEFENSE",
                "signal-defense-east",
                Enum.Parse(kindType, "DefenseEnemySpawn"),
                1f);
            LogAssert.Expect(
                LogType.Error,
                "Duplicate narrative anchor ID 'signal-defense-east' exists in Demo_Combat.");
            Invoke(coordinator, "CacheAnchors");
            var cachedAnchors = (IDictionary)GetField<object>(coordinator, "anchors");
            Assert.That(cachedAnchors.Contains("signal-defense-east"), Is.False);
            UnityEngine.Object.Destroy(duplicate);
            yield return null;
            Invoke(coordinator, "CacheAnchors");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator DemoCombatStartsInsideFenrisAndReleasesEnemiesAfterDeparture()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Central.Combat.FenrisFrigatePrologue",
                300);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeDirector",
                300);

            var prologue = FindRuntimeObject(
                scene,
                "DontDiePlease.Central.Combat.FenrisFrigatePrologue");
            var director = FindRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeDirector");
            var spawner = FindSceneObjects(
                scene,
                RuntimeType("DontDiePlease.Central.Combat.CentralCombatSpawner")).Single();
            yield return WaitForReady(director, 300);

            for (var frame = 0;
                 frame < 120 && GetProperty<string>(director, "ActiveSequenceId") != "TRG_FENRIS_WAKE_V2";
                 frame++)
            {
                yield return null;
            }

            var state = GetProperty(director, "State");
            Assert.That(
                GetProperty<string>(director, "ActiveSequenceId"),
                Is.EqualTo("TRG_FENRIS_WAKE_V2"),
                $"objective={GetField<string>(state, "currentObjectiveId")} " +
                $"flags={string.Join(",", ((IList)GetField<object>(state, "flags")).Cast<object>())} " +
                $"sequences={string.Join(",", ((IList)GetField<object>(state, "completedSequenceIds")).Cast<object>())} " +
                $"inside={GetProperty<bool>(prologue, "IsPlayerInside")} " +
                $"exited={GetProperty<bool>(prologue, "HasExited")}");
            Assert.That(GetProperty<bool>(prologue, "BlocksCombat"), Is.True);
            Assert.That(GetProperty<bool>(prologue, "HasExited"), Is.False);
            Assert.That(GetProperty<bool>(prologue, "IsPlayerInside"), Is.True);
            Assert.That(GetProperty<bool>(prologue, "AirlockOpened"), Is.False);
            Assert.That(GetProperty<bool>(prologue, "TutorialComplete"), Is.False);
            Assert.That(GetProperty<bool>(prologue, "IsAirlockInteractionAvailable"), Is.False);
            var airlockAnchor = GetProperty<Transform>(prologue, "AirlockAnchor");
            var interiorSpawnAnchor = GetProperty<Transform>(prologue, "InteriorSpawnAnchor");
            var interiorSpawn = GetProperty<Vector3>(prologue, "InteriorSpawnPosition");
            Assert.That(airlockAnchor, Is.Not.Null);
            Assert.That(interiorSpawnAnchor, Is.Not.Null);
            Assert.That(interiorSpawnAnchor.name, Does.Contain("Bridge").IgnoreCase);
            Assert.That(Vector3.Distance(interiorSpawn, interiorSpawnAnchor.position), Is.LessThan(2.8f));
            Assert.That(GetProperty<bool>(prologue, "InteriorSpawnIsClear"), Is.True);
            Assert.That(GetProperty<int>(prologue, "InteriorRoomCount"), Is.GreaterThanOrEqualTo(10));
            Assert.That(GetProperty<int>(prologue, "InteriorVolumeCount"), Is.GreaterThanOrEqualTo(10));
            Assert.That(GetProperty<int>(prologue, "SolidColliderCount"), Is.GreaterThan(50));
            Assert.That(GetProperty<int>(prologue, "InteractableCount"), Is.GreaterThan(20));
            Assert.That(GetProperty<int>(prologue, "TutorialStationCount"), Is.EqualTo(4));
            Assert.That(GetProperty<int>(prologue, "CompletedTutorialStationCount"), Is.Zero);
            Assert.That(
                GetProperty<int>(prologue, "VisibleInteriorRoomCount"),
                Is.EqualTo(GetProperty<int>(prologue, "InteriorRoomCount")));
            Assert.That(GetProperty<float>(prologue, "ShipDistanceFromMapSpawn"), Is.LessThan(90f));
            var airlockPrompt = FindGameObject(scene, "OpenAirlockPrompt", true);
            Assert.That(airlockPrompt, Is.Not.Null);
            var tutorialPanel = FindGameObject(scene, "FenrisTutorialPanel");
            Assert.That(tutorialPanel, Is.Not.Null);
            Assert.That(tutorialPanel.activeInHierarchy, Is.True);
            var tutorialText = FindGameObject(scene, "TutorialText");
            Assert.That(tutorialText, Is.Not.Null);
            var tutorialLabel = tutorialText.GetComponent(RuntimeType("TMPro.TextMeshProUGUI", "Unity.TextMeshPro"));
            Assert.That(GetProperty<string>(tutorialLabel, "text"), Does.Contain("USE WASD TO MOVE"));
            var combatInfo = FindGameObject(scene, "CombatInfo", true);
            Assert.That(combatInfo, Is.Not.Null);
            Assert.That(combatInfo.activeInHierarchy, Is.False);
            Assert.That(
                Resources.FindObjectsOfTypeAll<GameObject>()
                    .Count(item =>
                        item != null &&
                        item.scene == scene &&
                        item.name.StartsWith("TutorialStation_", StringComparison.Ordinal)),
                Is.Zero);
            Assert.That(FindNamedSceneObjects(scene, "StationLight").Length, Is.Zero);
            var subtitle = FindGameObject(scene, "ExplorationSubtitle");
            var fullDialogue = FindGameObject(scene, "FullDialogue", true);
            Assert.That(subtitle, Is.Not.Null);
            Assert.That(subtitle.activeInHierarchy, Is.True);
            Assert.That(subtitle.GetComponent<RectTransform>().sizeDelta.y, Is.LessThanOrEqualTo(160f));
            var subtitleSpeakerRect = subtitle.transform.Find("Speaker").GetComponent<RectTransform>();
            var subtitleTextRect = subtitle.transform.Find("Subtitle").GetComponent<RectTransform>();
            var speakerCorners = new Vector3[4];
            var subtitleCorners = new Vector3[4];
            subtitleSpeakerRect.GetWorldCorners(speakerCorners);
            subtitleTextRect.GetWorldCorners(subtitleCorners);
            Assert.That(speakerCorners[0].y, Is.GreaterThanOrEqualTo(subtitleCorners[1].y));
            Assert.That(fullDialogue, Is.Not.Null);
            Assert.That(fullDialogue.activeInHierarchy, Is.False);
            var fullSpeakerRect = fullDialogue.transform.Find("Speaker").GetComponent<RectTransform>();
            var fullTextRect = fullDialogue.transform.Find("Dialogue").GetComponent<RectTransform>();
            var fullSpeakerCorners = new Vector3[4];
            var fullTextCorners = new Vector3[4];
            fullSpeakerRect.GetWorldCorners(fullSpeakerCorners);
            fullTextRect.GetWorldCorners(fullTextCorners);
            Assert.That(fullSpeakerCorners[0].y, Is.GreaterThanOrEqualTo(fullTextCorners[1].y));
            var placementBounds = GetField<Bounds>(prologue, "shipBounds");
            var landingTerrain = Terrain.activeTerrains.Single(terrain => terrain.gameObject.scene == scene);
            var terrainMin = landingTerrain.transform.position;
            var terrainMax = terrainMin + landingTerrain.terrainData.size;
            Assert.That(placementBounds.min.x, Is.GreaterThanOrEqualTo(terrainMin.x));
            Assert.That(placementBounds.max.x, Is.LessThanOrEqualTo(terrainMax.x));
            Assert.That(placementBounds.min.z, Is.GreaterThanOrEqualTo(terrainMin.z));
            Assert.That(placementBounds.max.z, Is.LessThanOrEqualTo(terrainMax.z));
            Assert.That(GetProperty<int>(spawner, "ActiveEnemyCount"), Is.Zero);

            var ship = GetProperty<GameObject>(prologue, "Ship");
            Assert.That(ship, Is.Not.Null);
            Assert.That(ship.name, Is.EqualTo("FenrisFrigate"));
            var renderers = ship.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(100));
            var materials = renderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            var invalidMaterials = materials
                .Where(material =>
                    material.shader == null ||
                    !material.shader.isSupported ||
                    material.shader.name == "Hidden/InternalErrorShader")
                .Select(material => material.name)
                .Distinct()
                .ToArray();
            Assert.That(materials.Length, Is.GreaterThan(20));
            Assert.That(invalidMaterials, Is.Empty, string.Join(", ", invalidMaterials));

            var player = GetField<Transform>(prologue, "player");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(GetProperty<bool>(prologue, "IsPlayerInside"), Is.True);
            Assert.That(Vector3.Distance(player.position, interiorSpawn), Is.LessThan(2f));
            Assert.That(GetProperty<bool>(prologue, "MapEntryIsClear"), Is.True);

            SetField(prologue, "departureCheckAt", 0f);
            Invoke(
                prologue,
                "MovePlayer",
                placementBounds.max + new Vector3(4f, 4f, 4f),
                Quaternion.identity);
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(GetProperty<bool>(prologue, "IsPlayerInside"), Is.True);

            SetField(prologue, "tutorialStep", 6);
            Invoke(
                prologue,
                "MovePlayer",
                GetProperty<Vector3>(prologue, "AirlockApproachPosition"),
                Quaternion.identity);
            yield return null;
            Assert.That(GetProperty<bool>(prologue, "TutorialComplete"), Is.True);
            Assert.That(GetProperty<bool>(prologue, "IsAirlockInteractionAvailable"), Is.True);
            Invoke(prologue, "OpenAirlock");
            yield return null;
            Assert.That(GetProperty<bool>(prologue, "AirlockOpened"), Is.True);
            Assert.That(tutorialPanel.activeInHierarchy, Is.True);
            Assert.That(
                GetProperty<string>(tutorialLabel, "text"),
                Does.Contain("PRESS E AGAIN TO EXIT"));
            var mapEntry = GetProperty<Vector3>(prologue, "MapEntryPosition");
            Invoke(prologue, "Disembark");
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(player.position, mapEntry), Is.LessThan(0.2f));
            Assert.That(tutorialPanel.activeInHierarchy, Is.False);
            var playerInput = player.GetComponentsInChildren<Behaviour>(true)
                .FirstOrDefault(component => component.GetType().Name == "CharacterInput");
            Assert.That(playerInput, Is.Not.Null);
            Assert.That(playerInput.enabled, Is.True);
            Invoke(prologue, "ReleaseCombat", true);
            yield return null;
            Assert.That(combatInfo.activeInHierarchy, Is.True);
            yield return new WaitForSeconds(2.5f);

            var airlockDoors = ship.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(behaviour =>
                    behaviour != null &&
                    behaviour.GetType().Name == "VattalusDoorController" &&
                    behaviour.gameObject.name.Contains("Airlock", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.That(airlockDoors, Is.Not.Empty);
            Assert.That(airlockDoors.All(door => (bool)Invoke(door, "isDoorOpen")), Is.True);
            Assert.That(GetProperty<bool>(prologue, "HasExited"), Is.True);
            Assert.That(GetProperty<bool>(prologue, "BlocksCombat"), Is.False);
            Assert.That(
                GetProperty<int>(spawner, "ActiveEnemyCount"),
                Is.GreaterThan(0),
                $"wave={GetProperty<int>(spawner, "CurrentWave")} " +
                $"configured={GetProperty<bool>(spawner, "IsConfigured")} " +
                $"encounter={GetField<bool>(spawner, "encounterActive")} " +
                $"automatic={GetField<bool>(spawner, "automaticWavesEnabled")} " +
                $"timer={GetField<float>(spawner, "nextWaveTimer"):0.00} " +
                $"timeScale={Time.timeScale:0.00}");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator DemoCombatSpawnsEveryRobotTypeAndEnemiesAttackThePlayer()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator",
                300);

            var coordinator = FindRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator");
            yield return WaitForReady(coordinator, 600);

            var spawnerType = RuntimeType("DontDiePlease.Central.Combat.CentralCombatSpawner");
            var enemyType = RuntimeType("DontDiePlease.Central.Combat.CentralCombatEnemy");
            var visualType = RuntimeType("DontDiePlease.Central.Combat.CentralEnemyVisualDriver");
            var spawner = FindSceneObjects(scene, spawnerType).Single();
            Invoke(spawner, "SetAutomaticWaves", false, true);
            SetField(spawner, "wave", 0);
            Invoke(spawner, "SpawnNextWave");
            yield return null;

            var enemies = FindSceneObjects(scene, enemyType).OfType<Component>().ToArray();
            Assert.That(enemies.Length, Is.EqualTo(4));
            var archetypes = enemies
                .Select(enemy => GetField<object>(GetProperty(enemy, "Config"), "archetype").ToString())
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(archetypes, Is.EqualTo(new[] { "Heavy", "Rusher", "Shooter", "Stalker" }));

            foreach (var enemy in enemies)
            {
                var driver = enemy.GetComponent(visualType);
                var animator = enemy.GetComponentInChildren<Animator>(true);
                var renderers = enemy.GetComponentsInChildren<Renderer>(true);
                Assert.That(driver, Is.Not.Null, enemy.name);
                Assert.That(animator, Is.Not.Null, enemy.name);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null, enemy.name);
                Assert.That(renderers.Any(renderer => renderer != null && renderer.enabled), Is.True, enemy.name);
                var visibleMaterials = renderers
                    .Where(renderer => renderer != null && renderer.enabled)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .ToArray();
                Assert.That(visibleMaterials, Is.Not.Empty, enemy.name);
                Assert.That(
                    visibleMaterials.All(material =>
                        material.shader != null &&
                        material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)),
                    Is.True,
                    enemy.name);
                Assert.That(
                    visibleMaterials.Any(material =>
                        material.HasProperty("_BaseMap") &&
                        material.GetTexture("_BaseMap") != null),
                    Is.True,
                    enemy.name);
                Assert.That(GetField<int>(driver, "attackState"), Is.Not.Zero, enemy.name);
            }

            var attackPoint = enemies[0].transform.position;
            Invoke(spawner, "ClearActiveEnemies");
            yield return null;

            var characterType = RuntimeType("Akila.FPSFramework.CharacterManager", "Akila.FPSFramework");
            var character = FindSceneObjects(scene, characterType).OfType<Component>().Single();
            var damageableType = RuntimeType("Akila.FPSFramework.Damageable", "Akila.FPSFramework");
            var playerHealth = character.GetComponentInChildren(damageableType, true);
            Assert.That(playerHealth, Is.Not.Null);
            var before = GetField<float>(playerHealth, "health");

            Assert.That(
                UnityEngine.AI.NavMesh.SamplePosition(
                    attackPoint,
                    out var hit,
                    1f,
                    UnityEngine.AI.NavMesh.AllAreas),
                Is.True);
            character.transform.position = hit.position + Vector3.up * 0.08f;
            Physics.SyncTransforms();

            var configType = RuntimeType("DontDiePlease.Central.Combat.CentralCombatEnemyConfig");
            var rusherFactory = configType.GetMethod("Rusher", BindingFlags.Public | BindingFlags.Static);
            Assert.That(rusherFactory, Is.Not.Null);
            var rusherConfig = rusherFactory.Invoke(null, null);
            var attacker = Invoke(spawner, "SpawnEncounterEnemy", rusherConfig, hit.position) as Component;
            Assert.That(attacker, Is.Not.Null);
            var aiType = RuntimeType("DontDiePlease.Central.Combat.CentralCombatEnemyAI");
            var attackerAi = attacker.GetComponent(aiType);
            var attackerAgent = attacker.GetComponent<UnityEngine.AI.NavMeshAgent>();
            SetField(attackerAi, "attackTimer", -0.01f);

            for (var frame = 0; frame < 180 && GetField<object>(attackerAi, "state").ToString() != "Attack"; frame++)
            {
                yield return null;
            }

            Assert.That(GetField<object>(attackerAi, "state").ToString(), Is.EqualTo("Attack"));
            SetField(attackerAi, "windupTimer", 0f);
            yield return null;
            Assert.That(
                GetField<float>(playerHealth, "health"),
                Is.LessThan(before),
                $"state={GetField<object>(attackerAi, "state")} " +
                $"target={GetProperty<bool>(attackerAi, "HasTarget")} " +
                $"canSee={Invoke(attackerAi, "CanSeeTarget")} " +
                $"distance={Vector3.Distance(attacker.transform.position, character.transform.position):0.00} " +
                $"onNavMesh={attackerAgent.isOnNavMesh} " +
                $"attackTimer={GetField<float>(attackerAi, "attackTimer"):0.00} " +
                $"actionEndsAt={GetField<float>(attacker.GetComponent(visualType), "actionEndsAt"):0.00}");
            var attackDriver = attacker.GetComponent(visualType);
            var attackAnimator = attacker.GetComponentInChildren<Animator>(true);
            var attackHash = GetField<int>(attackDriver, "attackState");
            var state = attackAnimator.GetCurrentAnimatorStateInfo(0);
            Assert.That(
                state.shortNameHash == attackHash ||
                state.fullPathHash == attackHash ||
                attackAnimator.IsInTransition(0),
                Is.True);

            var pausedHealth = GetField<float>(playerHealth, "health");
            Invoke(attackerAi, "BeginAttack");
            SetField(attackerAi, "windupTimer", 0f);
            SetField(attackerAi, "attackResolved", false);
            Time.timeScale = 0f;
            Invoke(attackerAi, "Update");
            Assert.That(GetField<float>(playerHealth, "health"), Is.EqualTo(pausedHealth));
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator FirstRobotProgressesOnlyFromItsOwnDeathEvent()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator",
                300);
            yield return null;

            var director = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeDirector");
            var coordinator = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator");
            yield return WaitForReady(coordinator, 600);
            var state = GetProperty(director, "State");
            Invoke(state, "SetFlag", "first_robot_seen");
            Invoke(coordinator, "SpawnFirstRobot");
            yield return null;

            var robot = FindGameObject(scene, "NarrativeFirstRobot");
            Assert.That(robot, Is.Not.Null);
            var healthType = RuntimeType("EnemyHealth");
            var health = robot.GetComponent(healthType);
            Assert.That(health, Is.Not.Null);
            Invoke(health, "TakeDamage", 10000f);
            yield return null;
            Assert.That((bool)Invoke(state, "HasFlag", "mechanical_component"), Is.True);
            yield return SkipUntilFlag(director, state, "first_robot_defeated", 120);
            Assert.That((bool)Invoke(state, "HasFlag", "first_robot_defeated"), Is.True);

            Invoke(coordinator, "SpawnFirstRobot");
            yield return null;
            var remaining = FindSceneObjects(scene, healthType)
                .OfType<Component>()
                .Count(component => component.gameObject.name == "NarrativeFirstRobot");
            Assert.That(remaining, Is.LessThanOrEqualTo(1));
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator RestoredDefenseSpawnsCappedEnemiesAndStopsWhilePaused()
        {
            var stateType = RuntimeType("DontDiePlease.Narrative.Runtime.StoryState");
            var adapterType = RuntimeType("DontDiePlease.Narrative.Persistence.NarrativeSaveAdapter");
            var savedState = Activator.CreateInstance(stateType);
            SetField(savedState, "worldSeed", 442211);
            SetField(savedState, "playthroughId", "defense-run");
            SetField(savedState, "startedAtUnixMs", 2000L);
            SetField(savedState, "signalDefenseActive", true);
            SetField(savedState, "signalDefenseRemainingSeconds", 108f);
            Invoke(savedState, "SetFlag", "signal_defense_started");
            Invoke(savedState, "CompleteSequence", "TRG_SIGNAL_CHARGE_25");
            var saveObject = new GameObject("DefenseSaveSetup");
            var adapter = saveObject.AddComponent(adapterType);
            Invoke(adapter, "SaveLocal", savedState);
            UnityEngine.Object.Destroy(saveObject);
            yield return null;

            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator",
                300);
            yield return null;
            var director = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeDirector");
            var coordinator = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator");
            yield return WaitForReady(director, 300);
            yield return WaitForReady(coordinator, 600);
            var state = GetProperty(director, "State");
            Assert.That(GetField<bool>(state, "signalDefenseActive"), Is.True);
            Assert.That(GetField<float>(state, "signalDefenseRemainingSeconds"), Is.LessThanOrEqualTo(108f));
            SetField(coordinator, "nextWaveAt", -1f);
            Invoke(coordinator, "UpdateDefenseWaves");
            yield return null;

            var firstWave = FindNamedSceneObjects(scene, "SignalDefenseRobot");
            Assert.That(firstWave.Length, Is.GreaterThan(0).And.LessThanOrEqualTo(3));
            var anchorType = RuntimeType("DontDiePlease.Narrative.Runtime.NarrativeSpawnAnchor");
            var defenseAnchors = FindSceneObjects(scene, anchorType)
                .OfType<Component>()
                .Where(anchor => GetProperty(anchor, "Kind").ToString() == "DefenseEnemySpawn")
                .ToArray();
            var usedAnchorIds = firstWave.Select(enemy =>
            {
                var nearest = defenseAnchors.OrderBy(anchor =>
                    Vector3.Distance(anchor.transform.position, enemy.transform.position)).First();
                Assert.That(Vector3.Distance(nearest.transform.position, enemy.transform.position), Is.LessThan(0.2f));
                return GetProperty<string>(nearest, "AnchorId");
            }).ToArray();
            Assert.That(usedAnchorIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(firstWave.Length));

            Time.timeScale = 0f;
            yield return null;
            SetField(coordinator, "nextWaveAt", -1f);
            Invoke(coordinator, "UpdateDefenseWaves");
            yield return null;
            Assert.That(FindNamedSceneObjects(scene, "SignalDefenseRobot").Length, Is.EqualTo(firstWave.Length));

            Time.timeScale = 1f;
            SetField(state, "signalDefenseActive", false);
            Invoke(coordinator, "UpdateDefenseWaves");
            yield return null;
            Assert.That(FindNamedSceneObjects(scene, "SignalDefenseRobot").Length, Is.Zero);
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator AkilaPistolDamagesEnemyHealthThroughTheFrameworkHitPath()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Central.Combat.FenrisFrigatePrologue",
                300);
            var prologue = FindRuntimeObject(
                scene,
                "DontDiePlease.Central.Combat.FenrisFrigatePrologue");
            Invoke(prologue, "SkipToMap");
            yield return null;
            var characterType = RuntimeType("Akila.FPSFramework.CharacterManager", "Akila.FPSFramework");
            var character = FindSceneObjects(scene, characterType).OfType<Component>().Single();
            var firearm = default(Component);
            yield return WaitForPistol(scene, character.transform, value => firearm = value, 300);
            Assert.That(firearm, Is.Not.Null);

            var bindings = Resources.Load("Narrative/NarrativeCombatBindings");
            Assert.That(bindings, Is.Not.Null);
            var prefab = GetProperty<GameObject>(bindings, "FirstRobotPrefab");
            Assert.That(
                UnityEngine.AI.NavMesh.SamplePosition(
                    character.transform.position + character.transform.forward * 5f,
                    out var robotPosition,
                    12f,
                    UnityEngine.AI.NavMesh.AllAreas),
                Is.True);
            var robot = UnityEngine.Object.Instantiate(prefab, robotPosition.position, Quaternion.identity);
            robot.name = "FirearmAdapterTestRobot";
            var health = robot.GetComponent(RuntimeType("EnemyHealth"));
            var adapterType = RuntimeType("DontDiePlease.Narrative.Runtime.EnemyHealthDamageAdapter");
            var adapter = robot.GetComponent(adapterType) ?? robot.AddComponent(adapterType);
            Assert.That(health, Is.Not.Null);
            Assert.That(adapter, Is.Not.Null);
            yield return null;

            var damage = GetFirearmDamage(firearm);
            var before = GetProperty<float>(health, "CurrentHealth");
            ApplyFirearmHit(firearm, FindDamageCollider(robot), damage);
            var after = GetProperty<float>(health, "CurrentHealth");
            Assert.That(after, Is.EqualTo(Mathf.Max(0f, before - damage)).Within(0.001f));

            while (!(bool)GetProperty(health, "IsDead"))
            {
                ApplyFirearmHit(firearm, FindDamageCollider(robot), damage);
            }

            var deadHealth = GetProperty<float>(health, "CurrentHealth");
            Invoke(adapter, "Damage", damage, character.gameObject);
            Assert.That(GetProperty<float>(health, "CurrentHealth"), Is.EqualTo(deadHealth));
            UnityEngine.Object.Destroy(robot);
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator DemoCombatKeepsTheAkilaPlayerGroundedAndRecoversFalls()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Central.Combat.FenrisFrigatePrologue",
                300);

            var characterType = RuntimeType("Akila.FPSFramework.CharacterManager", "Akila.FPSFramework");
            var character = FindSceneObjects(scene, characterType).OfType<Component>().Single();
            var prologue = FindRuntimeObject(
                scene,
                "DontDiePlease.Central.Combat.FenrisFrigatePrologue");
            var groundingType = RuntimeType("DontDiePlease.Central.Combat.CentralPlayerGrounding");
            var grounding = character.GetComponent(groundingType);
            var controller = character.GetComponent<CharacterController>();
            Assert.That(grounding, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);

            for (var frame = 0; frame < 120; frame++)
                yield return null;

            var groundHit = Physics.RaycastAll(
                    character.transform.position + Vector3.up * 0.5f,
                    Vector3.down,
                    10f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                .Where(hit =>
                    hit.transform != character.transform &&
                    !hit.transform.IsChildOf(character.transform))
                .OrderBy(hit => hit.distance)
                .FirstOrDefault();
            Assert.That(groundHit.collider, Is.Not.Null);
            var groundHeight = groundHit.point.y;
            Assert.That(character.transform.position.y, Is.InRange(groundHeight - 0.1f, groundHeight + 0.6f));

            var terrain = Terrain.activeTerrains.Single(value => value.gameObject.scene == scene);
            var terrainHeight = terrain.SampleHeight(character.transform.position) + terrain.transform.position.y;
            character.transform.position = new Vector3(
                character.transform.position.x,
                terrainHeight - 8f,
                character.transform.position.z);
            Physics.SyncTransforms();

            for (var frame = 0; frame < 12; frame++)
                yield return null;

            Assert.That(character.transform.position.y, Is.GreaterThanOrEqualTo(groundHeight - 0.1f));
            Assert.That(GetProperty<bool>(prologue, "IsPlayerInside"), Is.True);
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator DemoCombatDeathRestoresAkilaPlayerCameraAndWeapon()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            yield return WaitForRuntimeObject(
                scene,
                "DontDiePlease.Narrative.Runtime.NarrativeDirector",
                300);

            var characterType = RuntimeType("Akila.FPSFramework.CharacterManager", "Akila.FPSFramework");
            var damageableType = RuntimeType("Akila.FPSFramework.Damageable", "Akila.FPSFramework");
            var recoveryType = RuntimeType("DontDiePlease.Central.Combat.CentralPlayerRecovery");
            var original = FindSceneObjects(scene, characterType).OfType<Component>().Single();
            var recovery = original.GetComponent(recoveryType);
            var damageable = original.GetComponentInChildren(damageableType, true);
            Assert.That(recovery, Is.Not.Null);
            Assert.That(damageable, Is.Not.Null);

            yield return null;
            yield return null;
            Invoke(damageable, "Damage", 1000f, original.gameObject);

            var director = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeDirector");

            for (var frame = 0; frame < 180 && GetProperty<string>(director, "ActiveSequenceId") != "REACT_FIRST_DEATH"; frame++)
                yield return null;

            Assert.That(GetProperty<string>(director, "ActiveSequenceId"), Is.EqualTo("REACT_FIRST_DEATH"));
            Invoke(director, "StopActiveSequence", true, true);

            Component replacement = null;

            for (var frame = 0; frame < 180 && replacement == null; frame++)
            {
                replacement = FindSceneObjects(scene, characterType)
                    .OfType<Component>()
                    .FirstOrDefault(item => item != null && item != original && item.gameObject.activeInHierarchy);
                yield return null;
            }

            Assert.That(replacement, Is.Not.Null);
            Assert.That(original == null || !original.gameObject.activeInHierarchy, Is.True);

            var pistol = default(Component);
            yield return WaitForPistol(scene, replacement.transform, value => pistol = value, 300);
            Assert.That(pistol, Is.Not.Null);
            Assert.That(pistol.gameObject.activeInHierarchy, Is.True);

            var characterInputType = RuntimeType("Akila.FPSFramework.CharacterInput", "Akila.FPSFramework");
            var characterInput = replacement.GetComponent(characterInputType) as Behaviour;
            Assert.That(characterInput, Is.Not.Null);
            Assert.That(characterInput.enabled, Is.True);

            var enabledCameras = FindSceneObjects(scene, typeof(Camera))
                .OfType<Camera>()
                .Where(camera => camera.enabled && camera.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(enabledCameras.Count(camera => camera.name != "Overlay Camera"), Is.EqualTo(1));
            Assert.That(enabledCameras.All(camera => camera.transform.IsChildOf(replacement.transform)), Is.True);

            var enabledListeners = FindSceneObjects(scene, typeof(AudioListener))
                .OfType<AudioListener>()
                .Count(listener => listener.enabled && listener.gameObject.activeInHierarchy);
            Assert.That(enabledListeners, Is.EqualTo(1));
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator DemoCombatBuildsOneFpsPlayerAndAkilaHitsKillWarden()
        {
            yield return LoadScene("Demo_Combat");
            var scene = SceneManager.GetActiveScene();
            yield return WaitForConfiguredSpawner(scene, 600);
            var spawner = FindSceneObjects(
                scene,
                RuntimeType("DontDiePlease.Central.Combat.CentralCombatSpawner")).FirstOrDefault();
            Assert.That(spawner, Is.Not.Null);
            yield return WaitForGameObject(scene, "WardenKDefenseCore", 300);

            var characterType = RuntimeType("Akila.FPSFramework.CharacterManager", "Akila.FPSFramework");
            var characters = FindSceneObjects(scene, characterType).OfType<Component>().ToArray();
            Assert.That(characters.Length, Is.EqualTo(1));
            var firearm = default(Component);
            yield return WaitForPistol(scene, characters[0].transform, value => firearm = value, 300);
            Assert.That(firearm, Is.Not.Null);
            Assert.That(firearm.gameObject.activeInHierarchy, Is.True);
            Assert.That(GetProperty<int>(firearm, "remainingAmmoCount"), Is.GreaterThan(0));
            var firearmRenderers = firearm.GetComponentsInChildren<Renderer>(true);
            Assert.That(
                firearmRenderers.Any(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy),
                Is.True);
            var itemInputType = RuntimeType("Akila.FPSFramework.ItemInput", "Akila.FPSFramework");
            var itemInput = firearm.GetComponent(itemInputType) as Behaviour;
            Assert.That(itemInput, Is.Not.Null);
            Assert.That(itemInput.enabled, Is.True);
            Assert.That(GetProperty(itemInput, "Controls"), Is.Not.Null);
            Assert.That(GetProperty(itemInput, "Inventory"), Is.Not.Null);
            Assert.That(GetProperty(itemInput, "CharacterInput"), Is.Not.Null);
            var controls = GetProperty(itemInput, "Controls");
            var firearmActions = GetProperty(controls, "Firearm");
            var aimAction = GetProperty(firearmActions, "Aim") as InputAction;
            var fireAction = GetProperty(firearmActions, "Fire") as InputAction;
            Assert.That(aimAction, Is.Not.Null);
            Assert.That(aimAction.enabled, Is.True);
            Assert.That(fireAction, Is.Not.Null);
            Assert.That(fireAction.enabled, Is.True);
            Assert.That(
                aimAction.bindings.Any(binding =>
                    string.Equals(binding.effectivePath, "<Mouse>/rightButton", StringComparison.OrdinalIgnoreCase)),
                Is.True);
            Assert.That(
                fireAction.bindings.Any(binding =>
                    string.Equals(binding.effectivePath, "<Mouse>/leftButton", StringComparison.OrdinalIgnoreCase)),
                Is.True);
            var inventoryItemType = RuntimeType("Akila.FPSFramework.InventoryItem", "Akila.FPSFramework");
            var inventoryItem = firearm.GetComponent(inventoryItemType);
            Assert.That(inventoryItem, Is.Not.Null);
            Assert.That(GetProperty(inventoryItem, "aimingAnimation"), Is.Not.Null);

            for (var frame = 0; frame < 120 && !GetProperty<bool>(firearm, "readyToFire"); frame++)
                yield return null;

            var animatorState = string.Join(
                "; ",
                firearm.GetComponentsInChildren<Animator>(true).Select(animator =>
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    return $"{animator.name}:active={animator.gameObject.activeInHierarchy},enabled={animator.enabled},speed={animator.speed:0.00},take={state.IsName("Take")},pickup={state.IsName("Pickup")},time={state.normalizedTime:0.00}";
                }));
            Assert.That(
                GetProperty<bool>(firearm, "readyToFire"),
                Is.True,
                $"ammo={GetProperty<int>(firearm, "remainingAmmoCount")} reloading={GetProperty<bool>(firearm, "isReloading")} prevented={GetProperty<bool>(firearm, "firePrevented")} input={GetProperty<bool>(firearm, "isInputActive")} restricted={Invoke(firearm, "IsPlayingRestrictedAnimation")} scale={Time.timeScale:0.00} animators={animatorState}");
            var ammoBeforeInput = GetProperty<int>(firearm, "remainingAmmoCount");
            SetField(itemInput, "triggeredFire", true);
            Invoke(firearm, "Update");
            SetField(itemInput, "triggeredFire", false);
            Assert.That(GetProperty<int>(firearm, "remainingAmmoCount"), Is.LessThan(ammoBeforeInput));

            SetField(itemInput, "aimInput", true);

            for (var frame = 0; frame < 10; frame++)
                yield return null;

            Assert.That(GetProperty<float>(inventoryItem, "aimProgress"), Is.GreaterThan(0f));
            SetField(itemInput, "aimInput", false);

            var characterInputType = RuntimeType("Akila.FPSFramework.CharacterInput", "Akila.FPSFramework");
            var characterInput = characters[0].GetComponent(characterInputType) as Behaviour;
            Assert.That(characterInput, Is.Not.Null);
            Assert.That(characterInput.enabled, Is.True);
            var enabledCameras = FindSceneObjects(scene, typeof(Camera))
                .OfType<Camera>()
                .Where(camera => camera.enabled && camera.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(enabledCameras.Count(camera => camera.name != "Overlay Camera"), Is.EqualTo(1));
            Assert.That(enabledCameras.All(camera => camera.transform.IsChildOf(characters[0].transform)), Is.True);
            var enabledListeners = FindSceneObjects(scene, typeof(AudioListener))
                .OfType<AudioListener>()
                .Count(listener => listener.enabled && listener.gameObject.activeInHierarchy);
            Assert.That(enabledListeners, Is.EqualTo(1));

            var director = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeDirector");
            var coordinator = FindRuntimeObject(scene, "DontDiePlease.Narrative.Runtime.NarrativeCombatCoordinator");
            yield return WaitForReady(coordinator, 600);
            var state = GetProperty(director, "State");
            Invoke(state, "SetFlag", "warden_k_engaged");
            Invoke(coordinator, "SpawnWarden");
            yield return null;

            var warden = FindGameObject(scene, "Enemy_WARDEN-K");
            Assert.That(warden, Is.Not.Null);
            var wardenAnchor = FindSceneObjects(
                    scene,
                    RuntimeType("DontDiePlease.Narrative.Runtime.NarrativeSpawnAnchor"))
                .OfType<Component>()
                .Single(anchor => GetProperty<string>(anchor, "AnchorId") == "warden-k-spawn");
            Assert.That(Vector3.Distance(warden.transform.position, wardenAnchor.transform.position), Is.LessThan(12f));
            var enemy = warden.GetComponent(RuntimeType("DontDiePlease.Central.Combat.CentralCombatEnemy"));
            Assert.That(enemy, Is.Not.Null);
            Assert.That(
                ((Behaviour)enemy).isActiveAndEnabled,
                Is.True,
                $"enabled={((Behaviour)enemy).enabled} activeSelf={warden.activeSelf} activeInHierarchy={warden.activeInHierarchy}");
            Assert.That((float)GetProperty(enemy, "MaxHealth"), Is.EqualTo(320f));
            var damage = GetFirearmDamage(firearm);
            var before = GetProperty<float>(enemy, "Health");
            var damageCollider = FindDamageCollider(warden);
            ApplyFirearmHit(firearm, damageCollider, damage);
            Assert.That(GetProperty<float>(enemy, "Health"), Is.EqualTo(before - damage).Within(0.001f));

            var remainingShots = Mathf.CeilToInt(before / Mathf.Max(damage, 0.01f)) + 1;

            for (var shot = 1; shot < remainingShots && enemy != null; shot++)
            {
                if ((bool)GetProperty(enemy, "IsDead"))
                {
                    break;
                }

                ApplyFirearmHit(firearm, damageCollider, damage);
            }

            for (var frame = 0; frame < 10 && !GetProperty<bool>(enemy, "IsDead"); frame++)
            {
                yield return null;
            }

            Assert.That(
                GetProperty<bool>(enemy, "IsDead"),
                Is.True,
                $"Warden health remained at {GetProperty<float>(enemy, "Health"):0.00}");
            yield return SkipUntilFlag(director, state, "component_core", 180);
            Assert.That((bool)Invoke(state, "HasFlag", "component_core"), Is.True);
            yield return WaitForGameObject(scene, "SignalGeneratorAssemblyConsole", 120);
        }

        private static IEnumerator WaitForPistol(
            Scene scene,
            Transform character,
            Action<Component> assign,
            int frames)
        {
            var firearmType = RuntimeType("Akila.FPSFramework.Firearm", "Akila.FPSFramework");

            for (var frame = 0; frame < frames; frame++)
            {
                var firearm = FindSceneObjects(scene, firearmType)
                    .OfType<Component>()
                    .FirstOrDefault(item =>
                        item != null &&
                        item.transform.IsChildOf(character) &&
                        item.name.Contains("Pistol_1", StringComparison.OrdinalIgnoreCase));

                if (firearm != null)
                {
                    assign(firearm);
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("The configured Akila Pistol_1 firearm did not become available");
        }

        private static float GetFirearmDamage(Component firearm)
        {
            var presetField = firearm.GetType().GetField("preset", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(presetField, Is.Not.Null);
            var preset = presetField.GetValue(firearm);
            Assert.That(preset, Is.Not.Null);
            var damageField = preset.GetType().GetField("damage", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(damageField, Is.Not.Null);
            return (float)damageField.GetValue(preset);
        }

        private static Collider FindDamageCollider(GameObject target)
        {
            var collider = target.GetComponentsInChildren<Collider>(true)
                .FirstOrDefault(item => item != null && item.enabled && !item.isTrigger);
            Assert.That(collider, Is.Not.Null);
            return collider;
        }

        private static void ApplyFirearmHit(Component firearm, Collider target, float damage)
        {
            Physics.SyncTransforms();
            var bounds = target.bounds;
            var distance = Mathf.Max(2f, bounds.extents.magnitude + 2f);
            var ray = new Ray(bounds.center - Vector3.forward * distance, Vector3.forward);
            Assert.That(target.Raycast(ray, out var hit, distance * 2f), Is.True);
            var method = firearm.GetType().GetMethod(
                "UpdateHits",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            var direction = Activator.CreateInstance(method.GetParameters()[5].ParameterType);
            method.Invoke(null, new object[] { firearm, null, ray, hit, damage, direction });
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null, sceneName);

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static IEnumerator WaitForActiveScene(string sceneName, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                if (SceneManager.GetActiveScene().name == sceneName)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{sceneName} did not become active");
        }

        private static IEnumerator WaitForReady(object director, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                if ((bool)GetProperty(director, "IsReady"))
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Narrative director did not become ready");
        }

        private static void DestroyNarrativeRuntimes()
        {
            var directorType = RuntimeType("DontDiePlease.Narrative.Runtime.NarrativeDirector");
            var directors = Resources.FindObjectsOfTypeAll(directorType).OfType<Component>().ToArray();

            foreach (var director in directors)
            {
                if (director != null)
                {
                    UnityEngine.Object.Destroy(director.gameObject);
                }
            }
        }

        private static void DestroyDetachedProjectiles()
        {
            var scalerType = RuntimeType("Akila.FPSFramework.ProximityScaler", "Akila.FPSFramework");
            var characterType = RuntimeType("Akila.FPSFramework.CharacterManager", "Akila.FPSFramework");
            var scalers = Resources.FindObjectsOfTypeAll(scalerType).OfType<Component>().ToArray();

            foreach (var scaler in scalers)
            {
                if (scaler != null &&
                    scaler.gameObject.scene.IsValid() &&
                    scaler.gameObject.scene.isLoaded &&
                    scaler.GetComponentInParent(characterType) == null)
                {
                    UnityEngine.Object.Destroy(scaler.gameObject);
                }
            }
        }

        private static IEnumerator WaitForRuntimeObject(Scene scene, string typeName, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                if (FindRuntimeObject(scene, typeName) != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{typeName} was not created in {scene.name}");
        }

        private static IEnumerator WaitForGameObject(Scene scene, string objectName, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                if (FindGameObject(scene, objectName) != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{objectName} was not created in {scene.name}");
        }

        private static IEnumerator WaitForConfiguredSpawner(Scene scene, int frames)
        {
            var spawnerType = RuntimeType("DontDiePlease.Central.Combat.CentralCombatSpawner");

            for (var frame = 0; frame < frames; frame++)
            {
                var spawner = FindSceneObjects(scene, spawnerType).FirstOrDefault();

                if (spawner != null && (bool)GetProperty(spawner, "IsConfigured"))
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Central combat spawner was not configured in {scene.name}");
        }

        private static IEnumerator SkipUntilFlag(object director, object state, string flag, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                if ((bool)Invoke(state, "HasFlag", flag))
                {
                    yield break;
                }

                if ((bool)GetProperty(director, "IsPlaying"))
                {
                    Invoke(director, "SkipActiveSequence");
                }

                yield return null;
            }
        }

        private static GameObject FindGameObject(Scene scene, string objectName, bool includeInactive = false)
        {
            var objects = includeInactive
                ? Resources.FindObjectsOfTypeAll<GameObject>()
                : UnityEngine.Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            return objects.FirstOrDefault(item => item != null && item.scene == scene && item.name == objectName);
        }

        private static UnityEngine.Object FindRuntimeObject(Scene scene, string typeName)
        {
            return FindSceneObjects(scene, RuntimeType(typeName)).FirstOrDefault();
        }

        private static UnityEngine.Object[] FindSceneObjects(Scene scene, Type type)
        {
            return Resources.FindObjectsOfTypeAll(type)
                .Where(item =>
                {
                    var component = item as Component;
                    return component != null && component.gameObject.scene == scene;
                })
                .ToArray();
        }

        private static GameObject[] FindNamedSceneObjects(Scene scene, string objectName)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item != null && item.scene == scene && item.name == objectName)
                .ToArray();
        }

        private static Type RuntimeType(string fullName, string assemblyName = "Assembly-CSharp")
        {
            return Type.GetType($"{fullName}, {assemblyName}", true);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(target, args);
        }

        private static object GetProperty(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, propertyName);
            return property.GetValue(target);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)GetProperty(target, propertyName);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static int ListCount(object target, string fieldName)
        {
            return ((IList)GetField<object>(target, fieldName)).Count;
        }
    }
}
