using DontDiePlease.Central.Combat;
using UnityEditor;
using UnityEngine;

namespace DontDiePlease.Central.EditorTools
{
    public static class CentralCombatAssetCatalogBuilder
    {
        private const string CatalogPath = "Assets/Resources/Combat/CentralCombatAssetCatalog.asset";
        private const string PlayerPath = "Assets/Akila/FPS Framework/Prefabs/Characters/Player.prefab";
        private const string PistolPath = "Assets/Akila/FPS Framework/Prefabs/Weapons/Pistol_1.prefab";
        private const string RiflePath = "Assets/Akila/FPS Framework/Prefabs/Weapons/Assault Rifle_1.prefab";
        private const string GameManagerPath = "Assets/Akila/FPS Framework/Prefabs/World/Game Manager.prefab";
        private const string HudPath = "Assets/Akila/FPS Framework/Prefabs/HUD/HUD.prefab";

        public static void BuildFromCommandLine()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CentralCombatAssetCatalog>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CentralCombatAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure(
                Load(PlayerPath),
                Load(PistolPath),
                Load(RiflePath),
                Load(GameManagerPath),
                Load(HudPath),
                new[]
                {
                    Load("Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/9mm Ammo.prefab"),
                    Load("Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/5.56mm Ammo.prefab"),
                    Load("Assets/Akila/FPS Framework/Prefabs/Pickables/Ammo/7.62mm Ammo.prefab")
                });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                throw new UnityException($"Missing combat asset at {path}");

            return prefab;
        }
    }
}
