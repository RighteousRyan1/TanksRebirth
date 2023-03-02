using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID;

public class PingID
{
    public const int Generic = 0;
    public const int StayHere = 1;
    public const int WatchHere = 2;
    public const int AvoidHere = 3;
    public const int GoHere = 4;
    public const int FocusHere = 5;
    public const int GroupHere = 6;

    public static readonly ReflectionDictionary<PingID, int> Collection = new(MemberType.Fields);
}
