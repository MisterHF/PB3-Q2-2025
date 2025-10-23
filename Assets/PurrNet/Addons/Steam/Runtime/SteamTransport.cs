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
        [SerializeField] private List<string> Players = new List<string>();
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
            // Ignorer l'entrée de loopback/host souvent signalée comme id 0
            if (obj == 0)
                return;

            string _displayName = GetRemoteDisplayNameFromId(obj);
            string _localName = GetLocalDisplayName();

            // Ne pas ajouter si c'est en fait le même nom que le host
            if (_displayName == _localName)
                return;


            if (!Players.Contains(_displayName))
                Players.Add(_displayName);

            OnPlayerConnected?.Invoke(Players, true);
            var connection = new Connection(obj);
            _connections.Add(connection);
            onConnected?.Invoke(connection, true);

            // envoyer la liste mise à jour à tous (inclut le nouveau client)
            BroadcastPlayerList();
        }

        private void OnRemoteDisconnected(int obj)
        {
            // Ignorer l'entrée de loopback/host
            if (obj == 0)
                return;

            string displayName = GetRemoteDisplayNameFromId(obj);
            Players.RemoveAll(p => p == displayName);
            _connections.Remove(new Connection(obj));
            onDisconnected?.Invoke(new Connection(obj), DisconnectReason.ClientRequest, true);

            // informer les clients restants
            BroadcastPlayerList();
        }

        private void OnServerData(int conn, ByteData data)
        {
            onDataReceived?.Invoke(new Connection(conn), data, true);
        }

        public void StopListening()
        {
            if (listenerState != ConnectionState.Disconnected)
                listenerState = ConnectionState.Disconnecting;
            _server?.Stop();
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
            onDataReceived?.Invoke(new Connection(-1), data, false);
        }
        private void BroadcastPlayerList()
        {
            if (_server == null)
                return;

            // Format simple : "PLAYERS:name1|name2|name3"
            string payload = "PLAYERS:" + string.Join("|", Players);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(payload);

            // Création de ByteData à partir du tableau d'octets (adapter si l'API ByteData diffère)
            var data = new ByteData(bytes);

            foreach (var conn in _connections)
            {
                if (!conn.isValid)
                    continue;

                SendToClient(conn, data, Channel.ReliableOrdered);
            }
        }
        private void OnClientStateChanged(ConnectionState state)
        {
            string _localName = GetLocalDisplayName();
            if (state == ConnectionState.Connected)
            {
                if (!Players.Contains(_localName))
                {
                    Players.Add(_localName);
                }

                Debug.Log(_localName);

                OnPlayerConnected?.Invoke(new List<string>(Players), true);
                onConnected?.Invoke(new Connection(0), false);

                // demander/récupérer la liste du serveur nativement : ici on rebroadcast localement
                // si l'application serveur gère l'envoi, cette ligne côté server enverra la liste automatiquement.
                // Si vous souhaitez que le client demande explicitement la liste, envoyez une requête au serveur.
            }

            if (state == ConnectionState.Disconnected)
            {
                Players.RemoveAll(_P => _P == _localName);
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
            Players.RemoveAll(_P => _P == _localName);
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
            string display = obj.ToString();
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
            try
            {
                var steamId = new CSteamID((ulong)obj);
                string friendName = SteamFriends.GetFriendPersonaName(steamId);
                if (!string.IsNullOrEmpty(friendName))
                    display = friendName;
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
    }
}