using System;
using System.Collections;
using PurrNet;
using Steamworks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Script
{
    public class LobbyProfileManager : NetworkBehaviour
    {
        public static LobbyProfileManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnClientJoinedServer(PlayerID _Player, bool _IsReconnect, bool _AsServer)
        {
            if (!_AsServer) return;

            Debug.Log($"[LobbyProfileManager] Nouveau joueur : {SteamFriends.GetPersonaName()}");

            CSteamID _steamID = SteamUser.GetSteamID();

            StartCoroutine(LoadSteamAvatar(_steamID, SteamFriends.GetPersonaName()));
        }

        [ServerRpc]
        private IEnumerator LoadSteamAvatar(CSteamID _SteamID, string _Pseudo)
        {
            Debug.Log($"[LobbyProfileManager] LoadSteamAvatar démarré pour pseudo: {_Pseudo}");

            int _imageId = SteamFriends.GetLargeFriendAvatar(_SteamID);

            while (_imageId == -1)
            {
                yield return null;
                _imageId = SteamFriends.GetLargeFriendAvatar(_SteamID);
            }

            Sprite _avatarSprite = null;

            if (SteamUtils.GetImageSize(_imageId, out uint _width, out uint _height))
            {
                var _buffer = new byte[_width * _height * 4];
                if (SteamUtils.GetImageRGBA(_imageId, _buffer, (int)(_width * _height * 4)))
                {
                    Texture2D _tex = new Texture2D((int)_width, (int)_height, TextureFormat.RGBA32, false);
                    _tex.LoadRawTextureData(_buffer);
                    _tex.Apply();
                    _avatarSprite = Sprite.Create(_tex, new Rect(0, 0, _tex.width, _tex.height),
                        new Vector2(0.5f, 0.5f));
                }
            }

            LobbyUserCache.AddOrUpdate(_Pseudo, _avatarSprite);

            string _base64 = _avatarSprite != null
                ? Convert.ToBase64String(_avatarSprite.texture.EncodeToPNG())
                : null;

            Debug.Log(
                $"[LobbyProfileManager] Envoi RPC pour pseudo: {_Pseudo} avatar: {(_avatarSprite != null ? "OK" : "NULL")}");

            RpcClient_ProfileUpdated(_Pseudo, _base64);
        }

        [ObserversRpc]
        private void RpcClient_ProfileUpdated(string pseudo, string avatarBase64)
        {
            Debug.Log($"[LobbyProfileManager] RpcClient_ProfileUpdated reçu pour pseudo: {pseudo}");

            Sprite avatarSprite = null;

            if (!string.IsNullOrEmpty(avatarBase64))
            {
                byte[] bytes = Convert.FromBase64String(avatarBase64);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);
                avatarSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            LobbyUserCache.AddOrUpdate(pseudo, avatarSprite);

            Debug.Log(
                $"[LobbyProfileManager] Profil traité pour {pseudo} (avatar: {(avatarSprite != null ? "OK" : "NULL")})");
        }
    }
}