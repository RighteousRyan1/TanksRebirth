using System.Linq;
using TanksRebirth.Internals.Common.GameUI;

namespace TanksRebirth.GameContent.UI.MainMenu;

public static partial class MainMenuUI {
    public static void DrawCampaignsUI() {
        if (!campaignNames.Any(x => {
            if (x is UITextButton btn)
                return btn.Text == "Vanilla"; // i fucking hate this hardcode. but i'll cry about it later.
            return false;
        })) {
            BotherUserForNotHavingVanillaCampaign();
        }
        DrawCampaignMenuExtras();
    }
}
