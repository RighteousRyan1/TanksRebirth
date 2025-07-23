using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Enums;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.AI; 

public enum PivotType {
    RandomTurn = 2,
    NavTurn = 1,
}
public partial class AITank {
    public bool IsTooCloseToObstacle;

    public bool DoMovements = true;
    public bool DoMoveTowards = true;

    public int CurrentRandomMove;

    // random movements do not happen until this queue is empty
    // random turns ARE added to the pivot queue
    public List<(Vector2 Direction, PivotType Type)> PivotQueue = [];
    public List<Vector2> SubPivotQueue = [];


    public bool IsSurviving;

    // this code runs every "movement opportunity" (any number between RandomTimerMinMove and RandomTimerMaxMove)
    public void DoMovement() {
        // IsTurning is on crack?
        bool shouldMove = !IsTurning && CurMineStun <= 0 && CurShootStun <= 0;

        // CurrentRandomMove = 60;
        //Properties.ShellLimit = 0;
        //Properties.MineLimit = 0;

        if (!shouldMove) return;
        if (!Behaviors[0].IsModOf(CurrentRandomMove)) return;

        /*if (Properties.MaxSpeed > 0) {
            Console.WriteLine("Pivot Queue:    " + PivotQueue.Count);
            Console.WriteLine("Pivot Subqueue: " + SubPivotQueue.Count);
        }*/

        //Console.WriteLine("Creating new pivot sequence...");
        //PivotQueue.Add(Vector2.UnitY.Rotate(TankRotation + MathHelper.PiOver2));

        if (PivotQueue.Count == 0 && SubPivotQueue.Count == 0) {
            IsSurviving = false;
            // determine the frame timer for a new movement opportunity
            CurrentRandomMove = Client.ClientRandom.Next(Parameters.RandomTimerMinMove, Parameters.RandomTimerMaxMove);
            Behaviors[0].Value = 0;

            // might need to check specific dangers here like mines or shells
            // my loneliness is killing me (and i) I must confess, i still believe (still believe)

            var dangerPositions = GetEvasionData();

            DoBlockNav();

            // the tank avoids the average position of all dangers
            if (dangerPositions.Count > 0) {
                var averageDangerPosition = dangerPositions.Aggregate(Vector2.Zero, (sum, pos) => sum + pos) / dangerPositions.Count;

                // Console.WriteLine("Evading " + dangerPositions.Count + " dangers");

                // unsure yet if this should be added to the queue or not
                Avoid(averageDangerPosition);
            }
            else if (!IsTooCloseToObstacle && !IsSurviving) {
                DoRandomMove();
            }
        }
        // only generates a subqueue if there are no large pivot queues and there are not already a subqueue
        TryGenerateSubQueue();

        // only works the subqueue if there is a subqueue
        TryWorkSubQueue();
    }
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
        if (fracL.IsWithinRange(fracR, 0.0025f)) {
            // backwards, not down, lol
            dir = CollisionDirection.Down;
        }

        float vecRot;
        //if (goBackwards)
        //vecRot = MathHelper.Pi;
        //else
        var redirectAngle = MathHelper.PiOver2; // normally /2

        if (dir != CollisionDirection.Down)
            vecRot = dir == /*left*/ CollisionDirection.Left ? -redirectAngle : redirectAngle;
        else
            vecRot = MathHelper.Pi;

        var movementDirection = Vector2.UnitY.Rotate(TankRotation + vecRot);

        // SubPivotQueue.Clear();

        if (/*PivotQueue.Count > 0*/ !PivotQueue.Any(x => x.Type == PivotType.NavTurn)) {
            PivotQueue.Insert(0, (movementDirection, PivotType.NavTurn));
            // Console.WriteLine("Pivot 0 overwritten.");
        }
        else {
            // PivotQueue.Add(movementDirection);
        }
    }
    public void DoRandomMove() {
        // previous impl
        var randomTurn = Client.ClientRandom.NextFloat(-Parameters.MaxAngleRandomTurn, Parameters.MaxAngleRandomTurn);

        if (TargetTank is not null) {
            var toTarget = Vector2.Normalize(TargetTank.Position - Position);
            float targetAngle = toTarget.ToRotation() - MathHelper.PiOver2;

            // shortest signed angle difference
            // MathHelper.WrapAngle() ?
            float angleDifference = MathHelper.WrapAngle(targetAngle - TankRotation);

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

        TargetTankRotation += randomTurn / 2;
    }
    // if raycasting based on radii, divide what is input to distance by two
    public bool RaycastAheadOfTank(float distance, float offset = 0f, RayCastReportFixtureDelegate? callback = null) {
        bool isPathBlocked = false;

        // the tank by default faces down, so positive Y.
        var dir = Vector2.UnitY.Rotate(TankRotation + offset);

        // switch to using game units if necessary?
        var gameUnits = GameUtils.Value_WiiTanksUnits(TNK_WIDTH + distance);
        var endpoint = Physics.Position + dir * gameUnits / UNITS_PER_METER;

        CollisionsWorld.RayCast((fixture, point, normal, fraction) => {
            callback?.Invoke(fixture, point, normal, fraction);

            if (fixture.Body.Tag is Block or GameScene.BoundsRenderer.BOUNDARY_TAG) {
                isPathBlocked = true;
            }

            return fraction;
            // divide by 2 because it's a radius, i think
        }, Physics.Position, endpoint);

        return isPathBlocked;
    }
    public bool TryGenerateSubQueue() {
        if (PivotQueue.Count == 0) return false;
        if (SubPivotQueue.Count > 0) return false;
        // grab from the top of the queue
        var pivot = PivotQueue[0];
        var desiredCuts = Parameters.MaxQueuedMovements;

        for (int i = 0; i < desiredCuts; i++) {
            //SubPivotQueue.Add(Vector2.UnitY.Rotate(MathHelper.PiOver2 * i));
            SubPivotQueue.Add(MathUtils.SlerpWtf(Vector2.UnitY.Rotate(TankRotation), pivot.Direction, 1f / desiredCuts * (i + 1)));
        }
        // drop the first element since this works as a queue under the hood
        PivotQueue.RemoveAt(0);

        return true;
    }
    public bool TryWorkSubQueue() {
        if (SubPivotQueue.Count == 0) return false;

        TargetTankRotation = SubPivotQueue[0].ToRotation() - MathHelper.PiOver2;

        // drop the first element again, but for the sub-queue
        SubPivotQueue.RemoveAt(0);

        return true;
    }
}
