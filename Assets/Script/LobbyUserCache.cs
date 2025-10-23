// csharp
// Fichier: `LobbyProfileCache.cs`
using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Script
{
    public static class LobbyUserCache
    {
        private static readonly Dictionary<string, Sprite> Profiles = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        // Evènement appelé quand un pseudo est ajouté/mis à jour: (pseudo, avatar)
        public static event Action<string, Sprite> ProfileUpdated;

        // Ajoute ou met à jour une entrée (pseudo -> avatar). Avatar peut être null.
        public static void AddOrUpdate(string _Pseudo, Sprite _Avatar)
        {
            if (string.IsNullOrEmpty(_Pseudo)) return;
            lock (_lock)
            {
                Profiles[_Pseudo] = _Avatar;
            }
            Debug.Log($"LobbyUserCache: AddOrUpdate {_Pseudo}");
            ProfileUpdated?.Invoke(_Pseudo, _Avatar);
        }

        // Essaie d'obtenir l'avatar pour un pseudo.
        public static bool TryGetAvatar(string _Pseudo, out Sprite _Avatar)
        {
            _Avatar = null;
            if (string.IsNullOrEmpty(_Pseudo)) return false;
            lock (_lock)
            {
                return Profiles.TryGetValue(_Pseudo, out _Avatar);
            }
        }

        // Supprime une entrée.
        public static bool Remove(string _Pseudo)
        {
            if (string.IsNullOrEmpty(_Pseudo)) return false;
            lock (_lock)
            {
                return Profiles.Remove(_Pseudo);
            }
        }

        // Vide le cache.
        public static void Clear()
        {
            lock (_lock)
            {
                Profiles.Clear();
            }
        }

        // Retourne une copie pour lecture.
        public static Dictionary<string, Sprite> GetAll()
        {
            lock (_lock)
            {
                return new Dictionary<string, Sprite>(Profiles, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}