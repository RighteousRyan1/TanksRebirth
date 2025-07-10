using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.ID;

public sealed class PlayerID
{
    public const int Blue = 0;
    public const int Red = 1;
    public const int Green = 2;
    public const int Yellow = 3;

    public static readonly Dictionary<int, Color> PlayerTankColors = new()
    {
        [Blue] = Color.Blue,
        [Red] = Color.Red,
        [Green] = Color.Lime,
        [Yellow] = Color.Yellow
    };

    public static ReflectionDictionary<PlayerID> Collection { get; internal set; } = new(MemberType.Fields);
}
