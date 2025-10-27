using PurrNet;
using Steamworks;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    public class DisplayName : NetworkBehaviour
    {
        private TextMeshProUGUI pseudo;
        private void OnEnable()
        {
            if(!isOwner) return;
            pseudo = GetComponent<TextMeshProUGUI>();
            pseudo.text = SteamFriends.GetPersonaName();
        }
    }
}
