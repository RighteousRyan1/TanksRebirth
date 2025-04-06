using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace TanksRebirth.GameContent.Systems.Pathfinding;

public class AStarPathfinder
{
    // possible movement directions w/ diagonals
    private static readonly Point[] Directions =
    [
        new Point(0, 1),   // down
        new Point(0, -1),  // up
        new Point(1, 0),   // right
        new Point(-1, 0),  // left
        new Point(1, 1),   // down-Right
        new Point(1, -1),  // up-Right
        new Point(-1, 1),  // down-Left
        new Point(-1, -1)  // up-Left
    ];

    public static List<Point> FindPath(Point start, Point end, Func<Point, bool> isWalkable) {
        var startNode = new AStarNode(start);
        startNode.GCost = 0;
        startNode.HCost = ManhattanDistance(start, end);

        var openList = new List<AStarNode> { startNode };
        var closedList = new HashSet<Point>();

        while (openList.Count > 0) {
            var currentNode = openList.OrderBy(n => n.FCost).First();

            if (currentNode.Position == end) {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.Position);

            foreach (var direction in Directions) {
                Point neighborPos = currentNode.Position + direction;

                // disallow diagonal cuts
                if (IsDiagonalMovementBlocked(currentNode.Position, direction, isWalkable))
                    continue;

                if (!isWalkable(neighborPos) || closedList.Contains(neighborPos))
                    continue;

                // diagonal is always slower than a straight line
                float newMovementCostToNeighbor = currentNode.GCost +
                    (direction.X != 0 && direction.Y != 0 ? 1.414f : 1);

                var existingNeighbor = openList.FirstOrDefault(n => n.Position == neighborPos);

                if (existingNeighbor is null) {
                    var neighborNode = new AStarNode(neighborPos) {
                        GCost = newMovementCostToNeighbor,
                        HCost = ManhattanDistance(neighborPos, end),
                        Parent = currentNode
                    };
                    openList.Add(neighborNode);
                }
                else if (newMovementCostToNeighbor < existingNeighbor.GCost) {
                    existingNeighbor.GCost = newMovementCostToNeighbor;
                    existingNeighbor.Parent = currentNode;
                }
            }
        }

        // no path :(
        return [];
    }

    private static bool IsDiagonalMovementBlocked(Point currentPos, Point direction, Func<Point, bool> isWalkable) {
        // if |x| + |y| = 2, it is a diagonal movement, so block it
        if (Math.Abs(direction.X) + Math.Abs(direction.Y) != 2)
            return false;

        Point horizontalNeighbor = new(currentPos.X + direction.X, currentPos.Y);
        Point verticalNeighbor = new(currentPos.X, currentPos.Y + direction.Y);

        return !isWalkable(horizontalNeighbor) || !isWalkable(verticalNeighbor);
    }

    private static float ManhattanDistance(Point a, Point b) {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private static List<Point> RetracePath(AStarNode startNode, AStarNode endNode) {
        var path = new List<Point>();
        var currentNode = endNode;

        while (currentNode != null && currentNode.Position != startNode.Position) {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }
}