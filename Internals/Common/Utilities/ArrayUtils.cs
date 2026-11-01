using System;
using System.Linq;

namespace TanksRebirth.Internals.Common.Utilities;

public static class ArrayUtils {
    /// <summary>
    /// Adjust an array. By moving a section of an array by <paramref name="n"/> indices. If it ends up being moved further than the length of the array, the array is resized to fit.
    /// </summary>
    /// <param name="array">The array to n.</param>
    /// <param name="n">how much to n. This can be negative.</param>
    /// <param name="z">Where to start adjusting.</param>
    /// <param name="w">Where to end adjusting. If 0, defaults to the end of the array.</param>
    /// <returns>The shifted array.</returns>
    public static T[] Shift<T>(T[] array, int n, int z = 0, int w = 0) {
        T[] arrayCopy = new T[array.Length];

        Array.Copy(array, arrayCopy, array.Length);

        n = -n;

        if (w == 0)
            w = array.Length;

        for (int i = z; i < w; i++) {
            if (i == 0 && n < 0)
                array[0] = default;
            else if (i == array.Length - 1 && n > 0)
                array[^1] = default;

            if (n > 0) {
                if (i != array.Length - 1)
                    array[i] = array[i + n];
            }
            else if (i != 0)
                array[i] = arrayCopy[i + n];

        }
        return array;
    }
    /// <summary>
    /// Resizes a Two-dimensional array.
    /// </summary>
    /// <typeparam name="T">The type of the 2D array.</typeparam>
    /// <param name="arr">The array.</param>
    /// <param name="rows">The new length of rows.</param>
    /// <param name="cols">The new length of columns.</param>
    /// <returns></returns>
    public static T[,] Resize2D<T>(T[,] arr, int rows, int cols) {
        var newArray = new T[rows, cols];
        int minRows = Math.Min(rows, arr.GetLength(0));
        int minCols = Math.Min(cols, arr.GetLength(1));
        for (int i = 0; i < minRows; i++)
            for (int j = 0; j < minCols; j++)
                newArray[i, j] = arr[i, j];
        return newArray;
    }

    /// <summary>
    /// Scans two arrays, reports the first mismatching element between the two, and includes the number of mismatches. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <param name="firstValue"></param>
    /// <param name="secondValue"></param>
    /// <param name="mismatchCount"></param>
    /// <returns>The index of the first mismatch.</returns>
    public static int FindFirstMismatch<T>(T[] first, T[] second, out T? firstValue, out T? secondValue, out int mismatchCount) where T : notnull {
        firstValue = default;
        secondValue = default;
        mismatchCount = 0;

        int firstMismatch = -1;
        if (first.Length != second.Length)
            return -1;
        for (int i = 0; i < first.Length; i++) {
            if (!first[i].Equals(second[i])) {
                mismatchCount++;
                firstValue = first[i];
                secondValue = second[i];
                if (firstMismatch < 0)
                    firstMismatch = i;
            }
        }

        return firstMismatch;
    }
    public static byte[] SequenceToUInt8Array(string sequence) => sequence == string.Empty ? [] : sequence.Split(',').Select(byte.Parse).ToArray();
    public static short[] SequenceToInt16Array(string sequence) => sequence == string.Empty ? [] : sequence.Split(',').Select(short.Parse).ToArray();
    public static int[] SequenceToInt32Array(string sequence) => sequence == string.Empty ? [] : sequence.Split(',').Select(int.Parse).ToArray();
    public static long[] SequenceToInt64Array(string sequence) => sequence == string.Empty ? [] : sequence.Split(',').Select(long.Parse).ToArray();
}