using Microsoft.Xna.Framework.Graphics;
using Steamworks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class SteamworksUtils {
    /// <summary>If the Steamworks API is initialized.</summary>
    public static bool IsInitialized { get; private set; }
    /// <summary>The player's Steam username.</summary>
    public static string? MyUsername { get; private set; }

    /// <summary>The user's Steam friend count.</summary>
    public static int FriendsCount { get; private set; }

    /// <summary>Indicates if the Steam overlay is active.</summary>
    public static bool IsOverlayActive { get; private set; }

    static Callback<GameOverlayActivated_t>? _overlayActivate;

    public static void Initialize() {
        SteamAPI.Init();

        IsInitialized = true;
        _overlayActivate = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);

        MyUsername = SteamFriends.GetPersonaName();
        FriendsCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll);
    }
    private static void OnGameOverlayActivated(GameOverlayActivated_t pCallback) {
        IsOverlayActive = pCallback.m_bActive != 0;
    }
    public static void Update() {
        SteamAPI.RunCallbacks();
    }

    /// <summary>
    /// Gets the texture/image data of a given Steam account's profile picture.
    /// </summary>
    /// <param name="id">The profile's Steam ID.</param>
    /// <returns></returns>
    public static Texture2D? GetAvatar(CSteamID id) {
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

    // doesn't exactly work?
    public static void SetSteamStatus(string status, string description) {
        SteamFriends.SetRichPresence(status, description);
    }
}