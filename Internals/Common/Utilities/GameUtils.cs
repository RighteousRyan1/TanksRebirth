using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.Internals.Common.Utilities
{
    public static class GameUtils
    {
        private static Vector2 _oldMousePos;

        /// <summary>
        /// <paramref name="from"/> means it gets the direction vector from <paramref name="vec"/>, otherwise to <paramref name="other"/>.
        /// </summary>
        /// <param name="from"></param>
        /// <returns>The direction vector to or from.</returns>
        public static Vector2 DirectionOf(this Vector2 vec, Vector2 other, bool from = false)
        {
            return from switch
            {
                true => vec - other,
                _ => other - vec,
            };
        }
        public static Vector3 DirectionOf(this Vector3 vec, Vector3 other, bool from = false)
        {
            return from switch
            {
                true => vec - other,
                _ => other - vec,
            };
        }
        public static Vector2 GetMouseVelocity()
        {
            var pos = MousePosition;
            var diff = pos - _oldMousePos;
            _oldMousePos = pos;

            return diff;
        }
        public static float GetRotationVectorOf(Vector2 initial, Vector2 target) => (target - initial).ToRotation();
        public static float ToRotation(this Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }
        public static float ToRotation(this Vector3 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }
        public static Vector2 RotatedByRadians(this Vector2 spinPoint, double radians, Vector2 center = default)
        {
            float cosRotation = (float)Math.Cos(radians);
            float sinRotation = (float)Math.Sin(radians);
            Vector2 newPoint = spinPoint - center;
            Vector2 result = center;
            result.X += newPoint.X * cosRotation - newPoint.Y * sinRotation;
            result.Y += newPoint.X * sinRotation + newPoint.Y * cosRotation;
            return result;
        }
        public static Vector3 ToVector3(this Vector2 twoD) => new(twoD.X, twoD.Y, 0f);
        public static Vector2 DistanceFrom(this Vector2 start, Vector2 target) => target - start;
        public static Vector2 MousePosition => new(Input.CurrentMouseSnapshot.X, Input.CurrentMouseSnapshot.Y);
        public static Vector2 MousePosition_ToBlockCoordinates => MousePosition / 16;
        public static Vector2 ToBlockCoordinates(this Vector2 vector) => vector / 16;
        public static Vector2 ToWorldCoordinates(this Vector2 vector) => vector * 16;
        public static int MouseX => (int)MousePosition.X;
        public static int MouseY => (int)MousePosition.Y;
        public static int MouseX_TBC => (int)MousePosition.X / 16;
        public static int MouseY_TBC => (int)MousePosition.Y / 16;
        public static int WindowWidth => TankGame.Instance.Window.ClientBounds.Width;
        public static int WindowHeight => TankGame.Instance.Window.ClientBounds.Height;
        public static Vector2 WindowBounds => new(WindowWidth, WindowHeight);
        public static Vector2 WindowCenter => WindowBounds / 2;
        public static Vector2 WindowBottom => new(WindowWidth / 2, WindowHeight);
        public static Vector2 WindowTop => new(WindowWidth / 2, 0);
        public static Vector2 WindowTopRight => new(WindowWidth, 0);
        public static Vector2 WindowBottomRight => new(WindowWidth, WindowHeight);
        public static Vector2 WindowTopLeft => new(0, 0);
        public static Vector2 WindowBottomLeft => new(0, WindowHeight);
        public static Vector2 WindowLeft => new(0, WindowHeight / 2);
        public static Vector2 WindowRight => new(WindowWidth, WindowHeight / 2);
        public static Vector2 Size(this Texture2D tex) => new(tex.Width, tex.Height);
        public static bool MouseOnScreen => MousePosition.X >= 0 && MousePosition.X <= WindowWidth && MousePosition.Y >= 0 && MousePosition.Y < WindowHeight;
        public static bool MouseOnScreenProtected => MousePosition.X > 16 && MousePosition.X < WindowWidth - 16 && MousePosition.Y > 16 && MousePosition.Y < WindowHeight - 16;
        public static void DrawTextWithBorder(SpriteFont font, string text, Vector2 pos, Color color, Color borderColor, float rot, float scale, int borderSize)
        {
            var origin = font.MeasureString(text) / 2;
            int yOffset = 0;
            int xOffset = 0;
            for (int i = 0; i < borderSize + 3; i++)
            {
                if (i == 0)
                    xOffset = -borderSize;
                if (i == 1)
                    xOffset = borderSize;
                if (i == 2)
                    yOffset = -borderSize;
                if (i == 3)
                    yOffset = borderSize;


                TankGame.spriteBatch.DrawString(font, text, pos + new Vector2(xOffset, yOffset), borderColor, rot, origin, scale, default, 0f);
            }
            TankGame.spriteBatch.DrawString(font, text, pos, color, rot, origin, scale, default, 0f);
        }
        public static void DrawTextureWithBorder(Texture2D texture, Vector2 pos, Color color, Color borderColor, float rot, float scale, int borderSize)
        {
            var origin = texture.Size() / 2;
            int yOffset = 0;
            int xOffset = 0;
            for (int i = 0; i < borderSize + 3; i++)
            {
                if (i == 0)
                    xOffset = -borderSize;
                if (i == 1)
                    xOffset = borderSize;
                if (i == 2)
                    yOffset = -borderSize;
                if (i == 3)
                    yOffset = borderSize;


                TankGame.spriteBatch.Draw(texture, pos + new Vector2(xOffset, yOffset), null, borderColor, rot, origin, scale, default, 0f);
            }
            TankGame.spriteBatch.Draw(texture, pos, null, color, rot, origin, scale, default, 0f);
        }
        public static T[,] Resize2D<T>(T[,] original, int rows, int cols)
        {
            var newArray = new T[rows, cols];
            int minRows = Math.Min(rows, original.GetLength(0));
            int minCols = Math.Min(cols, original.GetLength(1));
            for (int i = 0; i < minRows; i++)
                for (int j = 0; j < minCols; j++)
                    newArray[i, j] = original[i, j];
            return newArray;
        }
        public static Point ToPoint(this Vector2 vector2) => new((int)vector2.X, (int)vector2.Y);
        public static bool WindowActive => TankGame.Instance.IsActive;

        private static List<int> chosenTs = new();
        public static T PickRandom<T>(T[] input)
        {
            int rand = new Random().Next(0, input.Length);

            return input[rand];
        }
        public static List<T> PickRandom<T>(T[] input, int amount)
        {
            List<T> values = new();
            for (int i = 0; i < amount; i++)
            {
            ReRoll:
                int rand = new Random().Next(0, input.Length);

                if (!chosenTs.Contains(rand))
                {
                    chosenTs.Add(rand);
                    values.Add(input[rand]);
                }
                else
                    goto ReRoll;
            }
            chosenTs.Clear();
            return values;
        }
        public static void DrawStringAtMouse(object text) => TankGame.spriteBatch.DrawString(TankGame.Fonts.Default, text.ToString(), MousePosition + new Vector2(25), Color.White, 0f, Vector2.Zero, 1f, default, 0f);
        public static bool IsPlaying(this SoundEffectInstance instance) => instance.State == SoundState.Playing;
        public static bool IsPaused(this SoundEffectInstance instance) => instance.State == SoundState.Paused;
        public static bool IsStopped(this SoundEffectInstance instance) => instance.State == SoundState.Stopped;
        public static float QuickDistance(this Vector2 initial, Vector2 end) => Vector2.Distance(initial, end);
        public static float MaxDistanceValue(Vector2 initial, Vector2 end, float maxDist)
        {
            var init = initial.QuickDistance(end);

            float actual = 1f - init / maxDist <= 0 ? 0 : 1f - init / maxDist;

            return actual;
        }
        public static float CreateGradientValue(float value, float min, float max)
        {
            float mid = (max + min) / 2;
            float returnValue;

            if (value > mid)
            {
                var thing = 1f - (value - min) / (max - min) * 2;
                returnValue = 1f + thing;
                return MathHelper.Clamp(returnValue, 0f, 1f);
            }
            returnValue = (value - min) / (max - min) * 2;
            return MathHelper.Clamp(returnValue, 0f, 1f);
        }
        public static float InverseLerp(float begin, float end, float value, bool clamped = false)
        {
            if (clamped)
            {
                if (begin < end)
                {
                    if (value < begin)
                        return 0f;
                    if (value > end)
                        return 1f;
                }
                else
                {
                    if (value < end)
                        return 1f;
                    if (value > begin)
                        return 0f;
                }
            }
            return (value - begin) / (end - begin);
        }
        public static float ModifiedInverseLerp(float begin, float end, float value, bool clamped = false)
        {
            return InverseLerp(begin, end, value, clamped) * 2 - 1;
        }

        public static int[] GetFrames(int frames, int textureHeight)
        {
            // this displays frame num, not actual
            int frameCount = textureHeight / frames;
            return new int[frameCount];
        }
        public static Vector2 Flatten(this Vector3 vector) => new Vector2(vector.X, vector.Y);

        public static bool InDistanceOf(this float value, float midpoint, float distance)
        {
            return
                value > midpoint - distance
                && value < midpoint + distance;
        }
        // val: 5
        // mdpt: 15
        // dist: 10

        // check1: 5 > 15 - 10
        // check2: 5 < 15 + 10

        public static float AngleLerp(this float curAngle, float targetAngle, float amount)
        {
            float angle;
            if (targetAngle < curAngle)
            {
                float num = targetAngle + (float)Math.PI * 2f;
                angle = ((num - curAngle > curAngle - targetAngle) ? MathHelper.Lerp(curAngle, targetAngle, amount) : MathHelper.Lerp(curAngle, num, amount));
            }
            else
            {
                if (!(targetAngle > curAngle))
                {
                    return curAngle;
                }
                float num = targetAngle - (float)Math.PI * 2f;
                angle = ((targetAngle - curAngle > curAngle - num) ? MathHelper.Lerp(curAngle, num, amount) : MathHelper.Lerp(curAngle, targetAngle, amount));
            }
            return MathHelper.WrapAngle(angle);
        }
    }
}