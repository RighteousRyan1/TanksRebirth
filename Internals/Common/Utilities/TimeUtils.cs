using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class TimeUtils {
    public static string StringFormat(this TimeSpan span) {
        StringBuilder sb = new(32);
        switch (span.Days) {
            case 1:
                sb.Append(span.Days).Append(" day, ");
                break;
            case > 1:
                sb.Append(span.Days).Append(" days, ");
                break;
        }

        switch (span.Hours) {
            case 1:
                sb.Append(span.Hours).Append(" hour, ");
                break;
            case > 1:
                sb.Append(span.Hours).Append(" hours, ");
                break;
        }

        switch (span.Minutes) {
            case 1:
                sb.Append(span.Minutes).Append(" minute, ");
                break;
            case > 1:
                sb.Append(span.Minutes).Append(" minutes, ");
                break;
        }

        switch (span.Seconds) {
            case 1:
                sb.Append(span.Seconds).Append(" second, ");
                break;
            case > 1:
                sb.Append(span.Seconds).Append(" seconds, ");
                break;
        }

        return sb.ToString();
    }
    public static string StringFormat(this DateTime dt) {
        StringBuilder sb = new(32);
        switch (dt.Day) {
            case 1:
                sb.Append(dt.Day).Append(" day, ");
                break;
            case > 1:
                sb.Append(dt.Day).Append(" days, ");
                break;
        }

        switch (dt.Hour) {
            case 1:
                sb.Append(dt.Hour).Append(" hour, ");
                break;
            case > 1:
                sb.Append(dt.Hour).Append(" hours, ");
                break;
        }

        switch (dt.Minute) {
            case 1:
                sb.Append(dt.Minute).Append(" minute, ");
                break;
            case > 1:
                sb.Append(dt.Minute).Append(" minutes, ");
                break;
        }

        switch (dt.Second) {
            case 1:
                sb.Append(dt.Second).Append(" second, ");
                break;
            case > 1:
                sb.Append(dt.Second).Append(" seconds, ");
                break;
        }

        return sb.ToString();
    }
    public static string StringFormatCustom(this TimeSpan span, string between) {
        StringBuilder sb = new(2 * 4 + between.Length * 4);

        if (span.Days != 0)
            sb.Append(span.Days).Append(between);
        if (span.Hours != 0)
            sb.Append(span.Hours).Append(between);
        if (span.Minutes != 0)
            sb.Append(span.Minutes).Append(between);
        if (span.Seconds != 0)
            sb.Append(span.Seconds).Append(between);

        return sb.ToString();
    }

    public static string StringFormatCustom(this DateTime span, string between) {
        StringBuilder sb = new(2 * 4 + between.Length * 4);

        if (span.Day != 0)
            sb.Append(span.Day).Append(between);
        if (span.Hour != 0)
            sb.Append(span.Hour).Append(between);
        if (span.Minute != 0)
            sb.Append(span.Minute).Append(between);
        if (span.Second != 0)
            sb.Append(span.Second).Append(between);

        return sb.ToString();
    }

    public static string StopwatchFormat(this Stopwatch watch) {
        var span = watch.Elapsed;
        StringBuilder sb = new(64);
        
        // Append Hours
        switch (span.Hours) {
            case >= 10 and < 100:
                sb.Append('0').Append(span.Hours);
                break;
            case < 10:
                sb.Append("00").Append(span.Hours);
                break;
            default:
                sb.Append(span.Hours);
                break;
        }

        // Appends Minutes
        if (span.Minutes < 10)
            sb.Append('0').Append(span.Minutes);
        else
            sb.Append(span.Minutes);
        
        // Append Seconds
        if (span.Seconds < 10)
            sb.Append('0').Append(span.Seconds);
        else
            sb.Append(span.Seconds);

        // Append ms
        sb.Append(span.Milliseconds);

        return sb.ToString();
    }
}
