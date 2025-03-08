using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;

namespace TanksRebirth.Internals.Common.Utilities;

public static class ColorUtils
{
    public static Color[] AllColors { get; } = typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public).Select(x => (Color)x.GetValue(null)!).ToArray();
    public static Color[] BrightColors { get; } = AllColors.Where(x => GetLuminosity(x) > 0.33f).ToArray();
    public static Color DiscoPartyColor => HsvToRgb(TankGame.UpdateCount % 255 / 255f * 360, 1, 1);
    /// <summary>
    /// Creates color with corrected brightness.
    /// </summary>
    /// <param name="color">Color to correct.</param>
    /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
    /// Negative values produce darker colors.</param>
    /// <returns>
    /// Corrected <see cref="Color"/> structure.
    /// </returns>
    public static Color ChangeColorBrightness(Color color, float correctionFactor) {
        float red = color.R;
        float green = color.G;
        float blue = color.B;

        if (correctionFactor < 0) {
            correctionFactor = 1 + correctionFactor;
            red *= correctionFactor;
            green *= correctionFactor;
            blue *= correctionFactor;
        }
        else {
            red = (255 - red) * correctionFactor + red;
            green = (255 - green) * correctionFactor + green;
            blue = (255 - blue) * correctionFactor + blue;
        }

        return new((int)red, (int)green, (int)blue, color.A);
    }
    public static Color HsvToRgb(double h, double S, double V)
    {
        Color c = new();
        double H = h;
        while (H < 0) { H += 360; };
        while (H >= 360) { H -= 360; };
        double R, G, B;
        if (V <= 0)
            R = G = B = 0;
        else if (S <= 0)
            R = G = B = V;
        else
        {
            double hf = H / 60.0;
            int i = (int)Math.Floor(hf);
            double f = hf - i;
            double pv = V * (1 - S);
            double qv = V * (1 - S * f);
            double tv = V * (1 - S * (1 - f));
            switch (i)
            {

                // Red is the dominant color

                case 0:
                    R = V;
                    G = tv;
                    B = pv;
                    break;

                // Green is the dominant color

                case 1:
                    R = qv;
                    G = V;
                    B = pv;
                    break;
                case 2:
                    R = pv;
                    G = V;
                    B = tv;
                    break;

                // Blue is the dominant color

                case 3:
                    R = pv;
                    G = qv;
                    B = V;
                    break;
                case 4:
                    R = tv;
                    G = pv;
                    B = V;
                    break;

                // Red is the dominant color

                case 5:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                case 6:
                    R = V;
                    G = tv;
                    B = pv;
                    break;
                case -1:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                // The color is not defined, we should throw an error.

                default:
                    //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                    R = G = B = V; // Just pretend its black/white
                    break;
            }
        }
        c.R = Clamp((byte)(R * 255));
        c.G = Clamp((byte)(G * 255));
        c.B = Clamp((byte)(B * 255));
        c.A = 255;

        byte Clamp(byte i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

        return c;
    }
    public static float GetLuminosity(Color color) => Vector3.Dot(color.ToVector3(), new Vector3(0.299f, 0.587f, 0.114f));
    public static void FromPremultiplied(ref Texture2D texture)
    {
        var buffer =  new Color[texture.Width * texture.Height];
        texture.GetData(buffer);

        Span<Color> bufSpan = buffer;
        ref var searchSpaceBuf = ref MemoryMarshal.GetReference(bufSpan);
        for (int i = 0; i < buffer.Length; i++) {
            ref var buf = ref Unsafe.Add(ref searchSpaceBuf, i);
            buf = Color.FromNonPremultiplied(buf.R, buf.G, buf.B, buf.A);
        }
        texture.SetData(buffer);
    }
    public static Color ToColor(this Vector3 vec) => new((int)Math.Round(vec.X * 255), (int)Math.Round(vec.Y * 255), (int)Math.Round(vec.Z * 255));
}
