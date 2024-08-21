using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;
// maybe allow for changing the model.
public class ModBlock : ILoadable
{
    public int Type { get; internal set; }
    /// <summary>Initialize what you want alongside the loading of your modded tank.</summary>
    public virtual void OnLoad() { }
    /// <summary>Manually unload things that may not be automatically unloaded by the game.</summary>
    public virtual void OnUnload() { }
    public virtual void OnInitialize() { }
    public virtual void PostUpdate(Block block) { }
    public virtual void PostRender(Block block) { }
}
