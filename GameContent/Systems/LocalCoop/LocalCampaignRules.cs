using System;
using System.Collections.Generic;
using System.Linq;

namespace TanksRebirth.GameContent.Systems.LocalCoop;

public static class LocalCampaignRules {
    public static int[] ActivePlayerIds(LocalSession session) {
        ArgumentNullException.ThrowIfNull(session);
        return Enumerable.Range(0, session.PlayerCount).ToArray();
    }

    public static bool ShouldUseLives(LocalSession session) {
        ArgumentNullException.ThrowIfNull(session);
        return !session.IsLocalCoop;
    }

    public static bool ShouldSpawnPlayer(LocalSession session, int playerId, int livesRemaining) {
        ArgumentNullException.ThrowIfNull(session);
        return session.IsActivePlayer(playerId) && (!ShouldUseLives(session) || livesRemaining > 0);
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

    public static void ChangeLife(int[] lives, LocalSession session, int playerId, int delta) {
        ArgumentNullException.ThrowIfNull(lives);
        ArgumentNullException.ThrowIfNull(session);
        if (playerId < 0 || playerId >= lives.Length)
            throw new ArgumentOutOfRangeException(nameof(playerId));

        if (ShouldUseLives(session))
            lives[playerId] += delta;
    }

    public static void ChangeLivesForActivePlayers(int[] lives, LocalSession session, int delta) {
        ArgumentNullException.ThrowIfNull(lives);
        ArgumentNullException.ThrowIfNull(session);
        if (lives.Length < session.PlayerCount)
            throw new ArgumentOutOfRangeException(nameof(lives), "Lives array must contain every active local player.");

        foreach (var playerId in ActivePlayerIds(session))
            ChangeLife(lives, session, playerId, delta);
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

    public static bool ShouldEndAsGameOver(
        LocalSession session,
        IReadOnlyCollection<int> availablePlayerIds,
        IReadOnlyList<bool> playerAlive,
        IReadOnlyList<int> lives) {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(availablePlayerIds);
        ArgumentNullException.ThrowIfNull(playerAlive);
        ArgumentNullException.ThrowIfNull(lives);

        foreach (var playerId in availablePlayerIds) {
            if (playerId < 0 || playerId >= playerAlive.Count)
                throw new ArgumentOutOfRangeException(nameof(playerAlive), "Alive state must contain every available local player.");
            if (playerId >= lives.Count)
                throw new ArgumentOutOfRangeException(nameof(lives), "Lives must contain every available local player.");
        }

        var allAvailablePlayersDead = availablePlayerIds.Count > 0
            && availablePlayerIds.All(playerId => !playerAlive[playerId]);

        if (!allAvailablePlayersDead)
            return false;

        return session.IsLocalCoop || !availablePlayerIds.Any(playerId => lives[playerId] > 0);
    }

    public static bool ShouldUseBonusLifeSequence(LocalSession session, bool missionGrantsBonusLife, bool victory) {
        ArgumentNullException.ThrowIfNull(session);
        return ShouldUseLives(session) && missionGrantsBonusLife && victory;
    }

    public static bool IsLocalVictory(
        LocalSession session,
        IReadOnlyList<int> playerTeams,
        IReadOnlyList<bool> playerAlive,
        int finalTeam,
        int noTeam) {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(playerTeams);
        ArgumentNullException.ThrowIfNull(playerAlive);
        if (playerTeams.Count < session.PlayerCount)
            throw new ArgumentOutOfRangeException(nameof(playerTeams), "Team collection must contain every active local player.");
        if (playerAlive.Count < session.PlayerCount)
            throw new ArgumentOutOfRangeException(nameof(playerAlive), "Alive collection must contain every active local player.");

        if (!session.IsLocalCoop)
            return playerTeams[0] != noTeam && playerTeams[0] == finalTeam;

        return ActivePlayerIds(session).Any(playerId =>
            playerAlive[playerId] &&
            playerTeams[playerId] != noTeam &&
            playerTeams[playerId] == finalTeam);
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
