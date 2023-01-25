using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;

/// <summary>A class that contains an entrypoint into the game.</summary>
public class TanksMod : ILoadable {
    /// <summary>The display name of this mod. Generally named the internal name, but split by PascalCase.</summary>
    public virtual string Name { get; }
    /// <summary>The internal name of this mod. This is the name of the project the mod was built in.</summary>
    public virtual string InternalName { get; internal set; }
    /// <summary>
    /// This method is called when your mod is loaded.<para></para>
    /// Use this method to initialize hooking, add things to your mod, load content, etc.
    /// </summary>
    public virtual void OnLoad() { }
    /// <summary>
    /// This method is called when your mod is unloaded.<para></para>
    /// BE SURE to remove hooking, unsubscribe events, unload assets, and everything else that may remain in memory otherwise when your mod unloads, unused memory may remain.
    /// </summary>
    public virtual void OnUnload() { }
}