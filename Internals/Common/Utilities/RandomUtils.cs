using System;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Utilities;

public static class RandomUtils
{
    public static float NextFloat(this Random random, float min, float max)
    => (float)(Random.Shared.NextDouble() * (max - min) + min);
    public static double NextFloat(this Random random, double min, double max)
        => Random.Shared.NextDouble() * (max - min) + min;
    public static short Next(this Random random, short min, short max)
        => (short)random.Next(min, max);
    public static byte Next(this Random random, byte min, byte max)
        => (byte)random.Next(min, max);
    public static T PickRandom<T>(T[] input) => input[Random.Shared.Next(0, input.Length)];
    public static List<T> PickRandom<T>(T[] input, int amount)
    {
        List<T> values = new(amount);
        List<int> chosenTs = new(amount);
        for (var i = 0; i < amount; i++) {
            while (true) {
                var rand = new Random().Next(0, input.Length);

                if (chosenTs.Contains(rand)) continue;
                chosenTs.Add(rand);
                values.Add(input[rand]);
                break;
            }
        }
        chosenTs.Clear();
        return values;
    }
    public static TEnum PickRandom<TEnum>() where TEnum : struct, Enum
        => (TEnum)(object)Random.Shared.Next(0, Enum.GetNames<TEnum>().Length);
}
