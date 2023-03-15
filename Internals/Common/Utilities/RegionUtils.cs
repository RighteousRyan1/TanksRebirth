using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class RegionUtils
{
    public static bool IsSouthernHemisphere(string englishName) => englishName.ToLower() switch {
        "australia" => true,
        "new zealand" => true,
        "brazil" => true,
        "bolivia" => true,
        "chile" => true,
        "peru" => true,
        "argentina" => true,
        "paraguay" => true,
        "uruguay" => true,
        "ecuador" => true,
        "south africa" => true,
        "angola" => true,
        "namibia" => true,
        "congo" => true,
        "botswana" => true,
        "madagascar" => true,
        "mozambique" => true,
        "zimbabwe" => true,
        "tanzania" => true,
        "indonesia" => true,
        "papua new guinea" => true,
        _ => false
    };
}
