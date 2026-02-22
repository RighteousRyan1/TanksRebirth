using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Collision;
using TanksRebirth.Graphics;

namespace TanksRebirth.Internals.Common.Utilities;

public static class MathUtils {
    /// <summary>Scales from the center of the rectangle provided.</summary>
    public static Rectangle ScaleRect(Rectangle rect, float scale) {
        var center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        float w = rect.Width * scale;
        float h = rect.Height * scale;
        return new Rectangle((int)(center.X - w / 2), (int)(center.Y - h / 2), (int)w, (int)h);
    }
    /// <summary>Performs a Spherical Linear Interpolation on a 2D plane, with vector normalization.</summary>
    public static Vector2 Slerp2DNormalize(Vector2 from, Vector2 to, float t) {
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
    /// <summary>Performs a Spherical Linear Interpolation on a 2D plane, without vector normalization.</summary>
    public static Vector2 Slerp2D(Vector2 from, Vector2 to, float t) {
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
    /// <summary>Smooths the direction vector towards the next point in a collection of vectors.</summary>
    public static Vector2 GetSmoothedDirection(Vector2[] points, int index) {
        if (points.Length < 2) return Vector2.UnitX;

        // Average direction between previous and next segments
        Vector2 prev = index > 0 ? points[index] - points[index - 1] : Vector2.Zero;
        Vector2 next = index < points.Length - 1 ? points[index + 1] - points[index] : Vector2.Zero;

        Vector2 combined = prev + next;
        return combined != Vector2.Zero ? Vector2.Normalize(combined) : Vector2.UnitX;
    }
    /// <summary>Splits a given 3D vector into an array with its respective elements, [X, Y, Z].</summary>
    public static float[] ToFloatArray(this Vector3 v) => [v.X, v.Y, v.Z];
    /// <summary>Turns a 3-element array into a Vector3, must be ordered [x, y, z]</summary>
    public static Vector3 ToVector3(this float[] f) => new(f[0], f[1], f[2]);
    public static Vector2 DirectionTo(this Vector2 current, Vector2 destination) => destination - current;
    public static Rectangle ToRect(this AABB aabb) => AbsoluteRectangle(new((int)(aabb.Center.X - aabb.Width), (int)(aabb.Center.Y - aabb.Height), (int)aabb.Width, (int)aabb.Height));
    public static AABB ToAABB(this Rectangle rect) => new(new Vector2(rect.X, rect.Y), new Vector2(rect.X + rect.Width, rect.Y + rect.Width));
    /// <summary>Gets a rectangle that is guaranteed to have positive width and height.</summary>
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
    /// <summary>Converts a direction vector to an angle.</summary>
    public static float ToRotation(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);
    public static float AngleBetween(float a, float b) => (a - b + MathF.PI * 3) % (MathF.PI * 2) - MathF.PI;
    public static float AbsoluteAngleBetween(float a, float b) => MathF.Abs((a - b + MathF.PI * 3) % (MathF.PI * 2) - MathF.PI);
    public static Point ToPoint(this Vector2 vector2) => new((int)vector2.X, (int)vector2.Y);
    /// <summary>
    /// Rotates a point around a center point by a given angle in radians.
    /// </summary>
    /// <param name="spinPoint">The point being rotated.</param>
    /// <param name="radians">The rotation in radians.</param>
    /// <param name="center">The center.</param>
    /// <returns>The rotated vector.</returns>
    public static Vector2 RotatedBy(this Vector2 spinPoint, float radians, Vector2 center = default) {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        Vector2 newPoint = spinPoint - center;
        Vector2 result = center;
        result.X += newPoint.X * cos - newPoint.Y * sin;
        result.Y += newPoint.X * sin + newPoint.Y * cos;
        return result;
    }
    /// <summary>Gets the euler angles from a position to face a look-at position.</summary>
    /// <param name="position">The position to rotate towards <paramref name="lookAt"/>.</param>
    /// <param name="lookAt">The desired look-at position for <paramref name="position"/>.</param>
    /// <returns>The rotation as euler angles.</returns>
    public static EulerAngles GetLookAtEulerAngles(Vector3 position, Vector3 lookAt) {
        Vector3 rotation = Vector3.Zero;

        Vector3 direction = lookAt - position;

        if (direction.LengthSquared() < 0.0001f)
            return rotation.AsEulerAngles();

        // Normalize the direction
        direction.Normalize();

        // XZ rotation (yaw)
        rotation.Z = MathF.Atan2(direction.X, direction.Z);

        // XY rotation (pitch)
        float horizontalLength = MathF.Sqrt(direction.X * direction.X + direction.Z * direction.Z);
        rotation.Y = MathF.Atan2(-direction.Y, horizontalLength);

        // rotation around Z axis (Roll)
        rotation.X = 0.0f;

        return rotation.AsEulerAngles(); // Vector3(roll, pitch, yaw)
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
    public static Vector3 Rotate(this Vector3 vector, Vector3 axis, float angleInDegrees) {
        // Convert degrees to radians
        float radians = angleInDegrees * (MathF.PI / 180f);

        // Create the rotation quaternion
        // Note: Axis must be normalized
        Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), radians);

        // Transform the vector by the rotation
        return Vector3.Transform(vector, rotation);
    }
    public static EulerAngles AsEulerAngles(this Vector3 vector) => new(vector.Z, vector.Y, vector.X);
    public static EulerAngles ToEulerAngles(this Vector3 vector) {
        float roll = MathF.Atan2(vector.Y, vector.Z);
        float pitch = MathF.Atan2(-vector.X, MathF.Sqrt(vector.Y * vector.Y + vector.Z * vector.Z));
        float yaw = 0f;
        return new EulerAngles(roll, pitch, yaw);
    }
    public static float DistanceTo(this Vector2 initial, Vector2 other) => Vector2.Distance(initial, other);
    public static float MaxDistanceValue(Vector2 initial, Vector2 end, float maxDist) {
        var init = initial.DistanceTo(end);
        float actual = 1f - init / maxDist <= 0 ? 0 : 1f - init / maxDist;
        return actual;
    }
    /// <summary>
    /// Creates a tent-shaped triangle wave that maps a value to a 0.0 to 1.0 range, peaking at the midpoint.
    /// </summary>
    /// <remarks>
    /// As the value moves from <paramref name="min"/> to <paramref name="max"/>, the output ramps from 0.0 up to 1.0 at the center, 
    /// then back down to 0.0. This is useful for "in-and-out" animations or proximity effects.
    /// </remarks>
    /// <param name="value">The current input value to evaluate.</param>
    /// <param name="min">The inclusive lower bound of the range.</param>
    /// <param name="max">The inclusive upper bound of the range.</param>
    /// <returns>A value between 0.0f and 1.0f representing the progress through the triangle wave.</returns>
    public static float TriangleWave(float value, float min, float max) {
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
    /// <summary>
    /// Linearly normalizes a value from a specific range into a -1.0 to 1.0 range.
    /// </summary>
    /// <remarks>
    /// This performs a linear remapping where the midpoint of the range becomes 0.0. 
    /// Commonly used for calculating relative offsets, steering inputs, or normalized screen coordinates.
    /// </remarks>
    /// <param name="value">The current input value to evaluate.</param>
    /// <param name="min">The lower bound of the source range (maps to -1.0).</param>
    /// <param name="max">The upper bound of the source range (maps to 1.0).</param>
    /// <returns>A value clamped between -1.0f and 1.0f.</returns>
    public static float InverseTriangleWave(float value, float min, float max) {
        float mid = (max + min) / 2f;
        float halfRange = (max - min) / 2f;

        // Shift the value so the midpoint is 0, then scale to [-1, 1]
        float result = (value - mid) / halfRange;
        return MathHelper.Clamp(result, -1f, 1f);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsWithinRange(this float value, float target, float tolerance) => MathF.Abs(value - target) <= tolerance;
    /// <summary>
    /// Calculates the linear parameter (0.0 to 1.0) that produces the given <paramref name="value"/> within the range [<paramref name="begin"/>, <paramref name="end"/>].
    /// </summary>
    /// <remarks>
    /// This is the mathematical inverse of a Lerp. It determines where a value sits relative to a range. 
    /// For example, if the range is 10 to 20 and the value is 15, this returns 0.5.
    /// </remarks>
    /// <param name="begin">The start of the range (maps to 0.0).</param>
    /// <param name="end">The end of the range (maps to 1.0).</param>
    /// <param name="value">The value to evaluate.</param>
    /// <param name="clamped">If true, the result is restricted to the [0, 1] range.</param>
    /// <returns>A percentage representing the value's position in the range.</returns>
    public static float InverseLerp(float begin, float end, float value, bool clamped = false) {
        float result = (value - begin) / (end - begin);

        return clamped ? MathHelper.Clamp(result, 0f, 1f) : result;
    }

    /// <summary>
    /// Smoothly interpolates between two angles, ensuring the rotation takes the shortest path around a circle.
    /// </summary>
    /// <remarks>
    /// This method handles the wrap-around point of a circle (0 to 2π) so that the interpolation 
    /// doesn't spin the long way around when passing the threshold.
    /// </remarks>
    /// <param name="curAngle">The current angle in radians.</param>
    /// <param name="targetAngle">The desired target angle in radians.</param>
    /// <param name="amount">The interpolation factor (0.0 to 1.0).</param>
    /// <returns>The interpolated angle, wrapped between -π and π.</returns>
    public static float AngleLerp(this float curAngle, float targetAngle, float amount) {
        float delta = targetAngle - curAngle;
        delta = (delta % MathHelper.TwoPi + MathHelper.Pi * 3) % MathHelper.TwoPi - MathHelper.Pi;
        return MathHelper.WrapAngle(curAngle + delta * amount);
    }
    /// <summary>
    /// Increments or decrements a value toward a goal by a fixed step, snapping exactly to the goal if it would be overshot.
    /// </summary>
    /// <remarks>
    /// Unlike a linear interpolation (Lerp), this provides constant-speed movement that guarantees 
    /// hitting the <paramref name="goal"/> precisely.
    /// </remarks>
    /// <param name="value">The current value.</param>
    /// <param name="goal">The target value to reach.</param>
    /// <param name="step">The maximum amount to move the value by.</param>
    /// <returns>The updated value moved toward the goal.</returns>
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
    /// <summary>
    /// Shorthand for Lerp, but is easier to understand.
    /// </summary>
    public static float SoftStep(float value, float goal, float step) => MathHelper.Lerp(value, goal, MathHelper.Clamp(step, 0f, 1f));
    public static Vector3 ExpandZ(this Vector2 vector) => new(vector.X, 0, vector.Y);
    public static Vector3 Expand(this Vector2 vector) => new(vector, 0);
    public static Vector2 Flatten(this Vector3 vector, int excludedAxis = 2) {
        return excludedAxis switch {
            0 => new Vector2(vector.Y, vector.Z),
            1 => new Vector2(vector.X, vector.Z),
            2 => new Vector2(vector.X, vector.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(excludedAxis)),
        };
    }
    public static Vector2 FlattenZ(this Vector3 vector) => vector.Flatten(1);
    public static Rectangle GetScreenRect() => new(0, 0, TankGame.Instance.Window.ClientBounds.Width, TankGame.Instance.Window.ClientBounds.Height);
    public static float Damp(float source, float destination, float smoothing, float dt) => MathHelper.Lerp(source, destination, 1f - MathF.Pow(smoothing, dt));
    public static Vector2 Damp(Vector2 source, Vector2 destination, float smoothing, float dt) => new(Damp(source.X, destination.X, smoothing, dt), Damp(source.Y, destination.Y, smoothing, dt));
    public static Vector3 ToVector3(this Vector4 value) => new Vector3(value.X, value.Y, value.Z) / value.W;
    private static Vector2 BezierDestructive(float amount, Span<Vector2> points) {
        for (int i = points.Length - 1; i > 0; i--)
            for (int j = 0; j < i; j++)
                points[j] = new(MathHelper.Lerp(points[j].X, points[j + 1].X, amount), MathHelper.Lerp(points[j].Y, points[j + 1].Y, amount));
        return points[0];
    }
    /// <summary>
    /// Calculates a point on a Bézier curve of any degree using De Casteljau's algorithm.
    /// </summary>
    /// <remarks>
    /// To minimize Garbage Collection pressure, this method uses <c>stackalloc</c> for curves with 32 points or fewer. 
    /// For larger curves, it utilizes <see cref="GC.AllocateUninitializedArray{T}"/> to reduce allocation overhead.
    /// </remarks>
    /// <param name="progress">The interpolation progress (typically 0.0 to 1.0).</param>
    /// <param name="points">The control points defining the Bézier curve.</param>
    /// <returns>The calculated <see cref="Vector2"/> position along the curve at the given <paramref name="progress"/>.</returns>
    public static Vector2 Bezier(float progress, ReadOnlySpan<Vector2> points) {
        // allow for a small element count before allocating 
        // used to be <= 33. lol
        if (points.Length <= 32) {
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

    public static float WrapTauAngle(this float angle) {
        return angle switch {
            < 0 => angle + (float)(Math.Ceiling(-angle / Math.Tau) * Math.Tau),
            > (float)Math.Tau => angle - (float)((Math.Ceiling(angle / Math.Tau) - 1) * Math.Tau),
            _ => angle
        };
    }
    // in-place de casteljau, just like 2d
    static Vector3 BezierDestructive3D(float t, Span<Vector3> points) {
        for (int i = points.Length - 1; i > 0; i--)
            for (int j = 0; j < i; j++)
                points[j] = Vector3.Lerp(points[j], points[j + 1], t);
        return points[0];
    }
    /// <summary>
    /// A de Casteljau Bézier evaluation for 3D control points.
    /// Accepts ReadOnlySpan to avoid copies at the callsite- internally does one level then runs in-place.
    /// </summary>
    public static Vector3 Bezier3D(float t, ReadOnlySpan<Vector3> points) {
        int n = points.Length;
        if (n == 0) return default;
        if (n == 1) return points[0];
        if (n == 2) return Vector3.Lerp(points[0], points[1], t);

        // small-count fast path

        // same here?
        if (n <= 32) {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static Vector3 ByStackalloc(float tt, ReadOnlySpan<Vector3> src) {
                Span<Vector3> copy = stackalloc Vector3[32]; // enough for first level
                // First level of interpolation
                int last = src.Length - 1;
                for (int j = 0; j < last; j++)
                    copy[j] = Vector3.Lerp(src[j], src[j + 1], tt);

                // finish with in-place destructive pass over the reduced span
                return BezierDestructive3D(tt, copy[..last]);
            }
            return ByStackalloc(t, points);
        }

        // heap path for large control point sets
        int m = n - 1;
        Vector3[] copy = GC.AllocateUninitializedArray<Vector3>(m);
        for (int j = 0; j < m; j++)
            copy[j] = Vector3.Lerp(points[j], points[j + 1], t);

        return BezierDestructive3D(t, copy);
    }
}