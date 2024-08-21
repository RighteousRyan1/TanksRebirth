using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID;

public sealed class PlayerID
{
    public const int Blue = 0;
    public const int Red = 1;
    public const int GreenPlr = 2;
    public const int YellowPlr = 3;

    public static readonly Dictionary<int, Vector3> PlayerTankColors = new()
    {
        [Blue] = new Vector3(0, 0, 1),
        [Red] = new Vector3(1, 0, 0),
        [GreenPlr] = new Vector3(0, 1, 0),
        [YellowPlr] = new Vector3(1, 1, 0)
    };

    public static ReflectionDictionary<PlayerID> Collection { get; internal set; } = new(MemberType.Fields);
}
