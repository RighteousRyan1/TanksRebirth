using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TanksRebirth.Internals.Common.Framework.Collections;
using TanksRebirth.Internals.Common.Utilities;

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
    public static readonly Dictionary<int, Color> PlayerTankColorsBright = new() {
        [Blue] = ColorUtils.ChangeColorBrightness(Color.Blue, 0.5f),
        [Red] = ColorUtils.ChangeColorBrightness(Color.Red, 0.5f),
        [Green] = ColorUtils.ChangeColorBrightness(Color.Lime, 0.5f),
        [Yellow] = ColorUtils.ChangeColorBrightness(Color.Yellow, 0.5f)
    };

    public static ReflectionDictionary<PlayerID> Collection { get; internal set; } = new(MemberType.Fields);
}
