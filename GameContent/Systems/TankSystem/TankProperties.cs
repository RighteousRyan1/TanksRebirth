using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent;

public class TankProperties {
    /// <summary>Whether or not the tank has artillery-like function during gameplay.</summary>
    public bool Stationary { get; set; }

    /// <summary>Whether or not the tank should become invisible at mission start.</summary>
    public bool Invisible { get; set; }

    /// <summary>How fast the tank should accelerate towards its <see cref="MaxSpeed"/>.</summary>
    public float Acceleration { get; set; } = 0.6f;

    /// <summary>How fast the tank should decelerate when not moving.</summary>
    public float Deceleration { get; set; } = 0.3f;

    /// <summary>The maximum speed this tank can achieve.</summary>
    public float MaxSpeed { get; set; }

    /// <summary>How fast the bullets this <see cref="Tank"/> shoot are.</summary>
    public float ShellSpeed { get; set; }

    /// <summary>The volume of the footprint placement sounds.</summary>
    public float TreadVolume { get; set; }

    /// <summary>The pitch of the footprint placement sounds.</summary>
    public float TreadPitch { get; set; }

    /// <summary>The pitch of the shoot sound.</summary>
    public float ShootPitch { get; set; }

    /// <summary>The type of bullet this <see cref="Tank"/> shoots.</summary>
    public int ShellType { get; set; }

    /// <summary>The maximum amount of mines this <see cref="Tank"/> can place.</summary>
    public uint MineLimit { get; set; }

    /// <summary>How long this <see cref="Tank"/> will be immobile upon firing a bullet.</summary>
    public uint ShootStun { get; set; }

    /// <summary>How long this <see cref="Tank"/> will be immobile upon laying a mine.</summary>
    public uint MineStun { get; set; }

    /// <summary>How long this <see cref="Tank"/> has to wait until it can fire another bullet.</summary>
    public uint ShellCooldown { get; set; }

    /// <summary>How long until this <see cref="Tank"/> can lay another mine</summary>
    public uint MineCooldown { get; set; }

    /// <summary>How many times the <see cref="Shell"/> this <see cref="Tank"/> shoots can ricochet.</summary>
    public uint RicochetCount { get; set; }

    /// <summary>How many <see cref="Shell"/>s this <see cref="Tank"/> can own at any given time.</summary>
    public int ShellLimit { get; set; }

    /// <summary>How fast this <see cref="Tank"/> turns.</summary>
    public float TurningSpeed { get; set; }

    /// <summary>The maximum angle this <see cref="Tank"/> can turn (in radians) before it has to start pivoting.</summary>
    public float MaximalTurn { get; set; }

    /// <summary>Whether or not this <see cref="Tank"/> can lay a <see cref="TankFootprint"/>.</summary>
    public bool CanLayTread { get; set; } = true;

    /// <summary>Whether or not this <see cref="Tank"/> makes sounds while moving.</summary>
    public bool IsSilent { get; set; }

    /// <summary>The type of track that is laid.</summary>
    public int TrackType { get; set; }

    /// <summary>If <see cref="ShellShootCount"/> is greater than 1, this is how many radians each shot's offset will be when this <see cref="Tank"/> shoots.
    /// <para></para>
    /// A common formula to calculate values for when the bullets won't instantly collide is:
    /// <para></para>
    /// <c>(ShellShootCount / 12) - 0.05</c>
    /// <para></para>
    /// A table:
    /// <para></para>
    /// 3 = 0.3
    /// <para></para>
    /// 5 = 0.4
    /// <para></para>
    /// 7 = 0.65
    /// <para></para>
    /// 9 = 0.8
    /// </summary>
    public float ShellSpread { get; set; } = 0f;

    /// <summary>How many <see cref="Shell"/>s this <see cref="Tank"/> fires upon shooting in a spread.</summary>
    public int ShellShootCount { get; set; } = 1;

    /// <summary>The color of particle <see cref="Tank"/> emits upon destruction.</summary>
    public Color DestructionColor { get; set; } = Color.Black;

    /// <summary>The armor properties this <see cref="Tank"/> has.</summary>
    public Armor Armor { get; set; } = null;

    // Get it working before using this.
    /// <summary>How much this <see cref="Tank"/> is launched backward after firing a shell.</summary>
    public float Recoil { get; set; } = 0f;

    /// <summary>Whether or not this <see cref="Tank"/> has a turret to fire shells with.</summary>
    public bool HasTurret { get; set; } = true;

    /// <summary>Whether or not this <see cref="Tank"/> is able to be destroyed by <see cref="Mine"/>s.</summary>
    public bool VulnerableToMines { get; set; } = true;

    /// <summary>Whether or not this <see cref="Tank"/> is unable to be destroyed.</summary>
    public bool Immortal { get; set; } = false;

    /// <summary>The homing properties of the shells this <see cref="Tank"/> shoots.</summary>
    public Shell.HomingProperties ShellHoming = new();
}