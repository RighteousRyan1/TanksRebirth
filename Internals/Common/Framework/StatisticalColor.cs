using Microsoft.Xna.Framework;
using System;
using System.Numerics;

namespace TanksRebirth.Internals.Common.Framework;
public readonly struct StatisticalColor<T> where T : IComparable, INumber<T> {
    public readonly Color FinalColor;
    public readonly Color StartColor;
    public readonly Color EndColor;

    public readonly T Lower;
    public readonly T Current;
    public readonly T Upper;

    public StatisticalColor(Color startColor, Color endColor, T lower, T current, T upper) {
        StartColor = startColor;
        EndColor = endColor;
        Lower = lower;
        Current = current;
        Upper = upper;

        // Calculate the interpolation factor
        if (current.CompareTo(lower) <= 0) {
            // Current is at or below lower bound - use start color
            FinalColor = startColor;
        }
        else if (current.CompareTo(upper) >= 0) {
            // Current is at or above upper bound - use end color
            FinalColor = endColor;
        }
        else {
            // Current is between bounds - interpolate
            var range = upper - lower;
            var progress = current - lower;

            // Convert to float for Color.Lerp (which expects 0-1 range)
            float lerpValue = float.CreateChecked(progress) / float.CreateChecked(range);

            FinalColor = Color.Lerp(startColor, endColor, lerpValue);
        }
    }
}
