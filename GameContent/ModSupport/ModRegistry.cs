using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;

/// <summary>Request modded content here.</summary>
public static class ModRegistry {
    internal static Dictionary<Type, IModContent> singletonMap = [];
    internal static Dictionary<int, ModTank> idToModTank = [];
    internal static Dictionary<int, ModBlock> idToModBlock = [];
    internal static Dictionary<int, ModShell> idToModShell = [];
    /// <summary>A useful method that gets properties of a modded type. Can be used to manually swap properties after spawning an entity.</summary>
    /// <typeparam name="T">The <see cref="Type"/> of the modded content you wish to request data from.</typeparam>
    /// <returns>A singleton instance of any form of supported mod content.</returns>
    public static T GetSingleton<T>() where T : class, IModContent {
        if (singletonMap.TryGetValue(typeof(T), out var content))
            return (T)content;

        throw new ModRuntimeException($"Modded type '{typeof(T).Name}' not found.");
    }

    public static bool TryGetModTankById(int id, out ModTank tank) {
        tank = null;
        if (id < TankID.VanillaCount) return false;

        tank = idToModTank[id];
        return true;
    }
    public static bool TryGetModBlockById(int id, out ModBlock block) {
        block = null;
        if (id < BlockID.VanillaCount) return false;

        block = idToModBlock[id];
        return true;
    }
    public static bool TryGetModShellById(int id, out ModShell block) {
        block = null;
        if (id < BlockID.VanillaCount) return false;

        block = idToModShell[id];
        return true;
    }

    // Backend/non-api

    /*public static void AttachModdedContent(Block block) {
        for (int i = 0; i < ModLoader.ModBlocks.Length; i++) {
            var modBlock = ModLoader.ModBlocks[i];

            // associate values properly for modded data
            if (block.Type == modBlock.Type) {
                block.ModdedData = modBlock.Clone();
                block.ModdedData.Block = this;
            }
        }
    }*/
}
