// File: `Assets/PurrNet/Addons/Steam/Runtime/SteamTransport.cs`

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if STEAMWORKS_NET
#define STEAMWORKS_NET_PACKAGE
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PurrNet.Transports;
using Steamworks;
using UnityEngine;

namespace PurrNet.Steam
{
    [Serializable]
    public class PlayerInfo
    {
        public string Name;
        public string AvatarUrl;
        public string Id;

        public PlayerInfo()
        {
        }

        public PlayerInfo(string name, string avatarUrl = "", string id = null)
        {
            Name = name;
            AvatarUrl = avatarUrl;
            Id = id;
        }
    }

    [Serializable]
    public class LobbyData
    {
        public string HostName;
        public List<PlayerInfo> Players = new List<PlayerInfo>();

        public void AddOrUpdate(PlayerInfo p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Name)) return;
            var name = p.Name.Trim();
            var idx = Players.FindIndex(x => string.Equals(x?.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) Players[idx] = p;
            else Players.Add(p);
        }

        public void RemoveByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Players.RemoveAll(x => string.Equals(x?.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public void Clear()
        {
            Players.Clear();
            HostName = null;
        }

        public string ToJson() => JsonUtility.ToJson(this);

        public static LobbyData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new LobbyData();
            try
            {
                return JsonUtility.FromJson<LobbyData>(json) ?? new LobbyData();
            }
            catch
            {
                return new LobbyData();
            }
        }
    }

    [DefaultExecutionOrder(-100)]
    public class SteamTransport : GenericTransport, ITransport
    {
        [Header("Server Settings")] [SerializeField]
        private ushort _serverPort = 5003;

        [SerializeField] private bool _dedicatedServer;
        [SerializeField] private bool _peerToPeer = true;

        [Header("Client Settings")] [SerializeField]
        private string _address = "127.0.0.1";

        public ushort serverPort
        {
            get => _serverPort;
            set => _serverPort = value;
        }

        public bool dedicatedServer
        {
            get => _dedicatedServer;
            set => _dedicatedServer = value;
        }

        public bool peerToPeer
        {
            get => _peerToPeer;
            set => _peerToPeer = value;
        }

        public string address
        {
            get => _address;
            set => _address = value;
        }

#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
        public override bool isSupported => true;
#else
        public override bool isSupported => false;
#endif

        public override ITransport transport => this;

        private readonly List<Connection> _connections = new List<Connection>();
        public IReadOnlyList<Connection> connections => _connections;

        private ConnectionState _listenerState = ConnectionState.Disconnected;

        public ConnectionState listenerState
        {
            get => _listenerState;
            private set
            {
                if (_listenerState == value)
                    return;
                _listenerState = value;
                onConnectionState?.Invoke(_listenerState, true);
            }
        }

        private ConnectionState _clientState = ConnectionState.Disconnected;

        public ConnectionState clientState
        {
            get => _clientState;
            private set
            {
                if (_clientState == value)
                    return;
                _clientState = value;
                onConnectionState?.Invoke(_clientState, false);
            }
        }

        public event OnConnected onConnected;
        public event OnDisconnected onDisconnected;
        public event OnDataReceived onDataReceived;
        public event OnDataSent onDataSent;
        public event OnConnectionState onConnectionState;

        // Nouveaux événements pour UI/lobby
        public event Action<LobbyData, bool> OnLobbyUpdated; // bool = asServer
        public event Action<List<string>, bool> OnPlayerConnected; // liste de pseudos clients seulement

        private SteamServer _server;
        private SteamClient _client;

        // Liste locale "Players" (clients) manipulée à partir des messages LOBBY/PLAYERINFO
        private readonly List<string> Players = new List<string>();
        private readonly List<string> _clientOnlyNames = new List<string>();

        // Holder serveur (et client local miroir)
        [SerializeField] private readonly LobbyData _lobby = new LobbyData();

        protected override void StartClientInternal()
        {
            Connect(_address, _serverPort);
        }

