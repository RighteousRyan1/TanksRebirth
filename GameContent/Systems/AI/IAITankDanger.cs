using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Systems.AI;

public interface IAITankDanger {
    /// <summary>The location of this dangerous object.</summary>
    Vector2 Position { get; set; }
    int Team { get; }
    /// <summary>The priority tree of <see cref="AITank"/> avoidance for this object. The <see cref="AITank"/> will avoid higher priorities first.</summary>
    DangerPriority Priority { get; }
}
public enum DangerPriority {
    Low, Medium, High, VeryHigh
}