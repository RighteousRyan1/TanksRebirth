using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Localization;

namespace TanksRebirth.Internals.Common.Framework.Interfaces;

public interface IModContent {
    TanksMod Mod { get; internal set; }
    int Type { get; internal set; }
    string InternalName { get; internal set; }
}
