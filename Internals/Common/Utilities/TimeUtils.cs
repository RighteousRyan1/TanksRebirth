using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class TimeUtils {
    static string Unit(int amount, string name) => amount == 1 ? $"{amount} {name}" : $"{amount} {name}s";
    public static string StringFormat(this TimeSpan span) {
        span = span.Duration();

        List<string> parts = [];
        if (span.Days > 0) parts.Add(Unit(span.Days, "day"));
        if (span.Hours > 0) parts.Add(Unit(span.Hours, "hour"));
        if (span.Minutes > 0) parts.Add(Unit(span.Minutes, "minute"));
        if (span.Seconds > 0 || parts.Count == 0) parts.Add(Unit(span.Seconds, "second"));

        return string.Join(", ", parts);
    }
    public static string StringFormat(this DateTime dt) => (DateTime.Now - dt).StringFormat();
    public static string StringFormatCustom(this TimeSpan span, char between) {
        span = span.Duration();
        var hours = (int)span.TotalHours;
        return hours > 0
            ? $"{hours}{between}{span.Minutes:00}{between}{span.Seconds:00}"
            : $"{span.Minutes}{between}{span.Seconds:00}";
    }

    public static string StringFormatCustom(this DateTime dt, string between) {
        string[] fields = ["yyyy", "MM", "dd", "HH", "mm", "ss"];
        return string.Join(between, fields.Select(f => dt.ToString(f, CultureInfo.InvariantCulture)));
    }
    // i really must learn how ToString works...
    public static string StopwatchFormat(this TimeSpan span) {
        var hours = span.TotalHours >= 1 ? $"{(int)span.TotalHours:000}:" : string.Empty;
        return hours + span.ToString(@"mm\:ss\:fff");
    }
    public static float InterpolateMinuteToHour(TimeSpan timeSpan) {
        return (float)timeSpan.TotalMinutes % 60 / 60;
    }
    public static float InterpolateHourToDay(TimeSpan timeSpan) {
        return (float)timeSpan.TotalHours % 24 / 12;
    }
    public static float InterpolateMinuteToHour(DateTime dateTime) {
        var timeSpan = dateTime.TimeOfDay;
        return (float)timeSpan.TotalMinutes % 60 / 60;
    }
    public static float InterpolateHourToDay(DateTime dateTime) {
        var timeSpan = dateTime.TimeOfDay;
        return (float)timeSpan.TotalHours % 24 / 12;
    }
    public static float SineForSecond(DateTime dateTime, float mult = 1f) {
        float t = (float)dateTime.TimeOfDay.TotalSeconds;
        return MathF.Sin(t * MathF.Tau * mult);
    }
    public static float SineForSecond(TimeSpan timeSpan, float mult = 1f) {
        float t = (float)timeSpan.TotalSeconds;
        return MathF.Sin(t * MathF.Tau * mult);
    }

    public static int GetHourFromCircle(float percentRotation) {
        var time = percentRotation * MathHelper.TwoPi / (MathHelper.Tau / 12);
        return (int)Math.Floor(time);
    }
    public static int GetMinuteFromCircle(float percentRotation) {
        var time = percentRotation * MathHelper.TwoPi / (MathHelper.Tau / 60);
        return (int)Math.Floor(time);
    }
}
