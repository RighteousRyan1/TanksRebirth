using static TanksRebirth.GameContent.Shell;
using Microsoft.Xna.Framework;
using System;

namespace TanksRebirth.GameContent;

[Flags]
public enum VisualFlags : byte {
    /// <summary>Whether or not to emit smoke puffs.</summary>
    SmokePuff  = 1 << 0,
    /// <summary>Whether or not to emit flames from behind it.</summary>
    Flaming    = 1 << 1,
    /// <summary>Whether or not to emit a blazing trail.</summary>
    SmokeTrail = 1 << 2,
}

public struct ShellProperties() {
    /// <summary>The visual attributes of this <see cref="Shell"/>. Defaults to <see cref="VisualFlags.SmokePuff"/>.</summary>
    public VisualFlags Visuals { get; set; } = VisualFlags.SmokePuff;
    /// <summary>The color of the flame particles emitted when <see cref="Visuals"/> has the <see cref="VisualFlags.SmokePuff"/> flag.</summary>
    public Color FlameColor { get; set; } = Color.Orange;
    /// <summary>The color of the blazing trail emitted when <see cref="Visuals"/> has the <see cref="VisualFlags.SmokeTrail"/> flag.</summary>
    public Color TrailColor { get; set; } = Color.Gray;
    /// <summary>The color of the smoke puffs left when <see cref="Visuals"/> has the <see cref="VisualFlags.SmokePuff"/> flag.</summary>
    public Color SmokeColor { get; set; } = new Color(255, 255, 255, 255);
    /// <summary>Whether or not this <see cref="Shell"/> can hit friendlies. Defaults to <see langword="true"/>.</summary>
    public bool CanFriendlyFire { get; set; } = true;
    /// <summary>The amount of times this bullet can penetrate other ones. A value of -1 will penetrate infinitely.</summary>
    public int Penetration { get; set; }
    /// <summary>The homing properties of this <see cref="Shell"/>.</summary>
    public HomingProperties Homing = default;
}
