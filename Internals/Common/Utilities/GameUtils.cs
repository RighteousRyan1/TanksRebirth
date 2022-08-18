using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;

namespace TanksRebirth.Internals.Common.Utilities
{
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
        public static Vector2 ToResolution(this Vector2 input) => input * (WindowBounds / new Vector2(1920, 1080));
        public static Vector2 ToResolution(this float input) => input * (WindowBounds / new Vector2(1920, 1080));
        public static float ToResolutionX(this float input) => ToResolution(input).X;
        public static float ToResolutionY(this float input) => ToResolution(input).Y;
        public static float ToResolutionX(this int input) => ToResolution(input).X;
        public static float ToResolutionY(this int input) => ToResolution(input).Y;

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
        public static float Distance_WiiTanksUnits(Vector2 position, Vector2 endPoint)
        {
            return Vector2.Distance(position, endPoint) / 0.7f;
        }
        private static Vector2 _oldMousePos;

        public static void SetIf<T>(ref T value, T valSet, bool condition)
            => value = condition ? valSet : value;

        public static T SetIf<T>(T value, T valSet, bool condition)
            => value = condition ? valSet : value;

        public static float NextFloat(this Random random, float min, float max)
        {
            float val = (float)(random.NextDouble() * (max - min) + min);
            return val;
        }

        public static void GetYawPitchTo(Vector3 d, out float yaw, out float pitch)
        {
            yaw = MathF.Atan2(d.X, d.Z);
            pitch = MathF.Asin(-d.Y);
        }

        public static Vector3 ExpandZ(this Vector2 vector)
            => new(vector.X, 0, vector.Y);

        public static Vector3 Expand(this Vector2 vector)
            => new(vector, 0);

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
        public static Vector2 GetMouseVelocity(Vector2 fromOffset = default)
        {
            var pos = MousePosition + fromOffset;
            var diff = pos - _oldMousePos;
            _oldMousePos = pos;

            return diff;
        }
        public static float GetRotationVectorOf(Vector2 initial, Vector2 target) => (target - initial).ToRotation();
        public static float ToRotation(this Vector2 vector)
        {
            return MathF.Atan2(vector.Y, vector.X);
        }
        public static float ToRotation(this Vector3 vector)
        {
            // not working (man)
            return MathF.Atan2(vector.Z, vector.X);
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
        public static Vector2 DistanceVectorTo(this Vector2 start, Vector2 target) => target - start;
        public static Vector2 MousePosition => new(Input.CurrentMouseSnapshot.X, Input.CurrentMouseSnapshot.Y);
        public static int MouseX => (int)MousePosition.X;
        public static int MouseY => (int)MousePosition.Y;
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


                TankGame.SpriteRenderer.DrawString(font, text, pos + new Vector2(xOffset, yOffset), borderColor, rot, origin, scale, default, 0f);
            }
            TankGame.SpriteRenderer.DrawString(font, text, pos, color, rot, origin, scale, default, 0f);
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


