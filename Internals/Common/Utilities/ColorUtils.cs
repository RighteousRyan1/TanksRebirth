using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Utilities;

public static class ColorUtils
{
    static readonly PropertyInfo[] _colorProperties = typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public);
    public static Color[] AllColors { get; }
    // mainly used for book colors in the background. xd.
    public static Color[] BrightColors { get; }
    public static Color DiscoPartyColor => HsvToRgb(RuntimeData.RunTime % 255 / 255f * 360, 1, 1);

    public static readonly Dictionary<string, Color> ColorsByName;

    static ColorUtils() {
        AllColors = [.. _colorProperties.Select(x => (Color)x.GetValue(null)!)];
        BrightColors = [.. AllColors.Where(x => GetLuminosity(x) > 0.33f)];
        ColorsByName = [];
        for (int i = 0; i < _colorProperties.Length; i++) {
            var prop = _colorProperties[i];

            ColorsByName.Add(prop.Name, AllColors[i]);
        }
    }
    /// <summary>Returns black if the luminosity of <paramref name="input"/> is above <paramref name="lumCutoff"/>, otherwise, white.</summary>
    public static Color WhiteBlack(Color input, float lumCutoff = 0.85f) {
        if (GetLuminosity(input) > lumCutoff)
            return Color.Black;
        else return Color.White;
    }
    public static Color Invert(Color input) {
        return new Color {
            R = (byte)(255 - input.R),
            G = (byte)(255 - input.G),
            B = (byte)(255 - input.B),
            A = input.A
        };
    }

    /// <summary>Returns the average color of a given <see cref="Texture2D"/>.</summary>
    public static Color GetAverageColor(Texture2D texture) {
        ArgumentNullException.ThrowIfNull(texture);

        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        long totalR = 0;
        long totalG = 0;
        long totalB = 0;
        long totalA = 0;

        foreach (var pixel in pixels) {
            totalR += pixel.R;
            totalG += pixel.G;
            totalB += pixel.B;
            totalA += pixel.A;
        }

        int pixelCount = pixels.Length;

        return new Color(
            (byte)(totalR / pixelCount),
            (byte)(totalG / pixelCount),
            (byte)(totalB / pixelCount),
            (byte)(totalA / pixelCount)
        );
    }
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
    public static void AdjustSaturation(Texture2D texture, float saturation) {
        if (texture.Format != SurfaceFormat.Color)
            throw new InvalidOperationException("Texture format must be Color.");

        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        for (int i = 0; i < pixels.Length; i++) {
            var color = pixels[i];
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float gray = r * 0.3f + g * 0.59f + b * 0.11f;

            r = MathHelper.Clamp(gray + (r - gray) * saturation, 0f, 1f);
            g = MathHelper.Clamp(gray + (g - gray) * saturation, 0f, 1f);
            b = MathHelper.Clamp(gray + (b - gray) * saturation, 0f, 1f);

            pixels[i] = new Color(r, g, b, color.A / 255f);
        }

        texture.SetData(pixels);
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
