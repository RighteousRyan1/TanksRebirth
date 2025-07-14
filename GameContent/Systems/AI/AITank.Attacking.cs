using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Framework;
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
    public void HandleTurret() {
        TargetTurretRotation %= MathHelper.TwoPi;

        TurretRotation %= MathHelper.TwoPi;

        var diff = TargetTurretRotation - TurretRotation;

        if (diff > MathHelper.Pi)
            TargetTurretRotation -= MathHelper.TwoPi;
        else if (diff < -MathHelper.Pi)
            TargetTurretRotation += MathHelper.TwoPi;
        bool targetExists = Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null;
        if (targetExists) {
            if (!_isSeeking && !_predicts) {
                if (Behaviors[1].IsModOf(AiParams.TurretMovementTimer)) {
                    IsEnemySpotted = false;
                    if (TargetTank!.Properties.Invisible && TargetTank.timeSinceLastAction < 60) {
                        AimTarget = TargetTank.Position;
                        IsEnemySpotted = true;
                    }

                    if (!TargetTank.Properties.Invisible) {
                        AimTarget = TargetTank.Position;
                        IsEnemySpotted = true;
                    }
                }
            }
            if (DoAttack)
                UpdateAim();
        }
        if (Behaviors[1].IsModOf(AiParams.TurretMovementTimer)) {
            var dirVec = Position - AimTarget;
            TargetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + Client.ClientRandom.NextFloat(-AiParams.AimOffset, AiParams.AimOffset);
        }
        TurretRotation = MathUtils.RoughStep(TurretRotation, TargetTurretRotation, AiParams.TurretSpeed * TurretRotationMultiplier * RuntimeData.DeltaTime);
    }
    public void TryMineLay() {
        if (Properties.MineLimit <= 0) return;
        if (!Behaviors[3].IsModOf(CurrentRandomMineLay)) return;

        if (!TanksNear.Any(x => x.Team == Team)) {
            IsNearDestructibleObstacle = BlocksNear.Any(c => c.Properties.IsDestructible);
            if (AiParams.SmartMineLaying) {
                if (IsNearDestructibleObstacle) {
                    LayMine();
                }
            }
            else {
                // attempt via an opportunity to lay a mine
                var random = Client.ClientRandom.NextFloat(0, 1);
                if (random <= (IsNearDestructibleObstacle ? AiParams.ChanceMineLayNearBreakables : AiParams.ChanceMineLay)) {
                    // ensure no blocks are within iParams.MineObstacleAwareness

                    LayMine();

                    // check cardinals for a good movement direction
                    // but how...? i'll find out

                    CurrentRandomMineLay = Client.ClientRandom.Next(AiParams.RandomTimerMinMine, AiParams.RandomTimerMaxMine);
                }
            }
        }
    }
    // TODO: make view distance, and make tanks in path public
    public void UpdateAim() {
        _predicts = false;
        SeesTarget = false;

        bool tooCloseToExplosiveShell = false;

        List<Tank> tanksDef;

        if (Properties.ShellType == ShellID.Explosive) {
            tanksDef = GetTanksInPath(Vector2.UnitY.Rotate(TurretRotation - MathHelper.Pi), out var ricP, out var tnkCol, offset: Vector2.UnitY * 20, pattern: x => !x.Properties.IsDestructible && x.Properties.IsSolid || x.Type == BlockID.Teleporter, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);
            if (GameUtils.Distance_WiiTanksUnits(ricP[^1], Position) < 150f) // TODO: change from hardcode to normalcode :YES:
                tooCloseToExplosiveShell = true;
        }
        else {
            tanksDef = GetTanksInPath(
                Vector2.UnitY.Rotate(TurretRotation - MathHelper.Pi),
                out var ricP, out var tnkCol, offset: Vector2.UnitY * 20,
                missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

            TanksSpotted = [.. tanksDef];

            ShotPathRicochetPoints = ricP;
            ShotPathTankCollPoints = tnkCol;
        }
        if (AiParams.PredictsPositions) {
            if (TargetTank is not null) {
                var calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);
                float rot = -Position.DirectionTo(GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation))
                    .ToRotation() - MathHelper.PiOver2;

                tanksDef = GetTanksInPath(
                Vector2.UnitY.Rotate(-Position.DirectionTo(TargetTank.Position).ToRotation() - MathHelper.PiOver2),
                out var ricP, out var tnkCol, offset: AiParams.PredictsPositions ? Vector2.Zero : Vector2.UnitY * 20,
                missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

                var targ = GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation);
                var posPredict = GetTanksInPath(Vector2.UnitY.Rotate(rot),
                    out var ricP1, out var tnkCol2, offset: Vector2.UnitY * 20, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

                if (tanksDef.Contains(TargetTank)) {
                    _predicts = true;
                    TargetTurretRotation = rot + MathHelper.Pi;
                }
            }
        }

        // TODO: is findsSelf even necessary? findsEnemy is only true if findsSelf is false. eh, whatever. my brain is fucked.
        var findsEnemy = tanksDef.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
        var findsSelf = tanksDef.Any(tnk => tnk is not null && tnk == this);
        var findsFriendly = tanksDef.Any(tnk => tnk is not null && tnk.Team == Team && tnk.Team != TeamID.NoTeam);

        if (findsEnemy && !tooCloseToExplosiveShell)
            SeesTarget = true;

        if (AiParams.SmartRicochets) {
            //if (!seeks)
            _seekRotation += AiParams.TurretSpeed * 0.5f;
            var canShoot = !(CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit);
            if (canShoot) {
                var tanks = GetTanksInPath(Vector2.UnitY.Rotate(_seekRotation), out var ricP, out var tnkCol, false, default, AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

                var findsEnemy2 = tanks.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
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

        bool checkNoTeam = Team == TeamID.NoTeam || !TanksNear.Any(x => x.Team == Team);

        // tanks wont shoot when fleeing from a mine
        if (ClosestDanger is Mine)
            if (AiParams.CantShootWhileFleeing)
                return;

        if (Behaviors[2].IsModOf(CurrentRandomShoot)) {
            CurrentRandomShoot = Client.ClientRandom.Next(AiParams.RandomTimerMinShoot, AiParams.RandomTimerMaxShoot);

            if (AiParams.PredictsPositions) {
                if (SeesTarget && checkNoTeam)
                    if (CurShootCooldown <= 0)
                        Shoot(false);
            }
            else {
                if (SeesTarget && checkNoTeam && !findsSelf && !findsFriendly)
                    if (CurShootCooldown <= 0)
                        Shoot(false);
            }
        }
    }
    public Tank? GetClosestTarget() {
        Tank? target = null;
        var targetPosition = new Vector2(float.MaxValue);
        foreach (var tank in GameHandler.AllTanks) {
            if (tank is not null && !tank.Dead && (tank.Team != Team || tank.Team == TeamID.NoTeam) && tank != this) {
                if (GameUtils.Distance_WiiTanksUnits(tank.Position, Position) < GameUtils.Distance_WiiTanksUnits(targetPosition, Position)) {
                    // var closeness = Vector2.Distance(tank.Position, Position);
                    if (tank.Properties.Invisible && tank.timeSinceLastAction < 60 /*|| closeness <= Block.SIDE_LENGTH*/ || !tank.Properties.Invisible) {
                        target = tank;
                        targetPosition = tank.Position;
                    }
                }
            }
        }
        return target;
    }
    public Tank? TryOverrideTarget() {
        Tank? target = TargetTank;
        if (GameHandler.AllPlayerTanks.Any(x => x is not null && x.Team == Team)) {
            foreach (var ping in IngamePing.AllIngamePings) {
                if (ping is null) break;
                if (ping.TrackedTank is null) break;
                if (ping.TrackedTank.Team == Team) break; // no friendly fire
                target = ping.TrackedTank;
            }
        }
        return target;
    }

    // TODO: literally fix everything about these turret rotation values.
    private List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2[] ricochetPoints, out Vector2[] tankCollPoints,
        bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool>? pattern = null, bool doBounceReset = true) {
        const int MAX_PATH_UNITS = 1000;
        const int PATH_UNIT_LENGTH = 8;

        List<Tank> tanks = [];
        List<Vector2> ricoPoints = [];
        List<Vector2> tnkPoints = [];

        pattern ??= c => c.Properties.IsSolid || c.Type == BlockID.Teleporter;

        var whitePixel = TextureGlobals.Pixels[Color.White];
        Vector2 pathPos = Position + offset.Rotate(-TurretRotation);
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
                ricoPoints.Add(pathPos);
                pathDir.X *= -1;
                ricochetCount++;
                if (doBounceReset) uninterruptedIterations = 0;
            }
            else if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                ricoPoints.Add(pathPos);
                pathDir.Y *= -1;
                ricochetCount++;
                if (doBounceReset) uninterruptedIterations = 0;
            }

            // Setup hitbox once
            pathHitbox.X = (int)pathPos.X - 5;
            pathHitbox.Y = (int)pathPos.Y - 5;
            pathHitbox.Width = 8;
            pathHitbox.Height = 8;

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
                    ricoPoints.Add(pathPos);
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

            // Delay teleport until next frame
            if (teleported && i == tpTriggerIndex) {
                pathPos = teleportedTo;
            }

            // Check destroy conditions
            bool hitsInstant = i == 0 && Block.AllBlocks.Any(x => x != null && x.Hitbox.Intersects(pathHitbox) && pattern(x));
            bool hitsTooEarly = i < (int)Properties.ShellSpeed / 2 && ricochetCount > 0;
            bool ricochetLimitReached = ricochetCount > Properties.RicochetCount;

            if (hitsInstant || hitsTooEarly || ricochetLimitReached)
                break;

            // Check tanks BEFORE moving
            float realMiss = 1f + missDist * uninterruptedIterations;

            foreach (var enemy in GameHandler.AllTanks) {
                if (enemy is null || enemy.Dead || tanks.Contains(enemy)) continue;

                if (i > 15 && GameUtils.Distance_WiiTanksUnits(enemy.Position, pathPos) <= realMiss) {
                    var pathAngle = pathDir.ToRotation();
                    var toEnemy = pathPos.DirectionTo(enemy.Position).ToRotation();

                    if (MathUtils.AbsoluteAngleBetween(pathAngle, toEnemy) >= MathHelper.PiOver2)
                        tanks.Add(enemy);
                }

                var pathCircle = new Circle { Center = pathPos, Radius = 4 };
                if (enemy.CollisionCircle.Intersects(pathCircle)) {
                    tnkPoints.Add(pathPos);
                    tanks.Add(enemy);
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

        tankCollPoints = [.. tnkPoints];
        ricochetPoints = [.. ricoPoints];
        return tanks;
    }
}
