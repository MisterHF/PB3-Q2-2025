// csharp
// Fichier: `Assets/Script/UpdateLobbyList.cs`

using System;
using System.Collections;
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

        private void TransportOnOnPlayerConnected(string _Arg1, bool _Arg2)
        {
            GameObject _Player = Instantiate(PlayerPrefabUI, Viewport.transform);
            _Player.transform.GetComponentInChildren<TextMeshProUGUI>().text = _Arg1;
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