        protected override void StartServerInternal()
        {
            Listen(_serverPort);
        }

        public void Listen(ushort port)
        {
            if (_server != null)
                StopListening();

            listenerState = ConnectionState.Connecting;

            _server = new SteamServer();

            if (_peerToPeer)
                _server.ListenP2P(_dedicatedServer);
            else _server.Listen(port, _dedicatedServer);

            if (_server.listening)
            {
                listenerState = ConnectionState.Connected;
            }
            else
            {
                listenerState = ConnectionState.Disconnecting;
                listenerState = ConnectionState.Disconnected;
            }

            _server.onDataReceived += OnServerData;
            _server.onRemoteConnected += OnRemoteConnected;
            _server.onRemoteDisconnected += OnRemoteDisconnected;
        }

        private void OnRemoteConnected(int obj)
        {
            Debug.Log($"[SteamTransport] OnRemoteConnected - raw id={obj}");

            // accepter 0 aussi pour P2P
            if (obj < 0)
            {
                Debug.LogWarning($"[SteamTransport] Ignoring remote connected with invalid id={obj}");
                return;
            }

            // éviter doublons
            if (!_connections.Any(c => c.connectionId == obj))
            {
                var conn = new Connection(obj);
                _connections.Add(conn);
                Debug.Log($"[SteamTransport] Added connection id={obj} totalConnections={_connections.Count}");
                onConnected?.Invoke(conn, true);

                // envoyer immédiatement le lobby au nouvel arrivant
                try
                {
                    SendToClient(conn, BuildLobbyDataHolder(), Channel.ReliableOrdered);
                    Debug.Log($"[SteamTransport] Sent initial LOBBY to conn={obj}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SteamTransport] Failed to send initial LOBBY to conn={obj}: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"[SteamTransport] Connection id={obj} already present");
            }

            // si host non défini et il y a des players dans le lobby, définir proprement
            if (string.IsNullOrEmpty(_lobby.HostName) && _lobby.Players.Count > 0)
                _lobby.HostName = _lobby.Players[0].Name;
        }

        private void OnRemoteDisconnected(int obj)
        {
            Debug.Log($"[SteamTransport] OnRemoteDisconnected - raw id={obj}");

            if (obj < 0)
            {
                Debug.LogWarning($"[SteamTransport] Ignoring remote disconnected with invalid id={obj}");
                return;
            }

            // tenter de récupérer un pseudo associé dans le lobby
            var disconnectedName = FindNameByConnectionId(obj);
            if (!string.IsNullOrEmpty(disconnectedName))
            {
                _lobby.RemoveByName(disconnectedName);
                RemovePlayerByName(disconnectedName);
                Debug.Log(
                    $"[SteamTransport] Removed player '{disconnectedName}' from lobby due to disconnect id={obj}");
                BroadcastLobby();
            }

            _connections.RemoveAll(c => c.connectionId == obj);
            Debug.Log($"[SteamTransport] Connection removed id={obj} remaining={_connections.Count}");

            onDisconnected?.Invoke(new Connection(obj), DisconnectReason.ClientRequest, true);
        }

        private void OnServerData(int conn, ByteData data)
        {
            // Décodage sécurisé
            string message = TryDecodeMessage(data);

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith("PLAYERINFO:"))
                {
                    var json = message.Substring("PLAYERINFO:".Length);
                    var p = SafeFromJson<PlayerInfo>(json);
                    if (p != null)
                    {
                        var nameNormalized = NormalizeName(p.Name);
                        if (!string.IsNullOrEmpty(nameNormalized))
                        {
                            p.Name = nameNormalized; // normaliser dans le holder
                            p.Id = conn.ToString(); // associer l'id de connexion au PlayerInfo
                            _lobby.AddOrUpdate(p);

                            // mettre à jour Players list serveur-side (pour event OnPlayerConnected)
                            if (!PlayersContainsNormalized(p.Name))
                                Players.Add(p.Name);

                            Debug.Log(
                                $"[SteamTransport] OnServerData - PLAYERINFO from conn={conn} name={p.Name} id={p.Id}");
                            BroadcastLobby();
                        }
                        else
                        {
                            Debug.LogWarning($"[SteamTransport] PLAYERINFO rejected (invalid name) from conn={conn}");
                        }
                    }

                    return;
                }
                else if (message.StartsWith("REQUEST_LOBBY"))
                {
                    var target = _connections.Find(c => c.connectionId == conn);
                    if (target != null)
                    {
                        SendToClient(target, BuildLobbyDataHolder(), Channel.ReliableOrdered);
                        Debug.Log($"[SteamTransport] REQUEST_LOBBY served to conn={conn}");
                    }

                    return;
                }
            }

            // Forward non-lobby messages
            onDataReceived?.Invoke(new Connection(conn), data, true);
        }

