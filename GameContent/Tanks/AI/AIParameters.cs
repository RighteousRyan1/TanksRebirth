using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;


namespace TanksRebirth.GameContent.Tanks.AI;

// ref struct at some point
public class AIParameters {
    /// <summary>The amount of distance to check (in units) in all directions for obstacles to ensure laying a mine is safe. Word 2.</summary>
    public float ObstacleAwarenessMine { get; set; }
    /// <summary>The maximum amount of time before this <see cref="AITank"/> can lay a mine. Word 4.</summary>
    public int RandomTimerMaxMine { get; set; } = 2;
    /// <summary>The minimum amount of time before this <see cref="AITank"/> can lay a mine. Word 5.</summary>
    public int RandomTimerMinMine { get; set; } = 1;
    /// <summary>The amount of distance to check (in units) in all directions for friendly tanks to ensure laying a mine is safe. Word 6.</summary>
    public float TankAwarenessMine { get; set; }
    /// <summary>The value to override <see cref="ChanceMineLay"/> with while this <see cref="AITank"/> is near breakables. Word 7.</summary>
    public float ChanceMineLayNearBreakables { get; set; }
    /// <summary>On a given tick, it has this chance out of 1 to lay a mine. 
    /// <br></br>Do note that this value must be between 0 and 1. Word 8.</summary>
    public float ChanceMineLay { get; set; }
    /// <summary>The max angle of which this tank will change its movement direction. Word 13.</summary>
    public float MaxAngleRandomTurn { get; set; }
    /// <summary>The maximum amount of time before updating the tank's <see cref="Tank.DesiredChassisRotation"/> from 0 to <see cref="MaxAngleRandomTurn"/> radians. Word 14.</summary>
    public int RandomTimerMaxMove { get; set; } = 2;
    /// <summary>The minimum amount of time before updating the tank's <see cref="Tank.DesiredChassisRotation"/> from 0 to <see cref="MaxAngleRandomTurn"/> radians. Word 15.</summary>
    public int RandomTimerMinMove { get; set; } = 1;
    /// <summary>The distance of which this tank is wary of mines laid by allies and tries to move away from them. Word 16.</summary>
    public float AwarenessFriendlyMine { get; set; }
    /// <summary>The distance of which this tank is wary of projectiles shot by allies and tries to move away from them. Word 17.</summary>
    public float AwarenessFriendlyShell { get; set; }
    /// <summary>The distance of which this tank is wary of mines laid by enemies and tries to move away from them. Word 18.</summary>
    public float AwarenessHostileMine { get; set; }
    /// <summary>The distance of which this tank is wary of projectiles shot by enemies and tries to move away from them. Word 19.</summary>
    public float AwarenessHostileShell { get; set; }
    /// <summary>Determines whether or not this <see cref="AITank"/> can attack (shoot, lay a mine) while fleeing from an <see cref="IAITankDanger"/>. 
    /// <br></br>False means it can shoot, true means it can't. Word 20.</summary>
    public bool CantShootWhileFleeing { get; set; }
    /// <summary>Determines how much this tank will move in attempt to get closer to its target. 
    /// <br></br>Values reach absurdity under -1 or above 1. Word 21.</summary>
    public float AggressivenessBias { get; set; }
    /// <summary>The number of recent movement history slots available to average into movements. Word 22.</summary>
    // note: all tanks use this value at 4
    public int MaxMoveHistory { get; set; } = 4;
    /// <summary>How many frames of forward prediction this tank uses to navigate around obstacles. Word 28.</summary>
    public uint ObstacleAwarenessMovement { get; set; }
    /// <summary>How inaccurate (in radians) this tank is trying to aim at its target. Word 29.</summary>
    public float AimOffset { get; set; }
    /// <summary>The angle of the cone used to check whether the <see cref="AITank"/> would hit itself. Only checked on the path after the first ricochet. Word 31.</summary>
    public float DetectionForgivenessSelf { get; set; }
    /// <summary>The angle in which firing is denied if a friendly tank is within this angle of the shot path. A friendly very close to the path is also caught regardless of angle. Word 32.</summary>
    public float DetectionForgivenessFriendly { get; set; }
    /// <summary>The angle in which an enemy tank must be within the main shot path before it is allowed to fire.
    /// <br></br>If the tank detects itself in the <see cref="DetectionForgivenessSelf"/> cone or a friendly in the <see cref="DetectionForgivenessFriendly"/> cone,
    /// this tank will still not fire. Word 33.</summary>
    public float DetectionForgivenessHostile { get; set; }
    /// <summary>The maximum amount of time before this <see cref="AITank"/> shoot a <see cref="Shell"/>. Word 35.</summary>
    public int RandomTimerMaxShoot { get; set; } = 2;
    /// <summary>The minimum amount of time before this <see cref="AITank"/> shoot a <see cref="Shell"/>. Word 36.</summary>
    public int RandomTimerMinShoot { get; set; } = 1;
    /// <summary>How fast this tank's turret rotates towards its target. Word 39.</summary>
    public float TurretSpeed { get; set; }
    /// <summary>How often this tank will move its turret in the target's direction. It will be inaccurate at the measure of <see cref="AimOffset"/>. Word 40.</summary>
    public uint TurretMovementTimer { get; set; }
    /// <summary>How far this tank must be from a friendly <see cref="Tank"/> before it can shoot. Word 41.</summary>
    public float TankAwarenessShoot { get; set; }


    // ### SEPARATION BETWEEN ORIGINAL PARAMETERS AND REBIRTH-EXCLUSIVE PARAMETERS ###


    /// <summary>At each step of a forwards scan, scan this many units around that step in a circle for an obstacle.</summary>
    /// <remarks>Smaller values will make the tank more prone to enter tight spaces, larger values will make it desire more open areas.<para></para>
    /// This value is always 16 in the original game.</remarks>
    public float SweepScanRadius { get; set; } = GameUtils.WALL_SWEEP_DEF_RADIUS;
    /// <summary>How long this <see cref="AITank"/> remembers that an <see cref="TankProperties.Invisible"/> tank acted for. This affects how long it will target the invisible tank after an action.
    /// <br></br>This parameter is measured in ticks, so 60 (default) = 1 second.</summary>
    public uint Rememberance { get; set; } = 60;
    /// <summary>Whether or not this tank tries to find calculations all around it. This is not recommended for mobile tanks.</summary>
    public bool SmartRicochets { get; set; }

    /// <summary>Whether or not this tank's shot raycast resets it's distance check per-bounce.</summary>
    public bool BounceReset { get; set; } = true;

    /// <summary>Whether or not this tank predics the future position of its target.</summary>
    public bool PredictsPositions { get; set; }

    /// <summary>Whether or not this <see cref="AITank"/> should deflect incoming bullets to prevent being hit.</summary>
    public bool DeflectsBullets { get; set; }

    /// <summary>Whether or not this <see cref="AITank"/> will shoot mines that are near destructible obstacles.</summary>
    public bool ShootsMinesSmartly { get; set; } // WIP
    /// <summary>If true, this tank will target the tank that is actively firing at it rather than the closest, if applicable.</summary>
    public bool SmartTargeting { get; set; }
    /// <summary>The 'base' experience value the player gains killing this tank.</summary>
    public float BaseXP { get; set; }

    public AIParameters Clone() => (AIParameters)MemberwiseClone();
}