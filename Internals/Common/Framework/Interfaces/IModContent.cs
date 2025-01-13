using TanksRebirth.GameContent.ModSupport;

namespace TanksRebirth.Internals.Common.Framework.Interfaces;

public interface IModContent {
    TanksMod Mod { get; internal set; }
    int Type { get; internal set; }
}
