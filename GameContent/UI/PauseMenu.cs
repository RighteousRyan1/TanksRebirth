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
    }
}