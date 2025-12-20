using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Framework.Collisions;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.AI;
public partial class AITank {
    bool _predicts;
    bool _isSeeking;
    float _seekRotation;

    public bool DoAttack = true;

    public Tank? TargetTank;
    public float TurretRotationMultiplier = 1f;
    public bool IsEnemySpotted;

    public int CurrentRandomMineLay;
    public int CurrentRandomShoot;

    public float ObstacleAwarenessMineReal;

    /// <summary>The location(s) of which this tank's shot path hits an obstacle.</summary>
    public Vector2[] ShotPathRicochetPoints { get; private set; } = [];
    /// <summary>The location(s) of which this tank's shot path hits an tank.</summary>
    public Vector2[] ShotPathTankCollPoints { get; private set; } = [];

    readonly List<Tank> _tanksInPathBuffer = [];
    readonly List<Vector2> _ricochetPointsBuffer = [];
    readonly List<Vector2> _tankPointsBuffer = [];
    /// <summary>Updates turret directions/targets and updates tanks in the shoot path.</summary>
    public void HandleTurret() {
        TargetTurretRotation %= MathHelper.TwoPi;

        TurretRotation %= MathHelper.TwoPi;

        var diff = TargetTurretRotation - TurretRotation;
        if (diff > MathHelper.Pi)
            TargetTurretRotation -= MathHelper.TwoPi;
        else if (diff < -MathHelper.Pi)
            TargetTurretRotation += MathHelper.TwoPi;

        TurretRotation = MathUtils.RoughStep(TurretRotation, TargetTurretRotation, Parameters.TurretSpeed * TurretRotationMultiplier * RuntimeData.DeltaTime);

        // update things prior to timer check
        if (TargetTank is null) return;
        if (!_isSeeking && !_predicts) {
            IsEnemySpotted = false;
            if (TargetTank!.Properties.Invisible && TargetTank.TimeSinceLastAction < Parameters.Rememberance) {
                AimTarget = TargetTank.Position;
                IsEnemySpotted = true;
            }

            if (!TargetTank.Properties.Invisible) {
                AimTarget = TargetTank.Position;
                IsEnemySpotted = true;
            }
        }
        UpdateAim();

        if (!Behaviors[1].IsModOf(Parameters.TurretMovementTimer)) return;

        var dirVec = Position - AimTarget;
        TargetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + Client.ClientRandom.NextFloat(-Parameters.AimOffset, Parameters.AimOffset);
    }
    /// <summary>Attempts to lay a mine based on various conditions and environmental factors.</summary>
    /// <remarks>This method evaluates multiple conditions to determine whether a mine can be laid, including:
    /// <list type="bullet"> <item>Whether the tank is capable of laying mines.</item> <item>Whether the mine limit has
    /// been reached.</item> <item>Proximity to friendly tanks to avoid friendly fire.</item> <item>Presence of
    /// destructible obstacles nearby, which may influence the chance of laying a mine.</item> <item>Availability of
    /// safe directions to lay a mine, avoiding existing mines and obstacles.</item> </list> If all conditions are met,
    /// a mine is laid, and the tank selects a direction to flee.</remarks>
    public void TryMineLay() {
        // don't even bother if the tank can't lay mines
        if (!Behaviors[3].IsModOf(CurrentRandomMineLay)) return;

        // set our new random window, this gets set
        Behaviors[3].Value = 0;
        CurrentRandomMineLay = Client.ClientRandom.Next(Parameters.RandomTimerMinMine, Parameters.RandomTimerMaxMine);

        if (Properties.MineLimit <= 0) return;
        if (IsSurviving) return;

        // check for friendly tanks nearby, if there are any, don't even attempt to lay a mine
        for (int i = 0; i < TanksNearMineAwareness.Count; i++) {
            var tank = TanksNearMineAwareness[i];
            if (tank.IsOnSameTeamAs(Team))
                return;
        }

        bool nearDestructible = false;

        // call me the wizard of oz
        ObstacleAwarenessMineReal = 3 * Parameters.ObstacleAwarenessMine;

        var dist = ObstacleAwarenessMineReal / 2;
        var dirs = RayCastCardinals(dist, (fixture, point, normal, fraction) => {
            if (fixture.Body.Tag is Block b) {
                if (!nearDestructible)
                    nearDestructible = b.Properties.IsDestructible;

                // b.Stack = (byte)Client.ClientRandom.Next(1, 8);
            }
            return fraction;

            // this wizardry goes beyond me
        }, ChassisRotation - MathHelper.Pi);

        int goodDirsCount = 0;
        for (int i = 0; i < dirs.Length; i++) {
            if (dirs[i].Direction != CollisionDirection.None) {
                goodDirsCount++;
            }
        }

        if (goodDirsCount == 0) return;

        // Check for mines contamination
        // This loop is O(4 * Mines), usually small.
        bool isMineLayOk = false;
        var validIndex = -1;

        for (int i = 0; i < dirs.Length; i++) {
            if (dirs[i].Direction == CollisionDirection.None) continue;

            var pos = dirs[i].Vec * dist;
            bool contaminated = false;

            for (int j = 0; j < Mine.AllMines.Length; j++) {
                var mine = Mine.AllMines[j];
                if (mine is null) continue;

                if (GameUtils.Distance_WiiTanksUnits(pos, mine.Position) <= mine.ExplosionRadius * GameUtils.Value_WiiTanksUnits(70)) {
                    contaminated = true;
                    break;
                }
            }

            if (!contaminated) {
                isMineLayOk = true;
                validIndex = i; // Just pick the last valid one for now or random logic below
            }
            else {
                dirs[i].Direction = CollisionDirection.None;
                goodDirsCount--;
            }
        }

        /*var goodDirs = dirs.Where(x => x.Direction != CollisionDirection.None).ToArray();

        for (int i = 0; i < goodDirs.Length; i++) {
            var pos = goodDirs[i].Vec * dist;
            for (int j = 0; j < Mine.AllMines.Length; j++) {
                var mine = Mine.AllMines[j];
                if (mine is null) continue;

                // check against radius. ensures the tank doesn't move towards an already laid mine
                // 70 is the magic number for the default mine radius, multiplied by the scalar
                if (GameUtils.Distance_WiiTanksUnits(pos, mine.Position) <= mine.ExplosionRadius * GameUtils.Value_WiiTanksUnits(70)) {
                    // Console.WriteLine("Direction " + dirs[i].Direction + " is contaminated");
                    goodDirs[i].Direction = CollisionDirection.None;
                }
            }
        }*/

        /*Console.WriteLine();
        Console.WriteLine($"Opportunity: " +
            $"\nIsOk:             {isMineLayOk}" +
            $"\nDirsNoObstacle:   {string.Join(", ", goodDirs.Select(x => x.Direction))}" +
            $"\nNearDestructible: {nearDestructible}" +
            $"\nNewOpportunity:   {CurrentRandomMineLay}");*/

        //Console.WriteLine(isMineLayOk ? $"Mine-lay is ok! ({string.Join(", ", dirs.Where(x => x.Direction != CollisionDirection.None).Select(x => x.Direction))})" : "Mine-lay is not ok.");

        // don't lay a mine if the checks fail
        if (!isMineLayOk) return;

        // SmartMineLaying was removed in favor
        // attempt via an opportunity to lay a mine
        var random = Client.ClientRandom.NextFloat(0, 1);

        // change chance based on whether or not the tank is near a destructible obstacle
        var randomSuccess = random <= (nearDestructible ? Parameters.ChanceMineLayNearBreakables : Parameters.ChanceMineLay);

        if (!randomSuccess) return;

        // do not hurt the worker thread plskthx
        TankGame.MainThreadTasks.Enqueue(LayMine);

        // Pick a random valid direction
        int skips = Client.ClientRandom.Next(0, goodDirsCount);
        for (int i = 0; i < dirs.Length; i++) {
            if (dirs[i].Direction != CollisionDirection.None) {
                if (skips == 0) {
                    var rot = dirs[i].Vec.ToRotation();
                    DesiredChassisRotation = rot - MathHelper.PiOver2;
                    break;
                }
                skips--;
            }
        }
    }
    // TODO: make view distance, and make tanks in path public
    /// <summary>Updates meta-data related to aiming and shooting. The tank will not fire if <see cref="DoAttack"/> is false, but meta-data will still update.</summary>
    public void UpdateAim() {
        _predicts = false;
        SeesTarget = false;

        bool tooCloseToExplosiveShell = false;

        bool friendliesNearby = false;
        for (int i = 0; i < TanksNearShootAwareness.Count; i++) {
            var tank = TanksNearShootAwareness[i];
            if (IsOnSameTeamAs(tank.Team)) {
                friendliesNearby = true;
                break; // early exit like LINQ does
            }
        }
        // stop doing expensive checks if the tank can't even shoot anyway
        if (friendliesNearby) return;

        List<Tank> tanksDef;

        var turretDir = Vector2.UnitY.RotatedBy(TurretRotation - MathHelper.Pi);
        if (Properties.ShellType == ShellID.Explosive) {
            tanksDef = GetTanksInPath(turretDir, out var ricP, out var tnkCol, offset: Vector2.UnitY * 20, pattern: x => !x.Properties.IsDestructible && x.Properties.IsSolid || x.Type == BlockID.Teleporter, missDist: Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);
            if (ricP.Length > 0 && GameUtils.Distance_WiiTanksUnits(ricP[^1], Position) < 150f)
                tooCloseToExplosiveShell = true;
        }
        else {
            tanksDef = GetTanksInPath(
                turretDir,
                out var ricP, out var tnkCol, offset: Vector2.UnitY * 20,
                missDist: Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);

            if (tanksDef.Count != TanksSpotted.Length)
                TanksSpotted = [.. tanksDef]; // unavoidable alloc if size changes, but cheaper than constant realloc
            else
                tanksDef.CopyTo(TanksSpotted);

            ShotPathRicochetPoints = ricP;
            ShotPathTankCollPoints = tnkCol;
        }
        if (Parameters.PredictsPositions) {
            if (TargetTank is not null) {
                var calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);
                float rot = -Position.DirectionTo(GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation))
                    .ToRotation() - MathHelper.PiOver2;

                tanksDef = GetTanksInPath(
                Vector2.UnitY.RotatedBy(-Position.DirectionTo(TargetTank.Position).ToRotation() - MathHelper.PiOver2),
                out var ricP, out var tnkCol, offset: Parameters.PredictsPositions ? Vector2.Zero : Vector2.UnitY * 20,
                missDist: Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);

                var targ = GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation);
                var posPredict = GetTanksInPath(Vector2.UnitY.RotatedBy(rot),
                    out var ricP1, out var tnkCol2, offset: Vector2.UnitY * 20, missDist: Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);

