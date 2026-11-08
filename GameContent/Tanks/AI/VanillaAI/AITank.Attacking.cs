using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Tanks.AI.VanillaAI;

public partial struct VanillaAISystem {
    bool _predicts;
    bool _isSeeking;
    public float SeekRotation = MathHelper.Pi;

    public bool DoAttack = true;

    public float TurretRotationMultiplier = 1f;
    public bool IsEnemySpotted;

    public float ObstacleAwarenessMineReal;

    /// <summary>The location(s) of which this tank's shot path hits an obstacle.</summary>
    public Vector2[] ShotPathRicochetPoints { get; private set; } = [];
    /// <summary>The location(s) of which this tank's shot path hits an tank.</summary>
    public Vector2[] ShotPathTankCollPoints { get; private set; } = [];

    readonly List<Tank> _tanksInPathBuffer = [];
    readonly List<ShotSegment> _segments = [];
    readonly List<Vector2> _ricochetPointsBuffer = [];
    readonly List<Vector2> _tankPointsBuffer = [];
    /// <summary>Updates turret directions/targets and updates tanks in the shoot path.</summary>
    public void HandleTurret() {
        Owner.TargetTurretRotation = Owner.TargetTurretRotation.WrapTauAngle();
        Owner.TurretRotation = MathUtils.TurnTowards(Owner.TurretRotation, Owner.TargetTurretRotation, Owner.Parameters.TurretSpeed * TurretRotationMultiplier * AIManager.AIDeltaFrames);
        TurretRotationMultiplier = 1f;

        // update things prior to timer check
        if (Owner.TargetTank is null) return;

        if (!_isSeeking && !_predicts) {
            IsEnemySpotted = !Owner.TargetTank.Properties.Invisible || Owner.TargetTank.TimeSinceLastAction < Owner.Parameters.Rememberance;

            if (IsEnemySpotted) {
                Owner.AimTarget = Owner.TargetTank.Position;
            }
        }

        UpdateAim();

        if (_isSeeking) return;

        if (!TurretMovement.TimerSatisfies(Owner.Parameters.TurretMovementTimer)) return;

        var targTurrRot = MathUtils.RotationOf(Owner.AimTarget - Owner.Position);
        Owner.TargetTurretRotation = targTurrRot + PRNG.NextFloat(-Owner.Parameters.AimOffset, Owner.Parameters.AimOffset);
    }
    public void TryMineLay() {
        // don't even bother if the tank can't lay mines
        if (!MinePlace.TimerSatisfies(Owner.CurrentRandomMineLay)) return;

        MinePlace.Value = 0;
        Owner.CurrentRandomMineLay = PRNG.Next(Owner.Parameters.RandomTimerMinMine, Owner.Parameters.RandomTimerMaxMine);

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

        // this could probably not be a raycast but instead just a distance check
        var dist = ObstacleAwarenessMineReal / 2;
        Owner.RayCastCardinals(dist, (fixture, point, normal, fraction) => {
            if (fixture.Body.Tag is Block b) {
                if (!nearDestructible)
                    nearDestructible = b.Properties.IsDestructible;
            }
            return fraction;

            // this wizardry goes beyond me
        }, Owner.ChassisRotation - MathHelper.Pi);

        // SmartMineLaying was removed in favor
        // attempt via an opportunity to lay a mine
        var random = PRNG.NextFloat();

        // change chance based on whether or not the tank is near a destructible obstacle
        var randomSuccess = random <= (nearDestructible ? Owner.Parameters.ChanceMineLayNearBreakables : Owner.Parameters.ChanceMineLay);

        if (!randomSuccess) return;

        // original: the mine is only laid if an escape direction exists (Tanks_CPUTankFleeMineDirFind)
        if (!CanEscapeOwnMine()) return;

        Owner.LayMine();
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

        var turretDir = MathUtils.DirectionOf(Owner.TurretRotation);
        var shellOffset = Vector2.UnitY * Tank.SHELL_SPAWN_DISTANCE;
        bool explosive = Owner.Properties.ShellType == ShellID.Explosive;

        // explosive shells blow up destructible blocks, so they don't count as the end of the path
        Func<Block, bool>? pattern = explosive
            ? x => !x.Properties.IsDestructible && x.Properties.IsSolid || x.Type == BlockID.Teleporter
            : null;

        // the original's shot check: scan up to 3 straight segments, then test each cone against them
        _tanksInPathBuffer.Clear();
        var pathValid = ShotPath.Trace(Owner, Owner.Position + shellOffset.RotatedBy(Owner.TurretRotation), turretDir, _segments, pattern);
        var cones = pathValid ? ScanShotCones() : default;

        if (explosive) {
            // last ricochet point = where the final segment starts
            // ...arbitrary ahh number.
            if (_segments.Count > 1 && GameUtils.TanksDistance(_segments[^1].Start, Owner.Position) < 150f)
                tooCloseToExplosiveShell = true;
        }
        else {
            if (_tanksInPathBuffer.Count != Owner.TanksSpotted.Length)
                Owner.TanksSpotted = [.. _tanksInPathBuffer]; // unavoidable alloc if size changes, but cheaper than constant realloc
            else
                _tanksInPathBuffer.CopyTo(Owner.TanksSpotted);

            ShotPathRicochetPoints = GetRicochetPoints();
            ShotPathTankCollPoints = [];
        }

        // only set if the prediction below overrides the normal path
        List<Tank>? predictedPath = null;
        if (Owner.Parameters.PredictsPositions) {
            if (Owner.TargetTank is not null) {
                float t = GeometryUtils.QuadraticCoeff(Owner.TurretPosition, Owner.TargetTank.Position, Owner.TargetTank.Velocity, Owner.Properties.ShellSpeed);

                if (t < 0f) t = 0f;

                float rot = MathUtils.RotationOf(Owner.TurretPosition.DirectionTo(GeometryUtils.PredictFuturePosition(Owner.TargetTank.Position, Owner.TargetTank.Velocity, t)));

                predictedPath = GetTanksInPath(
                Vector2.Normalize(Owner.TurretPosition.DirectionTo(Owner.TargetTank.Position)),
                out _, out _, offset: Vector2.Zero,
                missDist: Owner.Parameters.DetectionForgivenessHostile, doBounceReset: Owner.Parameters.BounceReset);

                if (predictedPath.Contains(Owner.TargetTank)) {
                    _predicts = true;
                    Owner.TargetTurretRotation = rot;
                }
            }
        }

        // TODO: is findsSelf even necessary? findsEnemy is only true if findsSelf is false. eh, whatever. my brain is fucked.
        // old linq
        //var findsEnemy = tanksDef.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
        //var findsSelf = tanksDef.Any(tnk => tnk is not null && tnk == this);
        //var findsFriendly = tanksDef.Any(tnk => tnk is not null && tnk.Team == Team && tnk.Team != TeamID.NoTeam);

        bool findsEnemy = cones.Hostile;
        bool findsSelf = cones.Self;
        bool findsFriendly = cones.Friendly;

        // predicting tanks aim off the turret's line, so the normal cones don't apply to them
        if (predictedPath is not null) {
            findsEnemy = false;
            for (int i = 0; i < predictedPath.Count; i++) {
                var tnk = predictedPath[i];
                if (tnk != null && !tnk.IsOnSameTeamAs(Owner.Team) && tnk != Owner) findsEnemy = true;
            }
        }

        if (findsEnemy && !tooCloseToExplosiveShell)
            Owner.SeesTarget = true;

        if (Owner.Parameters.SmartRicochets) {
            if (!_isSeeking)
                SeekRotation = (SeekRotation - Owner.Parameters.TurretSpeed * AIManager.AIDeltaFrames).WrapTauAngle();
            else
                TurretRotationMultiplier = 3f;
            var canShoot = !(Owner.CurShootCooldown > 0 || Owner.OwnedShellCount >= Owner.Properties.ShellLimit);
            if (canShoot) {
                var tanks = GetTanksInPath(MathUtils.DirectionOf(SeekRotation), out var ricP, out var tnkCol, false, default, Owner.Parameters.DetectionForgivenessHostile, doBounceReset: Owner.Parameters.BounceReset);

                // linq -> loop
                var findsEnemy2 = false;
                for (int i = 0; i < tanks.Count; i++) {
                    var tnk = tanks[i];
                    if (tnk != null && !tnk.IsOnSameTeamAs(Owner.Team) && tnk != Owner) {
                        findsEnemy2 = true;
                        break;
                    }
                }

                var own = Owner;
                var badShot = tanks.Any(tnk => tnk == own || tnk.IsOnSameTeamAs(own.Team));
                if (findsEnemy2 && !_isSeeking && !badShot) {
                    _isSeeking = true;
                    Owner.TargetTurretRotation = SeekRotation;
                }
            }

            if (_isSeeking && MathUtils.AbsoluteAngleBetween(Owner.TurretRotation, Owner.TargetTurretRotation) < MathUtils.VERY_SMALL)
                _isSeeking = false;
        }
        // tanks wont shoot when fleeing from a mine
        // could move these conditions upwards...
        if (AttackPrevented)
            return;

        if (!DoAttack) return;
        if (!ShellFire.TimerSatisfies(Owner.CurrentRandomShoot)) return;
        // if (!Behaviors[2].IsModOf(CurrentRandomShoot)) return;

        Owner.CurrentRandomShoot = PRNG.Next(Owner.Parameters.RandomTimerMinShoot, Owner.Parameters.RandomTimerMaxShoot);
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
        // if t < 0, the shell is unhittable, so fallback to its current position
        Vector2 aimTarget = t > 0f
            ? GeometryUtils.PredictFuturePosition(shell.Position, shell.Velocity, t)
            : shell.Position;

        float rot = MathUtils.RotationOf(Owner.TurretPosition.DirectionTo(aimTarget));

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

    /// <summary>Which of the three cones found a tank on the shot path.</summary>
    public readonly record struct ShotCones(bool Self, bool Friendly, bool Hostile);

    readonly Vector2[] GetRicochetPoints() {
        if (_segments.Count < 2) return [];
        var points = new Vector2[_segments.Count - 1];
        for (int i = 0; i < points.Length; i++) points[i] = _segments[i + 1].Start;
        return points;
    }

    // whether the position is in the cone of any segment from firstSegment on
    readonly bool HitsPath(Vector2 position, int firstSegment, float coneAngle) {
        for (int i = firstSegment; i < _segments.Count; i++) {
            var seg = _segments[i];
            if (GeometryUtils.InCone(position, seg.Start, seg.Direction, coneAngle, seg.Length)) return true;
        }
        return false;
    }

    /// <summary>
    /// Tests every tank against <see cref="_segments"/> with the three detection cones.
    /// </summary>
    readonly ShotCones ScanShotCones() {
        bool self = false, friendly = false, hostile = false;
        var p = Owner.Parameters;

        foreach (var tank in GameHandler.AllTanks) {
            if (tank is null || tank.IsDestroyed) continue;

            // updates each flag if necessary
            bool found;
            if (tank == Owner)
                self |= found = HitsPath(tank.Position, 1, p.DetectionForgivenessSelf);
            else if (tank.IsOnSameTeamAs(Owner.Team))
                friendly |= found = HitsPath(tank.Position, 0, p.DetectionForgivenessFriendly);
            else
                hostile |= found = HitsPath(tank.Position, 0, p.DetectionForgivenessHostile);

            if (found) _tanksInPathBuffer.Add(tank);
        }
        return new(self, friendly, hostile);
    }

    readonly List<ShotSegment> _drawSegments = [];
    /// <summary>Draws shot path segments.</summary>
    public readonly void DrawShotCones(Matrix view, Matrix proj) {
        var p = Owner.Parameters;
        var floor = Vector3.Up * 0.3f;

        // it's simply visual overstimulation at 4 or more rays so i dont want to hurt people
        var tooManySegments = _segments.Count > 3;

        lock (_segments) {
            _drawSegments.Clear();
            _drawSegments.AddRange(_segments);
        }
        for (int i = 0; i < _drawSegments.Count; i++) {
            var seg = _drawSegments[i];
            DrawUtils.DrawLine(seg.Start.ExpandZ() + floor, seg.End.ExpandZ() + floor, Color.Black, view, proj);

            if (!tooManySegments) {
                DrawUtils.DrawCone(seg.Start, seg.Direction, seg.Length, p.DetectionForgivenessHostile, Color.Red, view, proj);
                // not gonna draw the friendly radius since it is LARGELY the same. it also just lowkey dyes the shot cone lol
                // DrawUtils.DrawCone(seg.Start, seg.Direction, seg.Length, p.DetectionForgivenessFriendly, Color.LimeGreen, view, proj, fill);
                if (i > 0)
                    DrawUtils.DrawCone(seg.Start, seg.Direction, seg.Length, p.DetectionForgivenessSelf, Color.Cyan, view, proj);
            }
        }
    }
    public readonly List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2[] ricochetPoints, out Vector2[] tankCollPoints,
            bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool>? pattern = null, bool doBounceReset = true) {
        // upper bound on frames simulated
        const int MAX_PATH_FRAMES = 1000;
        const float FORGIVENESS = 8f;

        _tanksInPathBuffer.Clear();
        _ricochetPointsBuffer.Clear();
        _tankPointsBuffer.Clear();

        pattern ??= c => c.Properties.IsSolid || c.Type == BlockID.Teleporter;

        var whitePixel = TextureGlobals.Pixels[Color.White];

        var sim = ShellSim.From(Owner, Owner.Position + offset.RotatedBy(Owner.TurretRotation), pathDir);

        // probably gonna leave teleporter shot path stuff out cuz its far too complicated for me rn
        //bool teleported = false;
        //Vector2? teleportTo = null;

        for (int i = 0; i < MAX_PATH_FRAMES; i++) {
            // delays teleport until next frame
            //if (teleportTo is { } to) {
            //    sim.Position = to;
            //    teleportTo = null;
            //}

            // check tanks BEFORE moving
            float realMiss = 1f + missDist * 2 * (sim.Distance / FORGIVENESS);

            var pos3d = new Vector3(sim.Position.X, 11, sim.Position.Y);
            foreach (var enemy in GameHandler.AllTanks) {
                if (enemy is null || enemy.IsDestroyed || _tanksInPathBuffer.Contains(enemy)) continue;

                var hurtboxMiddle = (enemy.Hurtbox.Max + enemy.Hurtbox.Min) * 0.5f;

                // 5 is just an eensy weensy magical number.
                if (i > 5 && Vector3.DistanceSquared(pos3d, hurtboxMiddle) < realMiss * realMiss) {
                    var pathAngle = sim.Velocity.ToRotation();
                    var toEnemy = sim.Position.DirectionTo(enemy.Position).ToRotation();

                    if (MathUtils.AbsoluteAngleBetween(pathAngle, toEnemy) >= MathHelper.PiOver2)
                        _tanksInPathBuffer.Add(enemy);
                }

                // this used to be a circle check, but it was probably overkill? we'll see
                if (GameUtils.TanksDistance(enemy.Position, sim.Position) <= 8f) {
                    _tankPointsBuffer.Add(sim.Position);
                    _tanksInPathBuffer.Add(enemy);
                }
            }

            // convert this (and the actual math) to triangle fans
            if (draw) {
                var screenPos = MatrixUtils.ConvertWorldToScreen(
                  Vector3.Zero,
                  Matrix.CreateTranslation(sim.Position.X, 11, sim.Position.Y),
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

            var result = sim.Step(pattern, out var block);

            /*if (block is { Type: BlockID.Teleporter } && !teleported) {
                var dest = block.GetLinkedTeleporter();
                if (dest is not null) {
                    teleported = true;
                    teleportTo = dest.Position;
                }
            }*/

            if (result == ShellSimResult.Destroyed) break;
            if (result == ShellSimResult.Ricochet) {
                _ricochetPointsBuffer.Add(sim.Position);
                if (doBounceReset) sim.Distance = 0f;
            }
        }

        tankCollPoints = [.. _tankPointsBuffer];
        ricochetPoints = [.. _ricochetPointsBuffer];
        return _tanksInPathBuffer;
    }
}
