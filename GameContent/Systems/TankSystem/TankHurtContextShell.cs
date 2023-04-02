namespace TanksRebirth.GameContent;

public struct TankHurtContextShell : ITankHurtContext {
    public bool IsPlayer { get; set; }
    public uint Bounces { get; set; }

    public int ShellType { get; set; }

    public Shell Shell { get; set; }

    public TankHurtContextShell(bool isPlayer, uint bounces, int type, Shell shell) {
        IsPlayer = isPlayer;
        Bounces = bounces;
        ShellType = type;
        Shell = shell;
    }
}