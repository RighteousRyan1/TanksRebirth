using Microsoft.Xna.Framework;
using static TanksRebirth.GameContent.Shell;

namespace TanksRebirth.GameContent;

public class ShellProperties
{
    /// <summary>Whether or not this shell should emit flames from behind it.</summary>
    public bool Flaming { get; set; }
    /// <summary>The color of the flame particles emitted by this <see cref="Shell"/> when <see cref="Flaming"/> is true.</summary>
    public Color FlameColor { get; set; } = Color.Orange;
    /// <summary>Whether or not this <see cref="Shell"/> should emit a blazing trail.</summary>
    public bool LeavesTrail { get; set; }
    /// <summary>The color of the blazing trail emitted when <see cref="LeavesTrail"/> is true.</summary>
    public Color TrailColor { get; set; } = Color.Gray;
    /// <summary>Whether or not this <see cref="Shell"/> emits smoke puffs.</summary>
    public bool EmitsSmoke { get; set; } = true;
    /// <summary>The color of the smoke puffs left by this <see cref="Shell"/> when <see cref="EmitsSmoke"/> is true.</summary>
    public Color SmokeColor { get; set; } = new Color(255, 255, 255, 255);
    /// <summary>Whether or not other <see cref="Shell"/>s can destroy this one.</summary>
    public bool IsDestructible { get; set; } = true;
    /// <summary>Whether or not this <see cref="Shell"/> can hit friendlies. Defaults to true.</summary>
    public bool CanFriendlyFire { get; set; } = true;
    /// <summary>The amount of times this bullet can penetrate other ones. A value of -1 will penetrate infinitely.</summary>
    public int Penetration;
    /// <summary>The homing properties of this <see cref="Shell"/>.</summary>
    public HomingProperties HomeProperties = default;
    /// <summary>Maximum amount of times this <see cref="Shell"/> can bounce off walls.</summary>
    public uint Ricochets { get; set; }
}
