using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common.Framework.Input;
using Xunit;

namespace TanksRebirth.Tests;

public sealed class LocalPlayerInputRouterTests {
    [Fact]
    public void P1KeysDoNotMoveP2() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.W), new KeyboardState(), default, default);

        Assert.Equal(new Vector2(0, -1), router.GetFrame(0).Movement);
        Assert.Equal(Vector2.Zero, router.GetFrame(1).Movement);
    }

    [Fact]
    public void P2KeysDoNotMoveP1() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.I), new KeyboardState(), default, default);

        Assert.Equal(Vector2.Zero, router.GetFrame(0).Movement);
        Assert.Equal(new Vector2(0, -1), router.GetFrame(1).Movement);
    }

    [Fact]
    public void P2AimRetainsItsLastDirection() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.Right), new KeyboardState(), default, default);
        var aimed = router.GetFrame(1).Aim;

        router.Update(new KeyboardState(), new KeyboardState(Keys.Right), default, default);

        Assert.Equal(Vector2.UnitX, aimed);
        Assert.Equal(Vector2.UnitX, router.GetFrame(1).Aim);
    }

    [Fact]
    public void P2AimInputClearsWhenArrowIsReleased() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.Right), new KeyboardState(), default, default);

        Assert.Equal(Vector2.UnitX, router.GetFrame(1).AimInput);

        router.Update(new KeyboardState(), new KeyboardState(Keys.Right), default, default);

        Assert.Equal(Vector2.Zero, router.GetFrame(1).AimInput);
        Assert.Equal(Vector2.UnitX, router.GetFrame(1).Aim);
    }

    [Fact]
    public void FireEdgesAreIndependent() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.Enter), new KeyboardState(), default, default);

        Assert.False(router.GetFrame(0).FireJustPressed);
        Assert.True(router.GetFrame(1).FireJustPressed);
    }

    [Fact]
    public void P1LeftClickFiresOnlyOnPressEdge() {
        var router = new LocalPlayerInputRouter();
        var released = MouseWithLeftButton(ButtonState.Released);
        var pressed = MouseWithLeftButton(ButtonState.Pressed);

        router.Update(new KeyboardState(), new KeyboardState(), pressed, released);
        Assert.True(router.GetFrame(0).FireJustPressed);

        router.Update(new KeyboardState(), new KeyboardState(), pressed, pressed);
        Assert.False(router.GetFrame(0).FireJustPressed);
    }

    [Fact]
    public void P1SpaceMineUsesPressEdge() {
        var router = new LocalPlayerInputRouter();

        router.Update(new KeyboardState(Keys.Space), new KeyboardState(), default, default);
        Assert.True(router.GetFrame(0).MineJustPressed);

        router.Update(new KeyboardState(Keys.Space), new KeyboardState(Keys.Space), default, default);
        Assert.False(router.GetFrame(0).MineJustPressed);
    }

    [Fact]
    public void P2RightShiftMineUsesPressEdge() {
        var router = new LocalPlayerInputRouter();

        router.Update(new KeyboardState(Keys.RightShift), new KeyboardState(), default, default);
        Assert.True(router.GetFrame(1).MineJustPressed);

        router.Update(new KeyboardState(Keys.RightShift), new KeyboardState(Keys.RightShift), default, default);
        Assert.False(router.GetFrame(1).MineJustPressed);
    }

    [Fact]
    public void P2DiagonalAimIsNormalized() {
        var router = new LocalPlayerInputRouter();

        router.Update(new KeyboardState(Keys.Up, Keys.Right), new KeyboardState(), default, default);

        var aim = router.GetFrame(1).Aim;
        Assert.InRange(aim.Length(), 0.9999f, 1.0001f);
        Assert.True(aim.X > 0);
        Assert.True(aim.Y < 0);
    }

    [Fact]
    public void P2DefaultAimPointsUp() {
        var router = new LocalPlayerInputRouter();

        Assert.Equal(-Vector2.UnitY, router.GetFrame(1).Aim);
        Assert.Equal(LocalAimSource.Direction, router.GetFrame(1).AimSource);
    }

    [Fact]
    public void P1MousePositionIsItsAimVector() {
        var router = new LocalPlayerInputRouter();

        router.Update(new KeyboardState(), new KeyboardState(), MouseAt(123, 456), default);

        Assert.Equal(new Vector2(123, 456), router.GetFrame(0).Aim);
        Assert.Equal(new Vector2(123, 456), router.GetFrame(0).AimInput);
        Assert.Equal(LocalAimSource.Mouse, router.GetFrame(0).AimSource);
    }

    [Fact]
    public void ShotPathBindingsAreIndependent() {
        var router = new LocalPlayerInputRouter();

        router.Update(new KeyboardState(Keys.Q), new KeyboardState(), default, default);
        Assert.True(router.GetFrame(0).ShotPathHeld);
        Assert.False(router.GetFrame(1).ShotPathHeld);

        router.Update(new KeyboardState(Keys.P), new KeyboardState(), default, default);
        Assert.False(router.GetFrame(0).ShotPathHeld);
        Assert.True(router.GetFrame(1).ShotPathHeld);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void InvalidPlayerIdIsRejected(int playerId) {
        var router = new LocalPlayerInputRouter();

        Assert.Throws<ArgumentOutOfRangeException>(() => router.GetFrame(playerId));
    }

    private static MouseState MouseAt(int x, int y) =>
        new(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    private static MouseState MouseWithLeftButton(ButtonState leftButton) =>
        new(0, 0, 0, leftButton, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
}
