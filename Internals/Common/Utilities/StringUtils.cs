using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities
{
    public static class StringUtils
    {
        public static string SplitByCamel(this string input) => Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
    }
}
