using TanksRebirth.GameContent.Systems.LocalCoop;
using Xunit;

namespace TanksRebirth.Tests;

public sealed class LocalCampaignRulesTests {
    [Fact]
    public void ActivePlayerIdsReturnsOnlyBlueForSinglePlayer() {
        var session = new LocalSession();
        session.StartSinglePlayer();

        Assert.Equal([0], LocalCampaignRules.ActivePlayerIds(session));
    }

    [Fact]
    public void ActivePlayerIdsReturnsBlueAndRedForLocalCoop() {
        var session = new LocalSession();
        session.StartLocalCoop();

        Assert.Equal([0, 1], LocalCampaignRules.ActivePlayerIds(session));
    }

    [Fact]
    public void ShouldSpawnPlayerExcludesInactivePlayerIds() {
        var session = new LocalSession();
        session.StartLocalCoop();

        Assert.True(LocalCampaignRules.ShouldSpawnPlayer(session, 0));
        Assert.True(LocalCampaignRules.ShouldSpawnPlayer(session, 1));
        Assert.False(LocalCampaignRules.ShouldSpawnPlayer(session, 2));
        Assert.False(LocalCampaignRules.ShouldSpawnPlayer(session, 3));
    }

    [Fact]
    public void BonusLifeChangesOnlyActiveLocalCoopPlayers() {
        var session = new LocalSession();
        session.StartLocalCoop();
        var lives = new[] { 3, 3, 0, 0 };

        LocalCampaignRules.ChangeLivesForActivePlayers(lives, session, 1);

        Assert.Equal([4, 4, 0, 0], lives);
    }

    [Fact]
    public void BonusLifeInSinglePlayerLeavesInactiveSlotsUnchanged() {
        var session = new LocalSession();
        session.StartSinglePlayer();
        var lives = new[] { 3, 7, 8, 9 };

        LocalCampaignRules.ChangeLivesForActivePlayers(lives, session, 1);

        Assert.Equal([4, 7, 8, 9], lives);
    }

    [Fact]
    public void ChangeLifeChangesOnlyTheSpecifiedPlayer() {
        var lives = new[] { 3, 3, 0, 0 };

        LocalCampaignRules.ChangeLife(lives, playerId: 1, delta: -1);

        Assert.Equal([3, 2, 0, 0], lives);
    }

    [Fact]
    public void TeamCanContinueWhileAnyActivePlayerHasLives() {
        var session = new LocalSession();
        session.StartLocalCoop();

        Assert.True(LocalCampaignRules.CanTeamContinue([0, 2, 0, 0], session));
    }

    [Fact]
    public void TeamCannotContinueWhenAllActivePlayersHaveNoLives() {
        var session = new LocalSession();
        session.StartLocalCoop();

        Assert.False(LocalCampaignRules.CanTeamContinue([0, 0, 5, 5], session));
    }

    [Fact]
    public void ZeroActivePlayerTemplatesProducesClearValidationError() {
        var session = new LocalSession();
        session.StartLocalCoop();

        var error = LocalCampaignRules.GetTemplateValidationError(0, session);

        Assert.Equal("No active player templates are available for the current local session.", error);
    }
}
