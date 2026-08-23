using System.Linq;

namespace TanksRebirth.GameContent.Systems.LocalCoop;

public static class LocalCampaignRules {
    public static int[] ActivePlayerIds(LocalSession session)
        => Enumerable.Range(0, session.PlayerCount).ToArray();

    public static bool ShouldSpawnPlayer(LocalSession session, int playerId)
        => session.IsActivePlayer(playerId);

    public static void ChangeLife(int[] lives, int playerId, int delta)
        => lives[playerId] += delta;

    public static void ChangeLivesForActivePlayers(int[] lives, LocalSession session, int delta) {
        foreach (var playerId in ActivePlayerIds(session))
            ChangeLife(lives, playerId, delta);
    }

    public static bool CanTeamContinue(int[] lives, LocalSession session)
        => ActivePlayerIds(session).Any(playerId => lives[playerId] > 0);

    public static string? GetTemplateValidationError(int availableCount, LocalSession session) {
        if (availableCount == 0)
            return "No active player templates are available for the current local session.";

        if (availableCount < session.PlayerCount)
            return $"The current local session needs {session.PlayerCount} active player templates, but this mission provides {availableCount}.";

        return null;
    }
}
