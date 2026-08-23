using TanksRebirth.GameContent.Systems.LocalCoop;
using Xunit;

namespace TanksRebirth.Tests;

public sealed class LocalControlPolicyTests {
    [Theory]
    [InlineData(false, false, 0, true)]
    [InlineData(false, false, 1, false)]
    [InlineData(false, true, 0, true)]
    [InlineData(false, true, 1, true)]
    [InlineData(false, true, 2, false)]
    [InlineData(true, true, 1, false)]
    public void ControlEligibilityIsIsolated(bool networkConnected, bool localCoop, int playerId, bool expected) {
        var session = new LocalSession();
        if (localCoop)
            session.StartLocalCoop();

        Assert.Equal(expected, LocalControlPolicy.CanControlOffline(networkConnected, session, playerId));
    }
}
