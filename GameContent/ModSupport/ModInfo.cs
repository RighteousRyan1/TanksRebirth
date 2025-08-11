using System;

namespace TanksRebirth.GameContent.ModSupport; 
/// <summary>A structure containing the mod's display name, description, and version. This is built from your mod's <c>mod_info.json</c>.</summary>
public readonly struct ModInfo(string displayName, string description, string version) {
    /// <summary>The display name of the mod. Generally named the internal name, but split by PascalCase.</summary>
    public readonly string DisplayName { get; } = displayName;
    /// <summary>The description of the mod. Put something interesting in <c>mod_info.json</c>!</summary>
    public readonly string Description { get; } = description;
    /// <summary></summary>
    public readonly Version Version { get; } = Version.Parse(version);
}
