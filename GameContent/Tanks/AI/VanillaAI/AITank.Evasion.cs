using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Tanks.AI.VanillaAI;
public partial struct VanillaAISystem {
    bool _nearOwnMine;
    // the original stops escaping its own mine once it is this far from it
    // this is the same as WALL_SWEEP_RADIUS. idk if i should keep the same or whatever
    const float OWN_MINE_CLEAR_RADIUS = GameUtils.WALL_SWEEP_DEF_RADIUS;
    public volatile List<IAITankDanger> NearbyDangers;
    public IAITankDanger? ClosestDanger;

    readonly List<IAITankDanger> _evasionDangersBuffer = [];

    // not 100% sure this does as intended anymore
    /// <summary>Makes this <see cref="AITank"/> profusely avoid the given location.</summary>
    public void Avoid(Vector2 location) {
        IsSurviving = true;
        if (Owner.CurMineStun <= 0 && Owner.CurShootStun <= 0) {
            var direction = Owner.Position - location;
            Owner.DesiredChassisRotation = MathUtils.RotationOf(direction);
        }
    }
    /// <summary>Gets a list of dangerous objects near the <see cref="AITank"/>.</summary>
    public List<IAITankDanger> GetEvasionData() {
        _evasionDangersBuffer.Clear();
        _nearOwnMine = false;

        var dangers = AITank.DangerSnapshot;
        for (int i = 0; i < dangers.Length; i++) {
            var danger = dangers[i];
            var isOwner = danger.Owner == Owner;
            var isHostile = !isOwner && !Owner.IsOnSameTeamAs(danger.Team);

            // mines and explosions should be treated differently and specially
            if (danger is Mine || danger is Explosion) {
                var distance = GameUtils.TanksDistance(Owner.Position, danger.Position);
                var isCloseEnough = distance <=
                    (isHostile ? Owner.Parameters.AwarenessHostileMine : Owner.Parameters.AwarenessFriendlyMine);

                if (isCloseEnough) {
                    _evasionDangersBuffer.Add(danger);

                    if (isOwner && danger is Mine && distance <= OWN_MINE_CLEAR_RADIUS) _nearOwnMine = true;
                }
            }
            else if (danger is Shell shell) {
                var isHeadingTowards = shell.IsHeadingTowards(Owner.Position, isHostile ? Owner.Parameters.AwarenessHostileShell : Owner.Parameters.AwarenessFriendlyShell, MathHelper.Pi);
                // already accounts for hostility via the above ^
                if (isHeadingTowards) {
                    _evasionDangersBuffer.Add(danger);
                }
            }
            // non-vanilla sources of danger
            else {
                _evasionDangersBuffer.Add(danger);
            }
        }
        return _evasionDangersBuffer;
    }
}
