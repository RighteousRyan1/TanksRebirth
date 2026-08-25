using Microsoft.Xna.Framework;

namespace TanksRebirth.Internals.Common.Framework.Input;

public enum LocalAimSource { Mouse, Direction }

public readonly record struct LocalPlayerInputFrame(
    Vector2 Movement,
    Vector2 Aim,
    LocalAimSource AimSource,
    bool FireJustPressed,
    bool MineJustPressed,
    bool ShotPathHeld);
