using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Collisions;

public struct Circle
{
    public float Radius;
    public Vector2 Center { get; set; }

    /// <summary>Whether or not this <see cref="Circle"/> intersects with <paramref name="other"/>.</summary>
    public readonly bool Intersects(Circle other)
        => Vector2.Distance(Center, other.Center) < Radius;
    public readonly bool Intersects(Rectangle rect, out CollisionResult collision) {
        // Clamp the circle center to the rectangle to find the closest point on the rect
        float closestX = MathHelper.Clamp(Center.X, rect.Left, rect.Right);
        float closestY = MathHelper.Clamp(Center.Y, rect.Top, rect.Bottom);
        Vector2 closestPoint = new(closestX, closestY);

        // Compute vector from closest point to circle center
        Vector2 difference = Center - closestPoint;
        float distanceSquared = difference.LengthSquared();

        if (distanceSquared <= Radius * Radius) {
            Vector2 normal;
            if (difference.LengthSquared() > 0)
                normal = Vector2.Normalize(difference); // outward from rectangle
            else
                normal = new Vector2(0, -1); // default normal if inside

            collision = new CollisionResult(closestPoint, normal);
            return true;
        }

        collision = default;
        return false;
    }

    /// <summary>Gets the area of this <see cref="Circle"/>.</summary>
    public readonly float GetArea()
        => MathF.Pow(MathHelper.Pi * Radius, 2);

    /// <summary>Gets the circumference of this <see cref="Circle"/>.</summary>
    public readonly float GetCircumference()
        => Radius * 2 * MathHelper.Pi;

    public readonly Vector2 GetRotatedPoint(float rads) 
        => Center + new Vector2(Radius, 0).Rotate(rads);
}
