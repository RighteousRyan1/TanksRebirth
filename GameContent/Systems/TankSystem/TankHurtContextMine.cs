namespace TanksRebirth.GameContent;

public struct TankHurtContextMine : ITankHurtContext {
    public bool IsPlayer { get; set; }
    public Explosion MineExplosion { get; set; }

    public TankHurtContextMine(bool isPlayer, Explosion mineExplosion) {
        IsPlayer = isPlayer;
        MineExplosion = mineExplosion;
    }
}