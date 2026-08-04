using Microsoft.Xna.Framework;
using System.Collections.Generic;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Enums;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Tanks.AI.VanillaAI;

public partial struct VanillaAISystem {
    public bool IsTooCloseToObstacle;

    /// <summary>Whether or not this tank should perform movement logic.</summary>
    public bool DoMovements = true;
    /// <summary>Whether or not this tank should update its desired direction.</summary>
    public bool DoMoveTowards = true;
    
    public Vector2 AvoidPosition;

    // random movements do not happen until this queue is empty
    // random turns SHOULD BE (but not now) added to the pivot queue
    /// <summary>A queue of movements that will be split into <see cref="AIParameters.MaxQueuedMovements"/> sub-turns, which are entered into <see cref="SubPivotQueue"/>.</summary>
    public Queue<Vector2> PivotQueue = [];
    /// <summary>The most recently removed <see cref="PivotQueue"/> entry, divided into <see cref="AIParameters.MaxQueuedMovements"/> turns.</summary>
    public Queue<Vector2> SubPivotQueue = [];

    /// <summary>Whether or not this <see cref="AITank"/> is avoiding a dangerous object.</summary>
    public bool IsSurviving;

    // this code runs every "movement opportunity" (any number between RandomTimerMinMove and RandomTimerMaxMove)
    /// <summary>Default movement handling for this <see cref="AITank"/>. Includes random movements, avoidance, PivotQueue/SubQueue working, and obstacle navigation.</summary>
    public void DoMovement() {
        // IsTurning is on crack?

        bool shouldMove = !Owner.IsTurning && Owner.CurMineStun <= 0 && Owner.CurShootStun <= 0;

        if (!shouldMove) return;
        if (!ChassisMovement.TimerSatisfies(Owner.CurrentRandomMove)) return;

        NearbyDangers = GetEvasionData();
        ClosestDanger = NearbyDangers.Closest(Owner.Position);

        // realistically... it will never avoid from its own position.
        // so this should be safe
        AvoidPosition = Vector2.Zero;

        Owner.CurrentRandomMove = Client.ClientRandom.Next(Owner.Parameters.RandomTimerMinMove, Owner.Parameters.RandomTimerMaxMove);
        ChassisMovement.Value = 0;

        if (PivotQueue.Count == 0 && SubPivotQueue.Count == 0 && !IsInDanger) {
            IsSurviving = false;

            if (!IsTooCloseToObstacle && !IsSurviving) {
                DoRandomMove();
            }
        }

        DoBlockNav();

        // the tank avoids the average position of all dangers
        if (NearbyDangers.Count > 0) {
            for (int i = 0; i < NearbyDangers.Count; i++)
                AvoidPosition += NearbyDangers[i].Position;

            var averageDangerPosition = AvoidPosition / NearbyDangers.Count;

            SubPivotQueue.Clear();
            PivotQueue.Clear();

            // Console.WriteLine("Evading " + dangerPositions.Count + " dangers");

            // unsure yet if this should be added to the queue or not
            Avoid(averageDangerPosition);
        }

        // something about this code (the order, probably) causes tanks to kind of stare at walls temporarily
        // only generates a subqueue if there are no large pivot queues and there are not already a subqueue
        TryGenerateSubQueue();

        // only works the subqueue if there is a subqueue
        TryWorkSubQueue();

        /*if (Properties.MaxSpeed > 0) {
            Console.WriteLine("Pivot Queue:    " + PivotQueue.Count);
            Console.WriteLine("Pivot Subqueue: " + SubPivotQueue.Count);
        }*/
    }
    /// <summary>Makes this <see cref="AITank"/> navigate around obstacles.</summary>
    public void DoBlockNav() {
        // dont navigate if running away from something
        if (IsSurviving) return;
        if (SubPivotQueue.Count > 0) return;
        //uint framesLookAhead = AiParams.ObstacleAwarenessMovement / 2;
        //var tankDirection = Vector2.UnitY.RotatedBy(TargetTankRotation);

        var checkDist = Owner.Parameters.ObstacleAwarenessMovement / 2;
        // var rayNormal = Vector2.Zero;
        // strictly 
        IsTooCloseToObstacle = Owner.RaycastAheadOfTank(checkDist /* Speed*/);

        // don't bother doing anything else since it's not blocked
        if (!IsTooCloseToObstacle) {
            // Console.WriteLine("Obstacle not found.. block navigation skipped.");
            return;
        }

        #region Perpendicular Wall Hit
        float angleDiff = MathHelper.PiOver4 / 2; // normally MathHelper.PiOver2

        float fracL = -1f;
        float fracR = -1f;

        bool checkLeft = Owner.RaycastAheadOfTank(checkDist * 100, -angleDiff,
            (fixture, point, normal, fraction) => {
                fracL = fraction;

                return fraction;
            });

        bool checkRight = Owner.RaycastAheadOfTank(checkDist * 100, angleDiff,
            (fixture, point, normal, fraction) => {
                fracR = fraction;
                return fraction;
            });
        /*var dir = CollisionDirection.Down;
        if (!checkLeft && checkRight)
            dir = CollisionDirection.Right;
        else if (checkLeft && !checkRight)
            dir = CollisionDirection.Left;*/
        var dir = fracL > fracR ? CollisionDirection.Left : CollisionDirection.Right;

        // if the rays are highly similar in distance, reverse, since you're most likely heading into a wall directly
        if (fracL.IsWithinRange(fracR, 0.00125f)) {
            // backwards, not down, lol
            dir = CollisionDirection.Down;
        }
        #endregion

        float vecRot;

        if (dir != CollisionDirection.Down) {
            var redirectAngle = 1.3f; // MathHelper.PiOver2; // normally /2
            vecRot = dir == CollisionDirection.Left ? -redirectAngle : redirectAngle;
        }
        else {
            vecRot = MathHelper.Pi + Client.ClientRandom.NextFloat(-0.5f, 0.5f);
        }

        PivotQueue.Clear();

        // old = Vector2.UnitY.RotatedBy(-rayNormal.ToRotation() - MathHelper.PiOver2);
        var movementDirection = Vector2.UnitY.RotatedBy(Owner.ChassisRotation + vecRot);

        PivotQueue.Enqueue(movementDirection);
    }
    /// <summary>Makes this <see cref="AITank"/> perform a random turn.</summary>
    public readonly void DoRandomMove() {
        var randomTurn = Client.ClientRandom.NextFloat(-Owner.Parameters.MaxAngleRandomTurn, Owner.Parameters.MaxAngleRandomTurn);

        // aggressiveness
        if (Owner.TargetTank is not null) {
            // dirvec to target -> gets that angle
            // difference in angle -> multiplies by aggressiveness
            var toTarget = Vector2.Normalize(Owner.TargetTank.Position - Owner.Position);
            float targetAngle = toTarget.ToRotation() - MathHelper.PiOver2;

            // shortest signed angle difference
            float angleDifference = MathHelper.WrapAngle(targetAngle - Owner.ChassisRotation);

            // negatives don't work?

            // applies bias toward or away from the target's angle
            randomTurn += angleDifference * Owner.Parameters.AggressivenessBias;
        }

        // this causes extremely weak movement...
        /*float finalAngle = ChassisRotation + randomTurn;
        Vector2 direction = Vector2.UnitY.RotatedBy(finalAngle);

        PivotQueue.Enqueue(direction);*/

        //ChatSystem.SendMessage("Start: " + MathHelper.ToDegrees(ChassisRotation), ColorUtils.DiscoPartyColor);
        //ChatSystem.SendMessage("End: " + MathHelper.ToDegrees(direction.ToRotation()), ColorUtils.DiscoPartyColor);

        // is / 2 necessary?
        // i think so for now. once i figure out how to get the queue to work with random movments, it will look crisp 
        Owner.DesiredChassisRotation += randomTurn / 2;
    }
   
