
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TanksRebirth.GameContent.Systems.Coordinates;
using TopDownShooterPrompt;

namespace TanksRebirth.GameContent.Systems.Pathfinding;

public class PathfindingComponent
{
    private Vector2 moveTo;
    private List<Vector2> pathNodes = new();

    public Vector2 Position;

    public PathfindingComponent(Vector2 pos)
    {
        moveTo = default;
        Position = pos;
    }

    public List<Vector2> FindPath(NodeGrid grid, Vector2 end)
    {
        pathNodes.Clear();
        // CubeMapPosition.Convert3D(new()) // use this to convert to the block mapping.
        var tempStart = grid.GetSlotFromPixel(Position, Vector2.Zero);

        var tempPath = grid.GetPath(tempStart, end, true);

        return tempPath;
    }
}
