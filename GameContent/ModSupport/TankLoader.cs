using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.Enums;
using System.Linq;
using TanksRebirth.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Core.Interfaces;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Systems;
using System.Reflection;

namespace TanksRebirth.GameContent.ModSupport
{
    /// <summary>Represents a system in which to load <see cref="ModTank"/>s.</summary>
    public static class TankLoader
    {
        public static int TankCount { get; set; }

        internal static ModTank[] tnkCache;

        internal static void LoadTanksToCache()
        {
            var list = ReflectionUtils.GetInheritedTypesOf<ModTank>(Assembly.GetExecutingAssembly());

            for (int i = 0; i < list.Count; i++)
                tnkCache[i].internal_tier = i;

            tnkCache = list.ToArray();
        }
    }
}
