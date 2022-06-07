using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework.Graphics
{
    public class Quad3D : IDisposable
    {
        internal static List<Quad3D> quads = new();

        public VertexBuffer Buffer { get; private set; }

        public BasicEffect Effect { get; }
        public Matrix World { get; set; }
        public Matrix View { get; set; }
        public Matrix Projection { get; set; }
        public static VertexPositionColorTexture[] VerticeColors;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Vector3[] vertices;

        public Color color = Color.White;
        public Quad3D(Vector3[] vertices, Color color)
        {
            // First, assign the values to the object's.
            this.color = color;
            this.vertices = vertices;

            VerticeColors = new VertexPositionColorTexture[vertices.Length];

            Effect = new(TankGame.Instance.GraphicsDevice);

            Buffer = new(TankGame.Instance.GraphicsDevice, typeof(VertexPositionColorTexture), vertices.Length, BufferUsage.WriteOnly);

            quads.Add(this);
        }

        public void Render()
        {
            for (int i = 0; i < vertices.Length; i++)
                VerticeColors[i] = new(vertices[i], color, Vector2.Zero);
            Buffer.SetData(VerticeColors);

            Effect.World = World;
            Effect.View = View;
            Effect.Projection = Projection;
            Effect.VertexColorEnabled = true;

            RasterizerState rasterizerState = new();
            rasterizerState.CullMode = CullMode.None;
            TankGame.Instance.GraphicsDevice.RasterizerState = rasterizerState;

            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                TankGame.Instance.GraphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, 1);
            }
        }

        public void Inflate(float x, float y, float z)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].X -= x / 2;
                vertices[i].X += x / 2;

                vertices[i].Y -= y / 2;
                vertices[i].Y += y / 2;

                vertices[i].Z -= z / 2;
                vertices[i].Z += z / 2;
            }
        }
    }
}