        public void StopListening()
        {
            if (listenerState != ConnectionState.Disconnected)
                listenerState = ConnectionState.Disconnecting;
            _server?.Stop();
            Debug.Log("[SteamTransport] StopListening - stopping server and clearing Players and lobby");
            Players.Clear();
            _lobby.Clear();
            listenerState = ConnectionState.Disconnected;
            _server = null;
        }

        private Coroutine _connectClientCoroutine;

        public void Connect(string ip, ushort port)
        {
            if (_client != null)
                Disconnect();

            _client = new SteamClient();
            _client.onConnectionState += OnClientStateChanged;
            _client.onDataReceived += OnClientDataReceived;

            _connectClientCoroutine = StartCoroutine(_peerToPeer
                ? _client.ConnectP2P(ip, _dedicatedServer)
                : _client.Connect(ip, port, _dedicatedServer));
        }

        private void OnClientDataReceived(ByteData data)
        {
            // Décodage et traitement des messages LOBBY: sur le client
            string message = TryDecodeMessage(data);

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith("LOBBY:"))
                {
                    var json = message.Substring("LOBBY:".Length);
                    var remoteLobby = LobbyData.FromJson(json);

                    // Mettre à jour liste locale Players (dédup + normalisation)
                    Players.Clear();
                    var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pi in remoteLobby.Players)
                    {
                        var n = NormalizeName(pi?.Name);
                        if (string.IsNullOrEmpty(n)) continue;
                        if (added.Add(n)) Players.Add(n);
                    }

                    // Mettre à jour holder local (miroir)
                    _lobby.Clear();
                    _lobby.HostName = remoteLobby.HostName;
                    foreach (var pi in remoteLobby.Players) _lobby.AddOrUpdate(pi);

                    UpdateClientOnlyNames();

