using FontStashSharp;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TanksRebirth.Internals.Common.Utilities;

public static class StringUtils
{
    public static string SplitByCamel(this string input) => Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
    public static string ToCtor(this Vector3 v) => $"new Vector3({v.X}f, {v.Y}f, {v.Z}f)";

    public static string RemoveTrailingZeros(Version version) {
        var parts = new[] {
            version.Major,
            version.Minor,
            version.Build,
            version.Revision
        };

        int lastNonZeroIndex = parts.Length - 1;

        // Trim trailing -1 (unset) and 0s
        while (lastNonZeroIndex > 0 && (parts[lastNonZeroIndex] <= 0))
            lastNonZeroIndex--;

        return string.Join('.', parts.Take(lastNonZeroIndex + 1));
    }

    public static int ComputeLevenshteinDistance(string s, string t) {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= m; j++) {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    public static string WrapText(SpriteFontBase font, string text, float maxLineWidth, float scale = 1f) {
        if (string.IsNullOrEmpty(text)) return "";

        string[] words = text.Split(' ');
        var sb = new StringBuilder();
        float lineWidth = 0f;
        float spaceWidth = font.MeasureString(" ").X * scale;

        foreach (var word in words) {
            Vector2 size = font.MeasureString(word) * scale;
            if (lineWidth + size.X < maxLineWidth) {
                sb.Append(word + " ");
                lineWidth += size.X + spaceWidth;
            }
            else {
                if (sb.Length > 0) sb.Append("\n");
                sb.Append(word + " ");
                lineWidth = size.X + spaceWidth;
            }
        }
        return sb.ToString();
    }
}
