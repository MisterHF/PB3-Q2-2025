using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class SteamAvatarFetcher
{
    // Télécharge l'avatar via Steam Web API GetPlayerSummaries.
    // onResult appelé avec le Sprite (ou null si échec).
    public static IEnumerator GetAvatarSpriteCoroutine(string steamWebApiKey, string steamId64, Action<Sprite> onResult, int pixelsPerUnit = 100)
    {
        if (string.IsNullOrEmpty(steamWebApiKey) || string.IsNullOrEmpty(steamId64))
        {
            onResult?.Invoke(null);
            yield break;
        }

        string api = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={UnityWebRequest.EscapeURL(steamWebApiKey)}&steamids={UnityWebRequest.EscapeURL(steamId64)}";
        using (var req = UnityWebRequest.Get(api))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"Steam API request failed: {req.error}");
                onResult?.Invoke(null);
                yield break;
            }

            string text = req.downloadHandler.text;
            // parsing minimaliste : chercher "avatarfull":"URL"
            string key = "\"avatarfull\":\"";
            int idx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                onResult?.Invoke(null);
                yield break;
            }
            idx += key.Length;
            int end = text.IndexOf('"', idx);
            if (end < 0)
            {
                onResult?.Invoke(null);
                yield break;
            }
            string avatarUrl = text.Substring(idx, end - idx).Replace("\\/", "/");

            if (string.IsNullOrEmpty(avatarUrl))
            {
                onResult?.Invoke(null);
                yield break;
            }

            using (var imgReq = UnityWebRequestTexture.GetTexture(avatarUrl))
            {
                yield return imgReq.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                if (imgReq.result != UnityWebRequest.Result.Success)
#else
                if (imgReq.isNetworkError || imgReq.isHttpError)
#endif
                {
                    Debug.LogWarning($"Failed to download avatar: {imgReq.error}");
                    onResult?.Invoke(null);
                    yield break;
                }

                var tex = DownloadHandlerTexture.GetContent(imgReq);
                if (tex == null)
                {
                    onResult?.Invoke(null);
                    yield break;
                }

                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                onResult?.Invoke(sprite);
            }
        }
    }
}
