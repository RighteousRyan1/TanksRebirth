using Microsoft.Xna.Framework;

namespace TanksRebirth.Internals.Common.Framework.Graphics
{
    public struct UnpackedColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public UnpackedColor(int r, int g, int b, int a)
        {
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
            A = (byte)a;
        }

        public static implicit operator Color(UnpackedColor uc) => new(uc.R, uc.G, uc.B, uc.A);
        public static implicit operator UnpackedColor(Color c) => new(c.R, c.G, c.B, c.A);
    }
}
