using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework;

public struct Circle
{
    public float Radius;
    public Vector2 Center { get; set; }

    /// <summary>Whether or not this <see cref="Circle"/> intersects with <paramref name="other"/>.</summary>
    public readonly bool Intersects(Circle other)
        => Vector2.Distance(Center, other.Center) < Radius;

    /// <summary>Gets the area of this <see cref="Circle"/>.</summary>
    public readonly float GetArea()
        => MathF.Pow(MathHelper.Pi * Radius, 2);

    /// <summary>Gets the circumference of this <see cref="Circle"/>.</summary>
    public readonly float GetCircumference()
        => Radius * 2 * MathHelper.Pi;

    public readonly Vector2 GetRotatedPoint(float rads) 
        => Center + new Vector2(Radius, 0).Rotate(rads);
}
