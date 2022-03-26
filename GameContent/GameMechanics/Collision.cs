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
            public float Value;
            public Vector2 Normal;
        }
        public static bool IsColliding(Rectangle movingBox, Rectangle collidingBox, Vector2 offset, out CollisionInfo info)
        {
            info = new();
            float x;
            if (offset.X > 0)
                x = (collidingBox.Left - movingBox.Right) / offset.X;
            else if (offset.X < 0)
                x = (collidingBox.Right - movingBox.Left) / offset.X;
            else
                x = -1.0f;

            float verticalT;
            if (offset.Y > 0)
                verticalT = (collidingBox.Top - movingBox.Bottom) / offset.Y;
            else if (offset.Y < 0)
                verticalT = (collidingBox.Bottom - movingBox.Top) / offset.Y;
            else
                verticalT = -1.0f;

            bool isHorizontal = true;
            if (x < 0.0f)
                isHorizontal = false;
            if (x > 1.0f)
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

            if (!isVertical || (x < verticalT && isHorizontal))
            {
                info.Value = x;
                if (offset.X > 0)
                    info.Normal = new(-1.0f, 0.0f);
                else
                    info.Normal = new(1.0f, 0.0f);
            }
            else
            {
                info.Value = verticalT;
                if (offset.Y > 0)
                    info.Normal = new(0.0f, -1.0f);
                else
                    info.Normal = new(0.0f, 1.0f);
            }
            return true;
        }


        public static void HandleCollisionSimple(Rectangle movingBox, Rectangle collidingBox, Vector2 velocity, ref Vector2 position, bool setpos = true)
        {
            var offset = Vector2.Zero;

            CollisionInfo collisionInfo = new();

            collisionInfo.Value = 1f;
            if (IsColliding(movingBox, collidingBox, velocity, out var info))
            {
                if (info.Value < collisionInfo.Value)
                    collisionInfo = info;
            }

            var pos = position;

            pos += offset * collisionInfo.Value;

            if (collisionInfo.Value < 1)
            {
                pos -= Vector2.Dot(velocity, collisionInfo.Normal) * collisionInfo.Normal;
                offset -= Vector2.Dot(offset, collisionInfo.Normal) * collisionInfo.Normal;
                offset *= 1f - collisionInfo.Value;
            }
            else
                return;

            if (setpos)
            {
                position.X = pos.X;
                position.Y = pos.Y;
            }
        }
        public static void HandleCollisionSimple(Rectangle movingBox, Rectangle collidingBox, ref Vector2 velocity, ref Vector2 position, out CollisionDirection direction, bool setpos = true)
        {
            direction = CollisionDirection.None;
            var offset = velocity;

            CollisionInfo collisionInfo = new();

            collisionInfo.Value = 1f;
            if (IsColliding(movingBox, collidingBox, velocity, out var info))
            {
                if (info.Value < collisionInfo.Value)
                    collisionInfo = info;
            }

            var pos = position;

            if (collisionInfo.Value < 1)
            {
                pos -= Vector2.Dot(velocity, collisionInfo.Normal) * collisionInfo.Normal;
                offset -= Vector2.Dot(offset, collisionInfo.Normal) * collisionInfo.Normal;
                offset *= 1f - collisionInfo.Value;
            }
            else
                return;

            pos += offset * collisionInfo.Value;

            if (setpos)
            {
                position.X = pos.X;
                position.Y = pos.Y;
            }

            if (collisionInfo.Normal.Y > 0)
            {
                // ceil

                direction = CollisionDirection.Up;
            }
            if (collisionInfo.Normal.Y < 0)
            {
                // floor

                direction = CollisionDirection.Down;
            }
            if (collisionInfo.Normal.X > 0)
            {
                // wall left

                direction = CollisionDirection.Left;
            }
            if (collisionInfo.Normal.X < 0)
            {
                // wall right

                direction = CollisionDirection.Right;
            }
        }

        public static void HandleCollisionSimple_ForBlocks(Rectangle movingBox, Vector2 velocity, ref Vector2 position, out CollisionDirection direction, out Block.BlockType type, bool setpos = true, Func<Block, bool> exclude = null)
        {
            type = (Block.BlockType)255;
            direction = CollisionDirection.None;
            var offset = velocity;

            CollisionInfo collisionInfo = new();

            collisionInfo.Value = 1f;

            foreach (var cube in Block.AllBlocks)
            {
                if (cube is not null)
                {
                    if (exclude is null)
                    {
                        if (IsColliding(movingBox, cube.collider2d, velocity, out var info))
                        {
                            if (info.Value < collisionInfo.Value)
                                collisionInfo = info;
                            type = cube.Type;
                        }
                    }
                    else
                    {
                        if (exclude.Invoke(cube))
                        {
                            if (IsColliding(movingBox, cube.collider2d, velocity, out var info))
                            {
                                if (info.Value < collisionInfo.Value)
                                    collisionInfo = info;
                                type = cube.Type;
                            }
                        }
                    }
                }
            }

            var pos = position;

            pos += offset * collisionInfo.Value;

            if (collisionInfo.Value < 1)
            {
                pos -= Vector2.Dot(velocity, collisionInfo.Normal) * collisionInfo.Normal;
                offset -= Vector2.Dot(offset, collisionInfo.Normal) * collisionInfo.Normal;
                offset *= 1f - collisionInfo.Value;
            }

            if (setpos)
            {
                position.X = pos.X;
                position.Y = pos.Y;
            }

            if (collisionInfo.Normal.Y > 0)
            {
                // ceil

                direction = CollisionDirection.Up;
            }
            if (collisionInfo.Normal.Y < 0)
            {
                // floor

                direction = CollisionDirection.Down;
            }
            if (collisionInfo.Normal.X > 0)
            {
                // wall left

                direction = CollisionDirection.Left;
            }
            if (collisionInfo.Normal.X < 0)
            {
                // wall right

                direction = CollisionDirection.Right;
            }
        }
    }
}
public enum CollisionDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}
