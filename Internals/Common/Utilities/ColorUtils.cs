using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Utilities;

public static class ColorUtils {
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
        if (GetLuminosity(input) > lumCutoff) return Color.Black;
        else return Color.White;
    }
    /// <summary>
    /// Inverts the colors of a <see cref="Color"/>.
    /// </summary>
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
    /// <param name="color">The color to correct.</param>
    /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
    /// Negative values produce darker colors.</param>
    /// <returns>
    /// The corrected <see cref="Color"/>.
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

    /// <summary>
    /// Adjusts the saturation of a color by a saturation factor.
    /// </summary>
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
    /// <summary>Converts a HSV color to RGB.</summary>
    public static Color HsvToRgb(double h, double s, double v) {
        // force hue to be 0-360
        double hue = (h % 360.0 + 360.0) % 360.0;

        // saturation / value clamp
        double sat = Math.Clamp(s, 0.0, 1.0);
        double val = Math.Clamp(v, 0.0, 1.0);

        // grayscale case
        if (sat <= 0.0 || val <= 0.0) {
            int gray = (int)(val * 255.0);
            return new Color(gray, gray, gray);
        }

        // color wheel segments
        double sector = hue / 60.0;
        int i = (int)Math.Floor(sector);
        double f = sector - i;

        double p = val * (1.0 - sat);
        double q = val * (1.0 - sat * f);
        double t = val * (1.0 - sat * (1.0 - f));

        // map to RGB
        var (r, g, b) = i switch {
            0 => (val, t, p),
            1 => (q, val, p),
            2 => (p, val, t),
            3 => (p, q, val),
            4 => (t, p, val),
            _ => (val, p, q) // fallback in case
        };

        return new Color((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }
    public static float GetLuminosity(Color color) => Vector3.Dot(color.ToVector3(), new Vector3(0.299f, 0.587f, 0.114f));
    /// <summary>Premultiplies the colors of a texture.</summary>
    public static void FromPremultiplied(ref Texture2D texture) {
        var buffer = new Color[texture.Width * texture.Height];
        texture.GetData(buffer);

        Span<Color> bufSpan = buffer;
        ref var searchSpaceBuf = ref MemoryMarshal.GetReference(bufSpan);
        for (int i = 0; i < buffer.Length; i++) {
            ref var buf = ref Unsafe.Add(ref searchSpaceBuf, i);
            buf = Color.FromNonPremultiplied(buf.R, buf.G, buf.B, buf.A);
        }
        texture.SetData(buffer);
    }
    /// <summary>Converts normalized RGB to RGB.</summary>
    public static Color ToColor(this Vector3 vec) => new((int)Math.Round(vec.X * 255), (int)Math.Round(vec.Y * 255), (int)Math.Round(vec.Z * 255));
}