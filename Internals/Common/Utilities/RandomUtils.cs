﻿using System;
using System.Collections.Generic;
using TanksRebirth.Net;

namespace TanksRebirth.Internals.Common.Utilities;

public static class RandomUtils
{
    public static float NextFloat(this Random random, float min, float max)
    => (float)(random.NextDouble() * (max - min) + min);
    public static double NextDouble(this Random random, double min, double max)
        => random.NextDouble() * (max - min) + min;
    public static short Next(this Random random, short min, short max)
        => (short)random.Next(min, max);
    public static byte Next(this Random random, byte min, byte max)
        => (byte)random.Next(min, max);
    public static T PickRandom<T>(T[] input) => input[Client.ClientRandom.Next(0, input.Length)];
    public static List<T> PickRandomClient<T>(T[] input, int amount)
    {
        List<T> values = [];
        List<int> chosenTs = [];
        for (int i = 0; i < amount; i++)
        {
        ReRoll:
            int rand = Client.ClientRandom.Next(0, input.Length);

            if (!chosenTs.Contains(rand))
            {
                chosenTs.Add(rand);
                values.Add(input[rand]);
            }
            else
                goto ReRoll;
        }
        chosenTs.Clear();
        return values;
    }
    public static List<T> PickRandomServer<T>(T[] input, int amount) {
        List<T> values = [];
        List<int> chosenTs = [];
        for (int i = 0; i < amount; i++) {
        ReRoll:
            int rand = Server.ServerRandom.Next(0, input.Length);

            if (!chosenTs.Contains(rand)) {
                chosenTs.Add(rand);
                values.Add(input[rand]);
            }
            else
                goto ReRoll;
        }
        chosenTs.Clear();
        return values;
    }
    public static TEnum PickRandom<TEnum>() where TEnum : struct, Enum => (TEnum)(object)Server.ServerRandom.Next(0, Enum.GetNames<TEnum>().Length);
}
