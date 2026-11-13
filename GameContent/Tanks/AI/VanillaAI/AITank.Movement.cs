using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Tanks.AI.VanillaAI;

public partial struct VanillaAISystem {
    /// <summary>Whether or not this tank should perform movement logic.</summary>
    public bool DoMovements = true;
    /// <summary>Whether or not this tank should update its desired direction.</summary>
    public bool DoMoveTowards = true;

    /// <summary>Whether or not this <see cref="AITank"/> is avoiding a dangerous object.</summary>
    public bool IsSurviving;
    /// <summary>Whether or not this <see cref="AITank"/> is running from a mine it laid itself. It keeps following its history until it's clear.</summary>
    public bool EscapingOwnMine;
    /// <summary>The active value of <see cref="AIParameters.CantShootWhileFleeing"/>.</summary>
    public bool AttackPrevented;

    public const int HISTORY_SLOTS = 8;
    // decomp values of weights used for each dangerous object. this could PROBABLY just be dropped and done mathematically, but it's ****slightly**** less faithful
    static readonly float[] _weights = [1f, 0.8f, 0.6f, 0.4f];

    // the last directions this tank picked, newest first. the desired direction is the average of the first MaxMoveHistory of these
    internal Vector2[] _history;
    readonly List<Shell> _moving = [];
    readonly List<IAITankDanger> _stationary = [];

    readonly float SpeedPerFrame => Owner.Properties.MaxSpeed * Tank.SPEED_SCALING; // same scale as the physics velocity

    // this code runs every "movement opportunity" (any number between RandomTimerMinMove and RandomTimerMaxMove)
    /// <summary>Default movement handling for this <see cref="AITank"/>. Wanders, dodges shells and mines, or runs from its own mine, then averages its history into <see cref="Tank.DesiredChassisRotation"/>.</summary>
    public void DoMovement() {
        // not sure this matches the original
        bool shouldMove = !Owner.IsTurning && Owner.CurMineStun <= 0 && Owner.CurShootStun <= 0;

        if (!shouldMove) return;
        if (!ChassisMovement.TimerSatisfies(Owner.CurrentRandomMove)) return;

        if (_history is null) {
            _history = new Vector2[HISTORY_SLOTS];
            Array.Fill(_history, MathUtils.DirectionOf(Owner.ChassisRotation));
        }

        IsSurviving = false;
        AttackPrevented = false;

        NearbyDangers = GetEvasionData();
        ClosestDanger = NearbyDangers.Closest(Owner.Position);

        Owner.CurrentRandomMove = PRNG.Next(Owner.Parameters.RandomTimerMinMove, Owner.Parameters.RandomTimerMaxMove);
        ChassisMovement.Value = 0;

        // for what i know, does something where the tank only dodges and meanders when moving away from a mine IT laid.
        // nintendo devs are so weird for this bro
        if (EscapingOwnMine && !_nearOwnMine)
            EscapingOwnMine = false;

        // while escaping we just keep following the history
        if (!EscapingOwnMine) {
            if (NearbyDangers.Count > 0)
                Dodge();
            else
                Meander();
        }

        var sum = Vector2.Zero;
        for (int i = 0; i < Math.Clamp(Owner.Parameters.MaxMoveHistory, 1, HISTORY_SLOTS); i++)
            sum += _history[i];

        if (sum != Vector2.Zero)
            Owner.DesiredChassisRotation = MathUtils.RotationOf(sum.NormalizeOrDefault());
    }

    readonly void PushHistory(Vector2 direction) {
        ArrayUtils.Shift(_history, 1);
        _history[0] = direction;
    }

    /// <summary>Makes this <see cref="AITank"/> pick a new random direction.</summary>
    readonly void Meander() {
        var direction = _history[0];

        if (Owner.TargetTank is not null)
            direction += (Owner.TargetTank.Position - Owner.Position).NormalizeOrDefault() * Owner.Parameters.AggressivenessBias;

        direction = direction.NormalizeOrDefault(_history[0]);

        // ObstacleAwarenessMovement is a number of frames here, not a distance
        var sweep = GameUtils.SweepForWalls(Owner.Position, direction, SpeedPerFrame, (int)Owner.Parameters.ObstacleAwarenessMovement, scanRadius: Owner.Parameters.SweepScanRadius);
        if (sweep.Hit)
            direction = (direction + sweep.Normal).NormalizeOrDefault(direction);

        var angle = PRNG.NextFloat(-Owner.Parameters.MaxAngleRandomTurn, Owner.Parameters.MaxAngleRandomTurn);

        PushHistory((direction + direction.Perpendicular() * MathF.Tan(angle)).NormalizeOrDefault(direction));
    }

