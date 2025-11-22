using System.Collections.Generic;
using System.Runtime.Loader;

namespace TanksRebirth.GameContent.ModSupport; 
public struct ModData {
    public string[] Dependencies { get; set; }
    public string LoadDirectory { get; set; }
    public AssemblyLoadContext Assemblies { get; set; }
    public List<ModTank> Tanks { get; set; }
    public List<ModShell> Shells { get; set; }
    public List<ModBlock> Blocks { get; set; }
}
