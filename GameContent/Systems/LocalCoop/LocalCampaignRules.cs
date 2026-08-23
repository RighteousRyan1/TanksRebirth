using System;
using System.Collections.Generic;
using System.Linq;

namespace TanksRebirth.GameContent.Systems.LocalCoop;

public static class LocalCampaignRules {
    public static int[] ActivePlayerIds(LocalSession session) {
        ArgumentNullException.ThrowIfNull(session);
        return Enumerable.Range(0, session.PlayerCount).ToArray();
    }

    public static bool ShouldSpawnPlayer(LocalSession session, int playerId) {
        ArgumentNullException.ThrowIfNull(session);
        return session.IsActivePlayer(playerId);
    }

    public static bool ShouldUseAiCompanionTemplate(LocalSession session, bool aiCompanionEnabled, int playerId) {
        ArgumentNullException.ThrowIfNull(session);
        const int redPlayerId = 1;
        return aiCompanionEnabled && !session.IsLocalCoop && playerId == redPlayerId;
    }

    public static int[] AvailableActivePlayerIds(LocalSession session, IEnumerable<int> templatePlayerIds) {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(templatePlayerIds);
        var availablePlayerIds = new List<int>();
        var seenPlayerIds = new HashSet<int>();
        foreach (var playerId in templatePlayerIds) {
            if (session.IsActivePlayer(playerId) && seenPlayerIds.Add(playerId))
                availablePlayerIds.Add(playerId);
        }

        return availablePlayerIds.ToArray();
    }

    public static bool ShouldApplyToMission(bool clientConnected, bool levelEditorActive)
        => !clientConnected && !levelEditorActive;

    public static void ChangeLife(int[] lives, int playerId, int delta) {
        ArgumentNullException.ThrowIfNull(lives);
        if (playerId < 0 || playerId >= lives.Length)
            throw new ArgumentOutOfRangeException(nameof(playerId));

        lives[playerId] += delta;
    }

    public static void ChangeLivesForActivePlayers(int[] lives, LocalSession session, int delta) {
        ArgumentNullException.ThrowIfNull(lives);
        ArgumentNullException.ThrowIfNull(session);
        if (lives.Length < session.PlayerCount)
            throw new ArgumentOutOfRangeException(nameof(lives), "Lives array must contain every active local player.");

        foreach (var playerId in ActivePlayerIds(session))
            ChangeLife(lives, playerId, delta);
    }

    public static bool CanTeamContinue(int[] lives, IReadOnlyCollection<int> availablePlayerIds) {
        ArgumentNullException.ThrowIfNull(lives);
        ArgumentNullException.ThrowIfNull(availablePlayerIds);
        foreach (var playerId in availablePlayerIds) {
            if (playerId < 0)
                throw new ArgumentOutOfRangeException(nameof(availablePlayerIds));
            if (playerId >= lives.Length)
                throw new ArgumentOutOfRangeException(nameof(lives), "Lives array must contain every available local player.");
        }

        return availablePlayerIds.Any(playerId => lives[playerId] > 0);
    }

    public static string? GetTemplateValidationError(IReadOnlyCollection<int> availablePlayerIds, LocalSession session) {
        ArgumentNullException.ThrowIfNull(availablePlayerIds);
        ArgumentNullException.ThrowIfNull(session);
        if (availablePlayerIds.Count == 0)
            return "No active player templates are available for the current local session.";

        if (availablePlayerIds.Count < session.PlayerCount)
            return $"The current local session needs {session.PlayerCount} active player templates, but this mission provides {availablePlayerIds.Count}.";

        return null;
    }
}
