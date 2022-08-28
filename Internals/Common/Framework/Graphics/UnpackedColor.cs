using Microsoft.Xna.Framework;
using System.Linq;

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

        public override string ToString() => $"{R},{G},{B}";

        public static UnpackedColor FromStringFormat(string format)
        {
            int count = format.Count(chr => chr == ',');

            if (count < 2)
                throw new System.Exception($"Invalid parse pargument. Parameter = {nameof(format)}");

            var bytes = format.Split(',').Select(str => byte.Parse(str)).ToArray();

            return new(bytes[0], bytes[1], bytes[2], bytes.Length == 4 ? bytes[3] : 0);
        }
    }
}
