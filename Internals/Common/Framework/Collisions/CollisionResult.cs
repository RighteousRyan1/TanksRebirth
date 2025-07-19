using Microsoft.Xna.Framework;

namespace TanksRebirth.Internals.Common.Framework.Collisions;
public readonly struct CollisionResult(Vector2 point, Vector2 normal) {
    public readonly Vector2 Point = point;  // Point of contact on the rectangle
    public readonly Vector2 Normal = normal; // Normal pointing from rectangle to circle
}
