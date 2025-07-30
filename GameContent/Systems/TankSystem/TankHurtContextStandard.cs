using TanksRebirth.GameContent.Systems.TankSystem;

namespace TanksRebirth.GameContent;

public struct TankHurtContextStandard(Tank? source) : ITankHurtContext {
    public readonly Tank? Source => source;
}