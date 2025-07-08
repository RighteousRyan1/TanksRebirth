using FontStashSharp;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu;

public static partial class MainMenuUI {
    public static void RenderCosmeticsUI() {
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, $"COMING SOON!", new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 6), Color.White, new Vector2(0.75f).ToResolution(), 0f, FontGlobals.RebirthFontLarge.MeasureString($"COMING SOON!") / 2);
    }
}
