using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.AI;
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
    /// <summary>
    /// Retrieves a list of all modded content for a given <see cref="IModContent"/>.
    /// </summary>
    /// <typeparam name="T">The modded content kind.</typeparam>
    public static List<T> GetContent<T>() where T : IModContent {
        return [.. singletonMap.Values.OfType<T>()];
    }
    /// <summary>
    /// Retrieves a list of all modded content for a given <see cref="IModContent"/> for a given <see cref="TanksMod"/>.
    /// </summary>
    /// <typeparam name="T">The modded content kind.</typeparam>
    public static List<T> GetContent<T>(TanksMod mod) where T : IModContent {
        return [.. singletonMap.Values.OfType<T>()];
    }

    public static bool TryGetModTankById(int id, out ModTank? tank) {
        tank = null;
        if (id < TankID.VanillaCount) return false;

        tank = idToModTank[id];
        return true;
    }
    public static bool TryGetModBlockById(int id, out ModBlock? block) {
        block = null;
        if (id < BlockID.VanillaCount) return false;

        block = idToModBlock[id];
        return true;
    }
    public static bool TryGetModShellById(int id, out ModShell? block) {
        block = null;
        if (id < BlockID.VanillaCount) return false;

        block = idToModShell[id];
        return true;
    }

    // Backend/non-api

    internal static void AttachModdedContent(this Block block) {
        if (idToModBlock.TryGetValue(block.Type, out var modBlock)) {
            // associate values properly for modded data
            block.ModdedData = modBlock.Clone();
            block.ModdedData.Block = block;
        }
    }

    internal static void AttachModContent(this AITank tank) {
        if (idToModTank.TryGetValue(tank.AiTankType, out var modTank)) {
            // associate values properly for modded data
            tank.ModdedData = modTank.Clone();
            tank.ModdedData.AITank = tank;
        }
    }

    internal static void AttachModContent(this Shell shell) {
        if (idToModShell.TryGetValue(shell.Type, out var modShell)) {
            // associate values properly for modded data
            shell.ModdedData = modShell.Clone();
            shell.ModdedData.Shell = shell;
        }
    }
}
