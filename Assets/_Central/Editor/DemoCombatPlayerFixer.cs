using System.IO;
using Akila.FPSFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using FpsInventory = Akila.FPSFramework.Inventory;
using FpsItem = Akila.FPSFramework.InventoryItem;

namespace DontDiePlease.Central.EditorTools
{
    public static class DemoCombatPlayerFixer
    {
        private const string DemoScene = "Assets/Scenes/Demo_Combat.unity";
        private const string PlayerPrefab = "Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab";
        private const string Pistol = "Assets/Akila/FPS Framework/Prefabs/Weapons/Pistol_1.prefab";
        private const string Rifle = "Assets/Akila/FPS Framework/Prefabs/Weapons/Assault Rifle_1.prefab";

        [MenuItem("Tools/Don't Die Please/Combat/Demo/Fix Demo Combat Player")]
        public static void Apply()
        {
            if (!File.Exists(FullPath(DemoScene))) {
                EditorUtility.DisplayDialog("Demo Combat", "Assets/Scenes/Demo_Combat.unity was not found.", "OK");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (prefab == null) {
                EditorUtility.DisplayDialog("Demo Combat", $"Could not load {PlayerPrefab}.", "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
            var player = GetOrCreatePlayer(scene, prefab);

            player.transform.SetPositionAndRotation(PickSpawn(scene), Quaternion.Euler(0f,15f,0f));
            PrepPlayer(player);

            KillOldPlayers(scene, player, prefab);
            FixCameras(scene, player);
            StopSpawners(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Demo Combat", "Demo_Combat player is fixed. Open the scene and press Play.", "OK");
        }

        public static void ApplyFromCommandLine()
        {
            Apply();
        }

        private static GameObject GetOrCreatePlayer(Scene scene, GameObject prefab)
        {
            foreach (var mgr in SceneObjs<CharacterManager>(scene))
            {
                var go = mgr != null ? mgr.gameObject : null;
                if (SamePrefab(go, prefab)) {
                    go.SetActive(true);
                    go.name = "AkilaFPSFrameworkPlayer";
                    return go;
                }
            }

            var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            player.name = "AkilaFPSFrameworkPlayer";
            player.SetActive(true);
            return player;
        }

        private static void PrepPlayer(GameObject player)
        {
            var ctrl = player.GetComponent<Akila.FPSFramework.FirstPersonController>();
            if (ctrl != null) {
                ctrl.enabled = true;
                ctrl.lockCursor = true;
            }

            var input = player.GetComponent<CharacterInput>();
            if (input != null)
                input.enabled = true;

            var hp = player.GetComponentInChildren<Damageable>(true);
            if (hp != null) {
                hp.type = DamagableType.Player;
                hp.health = Mathf.Max(100f, hp.health);
                hp.maxHealth = Mathf.Max(hp.maxHealth, hp.health);
            }

            var actor = player.GetComponentInChildren<Actor>(true);
            if (actor != null) {
                actor.actorName = "Survivor";
                actor.type = "Player";
                actor.teamId = 0;
                actor.respawnable = true;
                actor.playerCardActive = true;
                actor.playerUIEnabled = true;
            }

            var inv = player.GetComponentInChildren<FpsInventory>(true);
            if (inv == null)
                return;

            inv.enabled = true;
            inv.startItems.Clear();
            AddWeapon(inv, Pistol);
            AddWeapon(inv, Rifle);
            inv.maxSlots = Mathf.Max(3, inv.startItems.Count);
        }

        private static void AddWeapon(FpsInventory inv, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return;

            var weapon = prefab.GetComponent<FpsItem>() ?? prefab.GetComponentInChildren<FpsItem>(true);
            if (weapon != null && !inv.startItems.Contains(weapon))
                inv.startItems.Add(weapon);
        }

        private static void KillOldPlayers(Scene scene, GameObject fixedPlayer, GameObject prefab)
        {
            foreach (var mgr in SceneObjs<CharacterManager>(scene))
            {
                if (mgr == null || mgr.gameObject == fixedPlayer)
                    continue;

                if (!SamePrefab(mgr.gameObject, prefab))
                    mgr.gameObject.SetActive(false);
            }

            foreach (var mb in SceneObjs<MonoBehaviour>(scene))
            {
                if (mb == null || mb.gameObject == fixedPlayer || mb.transform.IsChildOf(fixedPlayer.transform))
                    continue;

                var name = mb.GetType().Name;
                if (name == "FirstPersonController" || name == "FirstPersonSceneModeSwitcher" || name == "FreeCamera" || name == "CameraController")
                    mb.enabled = false;
            }
        }

        private static void FixCameras(Scene scene, GameObject player)
        {
            var mainCam = GetMainCam(player);
            if (mainCam == null)
                return;

            mainCam.gameObject.SetActive(true);
            mainCam.enabled = true;
            mainCam.tag = "MainCamera";

            foreach (var cam in player.GetComponentsInChildren<Camera>(true))
            {
                if (cam != null)
                    cam.enabled = cam == mainCam || cam.name == "Overlay Camera";
            }

            foreach (var cam in SceneObjs<Camera>(scene))
            {
                if (cam == null)
                    continue;

                if (cam.transform.IsChildOf(player.transform))
                    continue;

                cam.enabled = false;
            }

            var listener = mainCam.GetComponent<AudioListener>() ?? mainCam.gameObject.AddComponent<AudioListener>();
            listener.enabled = true;

            foreach (var aud in SceneObjs<AudioListener>(scene))
            {
                if (aud != null)
                    aud.enabled = aud == listener;
            }
        }

        private static Camera GetMainCam(GameObject player)
        {
            var cams = player.GetComponentsInChildren<Camera>(true);
            Camera fallback = null;

            foreach (var cam in cams)
            {
                if (cam == null)
                    continue;

                fallback ??= cam;

                if (cam.name == "Main Camera")
                    return cam;
            }

            foreach (var cam in cams)
            {
                if (cam != null && cam.CompareTag("MainCamera"))
                    return cam;
            }

            foreach (var cam in cams)
            {
                if (cam != null && cam.GetComponent<AudioListener>() != null)
                    return cam;
            }

            return fallback;
        }

        private static void StopSpawners(Scene scene)
        {
            foreach (var mb in SceneObjs<MonoBehaviour>(scene))
            {
                if (mb == null)
                    continue;

                var type = mb.GetType().Name;
                if (type.Contains("Spawner") || type.Contains("CombatHud"))
                    mb.gameObject.SetActive(false);
            }
        }

        private static Vector3 PickSpawn(Scene scene)
        {
            var spots = new[]
            {
                new Vector3(14f, 60f, -31f), new Vector3(-22f, 60f, -20f),
                new Vector3(26f, 60f, 16f), new Vector3(-34f, 60f, 26f),
                new Vector3(0f, 60f, -18f), new Vector3(0f, 60f, 0f)
            };

            foreach (var spot in spots)
            {
                if (Physics.Raycast(spot, Vector3.down, out var hit, 180f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return hit.point + Vector3.up * 1.05f;
            }

            var renderers = SceneObjs<Renderer>(scene);
            Bounds? bounds = null;

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.GetComponentInParent<Canvas>() != null)
                    continue;

                bounds = bounds.HasValue ? Encapsulate(bounds.Value, renderer.bounds) : renderer.bounds;
            }

            if (!bounds.HasValue)
                return new Vector3(0f, 1.2f, -18f);

            var b = bounds.Value;
            return new Vector3(b.center.x, b.min.y + 1.05f, b.center.z - Mathf.Min(b.extents.z * 0.35f, 28f));
        }

        private static Bounds Encapsulate(Bounds baseBounds, Bounds next)
        {
            baseBounds.Encapsulate(next);
            return baseBounds;
        }

        private static bool SamePrefab(GameObject go, GameObject prefab)
        {
            if (go == null || prefab == null)
                return false;

            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(go) == prefab;
        }

        private static T[] SceneObjs<T>(Scene scene) where T : Object
        {
            var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            var count = 0;

            for (var idx = 0; idx < all.Length; idx++)
            {
                if (all[idx] != null && ObjScene(all[idx]) == scene)
                    count++;
            }

            var res = new T[count];
            var pos = 0;

            for (var idx = 0; idx < all.Length; idx++)
            {
                if (all[idx] == null || ObjScene(all[idx]) != scene)
                    continue;

                res[pos++] = all[idx];
            }

            return res;
        }

        private static Scene ObjScene(Object obj)
        {
            if (obj is Component cmp)
                return cmp.gameObject.scene;

            if (obj is GameObject go)
                return go.scene;

            return default;
        }

        private static string FullPath(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }
}
