namespace TanksRebirth.GameContent;

public struct TankHurtContextShell(Shell shell) : ITankHurtContext {
    public bool IsPlayer { get; } = shell.Owner is not null && shell.Owner is PlayerTank;
    public Shell Shell { get; set; } = shell;
}