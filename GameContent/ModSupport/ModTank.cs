using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.Loader;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Framework.Interfaces;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.ModSupport;
// events will be tied to the virtual methods like OnTargetsSpotted when a modded tank is spawned.

// what can probably be done:
// 1) do checks from AITank to see what tanks exist are modded tanks.
// 2) see if those tanks fit the bill for a certain overridden method (i.e: OnTargetsSpotted), then subscribe said methods to an event.
// 3) invoke per-ModTank type, like a modded X tank will only invoke Y method within the X ModTank class.
public class ModTank : ILoadable {
    public virtual LocalizedString Name { get; }

    //public TankProperties Properties { get; private set; }
    //public AiParameters AiParameters { get; private set; }
    public int TierType { get; private set; }
    public virtual void OnLoad() { }
    public virtual void OnUnload() { }
    // ### VOLATILE SIGNATURES
    // Signatures may be changed in the future depending on the expected parameters of an event.
    public virtual void OnTargetsSpotted(List<Tank> tanksSpotted) { }
    public virtual void OnTakeDamage() { }
    // ### 
    internal void SetupBackend() {
        // internal name will always be english. because yes.
        TierType = TankID.Collection.ForcefullyInsert(Name.GetLocalizedString(LangCode.English), TankID.Collection.Count);
    }

    internal void SpawnInternal(Vector2 position) {
        /*var tnk = new AITank(TierType, default, false);
        tnk.Position = position;
        tnk.AiParams = AiParameters;
        tnk.Properties = Properties;*/
    }

    internal void Unload() {
        // think about what to do here lol.
    }
}
public class Poop : ModTank {
    public override LocalizedString Name => new(new() {
        [LangCode.English] = "Poop",
        [LangCode.Spanish] = "Dulce"
    });
}
