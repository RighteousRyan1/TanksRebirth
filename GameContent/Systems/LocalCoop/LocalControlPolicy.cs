using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.LocalCoop;

public static class LocalControlPolicy {
    private const int GeneralDebugLevel = 0;

    public static bool ShouldRunLegacyDebugShellShortcut(
        bool debuggingEnabled,
        int debugLevel,
        bool clientConnected,
        bool localCoop,
        bool keyJustPressed) =>
        debuggingEnabled
        && debugLevel == GeneralDebugLevel
        && !clientConnected
        && !localCoop
        && keyJustPressed;

    public static bool CanControlOffline(bool networkConnected, LocalSession session, int playerId) =>
        !networkConnected && session.IsActivePlayer(playerId);

    public static float DirectionalTurretRotation(Vector2 aim) =>
        -aim.ToRotation() + MathHelper.PiOver2;

    public static bool CanUseLocalWeapon(
        bool gameplayInputAllowed,
        bool stationary,
        float shootStun,
        float mineStun,
        float cooldown,
        long ownedCount,
        long capacity) =>
        gameplayInputAllowed
        && !stationary
        && shootStun <= 0
        && mineStun <= 0
        && cooldown <= 0
        && ownedCount < capacity;
}
