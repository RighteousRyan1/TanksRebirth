using System.Text.RegularExpressions;

namespace TanksRebirth.Internals.Common.Utilities;

public static class StringUtils {
    private static readonly Regex _camelCaseRegex = new("([A-Z])", RegexOptions.Compiled);
    public static string SplitByCamel(this string input) => _camelCaseRegex.Replace(input, " $1").Trim();
}
