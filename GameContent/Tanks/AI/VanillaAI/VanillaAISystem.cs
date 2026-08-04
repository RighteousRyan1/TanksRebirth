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
    public AITank Owner { get; set; }
    // 0, 1, 2, 3
    public AIBehaviorState ChassisMovement;
    public AIBehaviorState TurretMovement;
    public AIBehaviorState ShellFire;
    public AIBehaviorState MinePlace;

    public VanillaAISystem(AITank tank) {
        Owner = tank;

        ChassisMovement.SetLabel("ChassisMovement");
        TurretMovement.SetLabel ("TurretMovement");
        ShellFire.SetLabel      ("ShellFire");
        MinePlace.SetLabel      ("MinePlace");


        NearbyDangers = [];
    }

    public readonly void Initialize() {
        Owner.Physics.OnCollision += Physics_OnCollision;
    }
    public void AILoop() {
        ChassisMovement.Value += RuntimeData.DeltaTime;
        TurretMovement.Value  += RuntimeData.DeltaTime;
        ShellFire.Value       += RuntimeData.DeltaTime;
        MinePlace.Value       += RuntimeData.DeltaTime;

        TurretRotationMultiplier = 1f;

        // Array.ForEach(Owner.Behaviors, x => x.Value += RuntimeData.DeltaTime);

        // nearby friendlies checks
        Owner.TanksNearMineAwareness.Clear();
        Owner.TanksNearShootAwareness.Clear();

        Span<Tank?> allTanks = GameHandler.AllTanks;
        ref var search = ref MemoryMarshal.GetReference(allTanks);

        for (int i = 0; i < allTanks.Length; i++) {
            var tank = Unsafe.Add(ref search, i);
            if (tank is null || tank == Owner || tank.IsDestroyed)
                continue;

            float distToBody = GameUtils.TanksDistance(Owner.Position, tank.Position);
            float distToTurret = GameUtils.TanksDistance(Owner.TurretPosition, tank.Position);

            if (distToBody <= Owner.Parameters.TankAwarenessMine)
                Owner.TanksNearMineAwareness.Add(tank);

            if (distToTurret <= Owner.Parameters.TankAwarenessShoot)
                Owner.TanksNearShootAwareness.Add(tank);
        }

        // if (ModdedData?.CustomAI() == false) return;
        if (Owner.ModdedData is not null) {
            if (!Owner.ModdedData.CustomAI())
                return;
        }

        var isShellNear = NearbyDangers.Count > 0 && ClosestDanger is Shell;

        // only use if checking the respective boolean!
        var shell = (ClosestDanger as Shell)!;

        // isShellNear already accounts for the direction arc
        if (Owner.Parameters.DeflectsBullets && isShellNear && Owner.Properties.ShellLimit - Owner.OwnedShellCount > 0) {
            DoDeflection(shell);
        }

        HandleTurret();
        if (DoMovements) {
            if (Owner.Properties.Stationary)
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
            var dir = Vector2.UnitY.RotatedBy(Owner.ChassisRotation);

            Owner.Velocity = Vector2.Normalize(dir) * Owner.Speed;
            Owner.ChassisRotation = MathUtils.RoughStep(Owner.ChassisRotation, Owner.DesiredChassisRotation, Owner.Properties.TurningSpeed * RuntimeData.DeltaTime);
        }
    }
}
