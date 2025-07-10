using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using Microsoft.Xna.Framework;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI.MainMenu;

public static partial class MainMenuUI {
    static bool _playerHasVanillaCampaign;

    const int MAX_CAMPAIGNS_PER_COLUMN = 8;

    public static UITextButton UpdateCampaignButton = new("Validate", FontGlobals.RebirthFont, Color.Black) {
        Position = new Vector2(-100)
    };
    public static void DrawCampaignsUI() {
        DrawCampaignMenuExtras();
    }
    private static void SetCampaignDisplay() {
        SetPlayButtonsVisibility(false);

        float width = 150;
        float height = 50;
        UpdateCampaignButton.IsVisible = true;
        UpdateCampaignButton.Text = "Validate";
        UpdateCampaignButton.Tooltip = "Ensures your Vanilla campaign is up-to-date.";
        UpdateCampaignButton.SetDimensions(WindowUtils.WindowWidth / 2 - width / 2, 10, width, height);
        UpdateCampaignButton.Color = Color.White;
        UpdateCampaignButton.Font = FontGlobals.RebirthFont;
        UpdateCampaignButton.OnLeftClick = (a) => {
            DownloadVanillaCampaign(true);
            ChatSystem.SendMessage("Validation complete!", Color.Lime);
        };

        foreach (var elem in campaignNames)
            elem?.Remove();

        // get all the campaign folders from the SaveDirectory + Campaigns
        var path = Path.Combine(TankGame.SaveDirectory, "Campaigns");
        Directory.CreateDirectory(path);
        // add a new UIElement for each campaign folder

        var campaignFiles = Directory.GetFiles(path).Where(file => file.EndsWith(".campaign")).ToArray();

        var defaultDimensions = new Vector2(300, 40);
        float padding = 20f;

        int totalCampaigns = campaignFiles.Length;
        int numColumns = (int)Math.Ceiling(totalCampaigns / (float)MAX_CAMPAIGNS_PER_COLUMN);

        float totalWidth = numColumns * defaultDimensions.X + (numColumns - 1) * padding;
        float uiStartX = (WindowUtils.WindowWidth - totalWidth.ToResolutionX()) / 2f;

        for (int i = 0; i < campaignFiles.Length; i++) {
            var yOffControl = i % MAX_CAMPAIGNS_PER_COLUMN;
            var xOffControl = i / MAX_CAMPAIGNS_PER_COLUMN;

            float offsetY = yOffControl * (defaultDimensions.Y + padding);
            float offsetX = xOffControl * (defaultDimensions.X + padding);

            var name = campaignFiles[i];

            int numTanks = 0;
            var campaign = Campaign.Load(name);
            var missions = campaign.CachedMissions;

            foreach (var mission in missions)
                numTanks += mission.Tanks.Count(x => !x.IsPlayer);

            var elem = new UITextButton(Path.GetFileNameWithoutExtension(name), FontGlobals.RebirthFont, Color.White, 0.8f) {
                IsVisible = true,
                Tooltip = missions.Length + " missions" +
                $"\n{numTanks} tanks total" +
                $"\n\nName: {campaign.MetaData.Name}" +
                $"\nDescription: {campaign.MetaData.Description}" +
                $"\nVersion: {campaign.MetaData.Version}" +
                $"\nStarting Lives: {campaign.MetaData.StartingLives}" +
                $"\nBonus Life Count: {campaign.MetaData.ExtraLivesMissions.Length}" +
                $"\nTags: {string.Join(", ", campaign.MetaData.Tags)}" +
                $"\n\nMiddle click to DELETE ME."
            };

            elem.SetDimensions(() =>
                new Vector2(
                    uiStartX.ToResolutionX() + offsetX.ToResolutionX(),
                    WindowUtils.WindowHeight * 0.15f + offsetY.ToResolutionY()
                ),
                () => defaultDimensions.ToResolution()
            );

            elem.OnLeftClick += (el) => {
                if (Client.IsConnected() && !Client.IsHost()) {
                    ChatSystem.SendMessage("You cannot initiate a game as you are not the host!", Color.Red);
                    SoundPlayer.SoundError();
                    return;
                }

                var noExt = Path.GetFileNameWithoutExtension(name);
                UpdateCampaignButton.IsVisible = false;
                PrepareGameplay(noExt, !Client.IsConnected() || Server.CurrentClientCount == 1, false);
                OnCampaignSelected?.Invoke(CampaignGlobals.LoadedCampaign);
            };

            elem.OnMiddleClick += (el) => {
                var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", elem.Text);
                File.Delete(path + ".campaign");
                SetCampaignDisplay();
            };

            elem.OnMouseOver = (_) => SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect);

            campaignNames.Add(elem);
        }
        var extra = new UITextButton("Freeplay", FontGlobals.RebirthFont, Color.White, 0.8f) {
            IsVisible = true,
            Tooltip = "Play without a campaign!",
        };
        extra.SetDimensions(() => new Vector2(WindowUtils.WindowWidth / 2 - defaultDimensions.X / 2, 90).ToResolution(), () => defaultDimensions.ToResolution());
        extra.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect); };
        //elem.HasScissor = true;
        //elem.
        extra.OnLeftClick += (el) => {
            foreach (var elem in campaignNames)
                elem.Remove();

            CampaignGlobals.ShouldMissionsProgress = false;

            IntermissionSystem.TimeBlack = 150;
        };
        campaignNames.Add(extra);
    }
    public static void BotherUserForNotHavingVanillaCampaign() {
        if (Client.IsConnected() && Client.IsHost())
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"The people who are connected to you MUST own this\ncampaign, and it MUST have the same file name.\nOtherwise, the campaign will not load.", new(12, WindowUtils.WindowHeight / 2), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);
    }
    // dlBytes is only non-null values when campaignExists is true
    public static bool IsVanillaCampaignUpToDate(out bool campaignExists, out byte[]? dlBytes, out string? dlName) {
        var checkPath = Path.Combine(TankGame.SaveDirectory, "Campaigns", "Vanilla.campaign");
        if (!File.Exists(checkPath)) {
            dlBytes = null;
            dlName = null;
            campaignExists = false;
            return false;
        }
        campaignExists = true;
        dlBytes = WebUtils.DownloadWebFile("https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/Vanilla.campaign?raw=true", out dlName);
        var fileBytes = File.ReadAllBytes(checkPath);


        return dlBytes.SequenceEqual(fileBytes);
    }
    public static void DownloadVanillaCampaign(bool inCampaignsMenu) {
        var bytes = WebUtils.DownloadWebFile("https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/Vanilla.campaign?raw=true", out var filename);
        var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", filename);
        File.WriteAllBytes(path, bytes);

        if (inCampaignsMenu)
            SetCampaignDisplay();
    }
    public static void DrawCampaignMenuExtras() {
        if (_oldwheel != InputUtils.DeltaScrollWheel)
            MissionCheckpoint += InputUtils.DeltaScrollWheel - _oldwheel;
        if (MissionCheckpoint < 0)
            MissionCheckpoint = 0;

        DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, $"You can scroll with your mouse to skip to a certain mission." +
            $"\nCurrently, you will skip to mission {MissionCheckpoint + 1}." +
            $"\nYou will be alerted if that mission does not exist.", new Vector2(WindowUtils.WindowWidth / 3, WindowUtils.WindowHeight * 0.6f),
            Color.White, Color.Black, new Vector2(0.75f).ToResolution(), 0f, Anchor.TopCenter);

        var recordsPos = new Vector2(WindowUtils.WindowWidth / 3 * 2, WindowUtils.WindowHeight * 0.6f);
        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/trophy");
        TankGame.SpriteRenderer.Draw(tex, recordsPos - new Vector2(175, -45).ToResolution(), null, Color.White, 0f, Anchor.RightCenter.GetTextureAnchor(tex), new Vector2(0.1f).ToResolution(), default, default);
        var text = $"Top {Speedrun.LoadedSpeedruns.Length} speedruns:\n" + string.Join(Environment.NewLine, Speedrun.LoadedSpeedruns);
        DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, text, recordsPos, Color.White, Color.Black, new Vector2(0.75f).ToResolution(), 0f, Anchor.TopCenter);
    }
}
