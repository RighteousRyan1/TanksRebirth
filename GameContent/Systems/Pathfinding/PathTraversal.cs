using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace TanksRebirth.GameContent.Systems.Pathfinding;

public class PathTraversal
{
    public Vector2 Position { get; private set; }
    public float Speed { get; set; } = 2f; // Pixels per frame

    private List<Point> _path;
    private int _currentPathIndex;

    public bool IsMoving => _path != null && _path.Count > 0;

    public void SetPath(List<Point> path, int cellSize) {
        _path = path;
        _currentPathIndex = 0;

        // Set initial position to first path point
        if (path.Count > 0) {
            Position = new Vector2(
                path[0].X * cellSize + cellSize / 2,
                path[0].Y * cellSize + cellSize / 2
            );
        }
    }

    public void Update(int cellSize) {
        if (_path == null || _path.Count == 0) return;

        // Current target is the next point in the path
        Vector2 target = new Vector2(
            _path[_currentPathIndex].X * cellSize + cellSize / 2,
            _path[_currentPathIndex].Y * cellSize + cellSize / 2
        );

        // Move towards the target
        Vector2 moveDirection = target - Position;
        if (moveDirection.Length() <= Speed) {
            // Reached the current path point
            Position = target;
            _currentPathIndex++;

            // Check if path is complete
            if (_currentPathIndex >= _path.Count) {
                _path = null;
                _currentPathIndex = 0;
            }
        }
        else {
            // Normalize and move
            moveDirection.Normalize();
            Position += moveDirection * Speed;
        }
    }
}