using System;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Tanks.AI.VanillaAI;

public unsafe struct AIBehaviorState {
    public fixed char Label[16];
    public float Value;

    public readonly bool TimerSatisfies(float rem) => Value % rem < RuntimeData.DeltaTime;
}
public partial struct VanillaAISystem : IAISystem {
    public AIBehaviorState ChassisMovement;
    public AIBehaviorState TurretMovement;
    public AIBehaviorState ShellFire;
    public AIBehaviorState MinePlace;

    public readonly void AILoop(AITank ai) {
        if (!MainMenuUI.IsActive && !CampaignGlobals.InMission) return;

        ai.TurretRotationMultiplier = 1f;

        // Array.ForEach(ai.Behaviors, x => x.Value += RuntimeData.DeltaTime);

        // nearby friendlies checks
        ai.TanksNearMineAwareness.Clear();
        ai.TanksNearShootAwareness.Clear();

        Span<Tank?> allTanks = GameHandler.AllTanks;
        ref var search = ref MemoryMarshal.GetReference(allTanks);

        for (int i = 0; i < allTanks.Length; i++) {
            var tank = Unsafe.Add(ref search, i);
            if (tank is null || tank == ai || tank.IsDestroyed)
                continue;

            float distToBody = GameUtils.TanksDistance(ai.Position, tank.Position);
            float distToTurret = GameUtils.TanksDistance(ai.TurretPosition, tank.Position);

            if (distToBody <= ai.Parameters.TankAwarenessMine)
                ai.TanksNearMineAwareness.Add(tank);

            if (distToTurret <= ai.Parameters.TankAwarenessShoot)
                ai.TanksNearShootAwareness.Add(tank);
        }

        // if (ModdedData?.CustomAI() == false) return;
        if (ai.ModdedData is not null) {
            if (!ai.ModdedData.CustomAI())
                return;
        }

        var isShellNear = ai.NearbyDangers.Count > 0 && ai.ClosestDanger is Shell;

        // only use if checking the respective boolean!
        var shell = (ai.ClosestDanger as Shell)!;

        // isShellNear already accounts for the direction arc
        if (ai.Parameters.DeflectsBullets && isShellNear && ai.Properties.ShellLimit - ai.OwnedShellCount > 0) {
            ai.DoDeflection(shell);
        }

        ai.HandleTurret();
        if (ai.DoMovements) {
            if (ai.Properties.Stationary)
                return;

            // facing down = 0 radians/2pi radians

            // "DoMovement" handles danger avoidance.
            // IsSurviving is only set every movement opportunity
            ai.DoMovement();

            // checks if it is entirely unable to lay mines first
            ai.TryMineLay();
        }

        // i really hope to remove this hardcode.
        if (ai.DoMoveTowards) {
            var dir = Vector2.UnitY.RotatedBy(ai.ChassisRotation);

            ai.Velocity = Vector2.Normalize(dir) * ai.Speed;
            ai.ChassisRotation = MathUtils.RoughStep(ai.ChassisRotation, ai.DesiredChassisRotation, ai.Properties.TurningSpeed * RuntimeData.DeltaTime);
        }
    }
}
