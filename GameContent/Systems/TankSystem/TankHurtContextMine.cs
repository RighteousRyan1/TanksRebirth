namespace TanksRebirth.GameContent;

public struct TankHurtContextMine(Explosion mineExplosion) : ITankHurtContext {
    public bool IsPlayer { get; } = mineExplosion.Owner is not null && mineExplosion.Owner is PlayerTank;
    public Explosion MineExplosion { get; set; } = mineExplosion;
}