                if (tanksDef.Contains(TargetTank)) {
                    _predicts = true;
                    TargetTurretRotation = rot + MathHelper.Pi;
                }
            }
        }

        // TODO: is findsSelf even necessary? findsEnemy is only true if findsSelf is false. eh, whatever. my brain is fucked.
        // old linq
        //var findsEnemy = tanksDef.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
        //var findsSelf = tanksDef.Any(tnk => tnk is not null && tnk == this);
        //var findsFriendly = tanksDef.Any(tnk => tnk is not null && tnk.Team == Team && tnk.Team != TeamID.NoTeam);

        bool findsEnemy = false;
        bool findsSelf = false;
        bool findsFriendly = false;

        if (findsEnemy && !tooCloseToExplosiveShell)
            SeesTarget = true;

        for (int i = 0; i < tanksDef.Count; i++) {
            var tnk = tanksDef[i];
            if (tnk == null) continue;
            if (tnk.Team != Team || tnk.Team == TeamID.NoTeam) {
                if (tnk != this) findsEnemy = true;
            }
            if (tnk == this) findsSelf = true;
            if (tnk.Team == Team && tnk.Team != TeamID.NoTeam) findsFriendly = true;
        }

        if (findsEnemy && !tooCloseToExplosiveShell)
            SeesTarget = true;

        if (Parameters.SmartRicochets) {
            //if (!seeks)
            _seekRotation += Parameters.TurretSpeed * 0.25f;
            var canShoot = !(CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit);
            if (canShoot) {
                var tanks = GetTanksInPath(Vector2.UnitY.RotatedBy(_seekRotation), out var ricP, out var tnkCol, false, default, Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);

                var findsEnemy2 = tanks.Any(tnk => tnk is not null && !tnk.IsOnSameTeamAs(Team) && tnk != this);
                // var findsSelf2 = tanks.Any(tnk => tnk is not null && tnk == this);
                // var findsFriendly2 = tanks.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TeamID.NoTeam));
                // ChatSystem.SendMessage($"{findsEnemy2} {findsFriendly2} | seek: {seeks}", Color.White);
                if (findsEnemy2/* && !findsFriendly2*/) {
                    _isSeeking = true;
                    TurretRotationMultiplier = 3f;
                    TargetTurretRotation = _seekRotation - MathHelper.Pi;
                }
            }

            if (TurretRotation == TargetTurretRotation || !canShoot)
                _isSeeking = false;
        }
        // tanks wont shoot when fleeing from a mine
        if (ClosestDanger is Mine)
            if (Parameters.CantShootWhileFleeing)
                return;

        if (!DoAttack) return;
        if (!Behaviors[2].IsModOf(CurrentRandomShoot)) return;
        CurrentRandomShoot = Client.ClientRandom.Next(Parameters.RandomTimerMinShoot, Parameters.RandomTimerMaxShoot);
        Behaviors[2].Value = 0;
        // Console.WriteLine(TanksSpotted.Length);

        // no need to check friendliesNearby because we return earlier in this method if there are any
        if (Parameters.PredictsPositions) {
            if (SeesTarget)
                if (CurShootCooldown <= 0)
                    TankGame.MainThreadTasks.Enqueue(() => Shoot(false));
                    //Shoot(false);
        }
        else {
            if (SeesTarget && !findsSelf && !findsFriendly)
                if (CurShootCooldown <= 0)
                    TankGame.MainThreadTasks.Enqueue(() => Shoot(false));
                    //Shoot(false);
        }
    }
    /// <summary>Gets the a <see cref="Tank"/> that is hostile and is targetable (with respect to <see cref="Parameters"/>).</summary>
    public Tank? GetAppropriateTarget() {
        Tank? target = null;
        float closestDistSq = float.MaxValue;

        if (Parameters.SmartTargeting) {
            var smallestActionTime = float.MaxValue;
            Tank? smallestActionTimeTank = TargetTank;

            foreach (var tank in GameHandler.AllTanks) {
                if (tank is null || tank.IsDestroyed || tank == this || tank.IsOnSameTeamAs(Team)) continue;
                if (tank.TimeSinceLastAction < smallestActionTime) {
                    smallestActionTime = tank.TimeSinceLastAction;
                    smallestActionTimeTank = tank;
                }
            }
            return smallestActionTimeTank;
        }

        foreach (var tank in GameHandler.AllTanks) {
            if (tank is null || tank.IsDestroyed || tank == this || tank.IsOnSameTeamAs(Team)) continue;

            float distSq = Vector2.DistanceSquared(tank.Position, Position);
            if (distSq < closestDistSq) {
                if (tank.Properties.Invisible && tank.TimeSinceLastAction < Parameters.Rememberance || !tank.Properties.Invisible) {
                    target = tank;
                    closestDistSq = distSq;
                }
            }
        }
        return target;
    }
    /// <summary>A method that simply changes an AI tank's target to a pinged tank.</summary>
    public Tank? TryOverrideTarget(out bool overridden) {
        overridden = false;
        Tank? target = TargetTank;

        // this might violate something if there are two teams wiht two players and two ai tanks...
        // it might want to kill their teammate if the other team's player pings them
        bool hasPlayerTeammate = false;

        for (int i = 0; i < GameHandler.AllPlayerTanks.Length; i++) {
            var pl = GameHandler.AllPlayerTanks[i];

            if (pl is null) continue;
            if (pl.IsOnSameTeamAs(Team)) {
                hasPlayerTeammate = true;
                break;
            }
        }

        if (GameHandler.AllPlayerTanks.Any(x => x is not null && x.IsOnSameTeamAs(Team))) {
            foreach (var ping in IngamePing.AllIngamePings) {
                if (ping is null) break;
                if (ping.TrackedTank is null) break;
                if (ping.TrackedTank == this) break; // no self-targeting
                if (ping.TrackedTank.Team == Team) break; // no friendly fire
                target = ping.TrackedTank;
                overridden = true;
                break;
            }
        }
        return target;
    }
    /// <summary>Makes this <see cref="AITank"/> attempt to shoot to destroy the given <see cref="Shell"/>.</summary>
    public void DoDeflection(Shell shell) {
        var calculation = (Position.Distance(shell.Position) - 20f) / (float)(Properties.ShellSpeed * 1.2f);
        float rot = -Position.DirectionTo(GeometryUtils.PredictFuturePosition(shell.Position, shell.Velocity, calculation))
            .ToRotation() + MathHelper.PiOver2;

        TargetTurretRotation = rot;

        TurretRotationMultiplier = 4f;

        // used to be rot %=... was it necessary?
        //TargetTurretRotation %= MathHelper.Tau;

        //if ((-TurretRotation + MathHelper.PiOver2).IsInRangeOf(TargetTurretRotation, 0.15f))

        Shoot(false);
    }

    // TODO: literally fix everything about these turret rotation values.
    List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2[] ricochetPoints, out Vector2[] tankCollPoints,
        bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool>? pattern = null, bool doBounceReset = true) {
        const int MAX_PATH_UNITS = 1000;
        const int PATH_UNIT_LENGTH = 8;

        _tanksInPathBuffer.Clear();
        _ricochetPointsBuffer.Clear();
        _tankPointsBuffer.Clear();

        pattern ??= c => c.Properties.IsSolid || c.Type == BlockID.Teleporter;

        var whitePixel = TextureGlobals.Pixels[Color.White];

        // genuine fucking stupidity as to why this is negative in so many calculations
        Vector2 pathPos = Position + offset.RotatedBy(-TurretRotation);
        pathDir.Y *= -1;
        pathDir *= PATH_UNIT_LENGTH;

        int ricochetCount = 0;
        int uninterruptedIterations = 0;

        bool teleported = false;
        int tpTriggerIndex = -1;
        Vector2 teleportedTo = Vector2.Zero;

        var pathHitbox = new Rectangle();

        for (int i = 0; i < MAX_PATH_UNITS; i++) {
            uninterruptedIterations++;

            // World bounds check
            if (pathPos.X < GameScene.MIN_X || pathPos.X > GameScene.MAX_X) {
                _ricochetPointsBuffer.Add(pathPos);
                pathDir.X *= -1;
                ricochetCount++;
                if (doBounceReset) uninterruptedIterations = 0;
            }
            else if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                _ricochetPointsBuffer.Add(pathPos);
                pathDir.Y *= -1;
                ricochetCount++;
                if (doBounceReset) uninterruptedIterations = 0;
            }

            // setup hitbox
            // path used to be XY - 5, WH = 8
            pathHitbox.X = (int)pathPos.X - Shell.COLL_RECT_DIM / 2;
            pathHitbox.Y = (int)pathPos.Y - Shell.COLL_RECT_DIM / 2;
            pathHitbox.Width = Shell.COLL_RECT_DIM;
            pathHitbox.Height = Shell.COLL_RECT_DIM;

            Vector2 dummy = Vector2.Zero;
            Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummy, out var dir, out var block, out bool corner, false, pattern);
            if (corner) break;

            if (block is not null) {
                if (block.Type == BlockID.Teleporter && !teleported) {
                    var dest = Block.AllBlocks.FirstOrDefault(bl => bl != null && bl != block && bl.TpLink == block.TpLink);
                    if (dest is not null) {
                        teleported = true;
                        teleportedTo = dest.Position;
                        tpTriggerIndex = i + 1;
                    }
                }
                else if (block.Properties.AllowShotPathBounce) {
                    _ricochetPointsBuffer.Add(pathPos);
                    ricochetCount += block.Properties.PathBounceCount;

                    switch (dir) {
                        case CollisionDirection.Up:
                        case CollisionDirection.Down:
                            pathDir.Y *= -1;
                            break;
                        case CollisionDirection.Left:
                        case CollisionDirection.Right:
                            pathDir.X *= -1;
                            break;
                    }

                    if (doBounceReset) uninterruptedIterations = 0;
                }
            }

            // delay teleport until next frame
            if (teleported && i == tpTriggerIndex) {
                pathPos = teleportedTo;
            }

            // check destroy conditions
            bool hitsInstant = i == 0 && Block.AllBlocks.Any(x => x != null && x.Hitbox.Intersects(pathHitbox) && pattern(x));
            bool hitsTooEarly = i < (int)Properties.ShellSpeed / 2 && ricochetCount > 0;
            bool ricochetLimitReached = ricochetCount > Properties.RicochetCount;

            if (hitsInstant || hitsTooEarly || ricochetLimitReached)
                break;

            // check tanks BEFORE moving
            float realMiss = 1f + missDist * 2 * uninterruptedIterations;

            foreach (var enemy in GameHandler.AllTanks) {
                if (enemy is null || enemy.IsDestroyed || _tanksInPathBuffer.Contains(enemy)) continue;

                // 15 is just an eensy weensy magical number.
                if (i > 15 && GameUtils.Distance_WiiTanksUnits(enemy.Position, pathPos) <= realMiss) {
                    var pathAngle = pathDir.ToRotation();
                    var toEnemy = pathPos.DirectionTo(enemy.Position).ToRotation();

                    if (MathUtils.AbsoluteAngleBetween(pathAngle, toEnemy) >= MathHelper.PiOver2)
                        _tanksInPathBuffer.Add(enemy);
                }

                // this used to be a circle check, but it was probably overkill? we'll see
                var closeEnough = GameUtils.Distance_WiiTanksUnits(enemy.Position, pathPos) <= 8f; // realMiss;
                if (closeEnough) {
                    _tankPointsBuffer.Add(pathPos);
                    _tanksInPathBuffer.Add(enemy);
                }
            }

            if (draw) {
                var screenPos = MatrixUtils.ConvertWorldToScreen(
                    Vector3.Zero,
                    Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y),
                    CameraGlobals.GameView,
                    CameraGlobals.GameProjection
                );

                TankGame.SpriteRenderer.Draw(
                    whitePixel,
                    screenPos,
                    null,
                    Color.White * 0.5f,
                    0,
                    whitePixel.Size() / 2,
                    realMiss,
                    default,
                    default
                );
            }

            pathPos += pathDir;
        }

        tankCollPoints = [.. _tankPointsBuffer];
        ricochetPoints = [.. _ricochetPointsBuffer];
        return _tanksInPathBuffer;
    }
}