                TankGame.SpriteRenderer.Draw(texture, pos + new Vector2(xOffset, yOffset), null, borderColor, rot, origin, scale, default, 0f);
            }
            TankGame.SpriteRenderer.Draw(texture, pos, null, color, rot, origin, scale, default, 0f);
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

        public static T PickRandom<T>(T[] input)
        {
            int rand = new Random().Next(0, input.Length);

            return input[rand];
        }
        public static TEnum PickRandom<TEnum>() where TEnum : struct, Enum
        {
            int rand = new Random().Next(0, Enum.GetNames<TEnum>().Length);

            return (TEnum)(object)rand;
        }
        public static List<T> PickRandom<T>(T[] input, int amount)
        {
            List<T> values = new();
            List<int> chosenTs = new();
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
        public static void DrawStringAtMouse(object text) => TankGame.SpriteRenderer.DrawString(TankGame.TextFont, text.ToString(), MousePosition + new Vector2(25), Color.White, new Vector2(1f), 0f, Vector2.Zero);
        public static bool IsPlaying(this SoundEffectInstance instance) => instance.State == SoundState.Playing;
        public static bool IsPaused(this SoundEffectInstance instance) => instance.State == SoundState.Paused;
        public static bool IsStopped(this SoundEffectInstance instance) => instance.State == SoundState.Stopped;
        public static float Distance(this Vector2 initial, Vector2 other) => Vector2.Distance(initial, other);
        public static float MaxDistanceValue(Vector2 initial, Vector2 end, float maxDist)
        {
            var init = initial.Distance(end);

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
        public static Vector2 ToNormalisedCoordinates(this Vector2 input)
        {
            return new Vector2(input.X / WindowWidth - 0.5f, input.Y / WindowHeight - 0.5f) * 2;
        }
        public static Vector2 Flatten(this Vector3 vector) => new(vector.X, vector.Y);

        public static Vector2 FlattenZ(this Vector3 vector) => new(vector.X, vector.Z);
        public static Vector2 FlattenZ_InvertZ(this Vector3 vector) => new(vector.X, -vector.Z);

        public static bool IsInRangeOf(this float value, float midpoint, float distance)
        {
            return
                value > midpoint - distance / 2
                && value < midpoint + distance / 2;
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
                float num = targetAngle + MathHelper.TwoPi;
                angle = (num - curAngle > curAngle - targetAngle) ? MathHelper.Lerp(curAngle, targetAngle, amount) : MathHelper.Lerp(curAngle, num, amount);
            }
            else
            {
                if (!(targetAngle > curAngle))
                {
                    return curAngle;
                }
                float num = targetAngle - (float)Math.PI * 2f;
                angle = (targetAngle - curAngle > curAngle - num) ? MathHelper.Lerp(curAngle, num, amount) : MathHelper.Lerp(curAngle, targetAngle, amount);
            }
            return MathHelper.WrapAngle(angle);
        }

        public static float AngleLerp_Test(this float curAngle, float targetAngle, float amount)
        {
            float angle = 0f;
            if (targetAngle < curAngle)
            {
                float num = targetAngle + MathHelper.TwoPi;
                angle += (num - curAngle > curAngle - targetAngle) ? targetAngle : num;
            }
            else
            {
                if (!(targetAngle > curAngle))
                {
                    return curAngle;
                }
                float num = targetAngle - MathHelper.TwoPi;
                angle = (targetAngle - curAngle > curAngle - num) ? num : targetAngle;
            }
            return angle;
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(max) > 0)
            {
                return max;
            }
            if (value.CompareTo(min) < 0)
            {
                return min;
            }
            return value;
        }

        public static T Clamp<T>(ref T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(max) > 0)
            {
                return max;
            }
            if (value.CompareTo(min) < 0)
            {
                return min;
            }
            return value;
        }

        public static float RoughStep(ref float value, float goal, float step)
        {
            if (value < goal)
            {
                value += step;

                if (value > goal)
                {
                    return goal;
                }
            }
            else if (value > goal)
            {
                value -= step;

                if (value < goal)
                {
                    return goal;
                }
            }

            return value;
        }

        public static float RoughStep(float value, float goal, float step)
        {
            if (value < goal)
            {
                value += step;

                if (value > goal)
                {
                    return goal;
                }
            }
            else if (value > goal)
            {
                value -= step;

                if (value < goal)
                {
                    return goal;
                }
            }

            return value;
        }

        public static int RoughStep(int value, int goal, int step)
        {
            if (value < goal)
            {
                value += step;

                if (value > goal)
                {
                    return goal;
                }
            }
            else if (value > goal)
            {
                value -= step;

                if (value < goal)
                {
                    return goal;
                }
            }

            return value;
        }

        public static float SoftStep(float value, float goal, float step)
        {
            if (value < goal)
            {
                value += step * (value / goal);

                if (value > goal)
                {
                    return goal;
                }
            }
            else if (value > goal)
            {
                value -= step * (value - goal);

                if (value < goal)
                {
                    return goal;
                }
            }

            return value;
        }
        public static float SoftStep(ref float value, float goal, float step)
        {
            if (value < goal)
            {
                value += step * (value * goal);

                if (value > goal)
                {
                    return goal;
                }
            }
            else if (value > goal)
            {
                value -= step * (value - goal);

                if (value < goal)
                {
                    return goal;
                }
            }

            return value;
        }

        public static float RoughStep_Pi(float radians, float goal, float step)
        {
            if (radians >= MathHelper.Pi && goal <= MathHelper.TwoPi)
            {
                radians += step;

                if (radians > goal)
                    radians = goal;
            }
            if (radians < MathHelper.Pi && goal >= 0)
            {
                radians -= step;

                if (radians < goal)
                    radians = goal;
            }

            return radians;
        }

        public static Vector3 GetWorldPosition(Vector2 screenCoords, float offset = 0f)
        {
            Plane gamePlane = new(Vector3.UnitY, offset);

            var nearPlane = GeometryUtils.ConvertScreenToWorld(new Vector3(screenCoords, 0), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);
            var farPlane = GeometryUtils.ConvertScreenToWorld(new Vector3(screenCoords, 1), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);

            var mouseRay = new Ray(nearPlane, Vector3.Normalize(farPlane - nearPlane));

            float? distance = mouseRay.Intersects(gamePlane);

            if (!distance.HasValue)
                return new();

            return mouseRay.Position + mouseRay.Direction * distance.Value;
        }

        public static Ray GetMouseToWorldRay()
        {
            var nearPlane = GeometryUtils.ConvertScreenToWorld(new Vector3(MousePosition, 0), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);
            var farPlane = GeometryUtils.ConvertScreenToWorld(new Vector3(MousePosition, 1), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);

            return new Ray(nearPlane, Vector3.Normalize(farPlane - nearPlane));
        }

        public static Color ToColor(this Vector3 vec) => new((int)Math.Round(vec.X * 255), (int)Math.Round(vec.Y * 255), (int)Math.Round(vec.Z * 255));
    }
}