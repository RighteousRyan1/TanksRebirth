using System;

namespace TanksRebirth.Internals.Common.Utilities
{
    public static class TweenUtils
    {
        public delegate float Easing(float t);
        /// <summary>
        /// Class for generating ease functions that input a specific time and bounds 
        /// and output a normalized value [0-1].
        /// Written specifically for Tanks: Rebirth because I can't find anything
        /// on the Internet that would work
        /// </summary>
        public class BoundedTween
        {

            public BoundedTween(double start, double end, Easing easing = null) {
                Start = start;
                End = end;
                Easing = easing;
            }

            public double GetNormValue(double time) {
                var invert = false;
                if (Start > End)
                    invert = true;
                var realTime = invert ? Math.Clamp(time, End, Start) : Math.Clamp(time, Start, End);
                var length = Math.Abs(End - Start);
                var norm = (realTime - (invert ? End : Start)) / length;
                if (invert)
                    norm = 1 - norm;
                return norm;
            }

            public double GetValue(double time) {
                if (Easing == null)
                    return GetNormValue(time);
                return Easing((float)GetNormValue(time));
            }
            public float GetValue(float time) {
                if (Easing == null)
                    return (float)GetNormValue(time);
                return Easing((float)GetNormValue(time));
            }

            public double Start { get; private set; }
            public double End { get; private set; }
            public Easing Easing { get; set; }
        }
    }
}
