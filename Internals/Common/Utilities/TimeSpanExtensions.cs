using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities
{
    public static class TimeSpanExtensions
    {
        public static string StringFormat(this TimeSpan span)
        {
            string days = span.Days == 0 ? "" : (span.Days == 1 ? $"{span.Days} day, " : $"{span.Days} days, ");
            string hours = span.Hours == 0 ? "" : (span.Hours == 1 ? $"{span.Hours} hour, " : $"{span.Hours} hours, ");
            string mins = span.Minutes == 0 ? "" : (span.Minutes == 1 ? $"{span.Minutes} minute, " : $"{span.Minutes} minutes, ");
            string secs = span.Seconds == 0 ? "" : (span.Seconds == 1 ? $"{span.Seconds} second" : $"{span.Seconds} seconds");

            return $"{days}{hours}{mins}{secs}";
        }
    }
}
