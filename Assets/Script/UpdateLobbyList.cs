using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Steam;
using PurrNet.Transports;
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

        [SerializeField] private LobbyData lobby;

        private void OnEnable()
        {
        }

        public void OnPlayerConnected(LobbyData _LobbyData, bool _AsServer)
        {
            lobby = _LobbyData;

            if (Viewport == null || PlayerPrefabUI == null)
                return;

            for (int _i = Viewport.childCount - 1; _i >= 0; _i--)
            {
                var _child = Viewport.GetChild(_i);
                if (_child != null)
                    Destroy(_child.gameObject);
            }

            if (lobby == null || lobby.Players.Count == 0)
                return;

            var _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var _raw in lobby.Players)
            {
                if (string.IsNullOrWhiteSpace(_raw.Name))
                    continue;

                var _name = _raw.Name.Trim();

                if (!_seen.Add(_name))
                    continue;

                var _avatar = _raw.Id;

                GameObject _player = Instantiate(PlayerPrefabUI, Viewport);
                var _text = _player.GetComponentInChildren<TextMeshProUGUI>();
                var _image = _player.transform.GetChild(0).GetComponent<Image>();
                if (_text != null)
                    _text.text = _name;
                if (_image != null)
                {
                    _image.sprite = LoadLocalMediumAvatar(_avatar);
                }
            }
        }
        
        private Sprite LoadLocalMediumAvatar(CSteamID _LocalId)
        {
            int _imageId = SteamFriends.GetMediumFriendAvatar(_LocalId);
            if (_imageId <= 0)
            {
                Debug.LogWarning("[ConnectionManager] Aucun avatar medium disponible.");
                return null;
            }

            if (!SteamUtils.GetImageSize(_imageId, out uint width, out uint height))
            {
                Debug.LogWarning("[ConnectionManager] Impossible de récupérer la taille de l'image avatar.");
                return null;
            }

            int _imageSize = (int)(width * height * 4);
            byte[] _image = new byte[_imageSize];

            if (!SteamUtils.GetImageRGBA(_imageId, _image, _imageSize))
            {
                Debug.LogWarning("[ConnectionManager] Impossible de récupérer les données RGBA de l'avatar.");
                return null;
            }

            Texture2D _tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            _tex.LoadRawTextureData(_image);
            _tex.Apply();

            Sprite _sprite = Sprite.Create(_tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            return _sprite;
        }

        private void OnDisable()
        {
        }
    }
}