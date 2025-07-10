namespace TanksRebirth.GameContent; // need i even rename this?

public struct TankHurtContextShell(Shell shell) : ITankHurtContext {
    public readonly Tank? Source => shell is not null && shell.Owner is not null ? Shell.Owner : null;
    public readonly Shell Shell => shell;
}