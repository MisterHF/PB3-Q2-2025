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
        [NonSerialized] public Texture2D AvatarTexture;
        public CSteamID Id;

        public PlayerInfo()
        {
        }

        public PlayerInfo(string _Name, CSteamID _ID, string _AvatarUrl = null, Texture2D _AvatarTexture = null)
        {
            Name = _Name;
            AvatarUrl = _AvatarUrl;
            AvatarTexture = _AvatarTexture;
            Id = _ID;
        }
    }

    [Serializable]
    public class LobbyData
    {
        public string HostName;
        public List<PlayerInfo> Players = new List<PlayerInfo>();

        public void AddOrUpdate(PlayerInfo p)
        {
            Debug.Log("Add");
            if (p == null || string.IsNullOrWhiteSpace(p.Name)) return;
            Debug.Log("Not Null");
            var name = p.Name.Trim();
            Debug.Log(name);
            var idx = Players.FindIndex(x => string.Equals(x?.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                Players[idx] = p;
            }
            else
            {
                Players.Add(p);
                Debug.Log(Players.Count);
                Debug.Log($"Added To Players in Lobby : {HostName}");
            }
        }

        public void RemoveByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Players.RemoveAll(x => string.Equals(x?.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // Nouvelle méthode : suppression par Id
        public void RemoveById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            Players.RemoveAll(x => string.Equals(x.Id.ToString(), id.Trim(), StringComparison.OrdinalIgnoreCase));
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
        // -------------------------
        // Serialized inspector fields
        // -------------------------
        [Header("Server Settings")] [SerializeField]
        private ushort _serverPort = 5003;

        [SerializeField] private bool _dedicatedServer;
        [SerializeField] private bool _peerToPeer = true;

        [Header("Client Settings")] [SerializeField]
        private string _address = "127.0.0.1";

        [Header("Debug")] [SerializeField] private bool enableDebugLogs = true;

        // -------------------------
        // Properties
        // -------------------------
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

        // -------------------------
        // Events
        // -------------------------
        public event OnConnected onConnected;
        public event OnDisconnected onDisconnected;
        public event OnDataReceived onDataReceived;
        public event OnDataSent onDataSent;
        public event OnConnectionState onConnectionState;

        // UI / lobby events
        public event Action<LobbyData, bool> OnLobbyUpdated; // bool = asServer
        public event Action<List<string>, bool> OnPlayerConnected; // client-only names

        // -------------------------
        // Private fields
        // -------------------------
        private readonly List<Connection> _connections = new List<Connection>();
        public IReadOnlyList<Connection> connections => _connections;

        private ConnectionState _listenerState = ConnectionState.Disconnected;
        private ConnectionState _clientState = ConnectionState.Disconnected;

        private SteamServer _server;
        private SteamClient _client;

        // Local players / lobby (maintained only by SendToServer)
        [SerializeField] private LobbyData lobby = new LobbyData();

        private Coroutine _connectClientCoroutine;

        // -------------------------
        // Connection state props
        // -------------------------
        public ConnectionState listenerState
        {
            get => _listenerState;
            private set
            {
                if (_listenerState == value) return;
                _listenerState = value;
                onConnectionState?.Invoke(_listenerState, true);
            }
        }

        public ConnectionState clientState
        {
            get => _client_state();
            private set
            {
                if (_clientState == value) return;
                _clientState = value;
                onConnectionState?.Invoke(_clientState, false);
            }
        }

        private ConnectionState _client_state()
        {
            return _clientState;
        }

        // -------------------------
        // Unity / GenericTransport overrides
        // -------------------------
        protected override void StartClientInternal()
        {
            Connect(_address, _serverPort);
        }

        protected override void StartServerInternal()
        {
            Listen(_serverPort);
        }

        // -------------------------
        // Server Listen / Stop
        // -------------------------
        public void Listen(ushort port)
        {
            if (_server != null) StopListening();

            listenerState = ConnectionState.Connecting;

            _server = new SteamServer();

            if (_peerToPeer) _server.ListenP2P(_dedicatedServer);
            else _server.Listen(port, _dedicatedServer);

            listenerState = _server.listening ? ConnectionState.Connected : ConnectionState.Disconnected;
            if (!_server.listening)
            {
                listenerState = ConnectionState.Disconnecting;
                listenerState = ConnectionState.Disconnected;
            }

            _server.onDataReceived += OnServerData;
            _server.onRemoteConnected += OnRemoteConnected;
            _server.onRemoteDisconnected += OnRemoteDisconnected;
        }

        public void StopListening()
        {
            if (listenerState != ConnectionState.Disconnected) listenerState = ConnectionState.Disconnecting;
            _server?.Stop();
            DbgLog("[SteamTransport] StopListening - stopping server");
            lobby.Clear();
            listenerState = ConnectionState.Disconnected;
            _server = null;
        }

        // -------------------------
        // Server callbacks
        // -------------------------
        private void OnRemoteConnected(int obj)
        {
            DbgLog($"[SteamTransport] OnRemoteConnected - raw id={obj}");

            if (obj < 0)
            {
                DbgWarn($"[SteamTransport] Ignoring remote connected with invalid id={obj}");
                return;
            }

            if (!_connections.Any(c => c.connectionId == obj))
            {
                var conn = new Connection(obj);
                _connections.Add(conn);
                OnLobbyUpdated?.Invoke(lobby, true);

                DbgLog($"[SteamTransport] Added connection id={obj} totalConnections={_connections.Count}");
                onConnected?.Invoke(conn, true);
            }
            else
            {
                DbgLog($"[SteamTransport] Connection id={obj} already present");
            }
        }

        private void OnRemoteDisconnected(int obj)
        {
            DbgLog($"[SteamTransport] OnRemoteDisconnected - raw id={obj}");

            if (obj < 0)
            {
                DbgWarn($"[SteamTransport] Ignoring remote disconnected with invalid id={obj}");
                return;
            }

            _connections.RemoveAll(c => c.connectionId == obj);
            DbgLog($"[SteamTransport] Connection removed id={obj} remaining={_connections.Count}");

            // Supprimer du lobby côté serveur et notifier
            try
            {
                lobby.RemoveById(obj.ToString());
                OnLobbyUpdated?.Invoke(lobby, true);
                BroadcastLobbyToClients();
            }
            catch (Exception e)
            {
                DbgWarn($"[SteamTransport] failed to update lobby on disconnect: {e.Message}");
            }

            onDisconnected?.Invoke(new Connection(obj), DisconnectReason.ClientRequest, true);
        }

        private void OnServerData(int conn, ByteData data)
        {
            try
            {
                var message = TryDecodeMessage(data);
                if (!string.IsNullOrEmpty(message) && message.StartsWith("PLAYERINFO:"))
                {
                    var json = message.Substring("PLAYERINFO:".Length);
                    var p = SafeFromJson<PlayerInfo>(json);
                    if (p != null)
                    {
                        var nameNormalized = NormalizeName(p.Name);
                        if (!string.IsNullOrEmpty(nameNormalized))
                        {
                            p.Name = nameNormalized;
                            lobby.AddOrUpdate(p);
                            if (string.IsNullOrEmpty(lobby.HostName) && lobby.Players.Count > 0)
                                lobby.HostName = lobby.Players[0].Name;
                            OnLobbyUpdated?.Invoke(lobby, true);
                            BroadcastLobbyToClients();
                            DbgLog(
                                $"[SteamTransport] Server lobby updated with PLAYERINFO name={p.Name} (conn={conn})");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DbgWarn($"[SteamTransport] OnServerData - lobby update failed: {e.Message}");
            }

            // Transmettre l'événement de donnée au reste du système
            onDataReceived?.Invoke(new Connection(conn), data, true);
        }

        // -------------------------
        // Client connection
        // -------------------------
        public void Connect(string ip, ushort port)
        {
            if (_client != null) Disconnect();

            _client = new SteamClient();
            _client.onConnectionState += OnClientStateChanged;
            _client.onDataReceived += OnClientDataReceived;

            _connectClientCoroutine = StartCoroutine(_peerToPeer
                ? _client.ConnectP2P(ip, _dedicatedServer)
                : _client.Connect(ip, port, _dedicatedServer));
        }

        public void Disconnect()
        {
            if (_connectClientCoroutine != null)
            {
                StopCoroutine(_connectClientCoroutine);
                _connectClientCoroutine = null;
            }

            if (_client == null) return;

            _client.Stop();
            _client = null;

            DbgLog("[SteamTransport] Disconnect - client stopped");
        }

        private void BroadcastLobbyToClients()
        {
            try
            {
                var _json = lobby.ToJson();
                var _msg = "LOBBYJSON:" + _json;
                var _bytes = Encoding.UTF8.GetBytes(_msg);

                object _bdObj = null;
                var _bdType = typeof(ByteData);
                var _ctor = _bdType.GetConstructor(new[] { typeof(byte[]) });
                if (_ctor != null)
                {
                    _bdObj = _ctor.Invoke(new object[] { _bytes });
                }
                else
                {
                    var _create = _bdType.GetMethod("Create",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                        new[] { typeof(byte[]) }, null);
                    if (_create != null)
                        _bdObj = _create.Invoke(null, new object[] { _bytes });
                }

                if (_bdObj == null) return;

                var _bd = (ByteData)_bdObj;

                foreach (var c in _connections)
                    SendToClient(c, _bd);
            }
            catch (Exception e)
            {
                DbgWarn($"[SteamTransport] BroadcastLobbyToClients failed: {e.Message}");
            }
        }

        private void OnClientDataReceived(ByteData data)
        {
            // Plus de traitement spécial LOBBY côté client ; on transmet simplement.
            onDataReceived?.Invoke(new Connection(-1), data, false);
        }

        private void OnClientStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
                onConnected?.Invoke(new Connection(0), false);

            if (state == ConnectionState.Disconnected)
                onDisconnected?.Invoke(new Connection(0), DisconnectReason.ClientRequest, false);

            clientState = state;
        }

        // -------------------------
        // Send / Receive wrappers
        // -------------------------
        public void SendToClient(Connection target, ByteData data, Channel method = Channel.ReliableOrdered)
        {
            if (_server == null) return;
            if (listenerState is not ConnectionState.Connected) return;
            if (!target.isValid) return;

            _server.SendToConnection(target.connectionId, data, method);
            OnLobbyUpdated.Invoke(lobby, true);
            RaiseDataSent(target, data, true);
        }

        public void SendToServer(ByteData data, Channel method = Channel.ReliableOrdered)
        {
            if (_client == null) return;
            try
            {
                _client.Send(data, method);
                RaiseDataSent(default, data, false);
            }
            catch (Exception e)
            {
                DbgWarn($"[SteamTransport] SendToServer failed: {e.Message}");
            }
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

        public void RaiseDataReceived(Connection conn, ByteData data, bool asServer)
        {
            onDataReceived?.Invoke(conn, data, asServer);
        }

        public void RaiseDataSent(Connection conn, ByteData data, bool asServer)
        {
            onDataSent?.Invoke(conn, data, asServer);
        }

        // -------------------------
        // Lobby helpers (utilisées uniquement par SendToServer)
        // -------------------------
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
                if (bytes != null && bytes.Length > 0) return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                DbgWarn($"[SteamTransport] decode failed: {e.Message}");
            }

            return null;
        }

        private byte[] ExtractBytesFromByteData(ByteData data)
        {
            try
            {
                var type = data.GetType();

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

                var method = type.GetMethod("ToArray",
                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                             ?? type.GetMethod("GetBytes",
                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    var res = method.Invoke(data, null);
                    if (res is byte[] b3) return b3;
                }

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
            }

            return null;
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return name.Trim();
        }

        [ContextMenu("Debug Log")]
        public void Test()
        {
            OnLobbyUpdated.Invoke(lobby, true);
        }

        // -------------------------
        // Debug helpers
        // -------------------------
        private void DbgLog(string message, UnityEngine.Object context = null)
        {
            if (!enableDebugLogs) return;
            if (context != null) Debug.Log(message, context);
            else Debug.Log(message);
        }

        private void DbgWarn(string message, UnityEngine.Object context = null)
        {
            if (!enableDebugLogs) return;
            if (context != null) Debug.LogWarning(message, context);
            else Debug.LogWarning(message);
        }

        private void DbgError(string message, UnityEngine.Object context = null)
        {
            if (!enableDebugLogs) return;
            if (context != null) Debug.LogError(message, context);
            else Debug.LogError(message);
        }
    }
}