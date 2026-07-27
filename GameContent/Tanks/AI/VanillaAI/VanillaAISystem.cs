using System;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Tanks.AI.VanillaAI;

// IDEA: make my own custom ai system!
// maybe make them patrol, and if seeing an enemy, tries to follow where it was last seen

public unsafe struct AIBehaviorState {
    const int LBLEN = 16;
    public fixed char Label[LBLEN];
    public float Value;

    public void SetLabel(string text) {
        int len = Math.Min(text.Length, LBLEN);

        for (int i = 0; i < len; i++) {
            Label[i] = text[i];
        }
    }

    public readonly bool TimerSatisfies(float rem) => Value % rem < RuntimeData.DeltaTime;
}

// TODO: class vs struct?
// TODO: Convert to VanillaAISytem and make it system-agnostic.
// Convert the rest of the AITank."" to this sytem.
public partial struct VanillaAISystem : IAISystem {
    public AITank Tank { get; set; }
    // 0, 1, 2, 3
    public AIBehaviorState ChassisMovement;
    public AIBehaviorState TurretMovement;
    public AIBehaviorState ShellFire;
    public AIBehaviorState MinePlace;

    public VanillaAISystem(AITank tank) {
        Tank = tank;

        ChassisMovement.SetLabel("ChassisMovement");
        TurretMovement.SetLabel ("TurretMovement");
        ShellFire.SetLabel      ("ShellFire");
        MinePlace.SetLabel      ("MinePlace");


        NearbyDangers = [];
    }

    public void AILoop() {
        ChassisMovement.Value += RuntimeData.DeltaTime;
        TurretMovement.Value  += RuntimeData.DeltaTime;
        ShellFire.Value       += RuntimeData.DeltaTime;
        MinePlace.Value       += RuntimeData.DeltaTime;

        TurretRotationMultiplier = 1f;

        // Array.ForEach(Tank.Behaviors, x => x.Value += RuntimeData.DeltaTime);

        // nearby friendlies checks
        Tank.TanksNearMineAwareness.Clear();
        Tank.TanksNearShootAwareness.Clear();

        Span<Tank?> allTanks = GameHandler.AllTanks;
        ref var search = ref MemoryMarshal.GetReference(allTanks);

        for (int i = 0; i < allTanks.Length; i++) {
            var tank = Unsafe.Add(ref search, i);
            if (tank is null || tank == Tank || tank.IsDestroyed)
                continue;

            float distToBody = GameUtils.TanksDistance(Tank.Position, tank.Position);
            float distToTurret = GameUtils.TanksDistance(Tank.TurretPosition, tank.Position);

            if (distToBody <= Tank.Parameters.TankAwarenessMine)
                Tank.TanksNearMineAwareness.Add(tank);

            if (distToTurret <= Tank.Parameters.TankAwarenessShoot)
                Tank.TanksNearShootAwareness.Add(tank);
        }

        // if (ModdedData?.CustomAI() == false) return;
        if (Tank.ModdedData is not null) {
            if (!Tank.ModdedData.CustomAI())
                return;
        }

        var isShellNear = NearbyDangers.Count > 0 && ClosestDanger is Shell;

        // only use if checking the respective boolean!
        var shell = (ClosestDanger as Shell)!;

        // isShellNear already accounts for the direction arc
        if (Tank.Parameters.DeflectsBullets && isShellNear && Tank.Properties.ShellLimit - Tank.OwnedShellCount > 0) {
            DoDeflection(shell);
        }

        HandleTurret();
        if (DoMovements) {
            if (Tank.Properties.Stationary)
                return;

            // facing down = 0 radians/2pi radians

            // "DoMovement" handles danger avoidance.
            // IsSurviving is only set every movement opportunity
            DoMovement();

            // checks if it is entirely unable to lay mines first
            TryMineLay();
        }

        // i really hope to remove this hardcode.
        if (DoMoveTowards) {
            var dir = Vector2.UnitY.RotatedBy(Tank.ChassisRotation);

            Tank.Velocity = Vector2.Normalize(dir) * Tank.Speed;
            Tank.ChassisRotation = MathUtils.RoughStep(Tank.ChassisRotation, Tank.DesiredChassisRotation, Tank.Properties.TurningSpeed * RuntimeData.DeltaTime);
        }
    }
}
