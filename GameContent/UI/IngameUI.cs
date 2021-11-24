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
            public static UITextButton BottomTabber;
        }

        internal static void Initialize() {
            UIElements.BottomTabber = new("Return", TankGame.Fonts.Default, Color.Black, Color.CornflowerBlue, 1.5f);
            UIElements.BottomTabber.SetDimensions(GameUtils.WindowWidth / 2, GameUtils.WindowHeight * 0.7f, 250, 30);
        }

        private static void QuickButton(UIImageButton imageButton, SpriteBatch spriteBatch, string text, bool onLeft = false) {
            SpriteFont font = TankGame.Fonts.Default;
            spriteBatch.Draw(TankGame.MagicPixel, imageButton.Hitbox, Color.DarkGray * 0.75f);
        }
    }
}