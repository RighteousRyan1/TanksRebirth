using System;
using Microsoft.Xna.Framework;

namespace TanksRebirth.Internals.Common.Utilities;

public enum Anchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    TopCenter,
    BottomCenter,
    Center,
    LeftCenter,
    RightCenter,
}
public static class GameUtils
{
    public static Vector2 GetAnchor(this Anchor a, Vector2 vector) {
        return a switch {
            Anchor.TopLeft => Vector2.Zero,
            Anchor.TopRight => new(vector.X, 0),
            Anchor.BottomLeft => new(0, vector.Y),
            Anchor.BottomRight => new(vector.X, vector.Y),
            Anchor.LeftCenter => new(0, vector.Y / 2),
            Anchor.RightCenter => new(vector.X, vector.Y / 2),
            Anchor.Center => new(vector.X / 2, vector.Y / 2),
            Anchor.TopCenter => new(vector.X / 2, 0),
            Anchor.BottomCenter => new(vector.X / 2, vector.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(a), a, "There is no Anchor set for this Anchor...")
        };
    }

    public static float Distance_WiiTanksUnits(Vector2 position, Vector2 endPoint) => Vector2.Distance(position, endPoint) / 0.7f;
}
