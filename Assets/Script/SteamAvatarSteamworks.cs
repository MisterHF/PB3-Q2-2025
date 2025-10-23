#if !DISABLE_STEAMWORKS
using System;
using Steamworks;
using UnityEngine;

public static class SteamAvatarSteamworks
{
    // Retourne null si échec.
    public static Sprite GetAvatarSpriteFromSteamworks(ulong steamId64, int pixelsPerUnit = 100)
    {
        // Tenter d'initialiser Steamworks si nécessaire.
        bool steamReady = false;
        try
        {
            steamReady = SteamAPI.Init();
        }
        catch (Exception)
        {
            steamReady = false;
        }

        if (!steamReady)
            return null;

        CSteamID id = new CSteamID(steamId64);
        int imgId = SteamFriends.GetLargeFriendAvatar(id);
        if (imgId <= 0)
            return null;

        if (!SteamUtils.GetImageSize(imgId, out uint width, out uint height) || width == 0 || height == 0)
            return null;

        int size = (int)(width * height * 4);
        byte[] rgba = new byte[size];
        if (!SteamUtils.GetImageRGBA(imgId, rgba, size))
            return null;

        var tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(rgba);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
#endif