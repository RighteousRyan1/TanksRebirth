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
    public static Vector2 GetAnchor(this Anchor a, Vector2 vector)
    {
        switch (a)
        {
            case Anchor.TopLeft:
                return Vector2.Zero;
            case Anchor.TopRight:
                return new(vector.X, 0);
            case Anchor.BottomLeft:
                return new(0, vector.Y);
            case Anchor.BottomRight:
                return new(vector.X, vector.Y);
            case Anchor.LeftCenter:
                return new(0, vector.Y / 2);
                case Anchor.RightCenter:
                return new(vector.X, vector.Y / 2);
            case Anchor.Center:
                return new(vector.X  /2 , vector.Y / 2);
            case Anchor.TopCenter:
                return new(vector.X / 2, 0);
            case Anchor.BottomCenter:
                return new(vector.X / 2, vector.Y);
        }
        return default;
    }

    public static float Distance_WiiTanksUnits(Vector2 position, Vector2 endPoint) => Vector2.Distance(position, endPoint) / 0.7f;
}
