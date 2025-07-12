using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Graphics.Metrics;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Core;

namespace TanksRebirth;

#pragma warning disable CA2211
public static class RuntimeData {
    /// <summary>Total memory used by the Garbage Collector.</summary>
    public static ulong GCMemory => (ulong)GC.GetTotalMemory(false);
    public static ulong MemoryUsageInBytes;

    /// <summary>Currently used physical memory by this application in bytes. Expensive to access to use sparingly.</summary>
    public static long ProcessMemory {
        get {
            using Process process = Process.GetCurrentProcess();
            return process.PrivateMemorySize64;
        }
    }

    /// <summary>The identifier of the main thread.</summary>
    public static int MainThreadId { get; internal set; }
    public static uint UpdateCount { get; internal set; }

    // ### FLOATING POINT ###
    /// <summary>The amount of ticks elapsed in a second of update time.</summary>
    public static float DeltaTime => Interp ? (!float.IsInfinity(60 / (float)LogicFPS) ? 60 / (float)LogicFPS : 0) : 1;
    public static float RunTime { get; internal set; }
    public static double LogicFPS { get; internal set; }
    public static double RenderFPS { get; internal set; }

    // ### BOOLEANS ###
    public static bool IsMainThread => Environment.CurrentManagedThreadId == MainThreadId;
    public static bool IsWindows => OS == OSPlatform.Windows;
    public static bool IsMac => OS == OSPlatform.OSX;
    public static bool IsLinux => OS == OSPlatform.Linux;
    public static bool Interp { get; set; } = true;

    public static bool IsSouthernHemi;

    // ### STRUCTURES / CLASSES ###

    /// <summary>The hardware used by the user's computer.</summary>
    public static ComputerSpecs CompSpecs { get; internal set; }
    public static TimeSpan RenderTime { get; internal set; }
    public static TimeSpan LogicTime { get; internal set; }
    public static DateTime LaunchTime;
    public static OSPlatform OS;
    public static CrashReportInfo CrashInfo;

    public static string ShortVersion;
    public static Version? GameVersion { get; internal set; }

    public static Graph RenderFpsGraph = new("Render", () => (float)RenderFPS, 200, 50, 3, 0.35f);
    public static Graph LogicFpsGraph = new("Logic", () => (float)LogicFPS, 200, 50, 3, 0.35f);

    public static Graph RenderTimeGraph = new("Render Time", () => RenderTime.Milliseconds, 50, 50, 3, 0.35f);
    public static Graph LogicTimeGraph = new("Logic Time", () => LogicTime.Milliseconds, 50, 50, 3, 0.35f);
}
