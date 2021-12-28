using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent.GameMechanics
{
    public static class Collision
    {
        public struct CollisionInfo
        {
            public float tValue;
            public Vector2 normal;
        }
        public static bool IsColliding(Rectangle movingBox, Rectangle collidingBox, Vector2 offset, out CollisionInfo info)
        {
            info = new();
            float horizontalT;
            if (offset.X > 0)
                horizontalT = (float)(collidingBox.Left - movingBox.Right) / (float)(offset.X);
            else if (offset.X < 0)
                horizontalT = (float)(collidingBox.Right - movingBox.Left) / (float)(offset.X);
            else
                horizontalT = -1.0f;

            float verticalT;
            if (offset.Y > 0)
                verticalT = (float)(collidingBox.Top - movingBox.Bottom) / (float)(offset.Y);
            else if (offset.Y < 0)
                verticalT = (float)(collidingBox.Bottom - movingBox.Top) / (float)(offset.Y);
            else
                verticalT = -1.0f;

            bool isHorizontal = true;
            if (horizontalT < 0.0f)
                isHorizontal = false;
            if (horizontalT > 1.0f)
                isHorizontal = false;
            if (collidingBox.Top >= movingBox.Bottom || collidingBox.Bottom <= movingBox.Top)
                isHorizontal = false;

            bool isVertical = true;
            if (verticalT < 0.0f)
                isVertical = false;
            if (verticalT > 1.0f)
                isVertical = false;
            if (collidingBox.Left >= movingBox.Right || collidingBox.Right <= movingBox.Left)
                isVertical = false;

            if (!isHorizontal && !isVertical)
                return false;

            if (!isVertical || (horizontalT < verticalT && isHorizontal))
            {
                info.tValue = horizontalT;
                if (offset.X > 0)
                    info.normal = new(-1.0f, 0.0f);
                else
                    info.normal = new(1.0f, 0.0f);
            }
            else
            {
                info.tValue = verticalT;
                if (offset.Y > 0)
                    info.normal = new(0.0f, -1.0f);
                else
                    info.normal = new(0.0f, 1.0f);
            }
            return true;
        }


        public static void HandleCollisionSimple(Rectangle movingBox, Rectangle collidingBox, ref Vector2 velocity, ref Vector3 position, bool setpos = true)
        {
            var offset = velocity;

            CollisionInfo collisionInfo = new();

            collisionInfo.tValue = 1f;
            if (IsColliding(movingBox, collidingBox, velocity, out var info))
            {
                if (info.tValue < collisionInfo.tValue)
                    collisionInfo = info;
            }

            var pos = position.FlattenZ();

            pos += offset * collisionInfo.tValue;

            if (collisionInfo.tValue < 1)
            {
                pos -= Vector2.Dot(velocity, collisionInfo.normal) * collisionInfo.normal;
                offset -= Vector2.Dot(offset, collisionInfo.normal) * collisionInfo.normal;
                offset *= 1f - collisionInfo.tValue;
            }
            else
                return;

            if (setpos)
            {
                position.X = pos.X;
                position.Z = pos.Y;
            }
        }
        public static void HandleCollisionSimple(Rectangle movingBox, Rectangle collidingBox, ref Vector2 velocity, ref Vector3 position, out CollisionDirection direction, bool setpos = true)
        {
            direction = CollisionDirection.None;
            var offset = velocity;

            CollisionInfo collisionInfo = new();

            collisionInfo.tValue = 1f;
            if (IsColliding(movingBox, collidingBox, velocity, out var info))
            {
                if (info.tValue < collisionInfo.tValue)
                    collisionInfo = info;
            }

            var pos = position.FlattenZ();

            pos += offset * collisionInfo.tValue;

            if (collisionInfo.tValue < 1)
            {
                pos -= Vector2.Dot(velocity, collisionInfo.normal) * collisionInfo.normal;
                offset -= Vector2.Dot(offset, collisionInfo.normal) * collisionInfo.normal;
                offset *= 1f - collisionInfo.tValue;
            }
            else
                return;

            if (setpos)
            {
                position.X = pos.X;
                position.Z = pos.Y;
            }

            if (collisionInfo.normal.Y > 0)
            {
                // ceil

                direction = CollisionDirection.Up;
            }
            if (velocity.Y == 0f)
            {
                // floor

                direction = CollisionDirection.Down;
            }
            if (collisionInfo.normal.X > 0)
            {
                // wall left

                direction = CollisionDirection.Left;
            }
            if (collisionInfo.normal.X < 0)
            {
                // wall right

                direction = CollisionDirection.Right;
            }
        }

        public static void HandleCollisionSimple_ForBlocks(Rectangle movingBox, ref Vector2 velocity, ref Vector3 position, out CollisionDirection direction, bool setpos = true)
        {
            direction = CollisionDirection.None;
            var offset = velocity;

            CollisionInfo collisionInfo = new();

            collisionInfo.tValue = 1f;

            foreach (var b in Cube.cubes.Where(c => c is not null))
            {
                if (IsColliding(movingBox, b.collider2d, velocity, out var info))
                {
                    if (info.tValue < collisionInfo.tValue)
                        collisionInfo = info;
                }
            }

            var pos = position.FlattenZ();

            pos += offset * collisionInfo.tValue;

            if (collisionInfo.tValue < 1)
            {
                pos -= Vector2.Dot(velocity, collisionInfo.normal) * collisionInfo.normal;
                offset -= Vector2.Dot(offset, collisionInfo.normal) * collisionInfo.normal;
                offset *= 1f - collisionInfo.tValue;
            }
            else
                return;

            if (setpos)
            {
                position.X = pos.X;
                position.Z = pos.Y;
            }

            if (collisionInfo.normal.Y > 0)
            {
                // ceil

                direction = CollisionDirection.Up;
            }
            if (velocity.Y == 0f)
            {
                // floor

                direction = CollisionDirection.Down;
            }
            if (collisionInfo.normal.X > 0)
            {
                // wall left

                direction = CollisionDirection.Left;
            }
            if (collisionInfo.normal.X < 0)
            {
                // wall right

                direction = CollisionDirection.Right;
            }
        }

        public enum CollisionDirection {
            None,
            Up,
            Down,
            Left,
            Right
        }
    }
}
