namespace TanksRebirth.GameContent.Systems.LocalCoop;

public static class LocalControlPolicy {
    public static bool CanControlOffline(bool networkConnected, LocalSession session, int playerId) =>
        !networkConnected && session.IsActivePlayer(playerId);

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
