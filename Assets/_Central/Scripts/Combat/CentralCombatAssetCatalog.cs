using System;
using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    [CreateAssetMenu(menuName = "Don't Die Please/Combat/Asset Catalog")]
    public sealed class CentralCombatAssetCatalog : ScriptableObject
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject pistolPrefab;
        [SerializeField] private GameObject assaultRiflePrefab;
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject hudPrefab;
        [SerializeField] private GameObject fenrisFrigatePrefab;
        [SerializeField] private GameObject[] pickupPrefabs = Array.Empty<GameObject>();

        public GameObject PlayerPrefab => playerPrefab;
        public GameObject PistolPrefab => pistolPrefab;
        public GameObject AssaultRiflePrefab => assaultRiflePrefab;
        public GameObject GameManagerPrefab => gameManagerPrefab;
        public GameObject HudPrefab => hudPrefab;
        public GameObject FenrisFrigatePrefab => fenrisFrigatePrefab;
        public GameObject[] PickupPrefabs => pickupPrefabs;

        public void Configure(
            GameObject player,
            GameObject pistol,
            GameObject rifle,
            GameObject gameManager,
            GameObject hud,
            GameObject[] pickups)
        {
            playerPrefab = player;
            pistolPrefab = pistol;
            assaultRiflePrefab = rifle;
            gameManagerPrefab = gameManager;
            hudPrefab = hud;
            pickupPrefabs = pickups ?? Array.Empty<GameObject>();
        }

        public void SetFenrisFrigate(GameObject prefab)
        {
            fenrisFrigatePrefab = prefab;
        }
    }
}
