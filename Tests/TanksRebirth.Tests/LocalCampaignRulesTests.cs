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
    public void SinglePlayerUsesEnabledRedTemplateForAiCompanion() {
        var session = new LocalSession();
        session.StartSinglePlayer();

        Assert.True(LocalCampaignRules.ShouldUseAiCompanionTemplate(session, aiCompanionEnabled: true, playerId: 1));
    }

    [Fact]
    public void LocalCoopNeverUsesActiveRedTemplateForAiCompanion() {
        var session = new LocalSession();
        session.StartLocalCoop();

        Assert.False(LocalCampaignRules.ShouldUseAiCompanionTemplate(session, aiCompanionEnabled: true, playerId: 1));
    }

    [Fact]
    public void DisabledAiCompanionNeverUsesRedTemplate() {
        var session = new LocalSession();
        session.StartSinglePlayer();

        Assert.False(LocalCampaignRules.ShouldUseAiCompanionTemplate(session, aiCompanionEnabled: false, playerId: 1));
    }

    [Fact]
    public void AvailableActivePlayerIdsAreDistinctAndKeepTemplateOrder() {
        var session = new LocalSession();
        session.StartLocalCoop();

        var available = LocalCampaignRules.AvailableActivePlayerIds(session, [1, 0, 1, 3, 0]);

        Assert.Equal([1, 0], available);
    }

    [Fact]
    public void DuplicateBlueTemplateDoesNotSatisfyMissingRedValidation() {
        var session = new LocalSession();
        session.StartLocalCoop();
        var available = LocalCampaignRules.AvailableActivePlayerIds(session, [0, 0]);

        var error = LocalCampaignRules.GetTemplateValidationError(available, session);

        Assert.Equal([0], available);
        Assert.Equal("The current local session needs 2 active player templates, but this mission provides 1.", error);
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
        Assert.True(LocalCampaignRules.CanTeamContinue([0, 2, 0, 0], [0, 1]));
    }

    [Fact]
    public void TeamCannotContinueWhenAllActivePlayersHaveNoLives() {
        Assert.False(LocalCampaignRules.CanTeamContinue([0, 0, 5, 5], [0, 1]));
    }

    [Fact]
    public void TeamContinuationIgnoresRetainedLivesForMissingMissionPlayer() {
        Assert.False(LocalCampaignRules.CanTeamContinue([0, 3, 0, 0], [0]));
    }

    [Fact]
    public void TeamCannotContinueWithNoAvailablePlayerTemplates() {
        Assert.False(LocalCampaignRules.CanTeamContinue([3, 3, 0, 0], []));
    }

    [Fact]
    public void ZeroActivePlayerTemplatesProducesClearValidationError() {
        var session = new LocalSession();
        session.StartLocalCoop();

        var error = LocalCampaignRules.GetTemplateValidationError([], session);

        Assert.Equal("No active player templates are available for the current local session.", error);
    }

    [Fact]
    public void EditorMissionDoesNotApplyLocalCampaignFiltering() {
        Assert.False(LocalCampaignRules.ShouldApplyToMission(clientConnected: false, levelEditorActive: true));
    }

    [Fact]
    public void OrdinaryOfflineMissionAppliesLocalCampaignFiltering() {
        Assert.True(LocalCampaignRules.ShouldApplyToMission(clientConnected: false, levelEditorActive: false));
    }

    [Fact]
    public void ConnectedMissionDoesNotApplyLocalCampaignFiltering() {
        Assert.False(LocalCampaignRules.ShouldApplyToMission(clientConnected: true, levelEditorActive: false));
    }

    [Fact]
    public void ChangeLifeRejectsNullLives() {
        var error = Assert.Throws<ArgumentNullException>(() => LocalCampaignRules.ChangeLife(null!, 0, -1));

        Assert.Equal("lives", error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void ChangeLifeRejectsInvalidPlayerId(int playerId) {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => LocalCampaignRules.ChangeLife([3, 3, 0, 0], playerId, -1));

        Assert.Equal("playerId", error.ParamName);
    }

    [Fact]
    public void ChangeLivesForActivePlayersRejectsShortLivesArray() {
        var session = new LocalSession();
        session.StartLocalCoop();

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LocalCampaignRules.ChangeLivesForActivePlayers([3], session, 1));

        Assert.Equal("lives", error.ParamName);
    }

    [Fact]
    public void CanTeamContinueRejectsNullLives() {
        var error = Assert.Throws<ArgumentNullException>(() => LocalCampaignRules.CanTeamContinue(null!, [0]));

        Assert.Equal("lives", error.ParamName);
    }

    [Fact]
    public void CanTeamContinueRejectsShortLivesArray() {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => LocalCampaignRules.CanTeamContinue([3], [0, 1]));

        Assert.Equal("lives", error.ParamName);
    }
}
