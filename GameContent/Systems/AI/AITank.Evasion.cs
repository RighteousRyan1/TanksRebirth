using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.AI; 
public partial class AITank {
    public bool IsInDanger;
    public List<IAITankDanger> NearbyDangers;
    public IAITankDanger? ClosestDanger;
    public void Avoid(Vector2 location) {
        IsSurviving = true;
        // TODO: add all shells that may or may not be near, average their position and make the tank go away from that position
        if (CurMineStun <= 0 && CurShootStun <= 0) {
            var direction = -Vector2.UnitY.Rotate(location.DirectionTo(Position).ToRotation());
            TargetTankRotation = direction.ToRotation();
        }
    }
    public List<Vector2> GetEvasionData() {
        var dangerPositions = new List<Vector2>();

        foreach (var danger in NearbyDangers) {
            var isHostile = danger.Team != Team && danger.Team != TeamID.NoTeam;

            // mines and explosions should be treated differently and specially
            if (danger is Mine || danger is Explosion) {
                var isCloseEnough = GameUtils.Distance_WiiTanksUnits(Position, danger.Position) <=
                    (isHostile ? Parameters.AwarenessHostileMine : Parameters.AwarenessFriendlyMine);

                if (isCloseEnough) {
                    dangerPositions.Add(danger.Position);
                    IsSurviving = true;
                }
            }
            else if (danger is Shell shell) {
                var isHeadingTowards = shell.IsHeadingTowards(Position, isHostile ? Parameters.AwarenessHostileShell : Parameters.AwarenessFriendlyShell, MathHelper.Pi);
                // already accounts for hostility via the above ^
                if (isHeadingTowards) {
                    dangerPositions.Add(danger.Position);
                    IsSurviving = true;
                }
            }
            // non-vanilla sources of danger
            else {
                dangerPositions.Add(danger.Position);
                IsSurviving = true;
            }
        }
        return dangerPositions;
    }
    // this might need to be redone completely because different dangers have difernernejakswklfsadkolf dasjkl fsadjklsaf dkjhlsfda jhknas dfjhkbsadf jhkbsadf jhkfsa djkhsa fd
    public bool TryGetDangerNear(float distance, out List<IAITankDanger> dangersNear, out IAITankDanger? dClosest) {
        IAITankDanger? closest = null;
        dangersNear = [];

        Span<IAITankDanger> dangers = Dangers.ToArray();

        ref var dangersSearchSpace = ref MemoryMarshal.GetReference(dangers);

        for (var i = 0; i < Dangers.Count; i++) {
            var currentDanger = Unsafe.Add(ref dangersSearchSpace, i);

            if (currentDanger is null) continue;

            var distanceToDanger = GameUtils.Distance_WiiTanksUnits(Position, currentDanger.Position);

            if (!(distanceToDanger < distance)) continue;

            dangersNear.Add(currentDanger);

            if (closest == null || distanceToDanger <
                GameUtils.Distance_WiiTanksUnits(Position, closest.Position)) {
                closest = currentDanger;
            }
        }

        dClosest = closest;
        return closest != null;
    }
}
