namespace TanksRebirth.GameContent;

public struct TankHurtContextOther : ITankHurtContext {
    public enum HurtContext {
        FromIngame,
        FromOther
    }

    public HurtContext Context { get; set; }
    public bool IsPlayer { get; set; }
    public int TankId { get; set; }

    public string Reason { get; }

    public TankHurtContextOther(HurtContext cxt) {
        Reason = string.Empty;
        Context = cxt;
        IsPlayer = false;
        TankId = -1;
    }

    public TankHurtContextOther(string reason) {
        Reason = reason;
        Context = HurtContext.FromOther;
        IsPlayer = false;
        TankId = -1;
    }
}