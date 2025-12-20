namespace TanksRebirth.GameContent.Systems.TankSystem;

public struct TankHurtContextStandard(Tank? source) : ITankHurtContext {
    public readonly Tank? Source => source;
}