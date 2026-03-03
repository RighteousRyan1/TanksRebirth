using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Tanks.AI;

/// <summary>
/// An interface that, when applied, tells <see cref="AITank"/>s that this is a dangerous object and to avoid it.
/// </summary>
public interface IAITankDanger {
    Vector2 Position { get; set; }
    int Team { get; }
}