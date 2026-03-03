using System;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Tanks.AI;
using TanksRebirth.Internals.Common.Framework;

namespace TanksRebirth.GameContent.Tanks;

public struct TankTemplate {
    /// <summary>If false, the template will contain data for an AI tank.</summary>
    public bool IsPlayer;

    public int AiTier;
    public int PlayerType;

    public Vector2 Position;

    private float _backingRotationField;

    public float Rotation { // Rounded to avoid issues when calculating rotation.
        readonly get => _backingRotationField;
        set => _backingRotationField = MathF.Round(value, 5);
    }

    public int Team;

    public Range<int> RandomizeRange;

    public readonly Tank GetTank() => IsPlayer ? GetPlayerTank() : GetAiTank();

    public readonly AITank GetAiTank() {
        if (IsPlayer)
            throw new Exception($"{nameof(IsPlayer)} is true. This method cannot execute.");

        var ai = new AITank(AiTier);
        ai.Physics.Position = Position / Tank.UNITS_PER_METER;
        ai.Position = Position;
        ai.ChassisRotation = Rotation;
        ai.DesiredChassisRotation = Rotation;
        ai.IsDestroyed = false;
        ai.TurretRotation = Rotation;
        ai.Team = Team;

        var placement = PlacementSquare.Placements.FindIndex(place => place.Position == ai.Position3D);
        if (placement > -1) {
            PlacementSquare.Placements[placement].TankId = ai.WorldId;
            PlacementSquare.Placements[placement].HasBlock = false;
        }

        return ai;
    }

    public readonly PlayerTank GetPlayerTank() {
        if (!IsPlayer)
            throw new Exception($"{nameof(IsPlayer)} is false. This method cannot execute.");

        PlayerTank player;

        // change player based on chosen difficulties
        if (Modifiers.Map[Modifiers.RANDOM_PLAYER])
            player = new PlayerTank(PlayerType, false, TankID.ServerRandomTier());
        else if (Modifiers.Map[Modifiers.DISGUISE])
            player = new PlayerTank(PlayerType, false, Modifiers.DisguiseValue);
        else
            player = new PlayerTank(PlayerType);
        player.Physics.Position = Position / Tank.UNITS_PER_METER;
        player.Position = Position;
        player.ChassisRotation = Rotation;
        player.TurretRotation = Rotation;
        player.IsDestroyed = false;
        player.Team = Team;

        var placement = PlacementSquare.Placements.FindIndex(place => place.Position == player.Position3D);
        if (placement > -1) {
            PlacementSquare.Placements[placement].TankId = player.WorldId;
            PlacementSquare.Placements[placement].HasBlock = false;
        }

        return player;
    }
}