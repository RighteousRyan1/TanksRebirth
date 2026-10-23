using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using System.Linq;

namespace TanksRebirth.Internals.Common.Framework.Graphics;

public struct UnpackedColor(int r, int g, int b, int a) {
    public byte R { get; set; } = (byte)r;
    public byte G { get; set; } = (byte)g;
    public byte B { get; set; } = (byte)b;
    public byte A { get; set; } = (byte)a;

    public static implicit operator Color(UnpackedColor uc) => new(uc.R, uc.G, uc.B, uc.A);
    public static implicit operator UnpackedColor(Color c) => new(c.R, c.G, c.B, c.A);

    public override readonly string ToString() => $"{R},{G},{B}";

    public static UnpackedColor FromStringFormat(string format) {
        int count = format.Count(chr => chr == ',');

        if (count < 2)
            throw new System.Exception($"Invalid parse pargument. Parameter = {nameof(format)}");

        var bytes = format.Split(',').Select(str => byte.Parse(str.Trim())).ToArray();

        return new(bytes[0], bytes[1], bytes[2], bytes.Length == 4 ? bytes[3] : 0);
    }
    /// <summary>
    /// Hex string into <see cref="UnpackedColor"/>.
    /// </summary>
    public static UnpackedColor ParseColor(string? text, UnpackedColor fallback) {
        if (text is null || text.Length != 7 || text[0] != '#'
            || !int.TryParse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return fallback;
        return new UnpackedColor((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, 255);
    }
}