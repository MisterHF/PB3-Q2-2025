using System;
using System.Reflection;
using System.Collections;
using PurrNet;
using UnityEngine;
using UnityEngine.UI;

namespace Script
{
    public class UpdateLobbyList : MonoBehaviour
    {
        public GameObject PlayerPrefabUI;
        public GameObject Viewport;
        public Sprite DefaultAvatar;

        private void OnEnable()
        {
            if (NetworkManager.main != null)
            {
                NetworkManager.main.onPlayerJoined += OnPlayerJoined;
            }
            else
            {
                StartCoroutine(WaitAndSubscribe());
            }
        }

        private IEnumerator WaitAndSubscribe()
        {
            yield return new WaitUntil(() => NetworkManager.main != null);
            NetworkManager.main.onPlayerJoined += OnPlayerJoined;
        }

        private void OnPlayerJoined(PlayerID _Player, bool _IsReconnect, bool _AsServer)
        {
            UpdateOrCreateEntryForPlayer(_Player);
        }

        private void OnDisable()
        {
            if (NetworkManager.main != null)
                NetworkManager.main.onPlayerJoined -= OnPlayerJoined;
        }

        private void UpdateOrCreateEntryForPlayer(PlayerID _Player)
        {
            if (Viewport == null) return;

            string idStr = _Player.ToString();
            if (string.IsNullOrEmpty(idStr)) return;

            var net = NetworkManager.main;
            if (net == null || net.players == null) return;

            // trouver l'objet joueur correspondant dans la liste réseau
            object playerObj = FindPlayerObjectByIdString(net.players, idStr);
            if (playerObj == null)
            {
                Debug.LogWarning($"Player object not found for id {idStr}");
                return;
            }

            string entryName = "Player_" + idStr;
            var existing = Viewport.transform.Find(entryName);

            GameObject entry;
            if (existing != null)
            {
                entry = existing.gameObject;
            }
            else
            {
                entry = Instantiate(PlayerPrefabUI, Viewport.transform);
                entry.name = entryName;
            }

            // trouver Image et Text dans le prefab (essaye un enfant nommé puis GetComponentInChildren)
            var avatarImage = entry.transform.Find("Avatar")?.GetComponent<Image>() ?? entry.GetComponentInChildren<Image>();
            var nameText = entry.transform.Find("Name")?.GetComponent<Text>() ?? entry.GetComponentInChildren<Text>();

            string playerName = GetPlayerName(playerObj);
            Sprite avatar = GetPlayerAvatar(playerObj) ?? DefaultAvatar;

            if (nameText != null) nameText.text = playerName;
            if (avatarImage != null) avatarImage.sprite = avatar;
        }

        private object FindPlayerObjectByIdString(System.Collections.IEnumerable playersEnumerable, string idStr)
        {
            if (playersEnumerable == null) return null;
            foreach (var p in playersEnumerable)
            {
                string pid = GetPlayerIdString(p);
                if (!string.IsNullOrEmpty(pid) && pid == idStr)
                    return p;
            }
            return null;
        }

        private string GetPlayerIdString(object _Player)
        {
            if (_Player == null) return null;
            var t = _Player.GetType();

            // candidats pour l'identifiant ; on retourne la valeur ToString() trouvée
            string[] candidates = { "id", "playerId", "userId", "steamId", "clientId", "uniqueId" };
            foreach (var n in candidates)
            {
                var prop = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var val = prop.GetValue(_Player);
                    if (val != null) return val.ToString();
                }
                var field = t.GetField(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var val = field.GetValue(_Player);
                    if (val != null) return val.ToString();
                }
            }

            // fallback : si l'objet expose directement une ToString utile
            try
            {
                var s = _Player.ToString();
                return string.IsNullOrEmpty(s) ? null : s;
            }
            catch { return null; }
        }

        private string GetPlayerName(object _Player)
        {
            if (_Player == null) return "Unknown";
            var _t = _Player.GetType();
            string[] _candidates = { "username", "displayName", "pseudo", "name" };
            foreach (var _n in _candidates)
            {
                var _prop = _t.GetProperty(_n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (_prop != null)
                {
                    var _val = _prop.GetValue(_Player) as string;
                    if (!string.IsNullOrEmpty(_val)) return _val;
                }
                var _field = _t.GetField(_n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (_field != null)
                {
                    var _val = _field.GetValue(_Player) as string;
                    if (!string.IsNullOrEmpty(_val)) return _val;
                }
            }
            return "Unknown";
        }

        private Sprite GetPlayerAvatar(object _Player)
        {
            if (_Player == null) return null;
            var _t = _Player.GetType();
            string[] _candidates = { "avatar", "avatarSprite", "profilePicture", "profileImage" };
            foreach (var _n in _candidates)
            {
                var _prop = _t.GetProperty(_n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (_prop != null && typeof(Sprite).IsAssignableFrom(_prop.PropertyType))
                {
                    return _prop.GetValue(_Player) as Sprite;
                }
                var _field = _t.GetField(_n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (_field != null && typeof(Sprite).IsAssignableFrom(_field.FieldType))
                {
                    return _field.GetValue(_Player) as Sprite;
                }
            }
            return null;
        }
    }
}
