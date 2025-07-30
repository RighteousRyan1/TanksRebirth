using Microsoft.Xna.Framework;
using NativeFileDialogSharp;
using System.IO;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Graphics;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.LevelEditor;

#pragma warning disable CS8618, CA2211
public static partial class LevelEditorUI {
    public static Rectangle LevelContentsPanel;
    // finish confirmation stuff later.
    public static UITextInput MissionName;
    public static UITextInput CampaignName;
    public static UITextInput CampaignDescription;
    public static UITextInput CampaignAuthor;
    public static UITextInput CampaignVersion;
    public static UITextInput CampaignTags;
    public static UITextButton MissionGrantsLife;
    public static UITextInput CampaignLoadingBannercolor;
    public static UITextInput CampaignLoadingBGColor;
    public static UITextButton SaveMenuReturn;
    public static UITextButton SaveLevelConfirm;
    public static UITextButton CampaignMajorVictory;
    public static UITextInput CampaignStartingLives;

    public static UITextButton SwapMenu;

    // reduce hardcode -- make a variable that tracks height.
    public static void InitializeSaveMenu() {
        LevelContentsPanel = new Rectangle(WindowUtils.WindowWidth / 4, (int)(WindowUtils.WindowHeight * 0.1f), WindowUtils.WindowWidth / 2, (int)(WindowUtils.WindowHeight * 0.625f));

        float padX = 20f;
        float height = 50f;

        // changed from 30 to 20.
        MissionName = new(FontGlobals.RebirthFont, Color.White, 1f, MAX_MISSION_CHARS);

        MissionName.SetDimensions(() => new(LevelContentsPanel.X + padX.ToResolutionX(),
                LevelContentsPanel.Y + 60.ToResolutionY()),
            () => new(LevelContentsPanel.Width - 40.ToResolutionX(),
                50.ToResolutionY()));
        MissionName.DefaultString = TankGame.GameLanguage.Name;

        MissionGrantsLife = new("", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution());
        MissionGrantsLife.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 120.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        MissionGrantsLife.Tooltip = TankGame.GameLanguage.GrantsBonusLifeFlavor;
        MissionGrantsLife.OnLeftClick = (a) => {
            // long ahh statement
           loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].GrantsExtraLife = 
            !loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].GrantsExtraLife;

        };


        SaveMenuReturn = new(TankGame.GameLanguage.Return, FontGlobals.RebirthFont, Color.White);
        SaveMenuReturn.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(),
                LevelContentsPanel.Y + LevelContentsPanel.Height - 60.ToResolutionY()),
            () => new(200.ToResolutionX(),
                50.ToResolutionY()));

        SaveMenuReturn.OnLeftClick = (l) => {
            GUICategory = UICategory.LevelEditor;
            var id = loadedCampaign.CurrentMissionId;
            if (MissionName.GetRealText() != string.Empty)
                loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].Name = MissionName.GetRealText();
            SetupMissionsBar(loadedCampaign, false);
            _missionButtons[id].Color = SelectedColor;
        };

        SaveLevelConfirm = new(TankGame.GameLanguage.Save, FontGlobals.RebirthFont, Color.White);
        SaveLevelConfirm.SetDimensions(() => new Vector2(LevelContentsPanel.X + 40.ToResolutionX() + SaveMenuReturn.Size.X,
                LevelContentsPanel.Y + LevelContentsPanel.Height - 60.ToResolutionY()),
            () => new(200.ToResolutionX(),
                50.ToResolutionY()));
        SaveLevelConfirm.OnLeftClick = (l) => {
            // Mission.Save(LevelName.GetRealText(), )
            var isValidStartingLives = int.TryParse(CampaignStartingLives.GetRealText(), out int startingLives);
            if (!isValidStartingLives && !_viewMissionDetails) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("Invalid 'Starting Lives' count!", Color.Red);
                return;
            }

            var res = Dialog.FileSave(_viewMissionDetails ? "mission,bin" : "campaign", TankGame.SaveDirectory);

            if (res.Path != null && res.IsOk) {
                try {
                    var name = _viewMissionDetails ? MissionName.Text : CampaignName.Text;
                    var realName = Path.HasExtension(res.Path) ? Path.GetFileNameWithoutExtension(res.Path) : Path.GetFileName(res.Path);

                    var misName = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].Name;
                    loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId] = Mission.GetCurrent(misName, loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].GrantsExtraLife);
                    cachedMission = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId];
                    if (_viewMissionDetails) {
                        var ext = Path.GetExtension(res.Path);
                        if (ext == ".mission" || ext == string.Empty)
                            cachedMission.Save(res.Path);
                        else if (ext == ".bin")
                            WiiMap.SaveToTanksBinFile(res.Path, true);
                    }
                    else {
                        loadedCampaign.MetaData.Name = CampaignName.GetRealText();
                        loadedCampaign.MetaData.Description = CampaignDescription.GetRealText();
                        loadedCampaign.MetaData.Author = CampaignAuthor.GetRealText();
                        var split = CampaignTags.GetRealText().Split(',');
                        loadedCampaign.MetaData.Tags = split;
                        loadedCampaign.MetaData.Version = CampaignVersion.GetRealText();
                        loadedCampaign.MetaData.BackgroundColor = UnpackedColor.FromStringFormat(CampaignLoadingBGColor.GetRealText());
                        loadedCampaign.MetaData.MissionStripColor = UnpackedColor.FromStringFormat(CampaignLoadingBannercolor.GetRealText());
                        loadedCampaign.MetaData.HasMajorVictory = _hasMajorVictory;
                        loadedCampaign.MetaData.StartingLives = startingLives;

                        Campaign.Save(res.Path, loadedCampaign);
                    }
                }
                catch {
                    // guh...
                    SoundPlayer.SoundError();
                    ChatSystem.SendMessage("Unable to save.", Color.Red);
                }
            }

            // GUICategory = UICategory.LevelEditor;
        };

        float width = 300;

        SwapMenu = new(TankGame.GameLanguage.CampaignDetails, FontGlobals.RebirthFont, Color.White);
        SwapMenu.SetDimensions(() => new Vector2(LevelContentsPanel.X + LevelContentsPanel.Width - width.ToResolutionX() - padX.ToResolutionX(),
                LevelContentsPanel.Y + LevelContentsPanel.Height - height.ToResolutionY() - 10.ToResolutionY()),
            () => new(width.ToResolutionX(),
                height.ToResolutionY()));

        SwapMenu.OnLeftClick = (l) => {
            _viewMissionDetails = !_viewMissionDetails;
            if (MissionName.GetRealText() != string.Empty)
                loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].Name = MissionName.GetRealText();
        };
        CampaignName = new(FontGlobals.RebirthFont, Color.White, 1f, 30);
        CampaignName.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(),
                LevelContentsPanel.Y + 60.ToResolutionY()),
            () => new(LevelContentsPanel.Width - 40.ToResolutionX(),
                50.ToResolutionY()));
        CampaignName.DefaultString = TankGame.GameLanguage.Name;
        CampaignName.Tooltip = TankGame.GameLanguage.CampaignNameFlavor;

        CampaignDescription = new(FontGlobals.RebirthFont, Color.White, 1f, 100);
        CampaignDescription.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 120.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignDescription.DefaultString = TankGame.GameLanguage.Description;
        CampaignDescription.Tooltip = TankGame.GameLanguage.DescriptionFlavor;

        CampaignAuthor = new(FontGlobals.RebirthFont, Color.White, 1f, 25);
        CampaignAuthor.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 180.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignAuthor.DefaultString = TankGame.GameLanguage.Author;
        CampaignAuthor.Tooltip = TankGame.GameLanguage.AuthorFlavor;

        CampaignTags = new(FontGlobals.RebirthFont, Color.White, 1f, 35);
        CampaignTags.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 240.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignTags.DefaultString = TankGame.GameLanguage.Tags;
        CampaignTags.Tooltip = TankGame.GameLanguage.TagsFlavor;

        CampaignVersion = new(FontGlobals.RebirthFont, Color.White, 1f, 10);
        CampaignVersion.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 300.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignVersion.DefaultString = TankGame.GameLanguage.Version;
        CampaignVersion.Tooltip = TankGame.GameLanguage.VersionFlavor;

        CampaignLoadingBGColor = new(FontGlobals.RebirthFont, Color.White, 1f, 11);
        CampaignLoadingBGColor.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 360.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignLoadingBGColor.DefaultString = TankGame.GameLanguage.BGColor;
        CampaignLoadingBGColor.Tooltip = TankGame.GameLanguage.BGColorFlavor;

        CampaignLoadingBannercolor = new(FontGlobals.RebirthFont, Color.White, 1f, 11);
        CampaignLoadingBannercolor.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 420.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignLoadingBannercolor.DefaultString = TankGame.GameLanguage.BannerColor;
        CampaignLoadingBannercolor.Tooltip = TankGame.GameLanguage.BannerColorFlavor;

        CampaignStartingLives = new(FontGlobals.RebirthFont, Color.White, 1f, 11);
        CampaignStartingLives.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 480.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignStartingLives.DefaultString = TankGame.GameLanguage.StartingLives;
        CampaignStartingLives.Tooltip = TankGame.GameLanguage.StartingLivesFlavor;

        CampaignMajorVictory = new("", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution());
        CampaignMajorVictory.SetDimensions(() => new Vector2(LevelContentsPanel.X + padX.ToResolutionX(), LevelContentsPanel.Y + 540.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), height.ToResolutionY()));
        CampaignMajorVictory.OnLeftClick = (a) => _hasMajorVictory = !_hasMajorVictory;
        CampaignMajorVictory.Tooltip = TankGame.GameLanguage.HasMajorVictoryThemeFlavor;

        SetSaveMenuVisibility(false);

        _campaignTextInputs.Add(MissionName);
        _campaignTextInputs.Add(CampaignName);
        _campaignTextInputs.Add(CampaignVersion);
        _campaignTextInputs.Add(CampaignLoadingBGColor);
        _campaignTextInputs.Add(CampaignLoadingBannercolor);
        _campaignTextInputs.Add(CampaignTags);
        _campaignTextInputs.Add(CampaignAuthor);
        _campaignTextInputs.Add(CampaignDescription);
        _campaignTextInputs.Add(CampaignStartingLives);
    }
}
