using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework.Graphics
{
    public class Quad2D : IDisposable
    {
        internal static List<Quad2D> quads = new();
        public void Dispose()
        {
            foreach (var tri in triangles)
                tri.Dispose();
            GC.SuppressFinalize(this);
        }

        public Triangle2D[] triangles = new Triangle2D[2];

        public Vector2[] vertices = new Vector2[4];

        public Color color = Color.White;

        private Color tColor1;
        private Color tColor2;

        public Quad2D(Vector2[] vertices, Color color)
        {
            // First, assign the values to the objects.
            this.color = color;
            this.vertices = vertices;

            tColor1 = color;
            tColor2 = color;

            triangles[0] = new(Vector2.Zero, Vector2.Zero, Vector2.Zero, tColor1);
            triangles[1] = new(Vector2.Zero, Vector2.Zero, Vector2.Zero, tColor2);
            // blash blasdabdsdbs

            // connects the top left
            triangles[0].vertices[0] = vertices[0];
            triangles[1].vertices[0] = vertices[0];

            // connects the bottom right
            triangles[0].vertices[2] = vertices[2];
            triangles[1].vertices[2] = vertices[2];

            // triangles[0] unshared vertex set to the 4th element in the corners array.
            triangles[0].vertices[1] = vertices[3];

            // triangles[1] unshared vertex set to the 2nd element in the corners array.
            triangles[1].vertices[1] = vertices[1];

            /* t[0,1][0] meet  t[1][1] vertex
             *         +-------+
             *         |\      |
             *         | \     |
             *         |  \    |
             *         |   \   |
             *         |    \  |
             *         |     \ |
             *         |      \|
             *         +-------+
             * t[0] vertex  t[0, 1][2] meet
             */

            quads.Add(this);
        }

        public void UpdateVerticePositions()
        {
            triangles[0].color = tColor1;
            triangles[1].color = tColor2;

            // connects the top left
            triangles[0].vertices[0] = vertices[0];
            triangles[1].vertices[0] = vertices[0];

            // connects the bottom right
            triangles[0].vertices[2] = vertices[2];
            triangles[1].vertices[2] = vertices[2];

            // triangles[0] unshared vertex set to the 4th element in the corners array.
            triangles[0].vertices[1] = vertices[3];

            // triangles[1] unshared vertex set to the 2nd element in the corners array.
            triangles[1].vertices[1] = vertices[1];
        }

        public void ChangeTriangle1Color(Color color)
            => tColor1 = color;
        public void ChangeTriangle2Color(Color color)
            => tColor2 = color;

        public void Inflate(float x, float y)
        {
            vertices[0].X -= x / 2;
            vertices[1].X += x / 2;

            vertices[2].X += x / 2;
            vertices[3].X -= x / 2;

            vertices[0].Y -= y / 2;
            vertices[1].Y -= y / 2;

            vertices[2].Y += y / 2;
            vertices[3].Y += y / 2;
        }
    }
}
