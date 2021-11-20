using Microsoft.Xna.Framework;
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
            /*UIElements.BottomTabber = new("Return", TankGame.Fonts.Default, Color.Black, Color.CornflowerBlue, 1.5f);
            UIElements.BottomTabber.SetDimensions(GameUtils.WindowWidth / 2, GameUtils.WindowHeight * 0.7f, 250, 30);*/
        }
    }
}