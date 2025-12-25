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

    // in order: Blue, Red, Green, Yellow
    public static readonly Color[] PlayerTankColors = [
        Color.Blue,
        Color.Red,
        Color.Lime,
        Color.Yellow
    ];
    public static readonly Color[] PlayerTankColorsBright = [
        ColorUtils.ChangeColorBrightness(Color.Blue, 0.5f),
        ColorUtils.ChangeColorBrightness(Color.Red, 0.5f),
        ColorUtils.ChangeColorBrightness(Color.Lime, 0.5f),
        ColorUtils.ChangeColorBrightness(Color.Yellow, 0.5f)
    ];

    public static string GetLocalizedPlayerColorName(int playerId) => playerId switch {
        Blue => TankGame.GameLanguage.Blue,
        Red => TankGame.GameLanguage.Red,
        Green => TankGame.GameLanguage.Green,
        Yellow => TankGame.GameLanguage.Yellow,
        // this should never happen, but just in case...
        _ => TankGame.GameLanguage.Disabled,
    };

    public static ReflectionDictionary<PlayerID> Collection { get; internal set; } = new(MemberType.Fields);
}
