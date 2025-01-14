using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;

/// <summary>Request modded content here.</summary>
public static class ModContent {
    internal static List<IModContent> moddedTypes = [];
    /// <summary>A useful method that gets properties of a modded type. Can be used to manually swap properties after spawning an entity.</summary>
    /// <typeparam name="T">The <see cref="Type"/> of the modded content you wish to request data from.</typeparam>
    /// <returns>A singleton instance of any form of supported mod content.</returns>
    public static T GetSingleton<T>() where T : IModContent {
        // this fails with shells upon mod reload. why?
        var properTypes = moddedTypes.OfType<T>().ToArray();
        var modContent = properTypes.FirstOrDefault();
        return modContent!;
    }
}