    // could make it where any danger with a velocity makes it dodge perpendicularly?
    /// <summary>Makes this <see cref="AITank"/> dodge. <see cref="Shell"/>s push it sideways and away.</summary>
    void Dodge() {
        IsSurviving = true;
        AttackPrevented = Owner.Parameters.CantShootWhileFleeing;

        var position = Owner.Position;

        _moving.Clear();
        _stationary.Clear();
        for (int i = 0; i < NearbyDangers.Count; i++) {
            if (NearbyDangers[i] is Shell shell)
                _moving.Add(shell);
            // everything that isn't a shell is just pushed away from by position
            else
                _stationary.Add(NearbyDangers[i]);
        }
        // sorting to apply _weights to, closest gets full priority
        _moving.Sort((a, b) => a.Position.DistanceTo(position).CompareTo(b.Position.DistanceTo(position)));
        _stationary.Sort((a, b) => a.Position.DistanceTo(position).CompareTo(b.Position.DistanceTo(position)));

        var push = Vector2.Zero;

        // for both loops, only iterate up to 4 times. refer to _weights comment later!!!
        for (int i = 0; i < Math.Min(_moving.Count, _weights.Length); i++) {
            var shell = _moving[i];
            var heading = shell.Velocity.NormalizeOrDefault();
            var away = position - shell.Position;

            // anti indiana jones movement
            var sideways = (away - heading * Vector2.Dot(away, heading)).NormalizeOrDefault(heading.Perpendicular());

            push += _weights[i] * (sideways + away.NormalizeOrDefault());
        }
        if (_moving.Count > 0) {
            push = push.NormalizeOrDefault(_history[0]);
            
            // doing what nintendo did, apparently. they called this twice each time
            PushHistory(push); PushHistory(push);
        }
        if (_stationary.Count == 0) return;

        // mines add on to whatever the shells already pushed
        for (int i = 0; i < Math.Min(_stationary.Count, _weights.Length); i++)
            push += _weights[i] * (position - _stationary[i].Position).NormalizeOrDefault();

        push = push.NormalizeOrDefault(_history[0]);

        PushHistory(push); PushHistory(push);
    }

    /// <summary>Picks the direction this <see cref="AITank"/> runs in after laying a mine.</summary>
    /// <returns>False if there's nowhere good to go, meaning the mine shouldn't be laid.</returns>
    public bool CanEscapeOwnMine() {
        var forward = MathUtils.DirectionOf(Owner.ChassisRotation);
        // all cardinals. truly disgusting that this is a heap object
        Vector2[] directions = [forward, -forward, forward.Perpendicular(), -forward.Perpendicular()];

        // ObstacleAwarenessMine is a distance in the original's units
        var escapeDistance = GameUtils.TanksUnits(Owner.Parameters.ObstacleAwarenessMine);
        var frames = (int)(3f * Owner.Parameters.ObstacleAwarenessMine / MathF.Max(Owner.Properties.MaxSpeed, MathUtils.VERY_SMALL));

        var farthest = escapeDistance;
        var farthestIndex = -1;

        for (int i = 0; i < directions.Length; i++) {
            var sweep = GameUtils.SweepForWalls(Owner.Position, directions[i], SpeedPerFrame, frames, scanRadius: Owner.Parameters.SweepScanRadius);

            // a direction with nothing in the way is taken right away
            if (!sweep.Hit) {
                InitMineEscape(directions[i]);
                return true;
            }
            // ...otherwise remember the one with the most room
            if (sweep.Distance > farthest) {
                farthest = sweep.Distance;
                farthestIndex = i;
            }
        }
        if (farthestIndex == -1) return false;

        InitMineEscape(directions[farthestIndex]);
        return true;
    }

    void InitMineEscape(Vector2 direction) {
        _history ??= new Vector2[HISTORY_SLOTS];
        Array.Fill(_history, direction);
        EscapingOwnMine = true;
        Owner.DesiredChassisRotation = MathUtils.RotationOf(direction);
    }
}
