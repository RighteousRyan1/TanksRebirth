using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Tanks.AI.VanillaAI;
public partial struct VanillaAISystem {
    bool _predicts;
    bool _isSeeking;
    public float SeekRotation;

    public bool DoAttack = true;

    public float TurretRotationMultiplier = 1f;
    public bool IsEnemySpotted;

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
        //TargetTurretRotation %= MathHelper.TwoPi;
        //TurretRotation %= MathHelper.TwoPi;
        Owner.TargetTurretRotation = MathHelper.WrapAngle(Owner.TargetTurretRotation);
        Owner.TurretRotation = MathHelper.WrapAngle(Owner.TurretRotation);

        var diff = Owner.TargetTurretRotation - Owner.TurretRotation;

        if (diff > MathHelper.Pi) Owner.TargetTurretRotation -= MathHelper.TwoPi;
        else if (diff < -MathHelper.Pi) Owner.TargetTurretRotation += MathHelper.TwoPi;

        Owner.TurretRotation = MathUtils.RoughStep(Owner.TurretRotation, Owner.TargetTurretRotation, Owner.Parameters.TurretSpeed * TurretRotationMultiplier * RuntimeData.DeltaTime);

        // update things prior to timer check
        if (Owner.TargetTank is null) return;

        if (!_isSeeking && !_predicts) {
            IsEnemySpotted = !Owner.TargetTank.Properties.Invisible || Owner.TargetTank.TimeSinceLastAction < Owner.Parameters.Rememberance;

            if (IsEnemySpotted) {
                Owner.AimTarget = Owner.TargetTank.Position;
            }
        }

        UpdateAim();

        if (!TurretMovement.TimerSatisfies(Owner.Parameters.TurretMovementTimer)) return;
        // if (!Behaviors[1].IsModOf(Parameters.TurretMovementTimer)) return;

