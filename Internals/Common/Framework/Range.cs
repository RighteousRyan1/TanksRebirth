using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent;

namespace TanksRebirth.Internals.Common.Framework
{
    public struct Range<T> where T : IComparable
    {
        public T Min;
        public T Max;

        public Range(T min, T max) { 
            Min = min; Max = max; 
        }

        // public static implicit operator T(Range<T> range) { return GameHandler.GameRand.Next(range.Min, range.Max); }
    }
}
