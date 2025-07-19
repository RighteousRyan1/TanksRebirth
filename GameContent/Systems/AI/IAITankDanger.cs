using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Systems.AI;

public interface IAITankDanger {
    /// <summary>The location of this dangerous object.</summary>
    Vector2 Position { get; set; }
    int Team { get; }
}