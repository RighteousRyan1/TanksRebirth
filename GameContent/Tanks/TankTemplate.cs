using System;
using TanksRebirth.Net;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Tanks.AI;
using TanksRebirth.Internals.Common.Framework;

namespace TanksRebirth.GameContent.Tanks;

/// <summary>The direction a placed tank faces, in quarter turns.</summary>
public enum Facing : byte {
    Down,
    Left,
    Up,
    Right
}

public static class FacingExtensions {
    /// <summary>The chassis rotation in radians for this facing.</summary>
    public static float ToRotation(this Facing facing) => (byte)facing * MathHelper.PiOver2;

    /// <summary>The facing closest to a rotation in radians.</summary>
    public static Facing ToFacing(this float rotation) {
        var quarters = (int)MathF.Round(rotation / MathHelper.PiOver2);
        return (Facing)((quarters % 4 + 4) % 4);
    }
}

public struct TankTemplate {
    /// <summary>If false, the template will contain data for an AI tank.</summary>
    public bool IsPlayer;

    public int AIType;
    public int PlayerType;

    public Vector2 Position;

    private float _backingRotationField;

    public float Rotation {
        readonly get => _backingRotationField;
        set => _backingRotationField = MathF.Round(value, 5);
    }

    /// <summary>The facing closest to <see cref="Rotation"/>. Setting it sets the rotation.</summary>
    public Facing Facing {
        readonly get => Rotation.ToFacing();
        set => Rotation = value.ToRotation();
    }

    public int Team;

    public Range<int> RandomizeRange;

    public readonly Tank GetTank() => IsPlayer ? GetPlayerTank() : GetAiTank();

    public readonly AITank GetAiTank() {
        if (IsPlayer)
            throw new Exception($"{nameof(IsPlayer)} is true. This method cannot execute.");

        var ai = new AITank(AIType);
        ai.Physics.Position = Position / Tank.UNITS_PER_METER;
        ai.Position = Position;
        ai.ChassisRotation = Rotation;
        ai.DesiredChassisRotation = Rotation;
        ai.IsDestroyed = false;
        ai.TurretRotation = Rotation;
        ai.Team = Team;

        var placement = EditorTile.GetFromClosest(ai.Position3D);
        if (placement is not null) {
            placement.TankId = ai.WorldId;
            placement.HasBlock = false;
        }

        return ai;
    }

    /// <param name="random">Picks the tier when the Random Player modifier is on. Pass one from <see cref="Server.RandomFor"/> so it's the same on every client.</param>
    public readonly PlayerTank GetPlayerTank(Random? random = null) {
        if (!IsPlayer)
            throw new Exception($"{nameof(IsPlayer)} is false. This method cannot execute.");

        PlayerTank player;

        // change player based on chosen difficulties
        if (Modifiers.IsOn(Modifiers.RANDOM_PLAYER)) {
            var randomNumber = random is null ? TankID.ServerRandomTier() : TankID.RandomTier(random);
            player = new PlayerTank(PlayerType, false, randomNumber);
        }
        else if (Modifiers.IsOn(Modifiers.DISGUISE))
            player = new PlayerTank(PlayerType, false, Modifiers.DisguiseValue);
        else
            player = new PlayerTank(PlayerType);
        player.Physics.Position = Position / Tank.UNITS_PER_METER;
        player.Position = Position;
        player.ChassisRotation = Rotation;
        player.TurretRotation = Rotation;
        player.IsDestroyed = false;
        player.Team = Team;

        var placement = EditorTile.GetFromClosest(player.Position3D);
        if (placement is not null) {
            placement.TankId = player.WorldId;
            placement.HasBlock = false;
        }

        return player;
    }
}