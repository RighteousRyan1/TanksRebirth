using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities
{
    public static class ArrayUtils
    {
        public static byte[] SequenceToUInt8Array(string sequence) => sequence.Split(',').Select(str => byte.Parse(str)).ToArray();
        public static short[] SequenceToInt16Array(string sequence) => sequence.Split(',').Select(str => short.Parse(str)).ToArray();
        public static int[] SequenceToInt32Array(string sequence) => sequence.Split(',').Select(str => int.Parse(str)).ToArray();
        public static long[] SequenceToInt64Array(string sequence) => sequence.Split(',').Select(str => long.Parse(str)).ToArray();
    }
}
