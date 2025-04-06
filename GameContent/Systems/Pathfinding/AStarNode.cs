using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Systems.Pathfinding;

public class AStarNode(Point position)
{
    public Point Position { get; private set; } = position;
    public float GCost { get; set; } = float.MaxValue;
    public float HCost { get; set; } = 0;
    public float FCost => GCost + HCost; // Total estimated cost
    public AStarNode? Parent { get; set; }
}
