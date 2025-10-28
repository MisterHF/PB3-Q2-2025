using System.Collections;
using PurrLobby;
using PurrNet;
using PurrNet.Steam;
using PurrNet.Transports;
using Script.LobbyManager;
using Script.UI;
using Steamworks;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using PlayerInfo = PurrNet.Steam.PlayerInfo;

namespace Script
{
    public sealed class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private Button hostButton;

        public Button HostButton => hostButton;
        public Button ClientButton => clientButton;

        [SerializeField] private Button clientButton;
        [SerializeField] private Image playerAvatar;
        [SerializeField] private TextMeshProUGUI playerPseudo;
        [SerializeField] private Button returnButton;
        [SerializeField] private CopyButton hostTextField;
        [SerializeField] private TMP_InputField clientInputField;
        [SerializeField] private UpdateLobbyList updateLobby;
        private SteamTransport transport;
        public static readonly UnityEvent UpdateLobbyEvent = new UnityEvent();

        private bool steamInitialized;

        private void Awake()
        {

            InitializeSteam();
        }


        private void OnEnable()
        {
            hostButton?.onClick.AddListener(HandleHostClicked);
            clientButton?.onClick.AddListener(HandleClientClicked);
            returnButton?.onClick.AddListener(StopClient);
        }

        private void OnDisable()
        {
            hostButton?.onClick.RemoveListener(HandleHostClicked);
            clientButton?.onClick.RemoveListener(HandleClientClicked);
            if (transport == null) return;
            transport.onConnected -= OnConnectedLocal;
            transport.OnLobbyUpdated -= OnLobbyUpdated;
        }

        private void OnConnectedLocal(Connection conn, bool asServer)
        {
            if (asServer) return;
            SendLocalPlayerInfo();
        }

        private void SendLocalPlayerInfo()
        {
            if (transport == null) return;
            var _localName = SteamFriends.GetPersonaName();
            Texture2D _avatar = LoadLocalMediumAvatar(SteamUser.GetSteamID());
            var _info = new PlayerInfo(_localName, SteamUser.GetSteamID(), "", _avatar);
            string _payload = "PLAYERINFO:" + JsonUtility.ToJson(_info);
            byte[] _bytes = System.Text.Encoding.UTF8.GetBytes(_payload);
            Debug.Log("SendToServer !");
            transport.SendToServer(new ByteData(_bytes), Channel.ReliableOrdered);
        }

        private void OnLobbyUpdated(LobbyData _Lobby, bool _AsServer)
        {
            Debug.Log($"[ConnectionManager] Lobby updated: host={_Lobby.HostName} players={_Lobby.Players.Count}");
            updateLobby.OnPlayerConnected(_Lobby, _AsServer);
        }
        
        private void OnDestroy()
        {
            if (steamInitialized)
            {
#if !UNITY_EDITOR
        SteamAPI.Shutdown();
#endif
                steamInitialized = false;
            }
        }

