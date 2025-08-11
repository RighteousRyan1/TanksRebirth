using System;
using System.Text.Json.Serialization;

namespace TanksRebirth.GameContent.ModSupport;
/// <summary>A structure containing the mod's display name, description, and version. This is built from your mod's <c>mod_info.json</c>.</summary>
[method: JsonConstructor]
/// <summary>A structure containing the mod's display name, description, and version. This is built from your mod's <c>mod_info.json</c>.</summary>
public readonly struct ModInfo(string displayName, string briefDescription, string version) {
    /// <summary>The display name of the mod. Generally named the internal name, but split by PascalCase.</summary>
    public readonly string DisplayName { get; } = displayName;
    /// <summary>A brief description of the mod. Put something interesting in <c>mod_info.json</c>!</summary>
    public readonly string BriefDescription { get; } = briefDescription;
    /// <summary></summary>
    public readonly string Version { get; } = version;
}
