using FontStashSharp;
using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu;

public static partial class MainMenuUI {
    public static void RenderCosmeticsUI() {
        TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, $"COMING SOON!", new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 6), Color.White, new Vector2(0.75f).ToResolution(), 0f, TankGame.TextFontLarge.MeasureString($"COMING SOON!") / 2);
    }
}
