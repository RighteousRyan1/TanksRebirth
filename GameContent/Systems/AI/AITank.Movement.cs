using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.AI; 

/// <summary>The kind of pivot entered into a pivot queue.</summary>
public enum PivotType {
    RandomTurn = 2,
    NavTurn = 1,
}
public partial class AITank {
    public bool IsTooCloseToObstacle;

    /// <summary>Whether or not this tank should perform movement logic.</summary>
    public bool DoMovements = true;
    /// <summary>Whether or not this tank should update its desired direction.</summary>
    public bool DoMoveTowards = true;

    public int CurrentRandomMove;

    // random movements do not happen until this queue is empty
    // random turns SHOULD BE (but not now) added to the pivot queue
    /// <summary>A queue of movements that will be split into <see cref="AIParameters.MaxQueuedMovements"/> sub-turns, which are entered into <see cref="SubPivotQueue"/>.</summary>
    public Queue<(Vector2 Direction, PivotType Type)> PivotQueue = [];
    /// <summary>The most recently removed <see cref="PivotQueue"/> entry, divided into <see cref="AIParameters.MaxQueuedMovements"/> turns.</summary>
    public Queue<Vector2> SubPivotQueue = [];

    /// <summary>Whether or not this <see cref="AITank"/> is avoiding a dangerous object.</summary>
    public bool IsSurviving;

