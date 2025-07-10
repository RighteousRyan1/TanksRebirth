namespace TanksRebirth.GameContent;

public readonly struct TankHurtContextOther(Tank? source, TankHurtContextOther.HurtContext cxt, string reason) : ITankHurtContext {
    public enum HurtContext {
        FromIngame,
        FromOther
    }
    public Tank? Source { get; } = source;
    public HurtContext Context { get; } = cxt;
    public string Reason { get; } = reason;
}