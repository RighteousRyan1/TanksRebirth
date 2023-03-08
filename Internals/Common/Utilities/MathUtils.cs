using Microsoft.Xna.Framework;
using TanksRebirth.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Collision;

namespace TanksRebirth.Internals.Common.Utilities;

public static class MathUtils
{
    public static Vector2 DirectionOf(this Vector2 vec, Vector2 other, bool from = false) => from switch {
            true => vec - other,
            _ => other - vec,
        };
    public static Rectangle ToRect(this AABB aabb) => AbsoluteRectangle(new((int)(aabb.Center.X - aabb.Width), (int)(aabb.Center.Y - aabb.Height), (int)aabb.Width, (int)aabb.Height));
    public static AABB ToAABB(this Rectangle rect) => new(new Vector2(rect.X, rect.Y), new Vector2(rect.X + rect.Width, rect.Y + rect.Width));
    public static Rectangle AbsoluteRectangle(Rectangle input) {
        var returnRect = input;

        if (input.Width < 0) {
            returnRect.X -= Math.Abs(input.Width);
            returnRect.Width = Math.Abs(input.Width);
        }

        if (input.Height > 0) return returnRect;

        returnRect.Y -= Math.Abs(input.Height);
        returnRect.Height = Math.Abs(input.Height);
        return returnRect;
    }
    public static float GetRotationVectorOf(Vector2 initial, Vector2 target) => (target - initial).ToRotation();
    public static float ToRotation(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);
    public static Point ToPoint(this Vector2 vector2) => new((int)vector2.X, (int)vector2.Y);
    public static Vector2 RotatedByRadians(this Vector2 spinPoint, double radians, Vector2 center = default) {
        var cos = (float)Math.Cos(radians);
        var sin = (float)Math.Sin(radians);
        var newPoint = spinPoint - center;
        var result = center;
        result.X += newPoint.X * cos - newPoint.Y * sin;
        result.Y += newPoint.X * sin + newPoint.Y * cos;
        return result;
    }
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
            var inverse = 1f - (value - min) / (max - min) * 2;
            returnValue = 1f + inverse;
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
    public static float ModifiedInverseLerp(float begin, float end, float value, bool clamped = false) => InverseLerp(begin, end, value, clamped) * 2 - 1;
    public static float AngleLerp(this float curAngle, float targetAngle, float amount) {
        float number;
        float angle;
        if (targetAngle < curAngle)
        {
            number = targetAngle + MathHelper.TwoPi;
            angle = (number - curAngle > curAngle - targetAngle) ? MathHelper.Lerp(curAngle, targetAngle, amount) : MathHelper.Lerp(curAngle, number, amount);
            return MathHelper.WrapAngle(angle);
        }
        
        number = targetAngle - MathHelper.TwoPi;
        angle = (targetAngle - curAngle > curAngle - number) ? MathHelper.Lerp(curAngle, number, amount) : MathHelper.Lerp(curAngle, targetAngle, amount);

        return MathHelper.WrapAngle(angle);
    }
    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T> {
        return value.CompareTo(max) > 0 ? max : value.CompareTo(min) < 0 ? min : value;
    }
    public static T Clamp<T>(ref T value, T min, T max) where T : IComparable<T> {
        return value.CompareTo(max) > 0 ? max : value.CompareTo(min) < 0 ? min : value;
    }
    public static float RoughStep(float value, float goal, float step) {
        if (value < goal) {
            value += step;
            if (value > goal)
                return goal;
        }
        else if (value > goal) {
            value -= step;
            if (value < goal)
                return goal;
        }

        return value;
    }
    public static int RoughStep(int value, int goal, int step)
    {
        if (value < goal) {
            value += step;
            if (value > goal)
                return goal;
        }
        else if (value > goal) {
            value -= step;
            if (value < goal)
                return goal;
        }

        return value;
    }
    public static float SoftStep(float value, float goal, float step)
    {
        if (value < goal) {
            value += step * (value / goal);
            if (value > goal)
                return goal;
        }
        else if (value > goal) {
            value -= step * (value - goal);
            if (value < goal)
                return goal;
        }

        return value;
    }
    public static Vector3 ExpandZ(this Vector2 vector)
        => new(vector.X, 0, vector.Y);
    public static Vector3 Expand(this Vector2 vector)
        => new(vector, 0);
    public static Vector2 Flatten(this Vector3 vector) => new(vector.X, vector.Y);
    public static Vector2 FlattenZ(this Vector3 vector) => new(vector.X, vector.Z);
    public static Vector2 FlattenZ_InvertZ(this Vector3 vector) => new(vector.X, -vector.Z);
    public static Rectangle GetScreenRect() => new(0, 0, TankGame.Instance.Window.ClientBounds.Width, TankGame.Instance.Window.ClientBounds.Height);
    public static float Damp(float source, float destination, float smoothing, float dt) => MathHelper.Lerp(source, destination, 1f - MathF.Pow(smoothing, dt));
    public static Vector2 Damp(Vector2 source, Vector2 destination, float smoothing, float dt) => new(Damp(source.X, destination.X, smoothing, dt), Damp(source.Y, destination.Y, smoothing, dt));
    public static Vector4 RoundV4(Vector4 value) => new Vector4(
            MathF.Round(value.X),
            MathF.Round(value.Y),
            MathF.Round(value.Z),
            MathF.Round(value.W));

    public static Vector3 ToVector3(this Vector4 value) => new Vector3(value.X, value.Y, value.Z) / value.W;
}
