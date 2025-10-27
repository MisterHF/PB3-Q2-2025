using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Script.Player
{
    public class Inventory : NetworkBehaviour
    {
        [SerializeField]
        private List<GameObject> inventory = new List<GameObject>();

        [ServerRpc]
        public void AddItem(GameObject _Item)
        {
            inventory.Add(_Item);
        }

        [ServerRpc]
        public void RemoveItem(GameObject _Item)
        {
            if (inventory.Contains(_Item))
                inventory.Remove(_Item);
        }
    }
}