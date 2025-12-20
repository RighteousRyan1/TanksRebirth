using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Systems.TankSystem.AI;

public interface IAITankDanger {
    /// <summary>The location of this dangerous object.</summary>
    Vector2 Position { get; set; }
    int Team { get; }
}