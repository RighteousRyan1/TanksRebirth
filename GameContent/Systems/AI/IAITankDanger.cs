using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Systems.AI;

public interface IAITankDanger {
    /// <summary>The location of this dangerous object.</summary>
    Vector2 Position { get; set; }
    /// <summary>Whether or not this object was created by a player or another AI.</summary>
    bool IsPlayerSourced { get; set; }

    DangerPriority Priority { get; set; }
}
public enum DangerPriority {
    Low, Medium, High, VeryHigh
}