    /// <summary>Attempts to dequeue from <see cref="PivotQueue"/> and split it into <see cref="AIParameters.MaxQueuedMovements"/> smaller turns.</summary>
    /// <returns>Whether or not the attempt was successful.</returns>
    public readonly bool TryGenerateSubQueue() {
        if (PivotQueue.Count == 0) return false;
        if (SubPivotQueue.Count > 0) return false;
        // grab from the top of the queue
        var pivot = PivotQueue.Dequeue(); //PivotQueue[0];
        var desiredCuts = Owner.Parameters.MaxQueuedMovements;

        for (int i = 0; i < desiredCuts; i++) {
            //SubPivotQueue.Add(Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i));
            SubPivotQueue.Enqueue(MathUtils.Slerp2D(Vector2.UnitY.RotatedBy(Owner.ChassisRotation), pivot, 1f / desiredCuts * (i + 1)));
        }
        // drop the first element since this works as a queue under the hood
        // PivotQueue.RemoveAt(0);

        return true;
    }
    /// <summary>Attempts to dequeue from <see cref="SubPivotQueue"/> and adjust this <see cref="AITank"/>'s <see cref="Tank.DesiredChassisRotation"/>.</summary>
    /// <returns>Whether or not the dequeue was successful.</returns>
    public readonly bool TryWorkSubQueue() {
        if (SubPivotQueue.Count == 0) return false;

        /*var aggro = 0f;
        if (TargetTank is not null) {
            // dirvec to target -> gets that angle
            // difference in angle -> multiplies by aggressiveness
            var toTarget = Vector2.Normalize(TargetTank.Position - Position);
            float targetAngle = toTarget.ToRotation() - MathHelper.PiOver2;

            // shortest signed angle difference
            float angleDifference = MathHelper.WrapAngle(targetAngle - ChassisRotation);

            // negatives don't work?

            // applies bias toward or away from the target's angle
            aggro += angleDifference * Parameters.AggressivenessBias;
        }*/

        Owner.DesiredChassisRotation = SubPivotQueue.Dequeue().ToRotation() - MathHelper.PiOver2;

        // drop the first element again, but for the sub-queue
        // SubPivotQueue.RemoveAt(0);

        return true;
    }

    // makes the tank turn if it happens to run into a block
    readonly bool Physics_OnCollision(Fixture sender, Fixture other, tainicom.Aether.Physics2D.Dynamics.Contacts.Contact contact) {
        if (other.Body.Tag is Block) {
            // contact.Manifold.LocalNormal
            // var pPos = Physics.Position;

            contact.GetWorldManifold(out var worldNormal, out var points);

            var collPoint = points[0] * Tank.UNITS_PER_METER;

            // GameHandler.Particles.MakeSmallExplosion(new Vector3(collPoint.X, 11, collPoint.Y), 10, 10, 0.5f, 1);

            // optimally, i'd want to make it turn towards where the current path bounce endpoint is... or something like that
            var oppNormal = Vector2.Normalize(Owner.Position - collPoint);
            PivotQueue.Enqueue(oppNormal);
        }   
        return true;
    }
}
