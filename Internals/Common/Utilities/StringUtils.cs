using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace TanksRebirth.Internals.Common.Utilities;

public static class StringUtils
{
    public static string SplitByCamel(this string input) => Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
    public static string ToCtor(this Vector3 v) => $"new Vector3({v.X}f, {v.Y}f, {v.Z}f)";
}
