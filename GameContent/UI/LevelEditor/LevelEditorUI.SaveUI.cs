using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.GameUI;

namespace TanksRebirth.GameContent.UI.LevelEditor; 
public static partial class LevelEditorUI {
    public static UITextButton TestLevel;
    public static UITextButton Perspective;

    public static UITextButton TerrainCategory;
    public static UITextButton EnemyTanksCategory;
    public static UITextButton PlayerTanksCategory;

    public static UITextButton Properties;
    public static UITextButton LoadLevel;

    public static UITextButton ReturnToEditor;

    public static UITextButton AddMissionBtn;
    public static UITextButton RemoveMissionBtn;
    public static UITextButton MoveMissionUp;
    public static UITextButton MoveMissionDown;

    #region ConfirmLevelContents

    public static Rectangle LevelContentsPanel;
    // finish confirmation stuff later.
    public static UITextInput MissionName;
    public static UITextInput CampaignName;
    public static UITextInput CampaignDescription;
    public static UITextInput CampaignAuthor;
    public static UITextInput CampaignVersion;
    public static UITextInput CampaignTags;
    public static UITextInput CampaignExtraLives;
    public static UITextInput CampaignLoadingStripColor;
    public static UITextInput CampaignLoadingBGColor;
    public static UITextButton SaveMenuReturn;
    public static UITextButton SaveLevelConfirm;
    public static UITextButton CampaignMajorVictory;

    public static UITextButton SwapMenu;

    #endregion
}