        private void Update()
        {
            if (steamInitialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        private void InitializeSteam()
        {
            try
            {
                if (!SteamAPI.Init())
                {
                    Debug.LogError("SteamAPI.Init() a échoué.");
                    steamInitialized = false;
                    return;
                }

                steamInitialized = true;

                CSteamID _localId = SteamUser.GetSteamID();
                ulong _steam64 = _localId.m_SteamID;
                playerPseudo.text = SteamFriends.GetPersonaName();

                Debug.Log($"✅ Steam initialisé : {SteamFriends.GetPersonaName()} (Steam64: {_steam64})");

                // Récupérer l'avatar medium dès l'init
                LoadLocalLargeAvatar(_localId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception lors de l'initialisation Steam : {e}");
                steamInitialized = false;
            }
        }

        private Texture2D LoadLocalMediumAvatar(CSteamID _LocalId)
        {
            if (playerAvatar == null)
            {
                Debug.LogWarning("[ConnectionManager] playerAvatar UI non assigné.");
                return null;
            }

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

            // Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            return _tex;
        }

        private void LoadLocalLargeAvatar(CSteamID _LocalId)
        {
            if (playerAvatar == null)
            {
                Debug.LogWarning("[ConnectionManager] playerAvatar UI non assigné.");
                return;
            }

            int _imageId = SteamFriends.GetLargeFriendAvatar(_LocalId);
            if (_imageId <= 0)
            {
                Debug.LogWarning("[ConnectionManager] Aucun avatar medium disponible.");
                return;
            }

            if (!SteamUtils.GetImageSize(_imageId, out uint _width, out uint _height))
            {
                Debug.LogWarning("[ConnectionManager] Impossible de récupérer la taille de l'image avatar.");
                return;
            }

            int _imageSize = (int)(_width * _height * 4);
            byte[] _image = new byte[_imageSize];

            if (!SteamUtils.GetImageRGBA(_imageId, _image, _imageSize))
            {
                Debug.LogWarning("[ConnectionManager] Impossible de récupérer les données RGBA de l'avatar.");
                return;
            }

            Texture2D _tex = new Texture2D((int)_width, (int)_height, TextureFormat.RGBA32, false);
            _tex.LoadRawTextureData(_image);
            _tex.Apply();

            Sprite _sprite = Sprite.Create(_tex, new Rect(0, 0, _width, _height), new Vector2(0.5f, 0.5f));
            playerAvatar.sprite = _sprite;
        }

        public void StartHost()
        {
            transport = NetworkManager.main.transport as SteamTransport;
            if (transport != null)
            {
                transport.onConnected += OnConnectedLocal;
                transport.OnLobbyUpdated += OnLobbyUpdated;
            }


            if (transport == null)
            {
                Debug.LogError("SteamTransport manquant sur le NetworkManager", this);
                return;
            }

            if (!steamInitialized)
            {
                Debug.LogError("Steam non initialisé !");
                return;
            }

            CSteamID _localId = SteamUser.GetSteamID();
            ulong _steam64 = _localId.m_SteamID;

            transport.peerToPeer = true;
            transport.dedicatedServer = false;
            transport.address = _steam64.ToString();

            NetworkManager.main.StartHost();

            Debug.Log($"🟢 Serveur Steam lancé pour SteamID64: {_steam64}");
            RaiseUpdateLobby();
        }

        private void RaiseUpdateLobby()
        {
            UpdateLobbyEvent?.Invoke();
        }

        // Client
        public void StartClient(string _SteamIdString)
        {
            transport = NetworkManager.main.transport as SteamTransport;
            if (transport != null)
            {
                Debug.Log("ListeningEvent");
                transport.onConnected += OnConnectedLocal;
                transport.OnLobbyUpdated += OnLobbyUpdated;
            }

            if (transport == null)
            {
                Debug.LogError("SteamTransport manquant sur le NetworkManager", this);
                return;
            }

            if (string.IsNullOrEmpty(_SteamIdString))
            {
                Debug.LogError("Adresse SteamID vide");
                return;
            }

            if (!ulong.TryParse(_SteamIdString, out var _hostId))
            {
                Debug.LogError("SteamID invalide.");
                return;
            }

            transport.peerToPeer = true;
            transport.dedicatedServer = false;
            transport.address = _hostId.ToString();

            NetworkManager.main.StartClient();

            Debug.Log($"🟡 Connexion au serveur SteamID: {_hostId}");
            RaiseUpdateLobby();
        }

        public void StopClient()
        {
            if (!NetworkManager.main.isHost)
                NetworkManager.main.StopClient();
            else
                NetworkManager.main.StopServer();
        }

        // UI handlers
        public void HandleHostClicked()
        {
            if (hostButton == null || hostTextField == null)
            {
                Debug.LogError("Bouton ou texte Host manquant.");
                return;
            }

            StartHost();

            if (NetworkManager.main.isOffline)
            {
                hostTextField.SetText("Serveur Offline ❌");
                hostButton.image.color = Color.red;
                return;
            }

            CSteamID _localId = SteamUser.GetSteamID();
            hostTextField.SetText(_localId.m_SteamID.ToString());
            hostButton.image.color = Color.green;

            hostButton.onClick.RemoveListener(HandleHostClicked);
            clientButton.onClick.RemoveListener(HandleClientClicked);
        }

        public void HandleClientClicked()
        {
            if (string.IsNullOrEmpty(clientInputField.text))
            {
                clientInputField.text = "Entrez l'ID Steam de l'hôte";
                clientButton.enabled = false;
                clientButton.image.color = Color.red;
                return;
            }

            clientButton.enabled = true;

            StartClient(clientInputField.text);

            clientInputField.text = $"Connexion à {clientInputField.text}";
            clientButton.image.color = Color.green;

            clientButton.onClick.RemoveListener(HandleClientClicked);
            hostButton.onClick.RemoveListener(HandleHostClicked);
        }

        public void LobbyPanelOpen(GameObject _LobbyPanel)
        {
            StartCoroutine(LobbyPanelCoroutine(_LobbyPanel));
        }

        private IEnumerator LobbyPanelCoroutine(GameObject _LobbyPanel)
        {
            yield return new WaitUntil(() => NetworkManager.main.isOffline);
            _LobbyPanel.SetActive(true);
        }

        public void Play()
        {
            NetworkManager.main.sceneModule.LoadSceneAsync("Feat-Character");
        }
    }
}