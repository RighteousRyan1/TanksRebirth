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
    public static T[] Shift<T>(this T[] array, int adjust, int z = 0, int w = 0) {
        T[] arrayCopy = new T[array.Length];

        Array.Copy(array, arrayCopy, array.Length);

        adjust = -adjust;

        if (w == 0)
            w = array.Length;

        for (int i = z; i < w; i++) {
            if (i == 0 && adjust < 0)
                array[0] = default;
            else if (i == array.Length - 1 && adjust > 0)
                array[^1] = default;

            if (adjust > 0) {
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

    /// <summary>
    /// Finds the first mismatch between two arrays.
    /// </summary>
    /// <param name="first">First Array; self</param>
    /// <param name="second">Second array.</param>
    /// <param name="firstValue">The value in the first array where the mismatch occurred.</param>
    /// <param name="secondValue">The value in the second array where the mismatch occurred.</param>
    /// <param name="mismatchCount">The amount of mis matches that occurred</param>
    /// <typeparam name="T">Type for the Array.</typeparam>
    /// <returns>An <see cref="int"/> containing the index of the first mismatch in the array.</returns>
    public static int FindFirstMismatch<T>(this T[] first, T[] second, out T firstValue, out T secondValue, out int mismatchCount) {
        firstValue = default;
        secondValue = default;
        mismatchCount = 0;

        var firstMismatch = -1;
        if (first.Length != second.Length)
            return -1;
        for (var i = 0; i < first.Length; i++) {
            if (first[i].Equals(second[i])) continue;

            mismatchCount++;
            firstValue = first[i];
            secondValue = second[i];

            if (firstMismatch == -1)
                firstMismatch = i;
        }

        return firstMismatch;
    }
    public static byte[] SequenceToUInt8Array(this string sequence) => InnerSequenceParser<byte>(sequence);
    public static short[] SequenceToInt16Array(this string sequence) => InnerSequenceParser<short>(sequence);
    public static int[] SequenceToInt32Array(this string sequence) => InnerSequenceParser<int>(sequence);
    public static long[] SequenceToInt64Array(this string sequence) => InnerSequenceParser<long>(sequence);

    private static T[] InnerSequenceParser<T>(string sequence) {
        if (string.IsNullOrEmpty(sequence))
            return Array.Empty<T>();
        ReadOnlySpan<char> strAsSpan = sequence;
        
        return sequence.Split(',').Select(x => (T)Convert.ChangeType(x, typeof(T))).ToArray();
    }
}
