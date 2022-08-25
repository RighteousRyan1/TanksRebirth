using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.GameMechanics
{
    public static class Collision
    {
        public struct CollisionInfo {
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
            direction = CollisionDirection.Other;
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
            else if (collisionInfo.Normal.Y < 0)
            {
                // floor

                direction = CollisionDirection.Down;
            }
            else if (collisionInfo.Normal.X > 0)
            {
                // wall left

                direction = CollisionDirection.Left;
            }
            else if (collisionInfo.Normal.X < 0)
            {
                // wall right

                direction = CollisionDirection.Right;
            }
        }

        public static void HandleCollisionSimple_ForBlocks(Rectangle movingBox, Vector2 velocity, ref Vector2 position, out CollisionDirection direction, out Block block, out bool cornerCollision, bool setpos = true, Func<Block, bool> exclude = null)
        {
            cornerCollision = false;
            block = null;
            direction = CollisionDirection.None;
            var offset = velocity;

            CollisionInfo collisionInfo = new();

            collisionInfo.Value = 1f;

            foreach (var cube in Block.AllBlocks)
            {
                if (cube is not null)
                {
                    if (movingBox.Intersects(cube.Hitbox))
                    {
                        if (exclude is not null)
                        {
                            if (exclude.Invoke(cube))
                                cornerCollision = true;
                            break;
                        }
                        else
                        {
                            cornerCollision = true;
                            break;
                        }
                    }
                    if (exclude is null)
                    {
                        if (IsColliding(movingBox, cube.Hitbox, velocity, out var info))
                        {
                            if (info.Value < collisionInfo.Value)
                                collisionInfo = info;
                            block = cube;
                        }
                    }
                    else
                    {
                        if (exclude.Invoke(cube))
                        {
                            if (IsColliding(movingBox, cube.Hitbox, velocity, out var info))
                            {
                                if (info.Value < collisionInfo.Value)
                                    collisionInfo = info;
                                block = cube;
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
            else if (collisionInfo.Normal.Y < 0)
            {
                // floor

                direction = CollisionDirection.Down;
            }
            else if (collisionInfo.Normal.X > 0)
            {
                // wall left

                direction = CollisionDirection.Left;
            }
            else if (collisionInfo.Normal.X < 0)
            {
                // wall right

                direction = CollisionDirection.Right;
            }
            /*else if (collisionInfo.Normal == Vector2.Zero)
            {
                direction = CollisionDirection.Other;
               
            }*/ // huh?
        }

        public static bool DoRaycast(Vector2 start, Vector2 destination, float forgiveness, bool draw = false)
        {
            const int PATH_UNIT_LENGTH = 4;
            const int MAX_DIST = 1000;

            // 20, 30

            var pathDir = GameUtils.DirectionOf(start, destination).ToRotation();

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = start + Vector2.Zero.RotatedByRadians(pathDir);

            pathDir *= PATH_UNIT_LENGTH;

            for (int i = 0; i < MAX_DIST; i++)
            {
                var dummyPos = Vector2.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    return false;
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    return false;
                }

                var pathHitbox = new Rectangle((int)pathPos.X, (int)pathPos.Y, 1, 1);

                // Why is velocity passed by reference here lol
                HandleCollisionSimple_ForBlocks(pathHitbox, start, ref dummyPos, out var dir, out var block, out bool corner, false);

                switch (dir)
                {
                    case CollisionDirection.Up:
                    case CollisionDirection.Down:
                        return false;
                    case CollisionDirection.Left:
                    case CollisionDirection.Right:
                        return false;
                }

                if (Vector2.Distance(pathPos, destination) < forgiveness)
                    return true;

                pathPos += Vector2.Normalize(GameUtils.DirectionOf(start, destination));

                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
                }

            }
            return true;
        }
    }
}
public enum CollisionDirection
{
    None,
    Other,
    Up,
    Down,
    Left,
    Right
}
