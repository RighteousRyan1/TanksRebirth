namespace TanksRebirth.GameContent.Systems.LocalCoop;

public static class LocalControlPolicy {
    public static bool CanControlOffline(bool networkConnected, LocalSession session, int playerId) =>
        !networkConnected && session.IsActivePlayer(playerId);
}
