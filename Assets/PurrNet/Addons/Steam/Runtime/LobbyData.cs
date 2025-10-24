// using System;
// using System.Collections.Generic;
// using Script.LobbyManager;
// using UnityEngine;
//
//
//     [Serializable]
//     public class LobbyData
//     {
//         public string HostName;
//         public List<PlayerInfo> Players = new List<PlayerInfo>();
//
//         public void AddOrUpdate(PlayerInfo _PlayerInfo)
//         {
//             if (_PlayerInfo == null || string.IsNullOrWhiteSpace(_PlayerInfo.Name)) return;
//             var _idx = Players.FindIndex(_X =>
//                 string.Equals(_X.Name?.Trim(), _PlayerInfo.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
//             if (_idx >= 0)
//                 Players[_idx] = _PlayerInfo;
//             else
//                 Players.Add(_PlayerInfo);
//         }
//
//         public void RemoveByName(string _Name)
//         {
//             if (string.IsNullOrWhiteSpace(_Name)) return;
//             Players.RemoveAll(_X => string.Equals(_X.Name?.Trim(), _Name.Trim(), StringComparison.OrdinalIgnoreCase));
//         }
//
//         public void Clear()
//         {
//             Players.Clear();
//             HostName = null;
//         }
//
//         public string ToJson()
//         {
//             return JsonUtility.ToJson(this);
//         }
//
//         public static LobbyData FromJson(string _Json)
//         {
//             if (string.IsNullOrEmpty(_Json)) return new LobbyData();
//             try
//             {
//                 return JsonUtility.FromJson<LobbyData>(_Json) ?? new LobbyData();
//             }
//             catch
//             {
//                 return new LobbyData();
//             }
//         }
//     }
