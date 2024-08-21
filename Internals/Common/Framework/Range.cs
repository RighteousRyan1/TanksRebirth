using System;

namespace TanksRebirth.Internals.Common.Framework;

/// <summary>Construct a range of values.</summary>
public struct Range<T> where T : IComparable {
    public T Min;
    public T Max;

    public Range(T min, T max) { 
        Min = min; Max = max; 
    }

    // public static implicit operator T(Range<T> range) { return GameHandler.GameRand.Next(range.Min, range.Max); }
}
