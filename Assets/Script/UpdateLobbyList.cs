using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Steam;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                transport.OnLobbyUpdated += TransportOnOnPlayerConnected;
        }

        private void TransportOnOnPlayerConnected(LobbyData _LobbyData, bool _Arg2)
        {
            if (Viewport == null || PlayerPrefabUI == null)
                return;

            for (int i = Viewport.childCount - 1; i >= 0; i--)
            {
                var child = Viewport.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }

            if (_LobbyData == null || _LobbyData.Players.Count == 0)
                return;

            var _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var _raw in _LobbyData.Players)
            {
                if (string.IsNullOrWhiteSpace(_raw.Name))
                    continue;

                var _name = _raw.Name.Trim();

                if (!_seen.Add(_name))
                    continue;
                
                var _avatar = _raw.AvatarUrl;

                GameObject _player = Instantiate(PlayerPrefabUI, Viewport);
                var _text = _player.GetComponentInChildren<TextMeshProUGUI>();
                var _image = _player.transform.GetChild(0).GetComponent<Image>();
                if (_text != null)
                    _text.text = _name;
                if (_image != null)
                {
                    var _rect = new Rect(0, 0, _avatar.width, _avatar.height);
                    var _pivot = new Vector2(0.5f, 0.5f);
                    var _sprite = Sprite.Create(_avatar, _rect, _pivot, 100f);
                    _image.sprite = _sprite;
                }
            }
        }

        private void OnDisable()
        {
            if (transport != null)
                transport.OnLobbyUpdated -= TransportOnOnPlayerConnected;
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
