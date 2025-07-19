using Microsoft.Xna.Framework;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Collision;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.AI; 
public partial class AITank {
    public bool IsTooCloseToObstacle;

    public bool DoMovements = true;
    public bool DoMoveTowards = true;

    public int CurrentRandomMove;

    // random movements do not happen until this queue is empty
    // random turns ARE added to the pivot queue
    public List<Vector2> PivotQueue = [];
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

        if (Properties.MaxSpeed > 0) {
            Console.WriteLine("Pivot Queue:    " + PivotQueue.Count);
            Console.WriteLine("Pivot Subqueue: " + SubPivotQueue.Count);
        }

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

                // divides because uh... idk. might change based on what bigkitty says
                // TargetTankRotation += randomTurn / 4;

                // aggression/pursuit handling
                // this implementation does NOT apply aggressiveness to the random movement bias, but rather just overrides it
                /*if (TargetTank is not null) {
                    var toTarget = Vector2.Normalize(TargetTank.Position - Position);
                    var toPath = Vector2.Normalize(PathEndpoint - Position);

                    var finalVector = Vector2.Normalize(Vector2.Lerp(toPath, toTarget, AiParams.AggressivenessBias));

                    TargetTankRotation = finalVector.ToRotation() - MathHelper.PiOver2;
                }*/
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

        // strictly 
        IsTooCloseToObstacle = RaycastAheadOfTank(Parameters.ObstacleAwarenessMovement * Speed);

        // don't bother doing anything else since it's not blocked
        if (!IsTooCloseToObstacle) {
            //Console.WriteLine("Obstacle not found.. block navigation skipped.");
            return;
        }

        var dist = Parameters.ObstacleAwarenessMovement;

        // returns if the path is blocked
        var left = !RaycastAheadOfTank(dist, -MathHelper.PiOver2) ? CollisionDirection.Left : CollisionDirection.None;
        var right = !RaycastAheadOfTank(dist, MathHelper.PiOver2) ? CollisionDirection.Right : CollisionDirection.None;

        bool goBackwards = false;

        if (left == CollisionDirection.None && right == CollisionDirection.None)
            goBackwards = true;

        var dir = CollisionDirection.None;

        if (!goBackwards) {
            if (left != CollisionDirection.None && right != CollisionDirection.None) {
                var random = Client.ClientRandom.Next(0, 2);

                dir = random == 0 ? left : right;
            } else {
                // should only be one or the other in this branch
                if (left != CollisionDirection.None)
                    dir = left;
                else
                    dir = right;
            }
            Console.WriteLine($"[{TankID.Collection.GetKey(AiTankType)} ({AITankId})] Wall detected, moving " + dir);
        }
        else {
            Console.WriteLine($"[{TankID.Collection.GetKey(AiTankType)} ({AITankId})] Wall detected, moving Backwards");
        }

        float vecRot;
        if (goBackwards)
            vecRot = MathHelper.Pi;
        else
            vecRot = dir == left ? -MathHelper.PiOver2 : MathHelper.PiOver2;

        var movementDirection = Vector2.UnitY.Rotate(TankRotation + vecRot);

        SubPivotQueue.Clear();

        if (PivotQueue.Count > 0) {
            PivotQueue.Insert(0, movementDirection);
            Console.WriteLine("Pivot 0 overwritten.");
        }
        else
            PivotQueue.Add(movementDirection);

        /*IsPathBlocked = IsObstacleInWay(framesLookAhead, tankDirection, out var travelPath, out var reflections, TankPathCheckSize);
        if (IsPathBlocked) {
            if (reflections.Length > 0) {
                var dirOf = travelPath.DirectionTo(Position).ToRotation();
                var refAngle = dirOf + MathHelper.PiOver2;

                // this is a very bandaid fix....
                if (refAngle % MathHelper.PiOver2 == 0) {
                    refAngle += Client.ClientRandom.NextFloat(0, MathHelper.TwoPi);
                }
                TargetTankRotation = refAngle;
            }

            // TODO: i literally do not understand this
        }*/
    }
    public void DoRandomMove() {
        var randomTurn = Client.ClientRandom.NextFloat(-Parameters.MaxAngleRandomTurn, Parameters.MaxAngleRandomTurn);

        if (TargetTank is not null) {
            var toTarget = Vector2.Normalize(TargetTank.Position - Position);
            float targetAngle = toTarget.ToRotation() - MathHelper.PiOver2;

            // shortest signed angle difference
            // MathHelper.WrapAngle() ?
            float angleDifference = MathHelper.WrapAngle(targetAngle - TankRotation);

            // negatives don't work?

            // applies bias toward or away from the target's angle
            randomTurn += angleDifference * 2 * Parameters.AggressivenessBias;
        }

        float finalAngle = TankRotation + randomTurn;
        Vector2 direction = Vector2.UnitY.Rotate(finalAngle);

        // add to list
        Console.WriteLine();
        Console.WriteLine("Random movement: " + MathHelper.ToDegrees(direction.ToRotation()));
        Console.WriteLine();
        PivotQueue.Add(direction);
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
        var pivotDir = PivotQueue[0];
        for (int i = 0; i < Parameters.MaxQueuedMovements; i++) {
            //SubPivotQueue.Add(Vector2.UnitY.Rotate(MathHelper.PiOver2 * i));
            SubPivotQueue.Add(MathUtils.SlerpWtf(Vector2.UnitY.Rotate(TankRotation), pivotDir, 1f / Parameters.MaxQueuedMovements * (i + 1)));
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
