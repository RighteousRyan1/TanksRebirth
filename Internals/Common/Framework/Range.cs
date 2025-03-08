using System;
using System.Numerics;

namespace TanksRebirth.Internals.Common.Framework;

/// <summary>Construct a range of values.</summary>
public struct Range<T>(T min, T max) where T : INumber<T> {
    public T Min = min;
    public T Max = max;

    public readonly T Difference => Max - Min;
    // public static implicit operator T(Range<T> range) { return GameHandler.GameRand.Next(range.Min, range.Max); }
}
