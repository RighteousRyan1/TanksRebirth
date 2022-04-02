using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework
{
    public struct Circle
    {
        public float radius;
        public Vector2 Center { get; set; }

        /// <summary>Whether or not this <see cref="Circle"/> intersects with <paramref name="other"/>.</summary>
        public bool Intersects(Circle other)
            => Vector2.Distance(Center, other.Center) < radius;

        /// <summary>Gets the area of this <see cref="Circle"/>.</summary>
        public float GetArea()
            => MathF.Pow(MathHelper.Pi * radius, 2);

        /// <summary>Gets the circumference of this <see cref="Circle"/>.</summary>
        public float GetCircumference()
            => radius * 2 * MathHelper.Pi;
    }
}
