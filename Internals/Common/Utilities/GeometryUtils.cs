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
    /// <remarks>This method is NOT pure.</remarks>
    public static T[] Shift<T>(ref T[] array, int index, int amount) {
        // extend the array by amount if we're out of bounds, then shift every element past index by amount
        if (index + amount > array.Length) {
            var newArray = new T[index + amount];
            array.CopyTo(newArray, 0);
            array = newArray;
        }
        for (var i = index; i < array.Length; i++) {
            if (i >= index)
                array[i] = array[i + amount];
        }
        return array;
    }
    // sigh no work

    public static Vector2 PredictFuturePosition(Vector2 source, Vector2 velocity, float time) {
        return source + velocity * time;
    }
    
    public static Rectangle CreateRectangleFromCenter(int x, int y, int width, int height) {
        return new Rectangle(x - width / 2, y - height / 2, width, height);
    }

    public static float GetPiRandom() {
        return new Random().GetPiRandom();
    }

    public static float GetPiRandom(this Random rand) {
        var seed = rand.Next(0, 4);

        return seed switch { 
            1 => MathHelper.PiOver2,
            2 => MathHelper.Pi,
            3 => MathHelper.Pi + MathHelper.PiOver2,
            _ => 0
        };
    }

    /// <summary>
    /// Converts the current <see cref="Quaternion"/> instance into a <see cref="EulerAngles"/> instance.
    /// </summary>
    /// <param name="quaternion">Quaternion Instance</param>
    /// <returns>The Quaternion represented as Euler Angles</returns>
    public static EulerAngles ToEulerAngles(this Quaternion quaternion) {
        EulerAngles angles = new();

        // Roll
        var wxyz = 2 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
        var xySq = 1 - 2 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
        angles.Roll = MathF.Atan2(wxyz, xySq);
        
        // Pitch
        var diffSin = 2 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
        angles.Pitch = MathF.Abs(diffSin) >= 1 ? 
            MathF.CopySign(MathHelper.PiOver2, diffSin) : // if sinp > 90 degrees, compress it to 90.
            MathF.Asin(diffSin);

        // Yaw
        var wzxy = 2 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
        var yzSq = 1 - 2 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
        angles.Yaw = MathF.Atan2(wzxy, yzSq);

        return angles;
    }

    public static float GetQuarterRotation(sbyte rot) {
        return MathHelper.PiOver2 * rot;
    }

    /// <remarks>This method is NOT pure</remarks>
    public static void Add(ref Vector3 v, float scale) {
        v.X += scale;
        v.Y += scale;
        v.Z += scale;
    }
    /// <remarks>This method is NOT pure</remarks>
    public static void Multiply(ref Vector3 v, float scale)
    {
        v.X *= scale;
        v.Y *= scale;
        v.Z *= scale;
    }

    /// <remarks>This method is NOT pure</remarks>
    public static float Average(ref Vector3 v) {
        return (v.X + v.Y + v.Z) / 3;
    }
}
/// <summary>
/// Useful for conversion of <see cref="Quaternion"/>s to basic Yaw/Pitch/Roll angles.
/// </summary>
public struct EulerAngles {
    public float Yaw;
    public float Pitch;
    public float Roll;
}
