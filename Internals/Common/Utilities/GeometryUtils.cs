using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TanksRebirth.Internals.Common.Utilities;

public static class GeometryUtils
{

    public static T[] Shift<T>(ref T[] array, int index, int amount)
    {
        // extend the array by amount if we're out of bounds, then shift every element past index by amount
        if (index + amount > array.Length)
        {
            var newArray = new T[index + amount];
            array.CopyTo(newArray, 0);
            array = newArray;
        }
        for (int i = index; i < array.Length; i++)
        {
            if (i >= index)
                array[i] = array[i + amount];
        }
        return array;
    }
    // sigh no work

    public static Vector2 PredictFuturePosition(Vector2 source, Vector2 velocity, float time)
    {
        return source + velocity * time;
    }
    /// <summary>
    /// Create a ray on a 2D plane either covering the X and Y axes of a plane or the X and Z axes of a plane.
    /// </summary>
    /// <param name="origin">The origin of this <see cref="Ray"/>.</param>
    /// <param name="destination">The place that will be the termination of this <see cref="Ray"/>.</param>
    /// <param name="zAxis">Whether or not this <see cref="Ray"/> will go along the Y or Z axis from the X axis.</param>
    /// <returns>The ray created.</returns>
    public static Ray CreateRayFrom2D(Vector2 origin, Vector2 destination, float excludedAxisOffset = 0f, bool zAxis = true)
    {
        Ray ray;

        if (zAxis)
            ray = new Ray(new Vector3(origin.X, excludedAxisOffset, origin.Y), new Vector3(destination.X, 0, destination.Y));
        else
            ray = new Ray(new Vector3(origin.X, origin.Y, excludedAxisOffset), new Vector3(destination.X, destination.Y, 0));

        return ray;
    }
    /// <summary>
    /// Create a ray on a 2D plane either covering the X and Y axes of a plane or the X and Z axes of a plane. This creates on the 3D plane with a 3D vector and moves along a 2D axis.
    /// </summary>
    /// <param name="origin">The origin of this <see cref="Ray"/>.</param>
    /// <param name="destination">The place that will be the termination of this <see cref="Ray"/>.</param>
    /// <param name="zAxis">Whether or not this <see cref="Ray"/> will go along the Y or Z axis from the X axis.</param>
    /// <returns>The ray created.</returns>
    public static Ray CreateRayFrom2D(Vector3 origin, Vector2 destination, float excludedAxisOffset = 0f, bool zAxis = true)
    {
        Ray ray;

        if (zAxis)
            ray = new Ray(origin + new Vector3(0, excludedAxisOffset, 0), new Vector3(destination.X, 0, destination.Y));
        else
            ray = new Ray(origin + new Vector3(0, 0, excludedAxisOffset), new Vector3(destination.X, destination.Y, 0));

        return ray;
    }

    public static Ray Reflect(Ray ray, float? distanceAlongRay)
    {
        if (!distanceAlongRay.HasValue)
            throw new NullReferenceException("The distance along the ray was null.");

        var distPos = ray.Position * Vector3.Normalize(ray.Direction) * distanceAlongRay.Value;

        var reflected = Vector3.Reflect(ray.Direction, distPos);

        return new(distPos, reflected);
    }

    public static Rectangle CreateRectangleFromCenter(int x, int y, int width, int height)
    {
        return new Rectangle(x - width / 2, y - height / 2, width, height);
    }

    public static Ray Flatten(this Ray ray, bool zAxis = true)
    {
        Ray usedRay;

        if (zAxis)
            usedRay = new Ray(new Vector3(ray.Position.X, 0, ray.Position.Y), new Vector3(ray.Direction.X, 0, ray.Direction.Y));
        else
            usedRay = new Ray(new Vector3(ray.Position.X, ray.Position.Y, 0), new Vector3(ray.Direction.X, ray.Direction.Y, 0));

        return usedRay;
    }

    public static float GetPiRandom()
    {
        var seed = new Random().Next(0, 4);

        return seed switch 
        { 
            0 => 0,
            1 => MathHelper.PiOver2,
            2 => MathHelper.Pi,
            3 => MathHelper.Pi + MathHelper.PiOver2,
            _ => 0
        };
    }

    public static EulerAngles AsEulerAngles(this Quaternion quaternion)
    {
        EulerAngles angles = new();

        // roll
        float wxyz = 2 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
        float xySq = 1 - 2 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
        angles.Roll = MathF.Atan2(wxyz, xySq);
        // pitch
        float diffSin = 2 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
        if (MathF.Abs(diffSin) >= 1)
            angles.Pitch = MathF.CopySign(MathHelper.PiOver2, diffSin); // if sinp > 90 degrees, compress it to 90.
        else
            angles.Pitch = MathF.Asin(diffSin);

        // yaw
        float wzxy = 2 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
        float yzSq = 1 - 2 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
        angles.Yaw = MathF.Atan2(wzxy, yzSq);

        return angles;
    }

    public static float GetQuarterRotation(sbyte rot)
    {
        return MathHelper.PiOver2 * rot;
    }

    public static void Add(ref Vector3 v, float scale)
    {
        v.X += scale;
        v.Y += scale;
        v.Z += scale;
    }
    public static void Multiply(ref Vector3 v, float scale)
    {
        v.X *= scale;
        v.Y *= scale;
        v.Z *= scale;
    }

    public static float Average(ref Vector3 v)
    {
        return (v.X + v.Y + v.Z) / 3;
    }
}
/// <summary>
/// Useful for conversion of <see cref="Quaternion"/>s to basic Yaw/Pitch/Roll angles.
/// </summary>
public struct EulerAngles
{
    public float Yaw;
    public float Pitch;
    public float Roll;
}
