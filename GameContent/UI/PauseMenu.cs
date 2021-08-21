using Microsoft.Xna.Framework;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.UI;
using WiiPlayTanksRemake.Internals.Common.GameUI;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class PauseMenu
    {
        public static UIParent MenuParent;

        public struct UIElements
        {
            public static UITextButton PauseButtonReturn;
            public static UITextButton PauseButtonExit;
        }

        internal static void Initialize() {
            MenuParent = new();
            UIElements.PauseButtonReturn = new("Return", TankGame.Fonts.Default, Color.Gray, Color.White, 1.5f)
            {
                InteractionBoxRelative = new OuRectangle(0.35f, 0.25f, 0.3f, 0.1f)
            };
            MenuParent.AppendElement(UIElements.PauseButtonReturn);
        }
    }
}