using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;

/// <summary>Request modded content here.</summary>
public static class ModContent {
    internal static List<object> moddedTypes = new();
    /// <summary>Use in tandem with a <see cref="ModTank"/> class <see cref="Type"/> to retrieve data about a modded tank.</summary>
    /// <typeparam name="T">The <see cref="Type"/> of the modded tank you wish to request data from.</typeparam>
    /// <returns>A singleton instance of a <see cref="ModTank"/>.</returns>
    public static T GetModTank<T>() where T : ModTank {
        var modTank = moddedTypes.OfType<T>().FirstOrDefault();
        return modTank;
    }
}
