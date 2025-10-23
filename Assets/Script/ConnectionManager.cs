using PurrNet;
using PurrNet.Steam;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Script
{
    public sealed class ConnectionManager : NetworkIdentity
    {
        [SerializeField] private Button hostButton;

        public Button HostButton => hostButton;

        public Button ClientButton => clientButton;

        [SerializeField] private Button clientButton;
        [SerializeField] private TMP_Text hostTextField;
        [SerializeField] private TMP_InputField clientInputField;
        
        public static readonly UnityEvent UpdateLobbyEvent = new UnityEvent();

        private bool steamInitialized;

        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);

            if(InstanceHandler.GetInstance<ConnectionManager>() != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(this);

            InitializeSteam();
        }

        private void OnEnable()
        {
            hostButton?.onClick.AddListener(HandleHostClicked);
            clientButton?.onClick.AddListener(HandleClientClicked);
        }

        private void OnDisable()
        {
            hostButton?.onClick.RemoveListener(HandleHostClicked);
            clientButton?.onClick.RemoveListener(HandleClientClicked);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InstanceHandler.UnregisterInstance<ConnectionManager>();

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
                ulong _steam64 = _localId.m_SteamID; // ou localId.ToUInt64() si disponible

                Debug.Log($"✅ Steam initialisé : {SteamFriends.GetPersonaName()} (Steam64: {_steam64})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Exception lors de l'initialisation Steam : {e}");
                steamInitialized = false;
            }
        }

        public void StartHost()
        {
            var _steamTransport = NetworkManager.main.transport as SteamTransport;
            if (_steamTransport == null)
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

            _steamTransport.peerToPeer = true;
            _steamTransport.dedicatedServer = false;
            _steamTransport.address = _steam64.ToString();

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
            var _steamTransport = NetworkManager.main.transport as SteamTransport;
            if (_steamTransport == null)
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

            _steamTransport.peerToPeer = true;
            _steamTransport.dedicatedServer = false;
            _steamTransport.address = _hostId.ToString();

            NetworkManager.main.StartClient();

            Debug.Log($"🟡 Connexion au serveur SteamID: {_hostId}");
            RaiseUpdateLobby();
        }

        // UI handlers
        private void HandleHostClicked()
        {
            if (hostButton == null || hostTextField == null)
            {
                Debug.LogError("Bouton ou texte Host manquant.");
                return;
            }

            StartHost();

            if (NetworkManager.main.isOffline)
            {
                hostTextField.text = "Serveur Offline ❌";
                hostButton.image.color = Color.red;
                return;
            }

            CSteamID _localId = SteamUser.GetSteamID();
            hostTextField.text = _localId.m_SteamID.ToString();
            hostButton.image.color = Color.green;

            hostButton.onClick.RemoveListener(HandleHostClicked);
            clientButton.onClick.RemoveListener(HandleClientClicked);
        }

        private void HandleClientClicked()
        {
            if (string.IsNullOrEmpty(clientInputField.text))
            {
                clientInputField.text = "Entrez l'ID Steam de l'hôte";
                clientButton.image.color = Color.red;
                return;
            }

            StartClient(clientInputField.text);

            clientInputField.text = $"Connexion à {clientInputField.text}";
            clientButton.image.color = Color.green;

            clientButton.onClick.RemoveListener(HandleClientClicked);
            hostButton.onClick.RemoveListener(HandleHostClicked);
        }
    }
}