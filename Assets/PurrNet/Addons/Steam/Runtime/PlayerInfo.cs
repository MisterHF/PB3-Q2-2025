using UnityEngine;

namespace Script.LobbyManager
{
    using System;
    [Serializable]
    public class PlayerInfo
    {
        public string Name;
        public string AvatarUrl;
        public string ID;

        public PlayerInfo() { }

        public PlayerInfo(string _Name, string _AvatarUrl, string _ID = null)
        {
            this.Name = _Name;
            this.AvatarUrl = _AvatarUrl;
            this.ID = _ID;
        }
    }
}
