using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent;

public record AiParameters {
    /// <summary>The max angle of which this tank will "meander," or change its movement direction.</summary>
    public float MeanderAngle { get; set; }

    /// <summary>How often this tank will take a turn at <see cref="MeanderAngle"/> radians.</summary>
    public int MeanderFrequency { get; set; }

    /// <summary>Determines how much this tank will move in attempt to get closer to its target. Keep value between -1 and 1.<para></para>
    /// This value does not directly translate from Wii Tanks. 0.1 in Wii Tanks = ~0.5 in Tanks Rebirth.</summary>
    public float PursuitLevel { get; set; }

    /// <summary>Determines how often this tank will try to move towards its target.</summary>
    public int PursuitFrequency { get; set; }

    /// <summary>How often this tank will move its turret in the target's direction. It will be inaccurate at the measure of <see cref="AimOffset"/>.</summary>
    public int TurretMeanderFrequency { get; set; }

    /// <summary>How fast this tank's turret rotates towards its target.</summary>
    public float TurretSpeed { get; set; }

    /// <summary>How inaccurate (in radians) this tank is trying to aim at its target.</summary>
    public float AimOffset { get; set; }

    /// <summary>The distance of which this tank is wary of projectiles shot by <see cref="PlayerTank"/>s and tries to move away from them.</summary>
    public float ProjectileWarinessRadius_PlayerShot { get; set; }

    /// <summary>The distance of which this tank is wary of projectiles shot by <see cref="AITank"/>s and tries to move away from them.</summary>
    public float ProjectileWarinessRadius_AIShot { get; set; }

    /// <summary>The distance of which this tank is wary of mines laid by <see cref="PlayerTank"/>s and tries to move away from them.</summary>
    public float MineWarinessRadius_PlayerLaid { get; set; }

    /// <summary>The distance of which this tank is wary of mines laid by <see cref="AITank"/>s and tries to move away from them.</summary>
    public float MineWarinessRadius_AILaid { get; set; }

    /// <summary>On a given tick, it has this chance out of 1 to lay a mine. <para>Do note that this value must be greater than 0 and less than or equal to 1.</para></summary>
    public float MinePlacementChance { get; set; } // 0.0f to 1.0f

    /// <summary>The distance from the main shot calculation ray an enemy must be before this tank is allowed to fire.</summary>
    public float Inaccuracy { get; set; }

    /// <summary>How often this tank shoots when given the opportunity. 0 to 1 values only. Defaults to 1.</summary>
    public float ShootChance { get; set; } = 1f;

    /// <summary>How far ahead of this tank (in the direction the tank is going) that it is aware of obstacles and navigates around them.</summary>
    public int BlockWarinessDistance { get; set; } = 50;

    /// <summary>How often this tank reads the obstacles around it and navigates around them.</summary>
    public int BlockReadTime { get; set; } = 1;

    /// <summary>How far this tank must be from a teammate before it can lay a mine or fire a bullet.</summary>
    public float TankWarinessRadius { get; set; } = 50f;

    /// <summary>Whether or not this tank tries to find calculations all around it. This is not recommended for mobile tanks.</summary>
    public bool SmartRicochets { get; set; }

    /// <summary>Whether or not this tank attempts to lay mines near destructible obstacles rather than randomly. Useless for stationary tanks.</summary>
    public bool SmartMineLaying { get; set; }

    /// <summary>Whether or not this tank's shot raycast resets it's distance check per-bounce.</summary>
    public bool BounceReset { get; set; } = true;

    /// <summary>When this tank finds a wall in its path, it moves away at this angle every <see cref="BlockReadTime"/> ticks.</summary>
    public float RedirectAngle { get; set; } = MathHelper.ToRadians(5);

    /// <summary>Whether or not this tank predics the future position of its target.</summary>
    public bool PredictsPositions { get; set; }

    /// <summary>Whether or not this <see cref="AITank"/> should deflect incoming bullets to prevent being hit.</summary>
    public bool DeflectsBullets { get; set; }

    /// <summary>Whether or not this <see cref="AITank"/> will shoot mines that are near destructible obstacles.</summary>
    public bool ShootsMinesSmartly { get; set; }

    // TODO: make friendly check distances separate for bullets and mines
}