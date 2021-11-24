using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class IngameUI
    {
        public struct UIElements
        {
            public static UIPanel BottomTabber;
        }

        internal static void Initialize() {
            SpriteFont font = TankGame.Fonts.Default;
            Vector2 drawOrigin = font.MeasureString("Mission 1        x4") / 2f;
            UIElements.BottomTabber = new((uiPanel, spriteBatch) => spriteBatch.DrawString(font, "Mission 1        x4", uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f));
            UIElements.BottomTabber.BackgroundColor = Color.Red;
            UIElements.BottomTabber.SetDimensions(650, 1000, 500, 50);
        }

        private static void QuickButton(UIImage imageButton, SpriteBatch spriteBatch, string text, bool onLeft = false) {
            SpriteFont font = TankGame.Fonts.Default;
            spriteBatch.Draw(TankGame.MagicPixel, imageButton.Hitbox, Color.DarkGray * 0.75f);
            Vector2 drawOrigin = font.MeasureString(text) / 2f;
            spriteBatch.DrawString(font, text, imageButton.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1f, SpriteEffects.None, 1f);
        }
    }
}