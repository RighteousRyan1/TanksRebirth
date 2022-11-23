using System;
using System.Linq;

namespace TanksRebirth.Internals.Common.Utilities;

public static class ArrayUtils
{
    /// <summary>
    /// Adjust an array.
    /// </summary>
    /// <param name="array">The array to adjust.</param>
    /// <param name="adjust">how much to adjust. This can be negative.</param>
    /// <param name="z">Where to start adjusting.</param>
    /// <param name="w">Where to end adjusting. If 0, defaults to the end of the array.</param>
    /// <returns>The shifted array.</returns>
    public static T[] Shift<T>(T[] array, int adjust, int z = 0, int w = 0) {
        T[] arrayCopy = new T[array.Length];

        Array.Copy(array, arrayCopy, array.Length);

        if (w == 0)
            w = array.Length;

        for (int i = z; i < w; i++) {
            if (i == 0 && adjust > 0)
                array[0] = default;
            else if (i == array.Length - 1 && adjust < 0)
                array[^1] = default;

            if (adjust < 0) {
                if (i != array.Length - 1)
                    array[i] = array[i + adjust];
            }
            else if (i != 0)
                array[i] = arrayCopy[i + adjust];

        }
        return array;
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
    public static byte[] SequenceToUInt8Array(string sequence) => sequence == string.Empty ? Array.Empty<byte>() : sequence.Split(',').Select(str => byte.Parse(str)).ToArray();
    public static short[] SequenceToInt16Array(string sequence) => sequence == string.Empty ? Array.Empty<short>() : sequence.Split(',').Select(str => short.Parse(str)).ToArray();
    public static int[] SequenceToInt32Array(string sequence) => sequence == string.Empty ? Array.Empty<int>() : sequence.Split(',').Select(str => int.Parse(str)).ToArray();
    public static long[] SequenceToInt64Array(string sequence) => sequence == string.Empty ? Array.Empty<long>() : sequence.Split(',').Select(str => long.Parse(str)).ToArray();
}
