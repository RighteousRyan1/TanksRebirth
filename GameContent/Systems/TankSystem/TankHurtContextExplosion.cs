namespace TanksRebirth.GameContent;

public struct TankHurtContextExplosion(Explosion mineExplosion) : ITankHurtContext {
    public readonly Tank? Source => mineExplosion is not null && mineExplosion.Owner is not null ? mineExplosion.Owner : null;
    public readonly Explosion Explosion => mineExplosion;
}