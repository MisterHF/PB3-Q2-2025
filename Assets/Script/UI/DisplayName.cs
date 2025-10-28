using PurrNet;
using Steamworks;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    public class DisplayName : NetworkBehaviour
    {
        private TextMeshProUGUI pseudo;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            pseudo = GetComponent<TextMeshProUGUI>();

            if (isOwner)
            {
                var _localName = SteamFriends.GetPersonaName();
                SetNameServerRpc(_localName);
                if (pseudo != null) pseudo.text = _localName;
            }
        }
        
        [ServerRpc(requireOwnership: false)]
        private void SetNameServerRpc(string _Name)
        {
            UpdateNameClientRpc(_Name);
        }

        [ObserversRpc]
        private void UpdateNameClientRpc(string _Name)
        {
            if (pseudo == null) pseudo = GetComponent<TextMeshProUGUI>();
            if (pseudo != null) pseudo.text = _Name;
        }
    }
}