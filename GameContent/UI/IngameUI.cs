using Microsoft.Xna.Framework;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.Internals.Common.GameUI;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class IngameUI
    {
        public struct UIElements
        {
            public static UITextButton BottomTabber;
        }

        internal static void Initialize() {
            UIElements.BottomTabber = new("Return", TankGame.Fonts.Default, Color.Tan, Color.Red, 1.5f);
            UIElements.BottomTabber.SetDimensions(400, 550, 250, 30);
        }
    }
}