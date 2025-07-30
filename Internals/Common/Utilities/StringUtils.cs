using Microsoft.Xna.Framework;
using System;
using System.Linq;
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
}
