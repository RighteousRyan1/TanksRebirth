using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu; 
public partial class MainMenuUI {
    public static void DrawCredits(SpriteBatch sb) {
        CreditsHandler.Draw(sb, FontGlobals.RebirthFontLarge, WindowUtils.WindowWidth * 0.5f);
    }
}
