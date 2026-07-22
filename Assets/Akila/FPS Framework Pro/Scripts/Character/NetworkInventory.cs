using System;
using System.Collections.Generic;
using Akila.FPSFramework;
using UnityEngine;
using AkilaInventory = Akila.FPSFramework.Inventory;
using AkilaInventoryItem = Akila.FPSFramework.InventoryItem;

#if MIRROR
using Mirror;
#endif

namespace Akila.FPSFrameworkPro
{
#if MIRROR
    public class NetworkInventory : NetworkBehaviour
#else
    public class NetworkInventory : MonoBehaviour
#endif
    {
#if MIRROR
        private AkilaInventory inventory;

        public List<AkilaInventoryItem> items = new List<AkilaInventoryItem>();

        [SyncVar(hook = nameof(UpdateActiveItem)), HideInInspector]
        public string activeItemName;

        public string localCurrentItem { get; private set; }

        private void Start()
        {
            inventory = GetComponent<AkilaInventory>();

            if (inventory == null)
            {
                Debug.LogError($"[{nameof(NetworkInventory)}] Akila inventory missing on {gameObject.name}", gameObject);
                enabled = false;
                return;
            }

            inventory.isInputActive = isLocalPlayer;
        }

        private void FixedUpdate()
        {
            localCurrentItem = GetCurrentItemName();

            if (!isLocalPlayer || activeItemName == localCurrentItem)
                return;

            RefreshCurrentItem();
        }

        public void RefreshCurrentItem()
        {
            if (!isLocalPlayer || !isClient || !NetworkClient.ready)
                return;

            CmdUpdateCurrentItem(localCurrentItem);
        }

        [Command]
        private void CmdUpdateCurrentItem(string newItemName)
        {
            activeItemName = newItemName;
        }

        private string GetCurrentItemName()
        {
            if (inventory?.items == null || inventory.items.Count == 0)
                return null;

            int idx = Mathf.Clamp(inventory.currentItemIndex, 0, inventory.items.Count - 1);
            return inventory.items[idx].Name;
        }

        private void UpdateActiveItem(string oldValue, string newValue)
        {
            try
            {
                if (isOwned)
                    return;

                transform.ClearChildren();

                if (string.IsNullOrEmpty(newValue))
                    return;

                AkilaInventoryItem itemToSpawn = items.Find(item => item.Name == newValue);

                if (itemToSpawn == null)
                {
                    Debug.LogError($"[{nameof(NetworkInventory)}] Item '{newValue}' missing on {gameObject.name}", gameObject);
                    return;
                }

                Instantiate(itemToSpawn, transform);
            }
            catch (Exception err)
            {
                Debug.LogException(err);
            }
        }
#endif
    }
}
