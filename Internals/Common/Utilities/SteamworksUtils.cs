using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Systems;

namespace TanksRebirth.Internals.Common.Utilities;

public static class SteamworksUtils
{
    public static bool IsInitialized { get; private set; }
    public static string? MyUsername { get; private set; }
    public static int FriendsCount { get; private set; }

    public static bool IsOverlayActive { get; private set; }

    private static Callback<GameOverlayActivated_t> _overlayActivate;

    public static void Initialize() {
        SteamAPI.Init();

        IsInitialized = true;

        _overlayActivate = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);

        MyUsername = SteamFriends.GetPersonaName();
    }
    private static void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
    {
        IsOverlayActive = pCallback.m_bActive != 0;
    }
    public static void Update() {
        if (TankGame.UpdateCount % 30 == 0)
            FriendsCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll);
        SteamAPI.RunCallbacks();
    }

    public static Texture2D GetAvatar(CSteamID id) {
        var avatar = SteamFriends.GetLargeFriendAvatar(id);
        var validSize = SteamUtils.GetImageSize(avatar, out var pnWidth, out var pnHeight);

        // we make this buffer the size of an RGBA color (4 bytes per-word)
        if (validSize) {
            var buffer = new byte[pnWidth * pnHeight * 4];
            var validRgba = SteamUtils.GetImageRGBA(avatar, buffer, (int)(pnWidth * pnHeight * 4));

            if (validRgba) {
                var tex = new Texture2D(TankGame.Instance.GraphicsDevice, (int)pnWidth, (int)pnHeight);
                tex.SetData(buffer);
                return tex;
            }
        }
        return null;
    }

    public static void SetSteamStatus(string status, string description) {
        SteamFriends.SetRichPresence(status, description);
    }
}
