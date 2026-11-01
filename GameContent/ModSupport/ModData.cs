using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace TanksRebirth.GameContent.ModSupport; 

/// <summary>A container for various mod metadata.</summary>
public struct ModData {
    internal AssemblyLoadContext assemblyContainer;

    /// <summary>The names of each dependency to this mod.</summary>
    public string[] Dependencies { get; set; }
    /// <summary>The file loaded as this mod's assembly.</summary>
    public string LoadDirectory { get; set; }
    /// <summary>All assemblies loaded as part of this mod.</summary>
    public IEnumerable<Assembly> Assemblies { get; set; }
    /// <summary>The modded tanks created by this mod.</summary>
    public List<ModTank> Tanks { get; set; }
    /// <summary>The modded shells created by this mod.</summary>
    public List<ModShell> Shells { get; set; }
    /// <summary>The modded blocks created by this mod.</summary>
    public List<ModBlock> Blocks { get; set; }
}
