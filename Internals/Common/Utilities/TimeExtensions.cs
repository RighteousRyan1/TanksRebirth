using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities
{
    public static class TimeExtensions
    {
        public static string StringFormat(this TimeSpan span)
        {
            string days = span.Days == 0 ? "" : (span.Days == 1 ? $"{span.Days} day, " : $"{span.Days} days, ");
            string hours = span.Hours == 0 ? "" : (span.Hours == 1 ? $"{span.Hours} hour, " : $"{span.Hours} hours, ");
            string mins = span.Minutes == 0 ? "" : (span.Minutes == 1 ? $"{span.Minutes} minute, " : $"{span.Minutes} minutes, ");
            string secs = span.Seconds == 0 ? "" : (span.Seconds == 1 ? $"{span.Seconds} second" : $"{span.Seconds} seconds");

            return $"{days}{hours}{mins}{secs}";
        }
        public static string StringFormat(this DateTime span)
        {
            string days = span.Day == 0 ? "" : (span.Day == 1 ? $"{span.Day} day, " : $"{span.Day} days, ");
            string hours = span.Hour == 0 ? "" : (span.Hour == 1 ? $"{span.Hour} hour, " : $"{span.Hour} hours, ");
            string mins = span.Minute == 0 ? "" : (span.Minute == 1 ? $"{span.Minute} minute, " : $"{span.Minute} minutes, ");
            string secs = span.Second == 0 ? "" : (span.Second == 1 ? $"{span.Second} second" : $"{span.Second} seconds");

            return $"{days}{hours}{mins}{secs}";
        }

        public static string StringFormatCustom(this DateTime span, string between)
        {
            string days = span.Day == 0 ? "" : (span.Day == 1 ? $"{span.Day}{between}" : $"{span.Day}{between}");
            string hours = span.Hour == 0 ? "" : (span.Hour == 1 ? $"{span.Hour}{between}" : $"{span.Hour}{between}");
            string mins = span.Minute == 0 ? "" : (span.Minute == 1 ? $"{span.Minute}{between}" : $"{span.Minute}{between}");
            string secs = span.Second == 0 ? "" : (span.Second == 1 ? $"{span.Second}" : $"{span.Second}");

            return $"{days}{hours}{mins}{secs}";
        }
    }
}
