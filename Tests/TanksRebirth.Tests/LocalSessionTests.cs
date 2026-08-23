using TanksRebirth.GameContent.Systems.LocalCoop;
using Xunit;

namespace TanksRebirth.Tests;

public sealed class LocalSessionTests {
    [Fact]
    public void SinglePlayerActivatesOnlyBlue() {
        var session = new LocalSession();
        session.StartSinglePlayer();

        Assert.Equal(1, session.PlayerCount);
        Assert.True(session.IsActivePlayer(0));
        Assert.False(session.IsActivePlayer(1));
    }

    [Fact]
    public void LocalCoopActivatesBlueAndRedOnly() {
        var session = new LocalSession();
        session.StartLocalCoop();

        Assert.Equal(2, session.PlayerCount);
        Assert.True(session.IsActivePlayer(0));
        Assert.True(session.IsActivePlayer(1));
        Assert.False(session.IsActivePlayer(2));
        Assert.False(session.IsActivePlayer(3));
    }
}
