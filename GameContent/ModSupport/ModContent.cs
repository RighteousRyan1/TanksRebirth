using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;

/// <summary>Request modded content here.</summary>
public static class ModContent {
    internal static List<IModContent> moddedTypes = [];
    /// <summary>Use in tandem with a <see cref="ModTank"/> class <see cref="Type"/> to retrieve data about a modded tank.</summary>
    /// <typeparam name="T">The <see cref="Type"/> of the modded tank you wish to request data from.</typeparam>
    /// <returns>A singleton instance of a <see cref="ModTank"/>.</returns>
    public static T GetModTank<T>() where T : ModTank {
        var modTank = moddedTypes.OfType<T>().FirstOrDefault();
        return modTank!;
    }
    public static T GetModBlock<T>() where T : ModBlock {
        var modBlock = moddedTypes.OfType<T>().FirstOrDefault();
        return modBlock!;
    }
    public static T GetModShell<T>() where T : ModShell {
        var modShell = moddedTypes.OfType<T>().FirstOrDefault();
        return modShell!;
    }
}
