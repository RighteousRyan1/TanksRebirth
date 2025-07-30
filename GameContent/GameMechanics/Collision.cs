using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.GameMechanics;

public static class Collision {
    public struct CollisionInfo {
        public float Value;
        public Vector2 Normal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(float value, float normalX, float normalY) {
            Value = value;
            Normal.X = normalX;
            Normal.Y = normalY;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsColliding(Rectangle movingBox, Rectangle collidingBox, Vector2 offset, out CollisionInfo info) {
        // early exit if no movement
        if (offset.X == 0f && offset.Y == 0f) {
            info = default;
            return false;
        }

        float horizontalT = -1f;
        float verticalT = -1f;

        // calculates horizontal collision time
        if (offset.X > 0f) {
            horizontalT = (collidingBox.Left - movingBox.Right) / offset.X;
        }
        else if (offset.X < 0f) {
            horizontalT = (collidingBox.Right - movingBox.Left) / offset.X;
        }

        // now vertical collision time
        if (offset.Y > 0f) {
            verticalT = (collidingBox.Top - movingBox.Bottom) / offset.Y;
        }
        else if (offset.Y < 0f) {
            verticalT = (collidingBox.Bottom - movingBox.Top) / offset.Y;
        }

        // check horizontal collision validity
        bool isHorizontalValid = horizontalT >= 0f && horizontalT <= 1f &&
            collidingBox.Top < movingBox.Bottom && collidingBox.Bottom > movingBox.Top;

        // ... now vertical
        bool isVerticalValid = verticalT >= 0f && verticalT <= 1f &&
            collidingBox.Left < movingBox.Right && collidingBox.Right > movingBox.Left;

        if (!isHorizontalValid && !isVerticalValid) {
            info = default;
            return false;
        }

        // choose the earliest collision
        if (!isVerticalValid || (isHorizontalValid && horizontalT < verticalT)) {
            info.Value = horizontalT;
            info.Normal.X = offset.X > 0f ? -1f : 1f;
            info.Normal.Y = 0f;
        }
        else {
            info.Value = verticalT;
            info.Normal.X = 0f;
            info.Normal.Y = offset.Y > 0f ? -1f : 1f;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HandleCollisionSimple(Rectangle movingBox, Rectangle collidingBox, Vector2 velocity, ref Vector2 position, bool setpos = true) {
        if (!IsColliding(movingBox, collidingBox, velocity, out var info) || info.Value >= 1f)
            return;

        if (setpos) {
            // Calculate dot product inline for better performance
            float dot = velocity.X * info.Normal.X + velocity.Y * info.Normal.Y;
            position.X -= dot * info.Normal.X;
            position.Y -= dot * info.Normal.Y;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HandleCollisionSimple(Rectangle movingBox, Rectangle collidingBox, ref Vector2 velocity, ref Vector2 position, out CollisionDirection direction, bool setpos = true) {
        if (!IsColliding(movingBox, collidingBox, velocity, out var info) || info.Value >= 1f) {
            direction = CollisionDirection.None;
            return;
        }

        if (setpos) {
            float dot = velocity.X * info.Normal.X + velocity.Y * info.Normal.Y;
            position.X -= dot * info.Normal.X;
            position.Y -= dot * info.Normal.Y;
        }

        // Branchless direction assignment using bit manipulation
        direction = info.Normal.Y > 0f ? CollisionDirection.Up :
                    info.Normal.Y < 0f ? CollisionDirection.Down :
                    info.Normal.X > 0f ? CollisionDirection.Left : CollisionDirection.Right;
    }

    // i asked ai to optimize this method since i have no clue what the hell i wrote before
    public static void HandleCollisionSimple_ForBlocks(Rectangle movingBox, Vector2 velocity, ref Vector2 position, out CollisionDirection direction, out Block block, out bool cornerCollision, bool setpos = true, Func<Block, bool> exclude = null) {
        cornerCollision = false;
        block = null;
        direction = CollisionDirection.None;

        var collisionInfo = new CollisionInfo { Value = 1f };
        int collisionCount = 0;

        // Cache blocks array to avoid repeated property access
        var blocks = Block.AllBlocks;
        var blocksLength = blocks.Length;

        // Use for loop instead of foreach for better performance
        for (int i = 0; i < blocksLength; i++) {
            var cube = blocks[i];
            if (cube == null) continue;

            if (movingBox.Intersects(cube.Hitbox)) {
                collisionCount++;
                if (exclude?.Invoke(cube) == true && collisionCount == 1) {
                    cornerCollision = true;
                    break;
                }
                else if (exclude == null) {
                    cornerCollision = true;
                    break;
                }
            }

            // Skip collision calculation if excluded
            if (exclude != null && !exclude(cube)) continue;

            if (IsColliding(movingBox, cube.Hitbox, velocity, out var info) && info.Value < collisionInfo.Value) {
                collisionInfo = info;
                block = cube;
            }
        }

        if (collisionInfo.Value >= 1f) return;

        if (setpos) {
            float dot = velocity.X * collisionInfo.Normal.X + velocity.Y * collisionInfo.Normal.Y;
            position.X -= dot * collisionInfo.Normal.X;
            position.Y -= dot * collisionInfo.Normal.Y;
        }

        direction = collisionInfo.Normal.Y > 0f ? CollisionDirection.Up :
                    collisionInfo.Normal.Y < 0f ? CollisionDirection.Down :
                    collisionInfo.Normal.X > 0f ? CollisionDirection.Left : CollisionDirection.Right;
    }
}