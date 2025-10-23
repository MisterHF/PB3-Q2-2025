// csharp
// Fichier: `Assets/Script/UpdateLobbyList.cs`

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PurrNet;
using PurrNet.Steam;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Script
{
    public class UpdateLobbyList : MonoBehaviour
    {
        public GameObject PlayerPrefabUI;
        public Transform Viewport;
        public Sprite DefaultAvatar;

        private SteamTransport transport;

        private void OnEnable()
        {
            transport = NetworkManager.main.transport as SteamTransport;
            transport.OnPlayerConnected += TransportOnOnPlayerConnected;
        }

        private void TransportOnOnPlayerConnected(List<string> _List, bool _Arg2)
        {
            if (Viewport == null || PlayerPrefabUI == null)
                return;

            // Détruire tous les enfants du viewport (boucle inverse)
            for (int i = Viewport.childCount - 1; i >= 0; i--)
            {
                var child = Viewport.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }

            // Recréer la liste UI
            foreach (var _item in _List)
            {
                GameObject _player = Instantiate(PlayerPrefabUI, Viewport);
                var text = _player.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = _item;
            }
        }

        private void OnDisable()
        {
            transport.OnPlayerConnected -= TransportOnOnPlayerConnected;
        }

        private void MainOnonPlayerJoined(PlayerID _Player, bool _IsReconnect, bool _AsServer)
        {
            CSteamID id = new CSteamID(_Player.id);
            Debug.Log(id.m_SteamID);
        }

        public void OnProfileUpdated()
        {
            Debug.Log("Connected");
        }
    }
}