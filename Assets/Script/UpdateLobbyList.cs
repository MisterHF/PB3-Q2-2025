// csharp
// Fichier: `Assets/Script/UpdateLobbyList.cs`

using System;
using System.Collections;
using System.Reflection;
using PurrNet;
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

        private void OnEnable()
        {
            NetworkManager.main.onPlayerJoined += MainOnonPlayerJoined;
        }

        private void OnDisable()
        {
            NetworkManager.main.onPlayerJoined -= MainOnonPlayerJoined;
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