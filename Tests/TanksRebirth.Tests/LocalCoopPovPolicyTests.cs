using System;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems.LocalCoop;
using Xunit;

namespace TanksRebirth.Tests;

public sealed class LocalCoopPovPolicyTests {
    [Fact]
    public void SplitScreenRequiresOfflineLocalCoopPovGameplay() {
        Assert.True(LocalCoopPovPolicy.ShouldUseSplitScreen(
            localCoop: true, pov: true, mainMenu: false, levelEditor: false, connected: false));

        Assert.False(LocalCoopPovPolicy.ShouldUseSplitScreen(
            localCoop: false, pov: true, mainMenu: false, levelEditor: false, connected: false));
        Assert.False(LocalCoopPovPolicy.ShouldUseSplitScreen(
            localCoop: true, pov: false, mainMenu: false, levelEditor: false, connected: false));
        Assert.False(LocalCoopPovPolicy.ShouldUseSplitScreen(
            localCoop: true, pov: true, mainMenu: true, levelEditor: false, connected: false));
        Assert.False(LocalCoopPovPolicy.ShouldUseSplitScreen(
            localCoop: true, pov: true, mainMenu: false, levelEditor: true, connected: false));
        Assert.False(LocalCoopPovPolicy.ShouldUseSplitScreen(
            localCoop: true, pov: true, mainMenu: false, levelEditor: false, connected: true));
    }

    [Fact]
    public void HorizontalLayoutCoversOddHeightWithoutOverlap() {
        var layout = LocalCoopPovPolicy.CreateHorizontalLayout(1440, 901);

        Assert.Equal(new Rectangle(0, 0, 1440, 450), layout.PlayerOne);
        Assert.Equal(new Rectangle(0, 450, 1440, 451), layout.PlayerTwo);
        Assert.Equal(901, layout.PlayerOne.Height + layout.PlayerTwo.Height);
    }

    [Fact]
    public void InvalidDimensionsAreRejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalCoopPovPolicy.CreateHorizontalLayout(0, 900));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalCoopPovPolicy.CreateHorizontalLayout(1440, 1));
    }

    [Fact]
    public void TargetSizesMatchDestinationRectangles() {
        var layout = LocalCoopPovPolicy.CreateHorizontalLayout(1440, 901);

        Assert.Equal(new Point(1440, 450), LocalCoopPovPolicy.TargetSize(layout.PlayerOne));
        Assert.Equal(new Point(1440, 451), LocalCoopPovPolicy.TargetSize(layout.PlayerTwo));
    }

    [Fact]
    public void DestroyedPlayerSpectatesSurvivor() {
        Assert.Equal(1, LocalCoopPovPolicy.ResolveCameraPlayerId(
            requestedPlayerId: 0,
            playerOneAvailable: true,
            playerOneDestroyed: true,
            playerTwoAvailable: true,
            playerTwoDestroyed: false));
    }

    [Fact]
    public void ActivePlayerKeepsOwnCamera() {
        Assert.Equal(1, LocalCoopPovPolicy.ResolveCameraPlayerId(
            requestedPlayerId: 1,
            playerOneAvailable: true,
            playerOneDestroyed: false,
            playerTwoAvailable: true,
            playerTwoDestroyed: false));
    }

    [Fact]
    public void NoAvailablePlayerReturnsNoCamera() {
        Assert.Equal(-1, LocalCoopPovPolicy.ResolveCameraPlayerId(
            requestedPlayerId: 0,
            playerOneAvailable: false,
            playerOneDestroyed: false,
            playerTwoAvailable: false,
            playerTwoDestroyed: false));
    }

    [Fact]
    public void PovCameraFactoryProducesFiniteMatrices() {
        var camera = CameraGlobals.CreatePovCamera(new Vector2(10, 20), MathHelper.PiOver4, 16f / 5f);

        Assert.True(IsFinite(camera.View));
        Assert.True(IsFinite(camera.Projection));
        Assert.Equal(new Vector3(10, 0, 20), camera.Position);
    }

    private static bool IsFinite(Matrix matrix) =>
        float.IsFinite(matrix.M11) && float.IsFinite(matrix.M12) && float.IsFinite(matrix.M13) && float.IsFinite(matrix.M14)
        && float.IsFinite(matrix.M21) && float.IsFinite(matrix.M22) && float.IsFinite(matrix.M23) && float.IsFinite(matrix.M24)
        && float.IsFinite(matrix.M31) && float.IsFinite(matrix.M32) && float.IsFinite(matrix.M33) && float.IsFinite(matrix.M34)
        && float.IsFinite(matrix.M41) && float.IsFinite(matrix.M42) && float.IsFinite(matrix.M43) && float.IsFinite(matrix.M44);
}
