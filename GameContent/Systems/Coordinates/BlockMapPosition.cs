using Microsoft.Xna.Framework;
using System;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.Coordinates;

public struct BlockMapPosition
{
    public const int MAP_WIDTH_169 = 22;
    public const int MAP_HEIGHT = 17;

    public const int MAP_WIDTH_43 = 16;

    public static implicit operator BlockMapPosition(Vector3 position) => ConvertFromVector3(position);
    public static implicit operator Vector2(BlockMapPosition position) => Convert2D(position);
    public static implicit operator Vector3(BlockMapPosition position) => Convert3D(position);

    public int X;
    public int Y;

    public BlockMapPosition(int x, int y) {
        X = x;
        Y = y;
    }
    public BlockMapPosition(int xy) {
        X = xy;
        Y = xy;
    }

    public static Vector2 Convert2D(BlockMapPosition pos) {
        // (0, 0) == (MIN_X, MIN_Y)

        var orig = new Vector2(GameScene.CUBE_MIN_X, GameScene.CUBE_MIN_Y);

        var real = new Vector2(orig.X + (pos.X * Block.SIDE_LENGTH), orig.Y + (pos.Y * Block.SIDE_LENGTH) - 110);

        return real;
    }

    public static Vector3 Convert3D(BlockMapPosition pos) {
        // (0, 0) == (MIN_X, MIN_Y)

        var orig = new Vector3(GameScene.CUBE_MIN_X, 0, GameScene.CUBE_MIN_Y);

        var real = new Vector3(orig.X + (pos.X * Block.SIDE_LENGTH) + 1f, 0, orig.Y + (pos.Y * Block.SIDE_LENGTH) - 43);

        return real;
    }

    /// <summary>
    /// Literally doesn't work in the slightest. Do NOT USE
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static BlockMapPosition ConvertFromVector3(Vector3 position) {
        // convert position into a CubeMapPosition, and grid lock it
        var invarX = (int)MathF.Round(position.X % Block.SIDE_LENGTH, 1);
        var invarY = (int)MathF.Round(position.Z % Block.SIDE_LENGTH, 1);
        var invar = new BlockMapPosition(invarX, invarY);

        return invar;

    }
    public static BlockMapPosition ConvertFromVector2(Vector2 position) {
        // convert position into a CubeMapPosition, and grid lock it
        var invarX = (int)MathF.Round(position.X % Block.SIDE_LENGTH, 1);
        var invarY = (int)MathF.Round(position.Y % Block.SIDE_LENGTH, 1);
        var invar = new BlockMapPosition(invarX, invarY);

        return invar;

    }

    public override string ToString() {
        var sb = new System.Text.StringBuilder()
            .Append("{ ")
            .Append($"X: {X} | Y: {Y}")
            .Append(" }");

        return sb.ToString();
    }
}