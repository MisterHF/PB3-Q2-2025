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
using System.Linq;

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
            if (transport != null)
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

            if (_List == null || _List.Count == 0)
                return;

            // Filtrer : trim, pas de chiffres, pas de vides, et unique (insensible à la casse)
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in _List)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var name = raw.Trim();
                if (name.Any(char.IsDigit))
                    continue;

                if (!seen.Add(name))
                    continue; // déjà ajouté

                GameObject _player = Instantiate(PlayerPrefabUI, Viewport);
                var text = _player.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = name;
            }
        }

        private void OnDisable()
        {
            if (transport != null)
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
