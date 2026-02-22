using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Net;

namespace TanksRebirth.Internals.Common.Utilities;

// this works, i guess.
public static class EnumCache<TEnum> where TEnum : struct, Enum {
    public static readonly TEnum[] Values = Enum.GetValues<TEnum>();
}
public static class RandomUtils {
    /// <summary>
    /// Generates a random <see langword="float"/> within the range [<paramref name="min"/>, <paramref name="max"/>).
    /// </summary>
    /// <param name="random">The random.</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="max">The exclusive maximum.</param>
    /// <returns>The random number.</returns>
    public static float NextFloat(this Random random, float min, float max) => random.NextSingle() * (max - min) + min;
    /// <summary>
    /// Generates a random <see langword="double"/> within the range [<paramref name="min"/>, <paramref name="max"/>).
    /// </summary>
    /// <param name="random">The random.</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="max">The exclusive maximum.</param>
    /// <returns>The random number.</returns>
    public static double NextDouble(this Random random, double min, double max) => random.NextDouble() * (max - min) + min;

    /// <summary>
    /// Selects a specified number of unique random elements from an array.
    /// </summary>
    /// <remarks>
    /// This implementation uses a partial Fisher-Yates shuffle to ensure O(n) performance 
    /// and zero re-rolls, even when picking a large percentage of the input.
    /// </remarks>
    public static List<T> Sample<T>(this Random random, T[] input, int amount) {
        if (amount > input.Length)
            amount = input.Length;

        if (amount <= 0)
            return [];

        T[] copy = [.. input];
        List<T> results = new(amount);

        // shuffle-ish
        for (int i = 0; i < amount; i++) {
            int nextIndex = random.Next(i, copy.Length);
            (copy[i], copy[nextIndex]) = (copy[nextIndex], copy[i]);
            results.Add(copy[i]);
        }

        return results;
    }
    /// <summary>
    /// Picks a random value from the specified <see langword="enum"/> type.
    /// </summary>
    public static TEnum NextEnum<TEnum>(this Random random) where TEnum : struct, Enum {
        var values = EnumCache<TEnum>.Values;
        return values[random.Next(values.Length)];
    }

    /// <summary>
    /// Picks a single random element from a collection.
    /// </summary>
    /// <remarks>
    /// Using ReadOnlySpan allows this to work on arrays or slices of memory with zero overhead.
    /// </remarks>
    public static T NextArrayValue<T>(this Random random, ReadOnlySpan<T> input) => input[random.Next(input.Length)];
}