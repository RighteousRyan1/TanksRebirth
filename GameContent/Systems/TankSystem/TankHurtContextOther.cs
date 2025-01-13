namespace TanksRebirth.GameContent;

public struct TankHurtContextOther(bool isPlayer, TankHurtContextOther.HurtContext cxt, string reason, object data) : ITankHurtContext {
    public enum HurtContext {
        FromIngame,
        FromOther
    }
    public bool IsPlayer { get; } = isPlayer;
    public HurtContext Context { get; } = cxt;
    public string Reason { get; } = reason;
    public object Data { get; } = data;
}