                    Debug.Log($"[SteamTransport] OnClientDataReceived - LOBBY received ({_lobby.Players.Count})");
                    OnLobbyUpdated?.Invoke(_lobby, false);
                    OnPlayerConnected?.Invoke(new List<string>(_clientOnlyNames), false);
                    return;
                }
            }

            // Forward autres messages
            onDataReceived?.Invoke(new Connection(-1), data, false);
        }

        private void OnClientStateChanged(ConnectionState state)
        {
            string _localName = GetLocalDisplayName();
            Debug.Log($"[SteamTransport] OnClientStateChanged - state={state} localName={_localName}");

            if (state == ConnectionState.Connected)
            {
                // envoyer PlayerInfo au serveur après connexion (avec SteamID si possible)
                try
                {
                    var info = new PlayerInfo(_localName, avatarUrl: "");
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
                    try
                    {
                        var steamId = SteamUser.GetSteamID().ToString();
                        info.Id = steamId;
                    }
                    catch
                    {
                        /* ignore si Steam non accessible */
                    }
#endif
                    string payload = "PLAYERINFO:" + JsonUtility.ToJson(info);
                    byte[] bytes = Encoding.UTF8.GetBytes(payload);
                    var reqData = new ByteData(bytes);
                    SendToServer(reqData, Channel.ReliableOrdered);
                    Debug.Log("[SteamTransport] Sent PLAYERINFO to server");

                    // demander explicitement le lobby au serveur (sécurité si broadcast manqué)
                    try
                    {
                        byte[] req = Encoding.UTF8.GetBytes("REQUEST_LOBBY");
                        SendToServer(new ByteData(req), Channel.ReliableOrdered);
                        Debug.Log("[SteamTransport] Sent REQUEST_LOBBY to server");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[SteamTransport] Send REQUEST_LOBBY failed: {e.Message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SteamTransport] Send PLAYERINFO failed: {e.Message}");
                }

                onConnected?.Invoke(default, false);
            }

            if (state == ConnectionState.Disconnected)
            {
                RemovePlayerByName(_localName);
                Debug.Log($"[SteamTransport] OnClientStateChanged - Disconnected local pseudo removed: {_localName}");
                onDisconnected?.Invoke(default, DisconnectReason.ClientRequest, false);
            }

            clientState = state;
        }

        public void Disconnect()
        {
            if (_connectClientCoroutine != null)
            {
                StopCoroutine(_connectClientCoroutine);
                _connectClientCoroutine = null;
            }

            if (_client == null)
                return;

            _client.Stop();
            _client = null;

            string _localName = GetLocalDisplayName();
            RemovePlayerByName(_localName);
            Debug.Log($"[SteamTransport] Disconnect - local player removed: {_localName}");
        }

        public void RaiseDataReceived(Connection conn, ByteData data, bool asServer)
        {
            onDataReceived?.Invoke(conn, data, asServer);
        }

        public void RaiseDataSent(Connection conn, ByteData data, bool asServer)
        {
            onDataSent?.Invoke(conn, data, asServer);
        }

        public void SendToClient(Connection target, ByteData data, Channel method = Channel.ReliableOrdered)
        {
            if (_server == null) return;
            if (listenerState is not ConnectionState.Connected) return;
            if (!target.isValid) return;

            _server.SendToConnection(target.connectionId, data, method);
            RaiseDataSent(target, data, true);
        }

        public void SendToServer(ByteData data, Channel method = Channel.ReliableOrdered)
        {
            if (_client == null) return;
            _client.Send(data, method);
            RaiseDataSent(default, data, false);
        }

        public void CloseConnection(Connection conn)
        {
            _server?.Kick(conn.connectionId);
        }

        public void ReceiveMessages(float delta)
        {
            _server?.ReceiveMessages();
            _client?.ReceiveMessages();
        }

        public void SendMessages(float delta)
        {
            _server?.SendMessages();
            _client?.SendMessages();
        }

        // ---------------------
        // Helpers / Lobby logic
        // ---------------------

        private ByteData BuildLobbyDataHolder()
        {
            string payload = "LOBBY:" + _lobby.ToJson();
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            return new ByteData(bytes);
        }

        private void BroadcastLobby()
        {
            var data = BuildLobbyDataHolder();
            foreach (var conn in _connections)
            {
                if (!conn.isValid) continue;
                SendToClient(conn, data, Channel.ReliableOrdered);
            }

            Debug.Log($"[SteamTransport] BroadcastLobby - players={_lobby.Players.Count}");
            // Notifier localement côté serveur également si utile
            OnLobbyUpdated?.Invoke(_lobby, true);
            UpdateClientOnlyNames();
            OnPlayerConnected?.Invoke(new List<string>(_clientOnlyNames), true);
        }

        private static T SafeFromJson<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return null;
            }
        }

        private string TryDecodeMessage(ByteData data)
        {
            try
            {
                var bytes = ExtractBytesFromByteData(data);
                if (bytes != null && bytes.Length > 0)
                    return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SteamTransport] decode failed: {e.Message}");
            }

            return null;
        }

        // Tentative générique pour récupérer le tableau d'octets depuis ByteData (réflexion fallback)
        private byte[] ExtractBytesFromByteData(ByteData data)
        {
            try
            {
                // Try common property/method names
                var type = data.GetType();
                // property "bytes" or "data" or "buffer"
                var prop = type.GetProperty("bytes",
                               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           ?? type.GetProperty("data",
                               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           ?? type.GetProperty("buffer",
                               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    var val = prop.GetValue(data);
                    if (val is byte[] b1) return b1;
                }

                // field "bytes" or "data" or "buffer"
                var field = type.GetField("bytes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? type.GetField("data",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? type.GetField("buffer",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(data);
                    if (val is byte[] b2) return b2;
                }

                // method ToArray or GetBytes
                var method = type.GetMethod("ToArray",
                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                             ?? type.GetMethod("GetBytes",
                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    var res = method.Invoke(data, null);
                    if (res is byte[] b3) return b3;
                }

                // as fallback, try ToString then decode base64 (unlikely)
                var ts = data.ToString();
                if (!string.IsNullOrEmpty(ts))
                {
                    try
                    {
                        return Convert.FromBase64String(ts);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var t = name.Trim();
            // interdiction des pseudos contenant des chiffres (si demandé)
            if (t.Any(char.IsDigit)) return null;
            return t;
        }

        private bool PlayersContainsNormalized(string name)
        {
            var n = NormalizeName(name);
            if (n == null) return false;
            return Players.Any(p => string.Equals(NormalizeName(p), n, StringComparison.OrdinalIgnoreCase));
        }

        private bool AddPlayerUnique(string name)
        {
            var n = NormalizeName(name);
            if (n == null) return false;
            if (PlayersContainsNormalized(n)) return false;
            Players.Add(n);
            return true;
        }

        private void RemovePlayerByName(string name)
        {
            var n = NormalizeName(name) ?? name?.Trim();
            if (string.IsNullOrEmpty(n)) return;
            var toRemove =
                Players.FirstOrDefault(p => string.Equals(NormalizeName(p), n, StringComparison.OrdinalIgnoreCase));
            if (toRemove != null) Players.Remove(toRemove);

            // also remove from lobby
            _lobby.RemoveByName(n);
        }

        private void UpdateClientOnlyNames()
        {
            _clientOnlyNames.Clear();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Players)
            {
                var t = NormalizeName(p);
                if (t == null) continue;
                if (seen.Add(t))
                    _clientOnlyNames.Add(t);
            }

            Debug.Log($"[SteamTransport] UpdateClientOnlyNames - count={_clientOnlyNames.Count}");
        }

        // Essai simple de résolution du nom distant depuis id
        private string GetRemoteDisplayNameFromId(int id)
        {
            // si vous avez une API Steam disponible, remplacez cette implémentation
            // par la lecture du nom via Steamworks/identifiant.
            // Ici, on tente de retrouver dans le lobby si possible
            var player = _lobby.Players.FirstOrDefault(p => p.Id == id.ToString());
            if (player != null) return player.Name;
            return $"Player_{id}";
        }

        private string GetLocalDisplayName()
        {
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
            try
            {
                var steamName = SteamFriends.GetPersonaName().ToLower();
                if (!string.IsNullOrWhiteSpace(steamName))
                {
                    var n = NormalizeName(steamName);
                    if (!string.IsNullOrEmpty(n)) return n;
                    // si la normalisation la rejette (ex: chiffres interdits), retombe sur le raw steamName nettoyé
                    return steamName.Trim();
                }
            }
            catch
            {
                // ignore si Steam non accessible
            }
#endif

            // fallback neutre (ne pas exposer le nom Windows)
            return "Player";
        }

        // tentative de recherche du pseudo associé à une connexion
        private string FindNameByConnectionId(int connectionId)
        {
            // si PlayerInfo.Id contient l'id de connexion (non garanti), on lira cela
            var pi = _lobby.Players.FirstOrDefault(p => p.Id == connectionId.ToString());
            if (pi != null) return pi.Name;
            // fallback: tenter de retourner Player_{id}
            return $"Player_{connectionId}";
        }
    }
}