using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrLobby;
using PurrNet;
using UnityEngine;

namespace Script.LobbyManager
{
    public class LobbyManager : MonoBehaviour
    {
        public ServerParameters ServerParametersArg = new ();
        public SerializableDictionary<string, string> ClientParametersArg = new ();
        
        private Lobby _currentLobby
        {
            get
            {
                if (!lobbyDataHolder)
                    return default;
                return lobbyDataHolder.CurrentLobby;
            }
            set
            {
                lobbyDataHolder.SetCurrentLobby(value);
            }
        }
        public Lobby CurrentLobby => _currentLobby;
        public ILobbyProvider CurrentProvider => currentProvider as ILobbyProvider;
        private ILobbyProvider currentProviderI;
        [SerializeField] private MonoBehaviour currentProvider;
        
        private LobbyDataHolder lobbyDataHolder;
        private int taskLock;


        public void CreateRoom()
        {
            CreateRoom(ServerParametersArg.MaxPlayers, ServerParametersArg.ServerParametersResearch.ToDictionary());
        }

        public void CreateRoom(int _MaxPlayers, Dictionary<string, string> _RoomProperties = null)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                Lobby _lobby = await currentProviderI.CreateLobbyAsync(_MaxPlayers, _RoomProperties);
                _currentLobby = _lobby;
            });
        }
        
        private void EnsureProviderSet()
        {
            if (currentProviderI == null)
                throw new InvalidOperationException("No lobby provider has been set.");
        }
        
        
        private async void RunTask(Func<Task> _Task)
        {
            if (_Task == null || currentProviderI == null) return;

            taskLock++;
            try
            {
                await _Task();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Task Error: {ex.Message}");
            }
            finally
            {
                taskLock--;
                if (taskLock < 0)
                    taskLock = 0;
            }
        }
    }

    [Serializable]
    public class ServerParameters
    {
        public int MaxPlayers = 5;
        public SerializableDictionary<string, string> ServerParametersResearch = null;
    }
}
