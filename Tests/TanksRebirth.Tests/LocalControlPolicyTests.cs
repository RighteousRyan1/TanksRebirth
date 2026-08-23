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

    [Theory]
    [InlineData(true, false, 0f, 0f, 0f, 0, 1, true)]
    [InlineData(false, false, 0f, 0f, 0f, 0, 1, false)]
    [InlineData(true, true, 0f, 0f, 0f, 0, 1, false)]
    [InlineData(true, false, 1f, 0f, 0f, 0, 1, false)]
    [InlineData(true, false, 0f, 1f, 0f, 0, 1, false)]
    [InlineData(true, false, 0f, 0f, 1f, 0, 1, false)]
    [InlineData(true, false, 0f, 0f, 0f, 1, 1, false)]
    public void LocalWeaponUseRequiresEveryGuard(
        bool gameplayInputAllowed,
        bool stationary,
        float shootStun,
        float mineStun,
        float cooldown,
        int ownedCount,
        int capacity,
        bool expected) {
        Assert.Equal(expected, LocalControlPolicy.CanUseLocalWeapon(
            gameplayInputAllowed,
            stationary,
            shootStun,
            mineStun,
            cooldown,
            ownedCount,
            capacity));
    }
}
