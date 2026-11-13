using System;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common;

// could totally use this in Tank AI code in (isolation?) to see how it matches up with the original.

/// <summary>
/// The random number generator the Wii Play tanks code uses... as I've interpreted it. 16 bits only.
/// </summary>
/// <remarks>Not thread safe. Should also not have differing RNG calls like <see cref="Random"/> does.</remarks>
public static class PRNG {
    const uint LCG_MULTIPLIER = 0x41C64E6D;
    const uint LCG_INCREMENT = 0x3039;
    const uint LFSR_MASK = 0x11020;
    const float INV_2_23 = 1f / (1 << 23);

    static uint _a = 1;
    static uint _b = 1;

    static PRNG() => Seed(Environment.TickCount);

    /// <summary>Sets both halves of the state directly. The original's <c>set_rng_seeds</c> does the same with two values. <paramref name="b"/> should not be zero.</summary>
    public static void Seed(uint a, uint b) {
        _a = a;
        _b = b == 0 ? 1u : b;
    }

    /// <summary>Seeds both halves from a single value.</summary>
    public static void Seed(int seed) {
        // splitmix32-ish, so near seeds don't give near states
        uint z = (uint)seed + 0x9E3779B9;
        z = (z ^ (z >> 16)) * 0x85EBCA6B;
        z = (z ^ (z >> 13)) * 0xC2B2AE35;
        var a = z ^ (z >> 16);
        z = a + 0x9E3779B9;
        z = (z ^ (z >> 16)) * 0x85EBCA6B;
        z = (z ^ (z >> 13)) * 0xC2B2AE35;
        Seed(a, z ^ (z >> 16));
    }

    public static (uint A, uint B) GetState() => (_a, _b);
    public static void SetState(uint a, uint b) => Seed(a, b);
    public static uint NextState() {
        _a = _a * LCG_MULTIPLIER + LCG_INCREMENT;
        if ((_b & 1) != 0)
            _b ^= LFSR_MASK;
        _b >>= 1;
        return _a ^ _b;
    }

    public static float NextFloat() => (NextState() & 0x7FFFFF) * INV_2_23;
    public static float NextFloat(float max) => NextFloat() * max;
    public static float NextFloat(float min, float max) => min + (max - min) * NextFloat();
    public static float NextAngle() => NextFloat(MathF.Tau);
    public static int Next() => (int)((NextState() >> 4) & 0xFFFF);
    public static int Next(int max) => Next(0, max);
    public static int Next(int min, int max) {
        var range = max - min;
        if (range <= 0)
            return min;
        var v = (int)((NextState() >> 4) & 0xFFFF);
        return min + v % range;
    }
    public static T NextArrayValue<T>(ReadOnlySpan<T> input) => input[Next(input.Length)];
    public static TEnum NextEnum<TEnum>() where TEnum : struct, Enum {
        var values = EnumCache<TEnum>.Values;
        return values[Next(values.Length)];
    }
    public static void Shuffle<T>(Span<T> span) {
        for (int i = span.Length - 1; i > 0; i--) {
            var j = Next(i + 1);
            (span[i], span[j]) = (span[j], span[i]);
        }
    }
}
