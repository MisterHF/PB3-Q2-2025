#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if STEAMWORKS_NET
#define STEAMWORKS_NET_PACKAGE
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using PurrNet.Editor;
using PurrNet.Transports;
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
using Steamworks;
#endif
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;

namespace PurrNet.Steam.Editor
{
    [CustomEditor(typeof(SteamTransport), true)]
    public class SteamTransportInspector : UnityEditor.Editor
    {
        private SteamTransport _steamTransport;

        public override void OnInspectorGUI()
        {
            var generic = (GenericTransport)target;
            if (!generic.isSupported)
            {
                GUI.enabled = false;
                base.OnInspectorGUI();

                if (!EditorApplication.isCompiling)
                    GUI.enabled = true;

                GUILayout.Space(10);

#if STEAMWORKS_NET_PACKAGE && DISABLESTEAMWORKS
                EditorGUILayout.HelpBox("SteamWorks.NET is disabled. Please enable it to use this transport.", MessageType.Warning);
                if (GUILayout.Button("Enable SteamWorks.NET"))
                {
                    RemoveDefineSymbols("DISABLESTEAMWORKS");
                }
#else
                EditorGUILayout.HelpBox("SteamWorks.NET is not installed. Please install it to use this transport.",
                    MessageType.Warning);
                if (GUILayout.Button("Add SteamWorks.NET to Package Manager"))
                {
                    if (GitHelper.CheckGit())
                    {
                        Client.Add(
                            "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2024.8.0");
                        Client.Resolve();
                    }
                }
#endif
                GUI.enabled = true;
            }
            else
            {
                base.OnInspectorGUI();
                TransportInspector.DrawTransportStatus(generic);

#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Copy my SteamID to Clipboard"))
                    {
                        string content = SteamFriends.GetPersonaName();
                        EditorGUIUtility.systemCopyBuffer = content;
                    }
                }
#endif

                // Affichage du lobby (lecture seule) si présent
                DrawLobbyReadonlySection();
            }
        }

        private void DrawLobbyReadonlySection()
        {
            if (_steamTransport == null) _steamTransport = target as SteamTransport;
            if (_steamTransport == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lobby (lecture seule depuis `_lobby`)", EditorStyles.boldLabel);

            // Récupère le champ privé `_lobby` via réflexion
            var t = _steamTransport.GetType();
            var field = t.GetField("_lobby", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                EditorGUILayout.HelpBox("Champ `_lobby` non trouvé. Assurez-vous que le champ existe dans SteamTransport.", MessageType.Warning);
                return;
            }

            var lobbyObj = field.GetValue(_steamTransport);
            if (lobbyObj == null)
            {
                EditorGUILayout.LabelField("Lobby = null");
                return;
            }

            // // JSON formaté (utile et rapide)
            // try
            // {
            //     string json = JsonUtility.ToJson(lobbyObj, true);
            //     EditorGUILayout.LabelField("JSON", EditorStyles.miniBoldLabel);
            //     EditorGUILayout.TextArea(json, GUILayout.Height(120));
            // }
            // catch
            // {
            //     EditorGUILayout.LabelField("Impossible de sérialiser en JSON.");
            // }

            // Affichage lisible : HostName + Players (si présents)
            var lobbyType = lobbyObj.GetType();
            // HostName peut être champ ou propriété
            string host = GetStringMemberValue(lobbyType, lobbyObj, "HostName") ?? GetStringMemberValue(lobbyType, lobbyObj, "hostName");
            EditorGUILayout.LabelField("HostName", host ?? "-");

            // Players peut être champ ou propriété
            var playersEnumerable = GetEnumerableMemberValue(lobbyType, lobbyObj, "Players") ?? GetEnumerableMemberValue(lobbyType, lobbyObj, "players");
            if (playersEnumerable != null)
            {
                EditorGUILayout.LabelField("Players:");
                EditorGUI.indentLevel++;
                foreach (var p in playersEnumerable)
                {
                    if (p == null) continue;
                    var pType = p.GetType();
                    string pname = GetStringMemberValue(pType, p, "Name") ?? GetStringMemberValue(pType, p, "name") ?? p.ToString();
                    string pid = GetStringMemberValue(pType, p, "Id") ?? GetStringMemberValue(pType, p, "id") ?? "";
                    string pavatar = GetStringMemberValue(pType, p, "AvatarUrl") ?? GetStringMemberValue(pType, p, "avatarUrl") ?? "";

                    EditorGUILayout.LabelField("- " + (pname ?? "Unnamed") + (string.IsNullOrEmpty(pid) ? "" : $" (id:{pid})"));
                    if (!string.IsNullOrEmpty(pavatar))
                        EditorGUILayout.LabelField("  avatar: " + pavatar);
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("Players list vide ou non itérable.");
            }
        }

        private static string GetStringMemberValue(Type type, object obj, string memberName)
        {
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                var v = field.GetValue(obj);
                return v as string;
            }

            var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                var v = prop.GetValue(obj);
                return v as string;
            }

            return null;
        }

        private static System.Collections.IEnumerable GetEnumerableMemberValue(Type type, object obj, string memberName)
        {
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(obj) as System.Collections.IEnumerable;

            var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
                return prop.GetValue(obj) as System.Collections.IEnumerable;

            return null;
        }

        [UsedImplicitly]
        static void RemoveDefineSymbols(string symbol)
        {
            string currentDefines;
            HashSet<string> defines;

#if UNITY_2021_1_OR_NEWER
            currentDefines =
                PlayerSettings.GetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
#else
      currentDefines =
 PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
            defines = new HashSet<string>(currentDefines.Split(';'));
            defines.Remove(symbol);

            string newDefines = string.Join(";", defines);
            if (newDefines != currentDefines)
            {
#if UNITY_2021_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
                    newDefines);
#else
       PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines);
#endif
            }
        }

        private void OnEnable()
        {
            _steamTransport = target as SteamTransport;
            var generic = _steamTransport as GenericTransport;
            if (generic != null)
            {
                generic.transport.onConnectionState += OnDirty;
            }

            // s'abonner aux événements runtime pour repaint si possible
            if (_steamTransport != null)
            {
                try
                {
                    _steamTransport.OnLobbyUpdated += OnDirtyLobby;
                    _steamTransport.OnPlayerConnected += OnDirtyPlayerList;
                }
                catch { /* ignore si runtime non dispo */ }
            }
        }

        private void OnDisable()
        {
            var generic = _steamTransport as GenericTransport;
            if (generic != null)
            {
                generic.transport.onConnectionState -= OnDirty;
            }

            if (_steamTransport != null)
            {
                try
                {
                    _steamTransport.OnLobbyUpdated -= OnDirtyLobby;
                    _steamTransport.OnPlayerConnected -= OnDirtyPlayerList;
                }
                catch { }
            }
        }

        private void OnDirty(ConnectionState state, bool asServer)
        {
            Repaint();
        }

        private void OnDirtyLobby(object _, bool __)
        {
            Repaint();
        }

        private void OnDirtyPlayerList(List<string> _, bool __)
        {
            Repaint();
        }
    }
}
