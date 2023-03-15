using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TanksRebirth.Internals.Common.Utilities;

public static class ColorUtils {
    public static Color HsvToRgb(double H, double S, double V) {
        Color c = new();
        // Define R, G, B.
        double R, G, B;

        // Increase HSV
        while (H < 0)
            H += 360;
        // Decrease HSV.
        while (H >= 360)
            H -= 360;

        switch (V) {
            case <= 0:
                R = G = B = 0; // Sets RGB to the value of 0.
                break;
            default: {
                if (S <= 0) {
                    R = G = B = V; // Sets RGB to the value of V.
                    break;
                }
                Inner_HsvToRgb(H, S, V, out R, out G, out B);

                break;
            }
        }

        
        c.R = Clamp((byte)(R * 255));
        c.G = Clamp((byte)(G * 255));
        c.B = Clamp((byte)(B * 255));
        c.A = 255;

        byte Clamp(byte i) {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

        return c;
    }

    private static void Inner_HsvToRgb(double H, double S, double V, out double R, out double G, out double B) {
        var pv = V * (1 - S);
        var hf = H / 60.0;
        var i = (int)Math.Floor(hf);
        var f = hf - i;
        var qv = V * (1 - S * f);
        var tv = V * (1 - S * (1 - f));
        
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

    public static void FromPremultiplied(ref Texture2D texture) {
        var buffer = new Color[texture.Width * texture.Height];
        texture.GetData(buffer);

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
        texture.SetData(buffer);
    }
    public static Color ToColor(this Vector3 vec) => new((int)Math.Round(vec.X * 255), (int)Math.Round(vec.Y * 255), (int)Math.Round(vec.Z * 255));
}