        var targTurrRot = (Owner.Position - Owner.AimTarget).ToRotation();
        Owner.TargetTurretRotation = GetRealAim(targTurrRot) + Client.ClientRandom.NextFloat(-Owner.Parameters.AimOffset, Owner.Parameters.AimOffset);
    }
    /// <summary>
    /// Given how the tank's forward vectors are handled (right now), magical mathematics are applied to <paramref name="inputAngle"/>.
    /// </summary>
    /// <param name="inputAngle">The input angle</param>
    /// <returns>The final/output rotation angle.</returns>
    public static float GetRealAim(float inputAngle) {
        return -inputAngle - MathHelper.PiOver2;
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
        // if (!Behaviors[3].IsModOf(CurrentRandomMineLay)) return;
        if (!MinePlace.TimerSatisfies(Owner.CurrentRandomMineLay)) return;

        // set our new random window, this gets set
        // Behaviors[3].Value = 0;
        MinePlace.Value = 0;
        Owner.CurrentRandomMineLay = Client.ClientRandom.Next(Owner.Parameters.RandomTimerMinMine, Owner.Parameters.RandomTimerMaxMine);

        if (Owner.Properties.MineLimit <= 0) return;
        if (IsSurviving) return;

        // check for friendly tanks nearby, if there are any, don't even attempt to lay a mine
        for (int i = 0; i < Owner.TanksNearMineAwareness.Count; i++) {
            var tank = Owner.TanksNearMineAwareness[i];
            if (tank.IsOnSameTeamAs(Owner.Team))
                return;
        }

        bool nearDestructible = false;

        // call me the wizard of oz
        ObstacleAwarenessMineReal = 3 * Owner.Parameters.ObstacleAwarenessMine;

        var dist = ObstacleAwarenessMineReal / 2;
        var dirs = Owner.RayCastCardinals(dist, (fixture, point, normal, fraction) => {
            if (fixture.Body.Tag is Block b) {
                if (!nearDestructible)
                    nearDestructible = b.Properties.IsDestructible;

                // b.Stack = (byte)Client.ClientRandom.Next(1, 8);
            }
            return fraction;

            // this wizardry goes beyond me
        }, Owner.ChassisRotation - MathHelper.Pi);

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

                if (GameUtils.TanksDistance(pos, mine.Position) <= mine.ExplosionRadius * GameUtils.TanksUnits(70)) {
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
                if (GameUtils.TanksDistance(pos, mine.Position) <= mine.ExplosionRadius * GameUtils.TanksUnits(70)) {
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
        var randomSuccess = random <= (nearDestructible ? Owner.Parameters.ChanceMineLayNearBreakables : Owner.Parameters.ChanceMineLay);

        if (!randomSuccess) return;

        Owner.LayMine();

        // Pick a random valid direction
        int skips = Client.ClientRandom.Next(0, goodDirsCount);
        for (int i = 0; i < dirs.Length; i++) {
            if (dirs[i].Direction != CollisionDirection.None) {
                if (skips == 0) {
                    var rot = dirs[i].Vec.ToRotation();
                    Owner.DesiredChassisRotation = rot - MathHelper.PiOver2;
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
        Owner.SeesTarget = false;

        bool tooCloseToExplosiveShell = false;

        bool friendliesNearby = false;
        for (int i = 0; i < Owner.TanksNearShootAwareness.Count; i++) {
            var tank = Owner.TanksNearShootAwareness[i];
            if (Owner.IsOnSameTeamAs(tank.Team)) {
                friendliesNearby = true;
                break; // early exit like LINQ does
            }
        }
        // stop doing expensive checks if the tank can't even shoot anyway
        if (friendliesNearby) return;

        List<Tank> tanksDef;

        var turretDir = Vector2.UnitY.RotatedBy(Owner.TurretRotation - MathHelper.Pi);
        if (Owner.Properties.ShellType == ShellID.Explosive) {
            tanksDef = GetTanksInPath(turretDir, out var ricP, out var tnkCol, offset: Vector2.UnitY * 20, pattern: x => !x.Properties.IsDestructible && x.Properties.IsSolid || x.Type == BlockID.Teleporter, missDist: Owner.Parameters.DetectionForgivenessHostile, doBounceReset: Owner.Parameters.BounceReset);
            if (ricP.Length > 0 && GameUtils.TanksDistance(ricP[^1], Owner.Position) < 150f)
                tooCloseToExplosiveShell = true;
        }
        else {
            tanksDef = GetTanksInPath(
                turretDir,
                out var ricP, out var tnkCol, offset: Vector2.UnitY * 20,
                missDist: Owner.Parameters.DetectionForgivenessHostile, doBounceReset: Owner.Parameters.BounceReset);

            if (tanksDef.Count != Owner.TanksSpotted.Length)
                Owner.TanksSpotted = [.. tanksDef]; // unavoidable alloc if size changes, but cheaper than constant realloc
            else
                tanksDef.CopyTo(Owner.TanksSpotted);

            ShotPathRicochetPoints = ricP;
            ShotPathTankCollPoints = tnkCol;
        }
        if (Owner.Parameters.PredictsPositions) {
            if (Owner.TargetTank is not null) {
                float t = GeometryUtils.QuadraticCoeff(Owner.TurretPosition, Owner.TargetTank.Position, Owner.TargetTank.Velocity, Owner.Properties.ShellSpeed);

                if (t < 0f) t = 0f;

                float rot = -Owner.TurretPosition.DirectionTo(GeometryUtils.PredictFuturePosition(Owner.TargetTank.Position, Owner.TargetTank.Velocity, t))
                    .ToRotation() - MathHelper.PiOver2;

                tanksDef = GetTanksInPath(
                Vector2.UnitY.RotatedBy(-Owner.TurretPosition.DirectionTo(Owner.TargetTank.Position).ToRotation() - MathHelper.PiOver2),
                out var ricP, out var tnkCol, offset: Vector2.Zero,
                missDist: Owner.Parameters.DetectionForgivenessHostile, doBounceReset: Owner.Parameters.BounceReset);

                if (tanksDef.Contains(Owner.TargetTank)) {
                    _predicts = true;
                    Owner.TargetTurretRotation = rot + MathHelper.Pi;
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

        for (int i = 0; i < tanksDef.Count; i++) {
            var tnk = tanksDef[i];
            if (tnk == null) continue;
            if (!tnk.IsOnSameTeamAs(Owner.Team) && tnk != Owner) findsEnemy = true;
            if (tnk == Owner) findsSelf = true;
            if (tnk.IsOnSameTeamAs(Owner.Team)) findsFriendly = true;
        }

        if (findsEnemy && !tooCloseToExplosiveShell)
            Owner.SeesTarget = true;

        if (Owner.Parameters.SmartRicochets) {
            //if (!seeks)
            SeekRotation += Owner.Parameters.TurretSpeed * 0.25f;
            var canShoot = !(Owner.CurShootCooldown > 0 || Owner.OwnedShellCount >= Owner.Properties.ShellLimit);
            if (canShoot) {
                var tanks = GetTanksInPath(Vector2.UnitY.RotatedBy(SeekRotation), out var ricP, out var tnkCol, false, default, Owner.Parameters.DetectionForgivenessHostile, doBounceReset: Owner.Parameters.BounceReset);

                // linq -> loop
                var findsEnemy2 = false;
                for (int i = 0; i < tanks.Count; i++) {
                    var tnk = tanks[i];
                    if (tnk != null && !tnk.IsOnSameTeamAs(Owner.Team) && tnk != Owner) {
                        findsEnemy2 = true;
                        break;
                    }
                }

                // var findsSelf2 = tanks.Any(tnk => tnk is not null && tnk == this);
                // var findsFriendly2 = tanks.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TeamID.NoTeam));
                // ChatSystem.SendMessage($"{findsEnemy2} {findsFriendly2} | seek: {seeks}", Color.White);
                if (findsEnemy2/* && !findsFriendly2*/) {
                    _isSeeking = true;
                    TurretRotationMultiplier = 3f;
                    Owner.TargetTurretRotation = (SeekRotation % MathHelper.TwoPi) - MathHelper.Pi; // minus pi... why?
                }
            }

            if (Owner.TurretRotation == Owner.TargetTurretRotation || !canShoot)
                _isSeeking = false;
        }
        // tanks wont shoot when fleeing from a mine
        // could move these conditions upwards...
        if (ClosestDanger is Mine && Owner.Parameters.CantShootWhileFleeing)
            return;

        if (!DoAttack) return;
        if (!ShellFire.TimerSatisfies(Owner.CurrentRandomShoot)) return;
        // if (!Behaviors[2].IsModOf(CurrentRandomShoot)) return;

        Owner.CurrentRandomShoot = Client.ClientRandom.Next(Owner.Parameters.RandomTimerMinShoot, Owner.Parameters.RandomTimerMaxShoot);
        // Behaviors[2].Value = 0;
        ShellFire.Value = 0;
        // Console.WriteLine(TanksSpotted.Length);

        // no need to check friendliesNearby because we return earlier in this method if there are any
        if (!Owner.SeesTarget || Owner.CurShootCooldown > 0) return;
        if (!Owner.Parameters.PredictsPositions && (findsSelf || findsFriendly)) return;

        Owner.Shoot(false);
    }
    /// <summary>Gets the a <see cref="Tanks.Tank"/> that is hostile and is targetable (with respect to <see cref="Parameters"/>).</summary>
    public readonly Tank? GetAppropriateTarget() {
        Tank? target = null;

        // cache the count to avoid property lookups on every iteration
        int tankCount = GameHandler.AllTanks.Length;

        if (Owner.Parameters.SmartTargeting) {
            var smallestActionTime = float.MaxValue;
            Tank? smallestActionTimeTank = Owner.TargetTank;

            for (int i = 0; i < tankCount; i++) {
                var tank = GameHandler.AllTanks[i];
                if (tank is null || tank.IsDestroyed || tank == Owner || tank.IsOnSameTeamAs(Owner.Team)) continue;

                if (tank.TimeSinceLastAction < smallestActionTime) {
                    smallestActionTime = tank.TimeSinceLastAction;
                    smallestActionTimeTank = tank;
                }
            }
            return smallestActionTimeTank;
        }

        float closestDistSq = float.MaxValue;

        for (int i = 0; i < tankCount; i++) {
            var tank = GameHandler.AllTanks[i];
            if (tank is null || tank.IsDestroyed || tank == Owner || tank.IsOnSameTeamAs(Owner.Team)) continue;

            float distSq = Vector2.DistanceSquared(tank.Position, Owner.Position);
            if (distSq < closestDistSq) {
                if (!tank.Properties.Invisible || tank.TimeSinceLastAction < Owner.Parameters.Rememberance) {
                    target = tank;
                    closestDistSq = distSq;
                }
            }
        }
        return target;
    }
    /// <summary>A method that simply changes an AI tank's target to a pinged tank.</summary>
    public readonly Tank? TryOverrideTarget(out bool overridden) {
        overridden = false;
        Tank? target = Owner.TargetTank;

        // this might violate something if there are two teams wiht two players and two Owner tanks...
        // it might want to kill their teammate if the other team's player pings them
        bool hasPlayerTeammate = false;

        for (int i = 0; i < GameHandler.AllPlayerTanks.Length; i++) {
            var pl = GameHandler.AllPlayerTanks[i];

            if (pl is null) continue;
            if (pl.IsOnSameTeamAs(Owner.Team)) {
                hasPlayerTeammate = true;
                break;
            }
        }

        if (hasPlayerTeammate) {
            foreach (var ping in IngamePing.AllIngamePings) {
                if (ping is null || ping.TrackedTank is null) continue;
                if (ping.TrackedTank == Owner) continue; // no self-targeting
                if (ping.TrackedTank.Team == Owner.Team) continue; // no friendly fire
                target = ping.TrackedTank;
                overridden = true;
                break;
            }
        }
        return target;
    }
    // maybe use WiiTanksDistance or whatever
    /// <summary>Makes this <see cref="AITank"/> attempt to shoot to destroy the given <see cref="Shell"/>.</summary>
    public void DoDeflection(Shell shell) {
        float t = GeometryUtils.QuadraticCoeff(Owner.TurretPosition, shell.Position, shell.Velocity, Owner.Properties.ShellSpeed);

        if (t < 0f) t = 0f;

        // if t = valid, aim at the perfect interception point
        // if t < 0, the shell is unhittable (moving away too fast), so fallback to its current position
        Vector2 aimTarget = t > 0f
            ? GeometryUtils.PredictFuturePosition(shell.Position, shell.Velocity, t)
            : shell.Position;

        float rot = -Owner.TurretPosition.DirectionTo(aimTarget).ToRotation() + MathHelper.PiOver2;

        Owner.TargetTurretRotation = rot;
        TurretRotationMultiplier = 4f; // introduce constant?

        // absolute shortest angular distance between current and target rotation
        float angleDiff = Math.Abs(MathHelper.WrapAngle(Owner.TargetTurretRotation - Owner.TurretRotation));

        // bool futureVisible = 
        // only shoot if it's aiming where it should
        if (angleDiff <= 0.05f) {
            //TankGame.MainThreadTasks.Enqueue(() => Shoot());
            Owner.Shoot();
        }
    }


    // let's just... keep this hidden for now.
    /*struct PathSegment {
        public Vector2 Start;
        public Vector2 End;
        public float StartRadius;
        public float EndRadius;
        public Vector2 Direction;
    }*/
    public List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2[] ricochetPoints, out Vector2[] tankCollPoints,
            bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool>? pattern = null, bool doBounceReset = true) {
        const int MAX_PATH_UNITS = 1000;
        const int PATH_UNIT_LENGTH = 8;

        _tanksInPathBuffer.Clear();
        _ricochetPointsBuffer.Clear();
        _tankPointsBuffer.Clear();

        pattern ??= c => c.Properties.IsSolid || c.Type == BlockID.Teleporter;

        var whitePixel = TextureGlobals.Pixels[Color.White];

        // genuine fucking stupidity as to why this is negative in so many calculations
        Vector2 pathPos = Owner.Position + offset.RotatedBy(-Owner.TurretRotation);
        pathDir.Y *= -1;
        pathDir *= PATH_UNIT_LENGTH;

        int ricochetCount = 0;
        int uninterruptedIterations = 0;

        bool teleported = false;
        int tpTriggerIndex = -1;
        Vector2 teleportedTo = Vector2.Zero;

        var pathHitbox = new Rectangle() {
            Width = Shell.COLL_RECT_DIM,
            Height = Shell.COLL_RECT_DIM
        };

        // TODO: find out why marine tanks don't shoot in shotguns mode
        // -> by that same coin, fix shot paths not drawing

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
            bool hitsTooEarly = i < (int)Owner.Properties.ShellSpeed / 2 && ricochetCount > 0;
            bool ricochetLimitReached = ricochetCount > Owner.Properties.RicochetCount;

            if (hitsInstant || hitsTooEarly || ricochetLimitReached)
                break;

            // check tanks BEFORE moving
            float realMiss = 1f + missDist * 2 * uninterruptedIterations;

            // 11 = y offset of shells traveling
            // var pathSphere = new BoundingSphere(new Vector3(pathPos.X, 11, pathPos.Y), realMiss);

            var pos3d = new Vector3(pathPos.X, 11, pathPos.Y);
            foreach (var enemy in GameHandler.AllTanks) {
                if (enemy is null || enemy.IsDestroyed || _tanksInPathBuffer.Contains(enemy)) continue;

                var hurtboxMiddle = (enemy.Hurtbox.Max + enemy.Hurtbox.Min) * 0.5f;

                // 5 is just an eensy weensy magical number.
                if (i > 5 && Vector3.DistanceSquared(pos3d, hurtboxMiddle) < realMiss * realMiss/*enemy.Hurtbox.Intersects(pathSphere)*/ /*GameUtils.TanksDistance(enemy.Position, pathPos) <= realMiss*/) {
                    var pathAngle = pathDir.ToRotation();
                    var toEnemy = pathPos.DirectionTo(enemy.Position).ToRotation();

                    if (MathUtils.AbsoluteAngleBetween(pathAngle, toEnemy) >= MathHelper.PiOver2)
                        _tanksInPathBuffer.Add(enemy);
                }

                // this used to be a circle check, but it was probably overkill? we'll see
                var closeEnough = GameUtils.TanksDistance(enemy.Position, pathPos) <= 8f; // realMiss;
                if (closeEnough) {
                    _tankPointsBuffer.Add(pathPos);
                    _tanksInPathBuffer.Add(enemy);
                }
            }

            // convert this (and the actual math) to triangle fans
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
    /*List<Owner> GetTanksInPath(Vector2 pathDir, out Vector2[] ricochetPoints, out Vector2[] tankCollPoints,
        bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool>? pattern = null, bool doBounceReset = true) {
        const int MAX_PATH_UNITS = 1000;
        const int PATH_UNIT_LENGTH = 8;

        _tanksInPathBuffer.Clear();
        _ricochetPointsBuffer.Clear();
        _tankPointsBuffer.Clear();

        pattern ??= c => c.Properties.IsSolid || c.Type == BlockID.Teleporter;

        Vector2 pathPos = Position + offset.RotatedBy(-TurretRotation);
        pathDir.Y *= -1;

        // normalize to accurately calculate future normals
        Vector2 normalizedDir = Vector2.Normalize(pathDir);
        pathDir = normalizedDir * PATH_UNIT_LENGTH;

        int ricochetCount = 0;
        int uninterruptedIterations = 0;

        bool teleported = false;
        int tpTriggerIndex = -1;
        Vector2 teleportedTo = Vector2.Zero;

        var pathHitbox = new Rectangle() {
            Width = Shell.COLL_RECT_DIM,
            Height = Shell.COLL_RECT_DIM
        };

        List<PathSegment> segments = [];
        Vector2 currentSegStart = pathPos;
        float currentSegStartRadius = 1f;

        for (int i = 0; i < MAX_PATH_UNITS; i++) {
            uninterruptedIterations++;
            float currentRadius = 1f + missDist * 2 * uninterruptedIterations;
            bool segmentBroken = false;

            if (pathPos.X < GameScene.MIN_X || pathPos.X > GameScene.MAX_X) {
                _ricochetPointsBuffer.Add(pathPos);
                pathDir.X *= -1;
                ricochetCount++;
                segmentBroken = true;
            }
            else if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                _ricochetPointsBuffer.Add(pathPos);
                pathDir.Y *= -1;
                ricochetCount++;
                segmentBroken = true;
            }

            pathHitbox.X = (int)pathPos.X - Shell.COLL_RECT_DIM / 2;
            pathHitbox.Y = (int)pathPos.Y - Shell.COLL_RECT_DIM / 2;

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
                        segmentBroken = true;
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
                    segmentBroken = true;
                }
            }

            // if bounce or tp, seal segment
            if (segmentBroken) {
                segments.Add(new PathSegment {
                    Start = currentSegStart,
                    End = pathPos,
                    StartRadius = currentSegStartRadius,
                    EndRadius = currentRadius,
                    Direction = normalizedDir
                });

                if (doBounceReset) uninterruptedIterations = 0;

                normalizedDir = Vector2.Normalize(pathDir);
                currentSegStart = teleported && i == tpTriggerIndex ? teleportedTo : pathPos;
                currentSegStartRadius = 1f + missDist * 2 * uninterruptedIterations;
            }

            if (teleported && i == tpTriggerIndex) pathPos = teleportedTo;

            bool hitsInstant = i == 0 && Block.AllBlocks.Any(x => x != null && x.Hitbox.Intersects(pathHitbox) && pattern(x));
            bool hitsTooEarly = i < (int)Properties.ShellSpeed / 2 && ricochetCount > 0;
            bool ricochetLimitReached = ricochetCount > Properties.RicochetCount;

            if (hitsInstant || hitsTooEarly || ricochetLimitReached) break;

            pathPos += pathDir;
        }

        // seal final segment
        segments.Add(new PathSegment {
            Start = currentSegStart,
            End = pathPos,
            StartRadius = currentSegStartRadius,
            EndRadius = 1f + missDist * 2 * uninterruptedIterations,
            Direction = normalizedDir
        });

        // triangle fan detection
        foreach (var enemy in GameHandler.AllTanks) {
            // MUST check 'enemy == this' so the tank doesn't detect its own barrel!
            if (enemy is null || enemy.IsDestroyed || enemy == this || _tanksInPathBuffer.Contains(enemy)) continue;

            // should probably just convert ts to a method...
            foreach (var seg in segments) {
                // closest point from the segment to the tank
                var AP = enemy.Position - seg.Start;
                var AB = seg.End - seg.Start;
                float magAB2 = AB.LengthSquared();

                if (magAB2 == 0) continue;

                // t is the normalized distance along the segment
                float t = Vector2.Dot(AP, AB) / magAB2;

                if (t < 0f || t > 1f) continue;

                var closestPoint = seg.Start + AB * t;

                // exact cone radius at this point
                float tRadius = MathHelper.Lerp(seg.StartRadius, seg.EndRadius, t);

                float shellRadius = Shell.COLL_RECT_DIM / 2f;
                float enemyRadius = TNK_WIDTH / 2f;
                float trueDetectionRadius = tRadius + shellRadius + enemyRadius;

                if (Vector2.DistanceSquared(enemy.Position, closestPoint) <= trueDetectionRadius * trueDetectionRadius) {
                    if (IsSpaceClear(closestPoint, enemy.Position)) {
                        _tanksInPathBuffer.Add(enemy);
                        _tankPointsBuffer.Add(closestPoint);
                        break;
                    }
                }
            }
        }

        if (draw && segments.Count > 0) {
            DrawPathPolygon(segments);
        }

        tankCollPoints = [.. _tankPointsBuffer];
        ricochetPoints = [.. _ricochetPointsBuffer];
        return _tanksInPathBuffer;

        // this implementation is... kind of problematic.
        bool IsSpaceClear(Vector2 pathPoint, Vector2 targetCenter) {
            Vector2 dir = targetCenter - pathPoint;
            float dist = dir.Length();
            if (dist <= 0) return true;
            dir /= dist;

            float stepSize = Shell.COLL_RECT_DIM / 2f;
            Rectangle rect = new(0, 0, Shell.COLL_RECT_DIM, Shell.COLL_RECT_DIM);
            var blocks = Block.AllBlocks;

            for (float d = 0; d < dist; d += stepSize) {
                Vector2 pos = pathPoint + dir * d;
                rect.X = (int)pos.X - Shell.COLL_RECT_DIM / 2;
                rect.Y = (int)pos.Y - Shell.COLL_RECT_DIM / 2;

                for (int i = 0; i < blocks.Length; i++) {
                    var b = blocks[i];
                    if (b != null && pattern!(b) && b.Hitbox.Intersects(rect))
                        return false; // A block is between the path and the tank
                }
            }
            return true;
        }
    }*/


    // probably gonna move this into Owner...
    /*void DrawPathPolygon(List<PathSegment> segments) {
        List<VertexPositionColor> vertices = [];
        Color pathColor = Color.Red * 0.2f;


        // a little bit higher to prevent z fighting
        // maybe keep slightly offset based on tank id?
        float heightY = 11.5f + (AITankId * 0.01f);

        foreach (var seg in segments) {
            // left and right perpendicular vectors
            Vector2 normal = new(-seg.Direction.Y, seg.Direction.X);

            // Convert expanding 2D cone limits to 3D world vertices
            Vector3 backLeft = new(seg.Start.X - normal.X * seg.StartRadius, heightY, seg.Start.Y - normal.Y * seg.StartRadius);
            Vector3 backRight = new(seg.Start.X + normal.X * seg.StartRadius, heightY, seg.Start.Y + normal.Y * seg.StartRadius);
            Vector3 frontLeft = new(seg.End.X - normal.X * seg.EndRadius, heightY, seg.End.Y - normal.Y * seg.EndRadius);
            Vector3 frontRight = new(seg.End.X + normal.X * seg.EndRadius, heightY, seg.End.Y + normal.Y * seg.EndRadius);

            // triangle 1
            vertices.Add(new(backLeft, pathColor));
            vertices.Add(new(backRight, pathColor));
            vertices.Add(new(frontLeft, pathColor));

            // triangle 2
            vertices.Add(new(backRight, pathColor));
            vertices.Add(new(frontRight, pathColor));
            vertices.Add(new(frontLeft, pathColor));
        }

        var effect = TankBasicEffectHandler;
        effect.World = Matrix.Identity;
        effect.View = CameraGlobals.GameView;
        effect.Projection = CameraGlobals.GameProjection;
        effect.VertexColorEnabled = true;
        effect.TextureEnabled = false;

        foreach (var pass in effect.CurrentTechnique.Passes) {
            pass.Apply();
            TankGame.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count / 3);
        }
    }*/
}
