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

    /// <summary>Indicates if this instance of the game is running on a Steam Deck.</summary>
    public static bool IsSteamDeck { get; private set; }

    static Callback<GameOverlayActivated_t>? _overlayActivate;

    public static void Initialize() {
        SteamAPI.Init();

        IsInitialized = true;
        _overlayActivate = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);

        IsSteamDeck = SteamUtils.IsSteamRunningOnSteamDeck();

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

        // we make this buffer the size of an RGBA color (4 bytes per)
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

    /// <summary>
    /// Opens the Steam overlay directly to the friend invitation dialog for the current lobby.
    /// </summary>
    /// <param name="lobbyId">The active Steam Matchmaking lobby ID.</param>
    public static void OpenInviteOverlay(CSteamID lobbyId) {
        if (!IsInitialized) return;
        SteamFriends.ActivateGameOverlayInviteDialog(lobbyId);
    }
    /// <summary>
    /// Opens a web URL inside the Steam overlay's integrated web browser.
    /// </summary>
    public static void OpenWebpageInOverlay(string url) {
        if (!IsInitialized) return;
        SteamFriends.ActivateGameOverlayToWebPage(url);
    }
    /// <summary>
    /// Opens the Steam overlay to a specific dialog (e.g., "Friends", "Community", "Players", "Settings", "OfficialGameGroup", "Stats", "Achievements").
    /// </summary>
    public static void OpenOverlay(string dialog = "Friends") {
        if (!IsInitialized) return;
        SteamFriends.ActivateGameOverlay(dialog);
    }

    /// <summary>
    /// Opens the Steam overlay directly to a specific user's Steam profile page.
    /// </summary>
    /// <param name="steamId">The Steam ID of the user to view.</param>
    public static void OpenUserProfile(CSteamID steamId) {
        if (!IsInitialized) return;

        // the "steamid" dialog key specifically tells the overlay to open their profile
        SteamFriends.ActivateGameOverlayToUser("steamid", steamId);
    }

    // doesn't exactly work?
    public static void SetSteamStatus(string description) {
        SteamFriends.SetRichPresence("status", description);
    }
}