namespace TanksRebirth.GameContent.Tanks;

public struct TankHurtContextStandard(Tank? source) : ITankHurtContext {
    public readonly Tank? Source => source;
}