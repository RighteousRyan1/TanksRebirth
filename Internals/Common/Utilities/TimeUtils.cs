using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class TimeUtils
{
    public static string StringFormat(this TimeSpan span)
    {
        string days = span.Days == 0 ? "" : (span.Days == 1 ? $"{span.Days} day, " : $"{span.Days} days, ");
        string hours = span.Hours == 0 ? "" : (span.Hours == 1 ? $"{span.Hours} hour, " : $"{span.Hours} hours, ");
        string mins = span.Minutes == 0 ? "" : (span.Minutes == 1 ? $"{span.Minutes} minute, " : $"{span.Minutes} minutes, ");
        string secs = span.Seconds == 0 ? "" : (span.Seconds == 1 ? $"{span.Seconds} second" : $"{span.Seconds} seconds");

        return $"{days}{hours}{mins}{secs}";
    }
    public static string StringFormat(this DateTime dt)
    {
        string days = dt.Day == 0 ? "" : (dt.Day == 1 ? $"{dt.Day} day, " : $"{dt.Day} days, ");
        string hours = dt.Hour == 0 ? "" : (dt.Hour == 1 ? $"{dt.Hour} hour, " : $"{dt.Hour} hours, ");
        string mins = dt.Minute == 0 ? "" : (dt.Minute == 1 ? $"{dt.Minute} minute, " : $"{dt.Minute} minutes, ");
        string secs = dt.Second == 0 ? "" : (dt.Second == 1 ? $"{dt.Second} second" : $"{dt.Second} seconds");

        return $"{days}{hours}{mins}{secs}";
    }
    public static string StringFormatCustom(this TimeSpan span, string between)
    {
        string days = span.Days == 0 ? "" : (span.Days == 1 ? $"{span.Days}{between}" : $"{span.Days}{between}");
        string hours = span.Hours == 0 ? "" : (span.Hours == 1 ? $"{span.Hours}{between}" : $"{span.Hours}{between}");
        string mins = span.Minutes == 0 ? "" : (span.Minutes == 1 ? $"{span.Minutes}{between}" : $"{span.Minutes}{between}");
        string secs = span.Seconds == 0 ? "" : (span.Seconds == 1 ? $"{span.Seconds}" : $"{span.Seconds}");

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

    public static string StopwatchFormat(this TimeSpan span)
    {
        string hours;
        if (span.Hours >= 10 && span.Hours < 100)
            hours = $"0{span.Hours}";
        else if (span.Hours < 10)
            hours = $"00{span.Hours}";
        else
            hours = $"{span.Hours}";
        string mins = span.Minutes < 10 ? $"0{span.Minutes}" : $"{span.Minutes}";
        string secs = span.Seconds < 10 ? $"0{span.Seconds}" : $"{span.Seconds}";
        int millisecs = span.Milliseconds;

        return $"{hours}:{mins}:{secs}:{millisecs}";
    }
}
