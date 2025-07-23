using Microsoft.Xna.Framework;
using TanksRebirth.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Collision;
using System.Runtime.CompilerServices;

namespace TanksRebirth.Internals.Common.Utilities;

public static class MathUtils
{
    public static Vector2 Slerp(Vector2 from, Vector2 to, float t) {
        t = MathHelper.Clamp(t, 0f, 1f);

        from.Normalize();
        to.Normalize();

        float dot = Vector2.Dot(from, to);
        dot = MathHelper.Clamp(dot, -1f, 1f);

        float theta = MathF.Acos(dot) * t;

        Vector2 relative = to - from * dot;
        relative.Normalize();

        Vector2 result = from * MathF.Cos(theta) + relative * MathF.Sin(theta);
        if (float.IsNaN(result.X) || float.IsNaN(result.Y))
            System.Diagnostics.Debugger.Break();

        return result;
    }
    public static Vector2 SlerpWtf(Vector2 from, Vector2 to, float t) {
        t = MathHelper.Clamp(t, 0f, 1f);

        from.Normalize();
        to.Normalize();

        float dot = Vector2.Dot(from, to);
        dot = MathHelper.Clamp(dot, -1f, 1f);

        float theta = MathF.Acos(dot) * t;

        Vector2 relative = to - from * dot;
        // relative.Normalize();

        Vector2 result = from * MathF.Cos(theta) + relative * MathF.Sin(theta);
        if (float.IsNaN(result.X) || float.IsNaN(result.Y))
            System.Diagnostics.Debugger.Break();

        return result;
    }
    public static Vector2 GetSmoothedDirection(Vector2[] points, int index) {
        if (points.Length < 2) return Vector2.UnitX;

        // Average direction between previous and next segments
        Vector2 prev = index > 0 ? points[index] - points[index - 1] : Vector2.Zero;
        Vector2 next = index < points.Length - 1 ? points[index + 1] - points[index] : Vector2.Zero;

        Vector2 combined = prev + next;
        return combined != Vector2.Zero ? Vector2.Normalize(combined) : Vector2.UnitX;
    }
    public static float[] ToFloatArray(this Vector3 v) => [v.X, v.Y, v.Z];
    /// <summary>Turns a 3-element array into a Vector3, must be ordered [x, y, z]</summary>
    public static Vector3 ToVector3(this float[] f) => new(f[0], f[1], f[2]);
    public static Vector2 DirectionTo(this Vector2 current, Vector2 destination) => destination - current;
    public static Rectangle ToRect(this AABB aabb) => AbsoluteRectangle(new((int)(aabb.Center.X - aabb.Width), (int)(aabb.Center.Y - aabb.Height), (int)aabb.Width, (int)aabb.Height));
    public static AABB ToAABB(this Rectangle rect) => new(new Vector2(rect.X, rect.Y), new Vector2(rect.X + rect.Width, rect.Y + rect.Width));
    public static Rectangle AbsoluteRectangle(Rectangle input) {
        var returnRect = input;

        if (input.Width < 0) {
            returnRect.X -= Math.Abs(input.Width);
            returnRect.Width = Math.Abs(input.Width);
        }
        if (input.Height < 0) {
            returnRect.Y -= Math.Abs(input.Height);
            returnRect.Height = Math.Abs(input.Height);
        }
        return returnRect;
    }
    public static float GetRotationVectorOf(Vector2 initial, Vector2 target) => (target - initial).ToRotation();
    public static float ToRotation(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);
    public static float UnitClamp(this float angle) => angle % MathHelper.Tau;
    public static float AngleBetween(float a, float b) {
        return (a - b + MathF.PI * 3) % (MathF.PI * 2) - MathF.PI;
    }
    public static float AbsoluteAngleBetween(float a, float b) {
        return MathF.Abs((a - b + MathF.PI * 3) % (MathF.PI * 2) - MathF.PI);
    }
    public static Point ToPoint(this Vector2 vector2) => new((int)vector2.X, (int)vector2.Y);
    public static Vector2 Rotate(this Vector2 spinPoint, float radians, Vector2 center = default) {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        Vector2 newPoint = spinPoint - center;
        Vector2 result = center;
        result.X += newPoint.X * cos - newPoint.Y * sin;
        result.Y += newPoint.X * sin + newPoint.Y * cos;
        return result;
    }
    public static Vector3 CalculateRotationToFaceCenter(Vector3 particlePosition, Vector3 centerPoint) {
        Vector3 rotation = Vector3.Zero;

        Vector3 direction = centerPoint - particlePosition;

        if (direction.LengthSquared() < 0.0001f)
            return rotation;

        // Normalize the direction
        direction.Normalize();

        // XZ rotation (yaw)
        rotation.Z = MathF.Atan2(direction.X, direction.Z);

        // XY rotation (pitch)
        float horizontalLength = MathF.Sqrt(direction.X * direction.X + direction.Z * direction.Z);
        rotation.Y = MathF.Atan2(-direction.Y, horizontalLength);

        // rotation around Z axis (Roll)
        rotation.X = 0.0f;

        return rotation; // Vector3(roll, pitch, yaw)
    }
    public static Vector3 RotateXZ(this Vector3 spinPoint3d, float radians, Vector3 center3d = default) {
        Vector2 spinPoint = spinPoint3d.FlattenZ();
        Vector2 center = center3d.FlattenZ();

        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        Vector2 newPoint = spinPoint - center;
        Vector2 result = center;
        result.X += newPoint.X * cos - newPoint.Y * sin;
        result.Y += newPoint.X * sin + newPoint.Y * cos;

        Vector3 result3d = result.ExpandZ();
        result3d.Y = spinPoint3d.Y;
        return result3d;
    }
    public static Vector3 RotateXY(this Vector3 spinPoint3d, float radians, Vector3 center3d = default) {
        Vector2 spinPoint = spinPoint3d.Flatten();
        Vector2 center = center3d.Flatten();

        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        Vector2 newPoint = spinPoint - center;
        Vector2 result = center;
        result.X += newPoint.X * cos - newPoint.Y * sin;
        result.Y += newPoint.X * sin + newPoint.Y * cos;

        Vector3 result3d = result.Expand();
        result3d.Z = spinPoint3d.Z;
        return result3d;
    }
    public static float Distance(this Vector2 initial, Vector2 other) => Vector2.Distance(initial, other);
    public static float MaxDistanceValue(Vector2 initial, Vector2 end, float maxDist) {
        var init = initial.Distance(end);

        float actual = 1f - init / maxDist <= 0 ? 0 : 1f - init / maxDist;

        return actual;
    }
    public static float CreateGradientValue(float value, float min, float max) {
        float mid = (max + min) / 2;
        float returnValue;

        if (value > mid) {
            var inverse = 1f - (value - min) / (max - min) * 2;
            returnValue = 1f + inverse;
            return MathHelper.Clamp(returnValue, 0f, 1f);
        }
        returnValue = (value - min) / (max - min) * 2;
        return MathHelper.Clamp(returnValue, 0f, 1f);
    }
    public static float CreateGradientValueWithNegative(float value, float min, float max) {
        float mid = (max + min) / 2f;
        float halfRange = (max - min) / 2f;

        // Shift the value so the midpoint is 0, then scale to [-1, 1]
        float result = (value - mid) / halfRange;
        return MathHelper.Clamp(result, -1f, 1f);
    }
    public static float CreateGradientCapped(float value, float min, float max) {
        if (value > ((min + max) / 2))
            return 1;
        return CreateGradientValue(value, min, max);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWithinRange(this float value, float target, float tolerance) => MathF.Abs(value - target) <= tolerance;
    public static float InverseLerp(float begin, float end, float value, bool clamped = false) {
        if (clamped) {
            if (begin < end) {
                if (value < begin)
                    return 0f;
                if (value > end)
                    return 1f;
            }
            else {
                if (value < end)
                    return 1f;
                if (value > begin)
                    return 0f;
            }
        }
        return (value - begin) / (end - begin);
    }
    public static float ModifiedInverseLerp(float begin, float end, float value, bool clamped = false) => InverseLerp(begin, end, value, clamped) * 2 - 0.5f;
    public static float AngleLerp(this float curAngle, float targetAngle, float amount) {
        float angle;
        if (targetAngle < curAngle) {
            float num = targetAngle + MathHelper.TwoPi;
            angle = (num - curAngle > curAngle - targetAngle) ? MathHelper.Lerp(curAngle, targetAngle, amount) : MathHelper.Lerp(curAngle, num, amount);
        }
        else {
            if (!(targetAngle > curAngle)) {
                return curAngle;
            }
            float num = targetAngle - (float)Math.PI * 2f;
            angle = (targetAngle - curAngle > curAngle - num) ? MathHelper.Lerp(curAngle, num, amount) : MathHelper.Lerp(curAngle, targetAngle, amount);
        }
        return MathHelper.WrapAngle(angle);
    }
    public static float RoughStep(float value, float goal, float step) {
        if (value < goal) {
            value += step;

            if (value > goal) {
                return goal;
            }
        }
        else if (value > goal) {
            value -= step;

            if (value < goal) {
                return goal;
            }
        }

        return value;
    }
    public static float SoftStep(float value, float goal, float step) {
        if (value < goal) {
            value += step * (value / goal);

            if (value > goal) {
                return goal;
            }
        }
        else if (value > goal) {
            value -= step * (value - goal);

            if (value < goal) {
                return goal;
            }
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

    private static Vector2 BezierDestructive(float amount, Span<Vector2> points) {
        for (int i = points.Length - 1; i > 0; i--)
            for (int j = 0; j < i; j++)
                points[j] = new(MathHelper.Lerp(points[j].X, points[j + 1].X, amount), MathHelper.Lerp(points[j].Y, points[j + 1].Y, amount));
        return points[0];
    }
    public static Vector2 Bezier(float progress, ReadOnlySpan<Vector2> points) {
        // allow for a small element count before allocating 
        if (points.Length <= 33) {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static Vector2 ByStackalloc(float progress, ReadOnlySpan<Vector2> points) {
                Span<Vector2> copy = stackalloc Vector2[32];
                for (int j = 0, jj = points.Length - 1; j < jj; j++)
                    copy[j] = new(MathHelper.Lerp(points[j].X, points[j + 1].X, progress), MathHelper.Lerp(points[j].Y, points[j + 1].Y, progress));
                return BezierDestructive(progress, copy[..(points.Length - 1)]);
            }
            return ByStackalloc(progress, points);
        }

        Vector2[] copy = GC.AllocateUninitializedArray<Vector2>(points.Length - 1);
        for (int j = 0, jj = points.Length - 1; j < jj; j++)
            copy[j] = new(MathHelper.Lerp(points[j].X, points[j + 1].X, progress), MathHelper.Lerp(points[j].Y, points[j + 1].Y, progress));
        return BezierDestructive(progress, copy);
    }
    public static int RoundToClosest(this int i, int multiple) {
        if (multiple <= 0 || multiple % 10 != 0)
            throw new ArgumentOutOfRangeException(nameof(multiple), "Must round to a positive multiple of 10");

        return (i + 5 * multiple / 10) / multiple * multiple;
    }
    public static float RoundToClosest(this float g, int multiple) {
        if (multiple <= 0 || multiple % 10 != 0)
            throw new ArgumentOutOfRangeException(nameof(multiple), "Must round to a positive multiple of 10");

        return (g + 5 * multiple / 10) / multiple * multiple;
    }

    public static float WrapTauAngle(this float angle) {
        return angle switch {
            < 0 => angle + (float)(Math.Ceiling(-angle / Math.Tau) * Math.Tau),
            > (float)Math.Tau => angle - (float)((Math.Ceiling(angle / Math.Tau) - 1) * Math.Tau),
            _ => angle
        };
    }
}