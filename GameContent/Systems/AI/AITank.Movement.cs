using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Framework.Collision;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.AI; 
public partial class AITank {
    public List<Tank> TanksNear = [];
    public List<Block> BlocksNear = [];
    public bool IsPathBlocked;
    public bool IsNearDestructibleObstacle;

    public bool DoMovements = true;
    public bool DoMoveTowards = true;

    public static int TankPathCheckSize = 3;

    public int CurrentRandomMove;

    // this code runs every "movement opportunity" (any number between RandomTimerMinMove and RandomTimerMaxMove)
    public void DoMovement() {
        bool shouldMove = !IsTurning && CurMineStun <= 0 && CurShootStun <= 0 && !IsPathBlocked;
        //Console.WriteLine(shouldMove);
        if (!shouldMove) return;
        if (Behaviors[0].IsModOf(CurrentRandomMove)) {
            DoBlockNav(); // determines IsPathBlocked

            // determine the frame timer for a new movement opportunity
            CurrentRandomMove = Client.ClientRandom.Next(AiParams.RandomTimerMinMove, AiParams.RandomTimerMaxMove);

            if (!IsPathBlocked) {
                var random = Client.ClientRandom.NextFloat(-AiParams.MaxAngleRandomTurn, AiParams.MaxAngleRandomTurn);

                TargetTankRotation += random;
            }

            // might need to check specific dangers here like mines or shells
            // my loneliness is killing me (and i) I must confess, i still believe (still believe)
            // oh baby baby, how was i supposed to know
            foreach (var danger in NearbyDangers) {
                var isHostile = danger.Team != Team && danger.Team != TeamID.NoTeam;
                if (danger is Mine mine) {
                    var isCloseEnough = GameUtils.Distance_WiiTanksUnits(Position, mine.Position) <= (isHostile ? AiParams.AwarenessHostileMine : AiParams.AwarenessFriendlyMine);
                    // already accounts for hostility via the above ^
                    if (isCloseEnough) {
                        Avoid(mine.Position);
                        break;
                    }
                }
                else if (danger is Shell shell) {
                    var isHeadingTowards = shell.IsHeadingTowards(Position, isHostile ? AiParams.AwarenessHostileShell : AiParams.AwarenessFriendlyShell, MathHelper.Pi);
                    // already accounts for hostility via the above ^
                    if (isHeadingTowards) {
                        Avoid(shell.Position);
                        break;
                    }
                }
                // handle non-vanilla sources of danger
                if (danger.Priority == DangerPriority.VeryHigh) {
                    Avoid(danger.Position);
                    break;
                }
                else if (danger.Priority == DangerPriority.High) {
                    Avoid(danger.Position);
                    break;
                }
                else if (danger.Priority == DangerPriority.Medium) {
                    Avoid(danger.Position);
                    break;
                }
                else if (danger.Priority == DangerPriority.Low) {
                    Avoid(danger.Position);
                }
            }

            // aggression/pursuit handling
            if (TargetTank is not null) {
                var targetDirVector = Vector2.Normalize(Position.DirectionTo(TargetTank!.Position));
                var dirDirVector = Vector2.Normalize(Position.DirectionTo(PathEndpoint));

                var finalVector = dirDirVector + targetDirVector * AiParams.AggressivenessBias;

                // negative plus pi/2...?
                TargetTankRotation = finalVector.ToRotation();
            }
        }
    }
    public void DoBlockNav() {
        uint framesLookAhead = AiParams.ObstacleAwarenessMovement / 2;
        var tankDirection = Vector2.UnitY.Rotate(TargetTankRotation);
        IsPathBlocked = IsObstacleInWay(framesLookAhead, tankDirection, out var travelPath, out var reflections, TankPathCheckSize);
        if (IsPathBlocked) {
            if (reflections.Length > 0) {
                // up = PiOver2
                //var normalRotation = reflections[0].Normal.RotatedByRadians(TargetTankRotation);
                var dirOf = travelPath.DirectionTo(Position).ToRotation();
                var refAngle = dirOf + MathHelper.PiOver2;

                // this is a very bandaid fix....
                if (refAngle % MathHelper.PiOver2 == 0) {
                    refAngle += Client.ClientRandom.NextFloat(0, MathHelper.TwoPi);
                }
                TargetTankRotation = refAngle;
            }

            // TODO: i literally do not understand this
        }
    }
    public Vector2 PathEndpoint;

    // reworked tahnkfully
    private bool IsObstacleInWay(uint checkDist, Vector2 pathDir, out Vector2 endpoint, out RaycastReflection[] reflections, int size = 1, bool draw = false) {
        bool hasCollided = false;

        var whitePixel = TextureGlobals.Pixels[Color.White];
        Vector2 pathPos = Position;
        endpoint = Position;
        reflections = [];

        // dynamic approach instead.... if Speed is zero then don't do anything
        // if pathing doesn't work right then blame this xd
        // if (Speed == 0) return false;

        List<RaycastReflection> reflectionList = [];

        // until fix/correction is made
        pathDir *= 8; //Speed;

        // Avoid constant allocation
        Rectangle pathHitbox = new();
        Vector2 dummyPos = Vector2.Zero;

        for (int i = 0; i < checkDist; i++) {
            // Check X bounds
            if (pathPos.X < GameScene.MIN_X || pathPos.X > GameScene.MAX_X) {
                pathDir.X *= -1;
                hasCollided = true;
                reflectionList.Add(new(pathPos, Vector2.UnitY));
            }

            // Check Y bounds
            if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                pathDir.Y *= -1;
                hasCollided = true;
                reflectionList.Add(new(pathPos, -Vector2.UnitY));
            }

            pathHitbox.X = (int)pathPos.X;
            pathHitbox.Y = (int)pathPos.Y;
            pathHitbox.Width = size;
            pathHitbox.Height = size;

            // Simplified collision logic
            Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out _, out _, false);

            if (dir != CollisionDirection.None) {
                hasCollided = true;
                Vector2 normal = dir switch {
                    CollisionDirection.Up => -Vector2.UnitY,
                    CollisionDirection.Down => Vector2.UnitY,
                    CollisionDirection.Left => Vector2.UnitX,
                    CollisionDirection.Right => -Vector2.UnitX,
                    _ => Vector2.Zero
                };

                if (dir is CollisionDirection.Left or CollisionDirection.Right)
                    pathDir.X *= -1;
                else
                    pathDir.Y *= -1;

                reflectionList.Add(new(pathPos, normal));
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
                    Color.White,
                    0,
                    whitePixel.Size() / 2,
                    new Vector2(size, size),
                    default,
                    default
                );
            }

            pathPos += pathDir;
        }

        reflections = [.. reflectionList];
        PathEndpoint = endpoint = pathPos;
        return hasCollided;
    }
}
