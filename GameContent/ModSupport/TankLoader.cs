using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using WiiPlayTanksRemake.Enums;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using WiiPlayTanksRemake.GameContent.GameMechanics;
using WiiPlayTanksRemake.GameContent.Systems;
using System.Reflection;

namespace WiiPlayTanksRemake.GameContent.ModSupport
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
