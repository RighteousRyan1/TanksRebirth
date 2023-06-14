using System;

namespace TanksRebirth.Internals.Common.Framework.Core;

public readonly struct CrashReportInfo {
    public readonly string Reason;
    public readonly string Description;
    public readonly Exception? Cause;

    public CrashReportInfo(string reason, string description, Exception cause) {
        Reason = reason;
        Description = description;
        Cause = cause;
    }
}