    // this code runs every "movement opportunity" (any number between RandomTimerMinMove and RandomTimerMaxMove)
    /// <summary>Default movement handling for this <see cref="AITank"/>. Includes random movements, avoidance, PivotQueue/SubQueue working, and obstacle navigation.</summary>
    public void DoMovement() {
        // IsTurning is on crack?
        bool shouldMove = !IsTurning && CurMineStun <= 0 && CurShootStun <= 0;

        if (!shouldMove) return;
        if (!Behaviors[0].IsModOf(CurrentRandomMove)) return;

        CurrentRandomMove = Client.ClientRandom.Next(Parameters.RandomTimerMinMove, Parameters.RandomTimerMaxMove);
        Behaviors[0].Value = 0;

        // only works the subqueue if there is a subqueue
        TryWorkSubQueue();

        if (PivotQueue.Count == 0 && SubPivotQueue.Count == 0 && !IsInDanger) {
            IsSurviving = false;

            if (!IsTooCloseToObstacle && !IsSurviving) {
                DoRandomMove();
            }
        }

        DoBlockNav();

        // the tank avoids the average position of all dangers
        if (NearbyDangers.Count > 0) {
            var averageDangerPosition = NearbyDangers.Aggregate(Vector2.Zero, (sum, dng) => sum + dng.Position) / NearbyDangers.Count;

            SubPivotQueue.Clear();
            PivotQueue.Clear();

            // Console.WriteLine("Evading " + dangerPositions.Count + " dangers");

            // unsure yet if this should be added to the queue or not
            Avoid(averageDangerPosition);
        }

        // only generates a subqueue if there are no large pivot queues and there are not already a subqueue
        TryGenerateSubQueue();

        /*if (Properties.MaxSpeed > 0) {
            Console.WriteLine("Pivot Queue:    " + PivotQueue.Count);
            Console.WriteLine("Pivot Subqueue: " + SubPivotQueue.Count);
        }*/
    }
    /// <summary>Makes this <see cref="AITank"/> navigate around obstacles.</summary>
    public void DoBlockNav() {
        // dont navigate if running away from something
        if (IsSurviving) return;
        //uint framesLookAhead = AiParams.ObstacleAwarenessMovement / 2;
        //var tankDirection = Vector2.UnitY.Rotate(TargetTankRotation);

        var checkDist = Parameters.ObstacleAwarenessMovement / 2;
        // strictly 
        IsTooCloseToObstacle = RaycastAheadOfTank(checkDist * Speed);

        // don't bother doing anything else since it's not blocked
        if (!IsTooCloseToObstacle) {
            // Console.WriteLine("Obstacle not found.. block navigation skipped.");
            return;
        }

        float angleDiff = MathHelper.PiOver4 / 2; // normally pi/2

        float fracL = -1f;
        float fracR = -1f;

        bool checkLeft = RaycastAheadOfTank(checkDist * 100, -angleDiff,
            (fixture, point, normal, fraction) => {
                fracL = fraction;
                return fraction;
            });

        bool checkRight = RaycastAheadOfTank(checkDist * 100, angleDiff,
            (fixture, point, normal, fraction) => {
                fracR = fraction;
                return fraction;
            });

        var dir = fracL > fracR ? CollisionDirection.Left : CollisionDirection.Right;

        // if the rays are highly similar in distance, reverse, since you're most likely heading into a wall directly
        if (fracL.IsWithinRange(fracR, 0.00125f)) {
            // backwards, not down, lol
            dir = CollisionDirection.Down;
        }

        float vecRot;
        //if (goBackwards)
        //vecRot = MathHelper.Pi;
        //else
        var redirectAngle = MathHelper.PiOver2; // normally /2

        if (dir != CollisionDirection.Down)
            vecRot = dir == CollisionDirection.Left ? -redirectAngle : redirectAngle;
        else
            vecRot = MathHelper.Pi + Client.ClientRandom.NextFloat(-0.5f, 0.5f);

        var movementDirection = Vector2.UnitY.Rotate(ChassisRotation + vecRot);

        // SubPivotQueue.Clear();

        if (!PivotQueue.Any(x => x.Type == PivotType.NavTurn)) {
            PivotQueue.Enqueue((movementDirection, PivotType.NavTurn));
            // Console.WriteLine("Pivot 0 overwritten.");
        }
    }
    /// <summary>Makes this <see cref="AITank"/> perform a random turn.</summary>
    public void DoRandomMove() {
        // previous impl
        var randomTurn = Client.ClientRandom.NextFloat(-Parameters.MaxAngleRandomTurn, Parameters.MaxAngleRandomTurn);

        if (TargetTank is not null) {
            var toTarget = Vector2.Normalize(TargetTank.Position - Position);
            float targetAngle = toTarget.ToRotation() - MathHelper.PiOver2;

            // shortest signed angle difference
            // MathHelper.WrapAngle() ?
            float angleDifference = MathHelper.WrapAngle(targetAngle - ChassisRotation);

            // negatives don't work?

            // applies bias toward or away from the target's angle
            randomTurn += angleDifference * Parameters.AggressivenessBias;
        }

        /*float finalAngle = TankRotation + randomTurn;
        Vector2 direction = Vector2.UnitY.Rotate(finalAngle);

        PivotQueue.Add((direction, PivotType.RandomTurn));

        // add to list
        Console.WriteLine();
        Console.WriteLine("Random movement: " + MathHelper.ToDegrees(direction.ToRotation()));
        Console.WriteLine();*/

        DesiredChassisRotation += randomTurn / 2;
    }
    // if raycasting based on radii, divide what is input to distance by two
    /// <summary>Performs a raycast to check for blocking objects, with respect to <see cref="Tank.ChassisRotation"/>.</summary>
    /// <param name="distance">The distance ahead of the tank, in world units, not physics units.</param>
    /// <param name="offset">The angle offset for the check.</param>
    /// <param name="callback">Callback data to do custom logic with the raycast result.</param>
    /// <returns>Whether or not the ray had an intersection.</returns>
    public bool RaycastAheadOfTank(float distance, float offset = 0f, RayCastReportFixtureDelegate? callback = null) {
        bool isPathBlocked = false;

        // the tank by default faces down, so positive Y.
        var dir = Vector2.UnitY.Rotate(ChassisRotation + offset);

        // switch to using game units if necessary?
        var gameUnits = GameUtils.Value_WiiTanksUnits(TNK_WIDTH + distance);
        var endpoint = Physics.Position + dir * gameUnits / UNITS_PER_METER;

        // exceptions thrown here, "Stack is empty", assuming race conditions? (and sometimes nullreference?)
        try {
            CollisionsWorld.RayCast((fixture, point, normal, fraction) => {
                callback?.Invoke(fixture, point, normal, fraction);

                if (fixture.Body.Tag is Block or GameScene.BoundsRenderer.BOUNDARY_TAG) {
                    isPathBlocked = true;
                }

                return fraction;
                // divide by 2 because it's a radius, i think
            }, Physics.Position, endpoint);
        }
        catch { return false; }
        return isPathBlocked;
    }
    /// <summary>Attempts to dequeue from <see cref="PivotQueue"/> and split it into <see cref="AIParameters.MaxQueuedMovements"/> smaller turns.</summary>
    /// <returns>Whether or not the attempt was successful.</returns>
    public bool TryGenerateSubQueue() {
        if (PivotQueue.Count == 0) return false;
        if (SubPivotQueue.Count > 0) return false;
        // grab from the top of the queue
        var pivot = PivotQueue.Dequeue(); //PivotQueue[0];
        var desiredCuts = Parameters.MaxQueuedMovements;

        for (int i = 0; i < desiredCuts; i++) {
            //SubPivotQueue.Add(Vector2.UnitY.Rotate(MathHelper.PiOver2 * i));
            SubPivotQueue.Enqueue(MathUtils.Slerp2D(Vector2.UnitY.Rotate(ChassisRotation), pivot.Direction, 1f / desiredCuts * (i + 1)));
        }
        // drop the first element since this works as a queue under the hood
        // PivotQueue.RemoveAt(0);

        return true;
    }
    /// <summary>Attempts to dequeue from <see cref="SubPivotQueue"/> and adjust this <see cref="AITank"/>'s <see cref="Tank.DesiredChassisRotation"/>.</summary>
    /// <returns>Whether or not the dequeue was successful.</returns>
    public bool TryWorkSubQueue() {
        if (SubPivotQueue.Count == 0) return false;

        DesiredChassisRotation = /*SubPivotQueue[0]*/SubPivotQueue.Dequeue().ToRotation() - MathHelper.PiOver2;

        // drop the first element again, but for the sub-queue
        // SubPivotQueue.RemoveAt(0);

        return true;
    }
}
