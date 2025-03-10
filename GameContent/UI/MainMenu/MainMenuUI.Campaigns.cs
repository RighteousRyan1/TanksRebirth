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
    private static void SetCampaignDisplay() {
        SetPlayButtonsVisibility(false);

        foreach (var elem in campaignNames)
            elem?.Remove();
        // get all the campaign folders from the SaveDirectory + Campaigns
        var path = Path.Combine(TankGame.SaveDirectory, "Campaigns");
        Directory.CreateDirectory(path);
        // add a new UIElement for each campaign folder

        var campaignFiles = Directory.GetFiles(path).Where(file => file.EndsWith(".campaign")).ToArray();

        for (int i = 0; i < campaignFiles.Length; i++) {
            int offset = i * 60;
            var name = campaignFiles[i];

            int numTanks = 0;
            var campaign = Campaign.Load(name);

            var missions = campaign.CachedMissions;

            foreach (var mission in missions) {
                // load the mission file, then count each tank, then add that to the total
                numTanks += mission.Tanks.Count(x => !x.IsPlayer);
            }

            var elem = new UITextButton(Path.GetFileNameWithoutExtension(name), TankGame.TextFont, Color.White, 0.8f) {
                IsVisible = true,
                Tooltip = missions.Length + " missions" +
                $"\n{numTanks} tanks total" +
                $"\n\nName: {campaign.MetaData.Name}" +
                $"\nDescription: {campaign.MetaData.Description}" +
                $"\nVersion: {campaign.MetaData.Version}" +
                $"\nStarting Lives: {campaign.MetaData.StartingLives}" +
                $"\nBonus Life Count: {campaign.MetaData.ExtraLivesMissions.Length}" +
                // display all tags in a string
                $"\nTags: {string.Join(", ", campaign.MetaData.Tags)}" +
                $"\n\nRight click to DELETE ME."
            };
            elem.SetDimensions(() => new Vector2(700, 100 + offset).ToResolution(), () => new Vector2(300, 40).ToResolution());
            //elem.HasScissor = true;
            //elem.
            elem.OnLeftClick += (el) => {
                if (Client.IsConnected() && !Client.IsHost()) {
                    ChatSystem.SendMessage("You cannot initiate a game as you are not the host!", Color.Red);
                    SoundPlayer.SoundError();
                    return;
                }
                var noExt = Path.GetFileNameWithoutExtension(name);
                PrepareGameplay(noExt, !Client.IsConnected() || Server.CurrentClientCount == 1, false); // switch second param to !Client.IsConnected() when it should check first.
                OnCampaignSelected?.Invoke(CampaignGlobals.LoadedCampaign);
            };
            elem.OnRightClick += (el) => {
                var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", elem.Text);

                File.Delete(path + ".campaign");
                SetCampaignDisplay();
            };
            elem.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect); };
            campaignNames.Add(elem);
        }
        var extra = new UITextButton("Freeplay", TankGame.TextFont, Color.White, 0.8f) {
            IsVisible = true,
            Tooltip = "Play without a campaign!",
        };
        extra.SetDimensions(() => new Vector2(1150, 100).ToResolution(), () => new Vector2(300, 40).ToResolution());
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
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"You are missing the vanilla campaign!" +
    $"\nTry downloading the Vanilla campaign by pressing 'Enter'." +
    $"\nCampaign files belong in '{Path.Combine(TankGame.SaveDirectory, "Campaigns").Replace(Environment.UserName, "%UserName%")}' (press TAB to open on Windows)", new Vector2(12, 12).ToResolution(), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);

        if (Client.IsConnected() && Client.IsHost())
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"The people who are connected to you MUST own this\ncampaign, and it MUST have the same file name.\nOtherwise, the campaign will not load.", new(12, WindowUtils.WindowHeight / 2), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);

        if (InputUtils.KeyJustPressed(Keys.Tab)) {
            if (Directory.Exists(Path.Combine(TankGame.SaveDirectory, "Campaigns")))
                Process.Start("explorer.exe", Path.Combine(TankGame.SaveDirectory, "Campaigns"));
            // do note that this fails on windows lol
        }
        if (InputUtils.KeyJustPressed(Keys.Enter)) {
            try {
                var bytes = WebUtils.DownloadWebFile("https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/Vanilla.campaign?raw=true", out var filename);
                var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", filename);
                File.WriteAllBytes(path, bytes);

                SetCampaignDisplay();

            }
            catch (Exception e) {
                TankGame.ReportError(e);
            }
        }
    }

    public static void DrawCampaignMenuExtras() {
        if (_oldwheel != InputUtils.DeltaScrollWheel)
            MissionCheckpoint += InputUtils.DeltaScrollWheel - _oldwheel;
        if (MissionCheckpoint < 0)
            MissionCheckpoint = 0;

        DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, TankGame.TextFont, $"You can scroll with your mouse to skip to a certain mission." +
            $"\nCurrently, you will skip to mission {MissionCheckpoint + 1}." +
            $"\nYou will be alerted if that mission does not exist.", new Vector2(12, 200).ToResolution(),
            Color.White, Color.Black, new Vector2(0.75f).ToResolution(), 0f, Anchor.TopLeft);
        //TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"You can scroll with your mouse to skip to a certain mission." +
        //$"\nCurrently, you will skip to mission {MissionCheckpoint + 1}." +
        //$"\nYou will be alerted if that mission does not exist.", new Vector2(12, 200).ToResolution(), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);

        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/trophy");
        var defPos = new Vector2(60, 380);
        TankGame.SpriteRenderer.Draw(tex, defPos.ToResolution(), null, Color.White, 0f, new Vector2(tex.Size().X, tex.Size().Y / 2), new Vector2(0.1f).ToResolution(), default, default);
        var text = $"Top {Speedrun.LoadedSpeedruns.Length} speedruns:\n" + string.Join(Environment.NewLine, Speedrun.LoadedSpeedruns);
        DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, TankGame.TextFont, text, defPos.ToResolution(), Color.White, Color.Black, new Vector2(0.75f).ToResolution(), 0f, Anchor.LeftCenter);
    }
}
