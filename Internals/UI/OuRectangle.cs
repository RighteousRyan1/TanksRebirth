using Microsoft.Xna.Framework;

namespace WiiPlayTanksRemake.Internals.UI
{
    public struct OuRectangle
    {
        public float X;

        public float Y;

        public float Width;

        public float Height;

        public Vector2 Center
        {
            get {
                return new Vector2(X - Width * 0.5f, Y - Height * 0.5f);
            }
        }

        public OuRectangle(float x, float y, float width, float height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rectangle ToRectangle() {
            return new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
        }

        public Vector2 Position() {
            return new Vector2(X, Y);
        }
    }
}