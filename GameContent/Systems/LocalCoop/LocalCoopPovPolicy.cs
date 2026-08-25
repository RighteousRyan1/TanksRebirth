using System;
using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Systems.LocalCoop;

public readonly record struct LocalCoopPovLayout(Rectangle PlayerOne, Rectangle PlayerTwo);

public static class LocalCoopPovPolicy {
    public static bool ShouldUseSplitScreen(
        bool localCoop,
        bool pov,
        bool mainMenu,
        bool levelEditor,
        bool connected) =>
        localCoop && pov && !mainMenu && !levelEditor && !connected;

    public static LocalCoopPovLayout CreateHorizontalLayout(int width, int height) {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Split-screen width must be positive.");
        if (height < 2)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Split-screen height must fit both players.");

        var playerOneHeight = height / 2;
        return new LocalCoopPovLayout(
            new Rectangle(0, 0, width, playerOneHeight),
            new Rectangle(0, playerOneHeight, width, height - playerOneHeight));
    }

    public static Point TargetSize(Rectangle destination) {
        if (destination.Width <= 0 || destination.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(destination), destination, "Render destination must have positive dimensions.");
        return destination.Size;
    }

    public static int ResolveCameraPlayerId(
        int requestedPlayerId,
        bool playerOneAvailable,
        bool playerOneDestroyed,
        bool playerTwoAvailable,
        bool playerTwoDestroyed) {
        if (requestedPlayerId is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(requestedPlayerId), requestedPlayerId, "Only local players 0 and 1 are supported.");

        var requestedAvailable = requestedPlayerId == 0 ? playerOneAvailable : playerTwoAvailable;
        var requestedDestroyed = requestedPlayerId == 0 ? playerOneDestroyed : playerTwoDestroyed;
        if (requestedAvailable && !requestedDestroyed)
            return requestedPlayerId;

        var otherPlayerId = 1 - requestedPlayerId;
        var otherAvailable = otherPlayerId == 0 ? playerOneAvailable : playerTwoAvailable;
        var otherDestroyed = otherPlayerId == 0 ? playerOneDestroyed : playerTwoDestroyed;
        if (otherAvailable && !otherDestroyed)
            return otherPlayerId;

        if (requestedAvailable)
            return requestedPlayerId;
        if (otherAvailable)
            return otherPlayerId;
        return -1;
    }
}
