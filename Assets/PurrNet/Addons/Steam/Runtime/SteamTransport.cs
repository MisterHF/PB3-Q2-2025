#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if STEAMWORKS_NET
#define STEAMWORKS_NET_PACKAGE
#endif

using System;
using System.Collections.Generic;
using PurrNet.Transports;
using UnityEngine;
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace PurrNet.Steam
{
    [DefaultExecutionOrder(-100)]
    public class SteamTransport : GenericTransport, ITransport
    {
        [Header("Server Settings")] [SerializeField]
        private ushort _serverPort = 5003;

        [SerializeField] private bool _dedicatedServer;
        [SerializeField] private bool _peerToPeer = true;
        [SerializeField] private SyncList<string> Players = new SyncList<string>();
        private readonly Dictionary<int, string> _connectionNames = new Dictionary<int, string>();
        public event Action<List<string>, bool> OnPlayerConnected;

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

        private SteamServer _server;
        private SteamClient _client;

        protected override void StartClientInternal()
        {
            Connect(_address, _serverPort);
        }

        protected override void StartServerInternal()
        {
            Listen(_serverPort);
        }

        private void BroadcastPlayers()
        {
            var data = BuildPlayersData();
            foreach (var conn in _connections)
            {
                if (!conn.isValid)
                    continue;
                SendToClient(conn, data, Channel.ReliableOrdered);
            }
        }

        private static byte[] ExtractBytesFromByteData(object byteData)
        {
            if (byteData == null) return null;

            // si c'est déjà un tableau
            if (byteData is byte[] direct) return direct;

            // si c'est une liste d'octets
            if (byteData is System.Collections.IList list)
            {
                try
                {
                    var arr = new byte[list.Count];
                    for (int i = 0; i < list.Count; i++)
                        arr[i] = Convert.ToByte(list[i]);
                    return arr;
                }
                catch
                {
                    // continue les tentatives
                }
            }

            var type = byteData.GetType();
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance;

            // champs / propriétés candidates (public et non public)
            var candidateNames = new[]
                { "bytes", "data", "buffer", "array", "raw", "_buffer", "_data", "m_Data", "m_bytes", "Value" };
            foreach (var name in candidateNames)
            {
                try
                {
                    var prop = type.GetProperty(name, flags);
                    if (prop != null)
                    {
                        var val = prop.GetValue(byteData);
                        if (val is byte[] pb) return pb;
                        if (val is System.Collections.IList pl)
                        {
                            var arr = new byte[pl.Count];
                            for (int i = 0; i < pl.Count; i++) arr[i] = Convert.ToByte(pl[i]);
                            return arr;
                        }
                    }

                    var field = type.GetField(name, flags);
                    if (field != null)
                    {
                        var val = field.GetValue(byteData);
                        if (val is byte[] fb) return fb;
                        if (val is System.Collections.IList fl)
                        {
                            var arr = new byte[fl.Count];
                            for (int i = 0; i < fl.Count; i++) arr[i] = Convert.ToByte(fl[i]);
                            return arr;
                        }
                    }
                }
                catch
                {
                    // ignorer et continuer
                }
            }

            // méthodes ToArray / ToBytes / GetBytes
            var toArray = type.GetMethod("ToArray", flags) ??
                          type.GetMethod("ToBytes", flags) ?? type.GetMethod("GetBytes", flags);
            if (toArray != null)
            {
                try
                {
                    var res = toArray.Invoke(byteData, null) as byte[];
                    if (res != null) return res;
                }
                catch
                {
                }
            }

            // si l'objet expose un indexeur (Item) et une propriété Count/Length
            var itemProp = type.GetProperty("Item", flags, null, null, new[] { typeof(int) }, null);
            var countProp = type.GetProperty("Count", flags) ?? type.GetProperty("Length", flags);
            if (itemProp != null && countProp != null)
            {
                try
                {
                    int count = Convert.ToInt32(countProp.GetValue(byteData));
                    var arr = new byte[count];
                    for (int i = 0; i < count; i++)
                    {
                        var v = itemProp.GetValue(byteData, new object[] { i });
                        arr[i] = Convert.ToByte(v);
                    }

                    return arr;
                }
                catch
                {
                }
            }

            // journalisation pour debug (permettra de voir le type réel reçu)
            Debug.LogWarning(
                $"[SteamTransport] ExtractBytesFromByteData: impossible d'extraire les octets depuis le type {type.FullName}");

            return null;
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

        private void OnEnable()
        {
            Players.onChanged += PlayersOnonChanged;
        }

        private void PlayersOnonChanged(SyncListChange<string> _Change)
        {
            // Log du changement et de la liste complète
            Debug.Log($"[SteamTransport] PlayersOnonChanged - type={_Change.value} - total={Players.Count}");
            OnPlayerConnected?.Invoke(new List<string>(Players), true);
            foreach (string _player in Players)
            {
                Debug.Log($"[SteamTransport] Player in list: {_player}");
            }
        }

        private void OnRemoteConnected(int obj)
        {
            // Ignorer l'entrée de loopback/host souvent signalée comme id 0
            if (obj == 0)
                return;

            string _displayName = GetRemoteDisplayNameFromId(obj);
            string _localName = GetLocalDisplayName();

            Debug.Log($"[SteamTransport] OnRemoteConnected - id={obj} - pseudoResolved={_displayName}");

            // Ne pas ajouter si c'est en fait le même nom que le host
            if (_displayName == _localName)
            {
                Debug.Log($"[SteamTransport] OnRemoteConnected - ignored (same as local): {_displayName}");
                return;
            }

            if (!Players.Contains(_displayName))
            {
                Players.Add(_displayName);
                Debug.Log($"[SteamTransport] OnRemoteConnected - added to Players: {_displayName}");
            }

            var connection = new Connection(obj);
            _connections.Add(connection);

            // notifier en interne et log
            onConnected?.Invoke(connection, true);

            Debug.Log(
                $"[SteamTransport] OnRemoteConnected - notified internal listeners for {__displayNameSafe(_displayName)} (id={obj})");
        }

        private void OnRemoteDisconnected(int obj)
        {
            // Ignorer l'entrée de loopback/host
            if (obj == 0)
                return;

            string displayName = GetRemoteDisplayNameFromId(obj);
            Debug.Log($"[SteamTransport] OnRemoteDisconnected - id={obj} - pseudoResolved={displayName}");

            Players.Remove(displayName);
            Debug.Log($"[SteamTransport] OnRemoteDisconnected - removed from Players: {displayName}");

            // retirer la connexion correspondante
            _connections.RemoveAll(c => c.connectionId == obj);

            onDisconnected?.Invoke(new Connection(obj), DisconnectReason.ClientRequest, true);

        }

        private void OnServerData(int conn, ByteData data)
        {
            string senderName = GetRemoteDisplayNameFromId(conn);
            int i = data.length;
            Debug.Log($"[SteamTransport] OnServerData - from id={conn} pseudo={senderName} - bytes={data.length}");

            // essayer de décoder en string
            string message = null;
            try
            {
                var bytes = ExtractBytesFromByteData(data);
                if (bytes != null && bytes.Length > 0)
                    message = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SteamTransport] OnServerData - decode failed: {e.Message}");
            }

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith("SETNAME:"))
                {
                    var name = message.Substring("SETNAME:".Length);
                    if (!string.IsNullOrEmpty(name))
                    {
                        _connectionNames[conn] = name;
                        if (!Players.Contains(name))
                        {
                            Players.Add(name);
                            Debug.Log($"[SteamTransport] OnServerData - SETNAME added: {name} (conn={conn})");
                        }

                        BroadcastPlayers();
                    }

                    return; // ne pas passer l'événement générique
                }
                else if (message == "REQUEST_PLAYERS")
                {
                    // envoyer la liste uniquement au demandeur
                    var target = _connections.Find(c => c.connectionId == conn);
                    if (target != null && target.isValid)
                    {
                        SendToClient(target, BuildPlayersData(), Channel.ReliableOrdered);
                    }

                    return;
                }
            }

            // comportement normal si ce n'est pas une commande SETNAME/REQUEST_PLAYERS
            onDataReceived?.Invoke(new Connection(conn), data, true);
        }

        public void StopListening()
        {
            if (listenerState != ConnectionState.Disconnected)
                listenerState = ConnectionState.Disconnecting;
            _server?.Stop();
            Debug.Log("[SteamTransport] StopListening - stopping server and clearing Players");
            Players.Clear();
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
            // tenter de décoder le message texte envoyé par le serveur
            string message = null;
            try
            {
                var bytes = ExtractBytesFromByteData(data);
                if (bytes != null && bytes.Length > 0)
                    message = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SteamTransport] OnClientDataReceived - decode failed: {e.Message}");
            }

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith("PLAYERS:"))
                {
                    var payload = message.Substring("PLAYERS:".Length);
                    var parts = string.IsNullOrEmpty(payload) ? Array.Empty<string>() : payload.Split('|');

                    // remplacer la liste locale par celle du serveur
                    Players.Clear();
                    foreach (var p in parts)
                    {
                        if (!string.IsNullOrEmpty(p))
                            Players.Add(p);
                    }

                    Debug.Log($"[SteamTransport] OnClientDataReceived - PLAYERS received ({Players.Count})");
                    // notifier listeners UI côté client (asServer = false)
                    return; // message géré
                }
            }

            // comportement par défaut
            onDataReceived?.Invoke(new Connection(-1), data, false);
        }

        private void OnClientStateChanged(ConnectionState state)
        {
            string _localName = GetLocalDisplayName();
            if (state == ConnectionState.Connected)
            {
                // if (!Players.Contains(_localName))
                // {
                //     Players.Add(_localName);
                //     Debug.Log($"[SteamTransport] OnClientStateChanged - added local player: {_localName}");
                // }

                Debug.Log($"[SteamTransport] OnClientStateChanged - Connected as local pseudo={_localName}");

                // envoyer SETNAME au serveur
                try
                {
                    string payload = "SETNAME:" + _localName;
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                    var reqData = new ByteData(bytes);
                    SendToServer(reqData, Channel.ReliableOrdered);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SteamTransport] Send SETNAME failed: {e.Message}");
                }

                // côté client : transmettre la liste complète (false)
                onConnected?.Invoke(new Connection(0), false);
            }

            if (state == ConnectionState.Disconnected)
            {
                Players.Remove(_localName);
                Debug.Log($"[SteamTransport] OnClientStateChanged - Disconnected local pseudo removed: {_localName}");
                onDisconnected?.Invoke(new Connection(0), DisconnectReason.ClientRequest, false);
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
            Players.Remove(_localName);
            Debug.Log($"[SteamTransport] Disconnect - local player removed: {_localName}");
        }

        private static string __displayNameSafe(string name) => name?.Replace("{", "{{").Replace("}", "}}") ?? "null";

        public void RaiseDataReceived(Connection conn, ByteData data, bool asServer)
        {
            onDataReceived?.Invoke(conn, data, asServer);
        }

        public void RaiseDataSent(Connection conn, ByteData data, bool asServer)
        {
            onDataSent?.Invoke(conn, data, asServer);
        }

        private ByteData BuildPlayersData()
        {
            string payload = "PLAYERS:" + string.Join("|", Players);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(payload);
            return new ByteData(bytes);
        }

        public void SendToClient(Connection target, ByteData data, Channel method = Channel.ReliableOrdered)
        {
            if (_server == null)
                return;

            if (listenerState is not ConnectionState.Connected)
                return;

            if (!target.isValid)
                return;

            _server.SendToConnection(target.connectionId, data, method);
            RaiseDataSent(target, data, true);
        }

        public void SendToServer(ByteData data, Channel method = Channel.ReliableOrdered)
        {
            if (_client == null)
                return;

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

        private string GetRemoteDisplayNameFromId(int obj)
        {
            // regarder si on a reçu un SETNAME pour cette connexion
            if (_connectionNames.TryGetValue(obj, out var storedName))
                return storedName;

            string display = obj.ToString();
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
            try
            {
                var steamId = new CSteamID((ulong)obj);
                // délègue à la surcharge qui prend un CSteamID
                return GetRemoteDisplayNameFromId(steamId);
            }
            catch
            {
                // fallback to id string
            }
#endif
            return display;
        }

        private string GetLocalDisplayName()
        {
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
            try
            {
                if (SteamAPI.IsSteamRunning())
                {
                    // utilisation correcte pour récupérer le nom local
                    string name = SteamFriends.GetPersonaName();
                    Debug.Log(name);

                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch
            {
                // fallback below
            }
#endif
            return "LocalClient";
        }

        private string GetRemoteDisplayNameFromId(CSteamID steamId)
        {
            string display = steamId.ToString();
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
            try
            {
                string friendName = SteamFriends.GetFriendPersonaName(steamId);
                Debug.Log(friendName);
                if (!string.IsNullOrEmpty(friendName))
                    display = friendName;
            }
            catch
            {
                // fallback to steamId string
            }
#endif
            return display;
        }
    }
}