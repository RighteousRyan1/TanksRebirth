using FontStashSharp;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.LevelEditor;

#pragma warning disable
public static partial class LevelEditorUI {
    // TODO: dynamically drawn 3d models on the UI.
    // TODO: rework scrollbar UI code, massively. my sanity is starting to taper off and achieve an all time low.
    // this will have the tanks at the bottom n stuff.
    // take advantage of DrawUtils.CenteredOrthoToScreen
    public static List<Particle> LevelEditorParticles = [];
    // then have texts n stuff under it.

    public static string AlertText;
    private static float _alertTime;
    public static float DefaultAlertDuration { get; set; } = 120;

    /// <summary>Displays an alert to the screen.</summary>
    /// <param name="alert">The text to show in the alert.</param>
    /// <param name="timeOverride">The amount of time to display the alert for. Defaults to <see cref="DefaultAlertDuration"/>.</param>
    public static void Alert(string alert, float timeOverride = 0f) {
        _alertTime = timeOverride != 0f ? timeOverride : DefaultAlertDuration;
        AlertText = alert;
        SoundPlayer.SoundError();
    }
    public static void DrawCampaigns() {
        if (loadedCampaign != null) {
            var heightDiff = 40;
            _missionButtonScissor = new Rectangle(_missionTab.X, _missionTab.Y + heightDiff, _missionTab.Width, _missionTab.Height - heightDiff * 2).ToResolution();
            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _missionTab.ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(_missionTab.X, _missionTab.Y, _missionTab.Width, heightDiff).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont,
                TankGame.GameLanguage.MissionList,
                new Vector2(175, 153).ToResolution(),
                Color.Black,
                Vector2.One.ToResolution(),
                0f,
                Anchor.TopCenter.GetAnchor(TankGame.TextFont.MeasureString(TankGame.GameLanguage.MissionList)));
        }
    }
    public static void DrawPlacementInfo() {
        // placement information
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)500.ToResolutionY()), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)40.ToResolutionY()), null, Color.White, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont,
            TankGame.GameLanguage.PlaceInfo,
            new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 3.ToResolutionY()),
            Color.Black,
            Vector2.One.ToResolution(),
            0f,
            Anchor.TopCenter.GetAnchor(TankGame.TextFont.MeasureString(TankGame.GameLanguage.PlaceInfo)));

        var helpText = TankGame.GameLanguage.PlacementTeamInfo;
        Vector2 start = new(WindowUtils.WindowWidth - 250.ToResolutionX(), 140.ToResolutionY());

        // draw tank placement info
        if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks) {
            // TODO: should be optimised. do later.
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TankGame.GameLanguage.TankTeams, new Vector2(start.X + 45.ToResolutionX(), start.Y - 80.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFont.MeasureString(TankGame.GameLanguage.TankTeams) / 2);

            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)start.X, (int)(start.Y - 40.ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, Color.Black, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TankGame.GameLanguage.NoTeam, new Vector2(start.X + 45.ToResolutionX(), start.Y - 40.ToResolutionY()), Color.Black, Vector2.One.ToResolution(), 0f, Vector2.Zero);
            for (int i = 0; i < TeamID.Collection.Count - 1; i++) {
                var color = TeamID.TeamColors[i + 1];

                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TeamColorsLocalized[i + 1], new Vector2(start.X + 45.ToResolutionX(), start.Y + (i * 40).ToResolutionY()), color, Vector2.One.ToResolution(), 0f, Vector2.Zero);
                TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)start.X, (int)(start.Y + (i * 40).ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, color, 0f, Vector2.Zero, default, 0f);
            }

            // draw the visual that indicates to the user that they can press up and down arrows
            TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, ">", new Vector2(start.X - 25.ToResolutionX(), start.Y + ((SelectedTankTeam - 1) * 40).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFontLarge.MeasureString(">") / 2);

            if (SelectedTankTeam != TeamID.Magenta)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "v", new Vector2(start.X - 25.ToResolutionX(), start.Y + ((SelectedTankTeam - 1) * 40 + 50).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFont.MeasureString("v") / 2);
            if (SelectedTankTeam != TeamID.NoTeam)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont,
                    "v",
                    new Vector2(start.X - 25.ToResolutionX(), start.Y + ((SelectedTankTeam - 1) * 40 - 10).ToResolutionY()),
                    Color.White,
                    Vector2.One.ToResolution(),
                    MathHelper.Pi,
                    TankGame.TextFont.MeasureString("v") / 2);
        }
        // draw obstacle placement info
        else if (CurCategory == Category.Terrain) {
            helpText = "UP and DOWN to change stack.";
            // TODO: add static dict for specific types?
            var tex = SelectedBlockType != BlockID.Hole ? $"{BlockID.Collection.GetKey(SelectedBlockType)}_{BlockHeight}" : $"{BlockID.Collection.GetKey(SelectedBlockType)}";
            var size = RenderTextures[tex].Size();
            start = new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 450.ToResolutionY());
            TankGame.SpriteRenderer.Draw(RenderTextures[tex], start, null, Color.White, 0f, new Vector2(size.X / 2, size.Y), Vector2.One.ToResolution(), default, 0f);
            // TODO: reduce the hardcode for modders, yeah
            if (SelectedBlockType != BlockID.Teleporter && SelectedBlockType != BlockID.Hole) {
                TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X + 100.ToResolutionX(), start.Y - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFontLarge.MeasureString("v") / 2);
                TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X - 100.ToResolutionX(), start.Y - 25.ToResolutionY()), Color.White, Vector2.One.ToResolution(), MathHelper.Pi, TankGame.TextFontLarge.MeasureString("v") / 2);
            }
        }
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, helpText, new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), WindowUtils.WindowHeight / 2 - 70.ToResolutionY()), Color.White, new Vector2(0.5f).ToResolution(), 0f, TankGame.TextFont.MeasureString(helpText) / 2);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _curHoverRect, null, HoverBoxColor * 0.5f, 0f, Vector2.Zero, default, 0f);
    }
    public static void DrawLevelInfo() {
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 125).ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 40).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);

        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TankGame.GameLanguage.LevelInfo, new Vector2(175, 3).ToResolution(), Color.Black, Vector2.One.ToResolution(), 0f, Anchor.TopCenter.GetAnchor(TankGame.TextFont.MeasureString(TankGame.GameLanguage.LevelInfo)));
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"{TankGame.GameLanguage.EnemyTankTotal}: {AIManager.CountAll()}", new Vector2(10, 40).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        // localize later.
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Total Terrain: {Block.AllBlocks.Count(x => x is not null)}", new Vector2(10, 60).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"{TankGame.GameLanguage.DifficultyRating}: {DifficultyAlgorithm.GetDifficulty(missionToRate):0.00}", new Vector2(10, 80).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
    }
    public static void DrawAlerts() {
        if (_alertTime > 0) {
            var scale = 0.5f;
            TankGame.SpriteRenderer.Draw(ChatSystem.ChatAlert,
                new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.625f - ChatSystem.ChatAlert.Size().Y.ToResolutionY() * scale),
                null,
                Color.White,
                0f,
                ChatSystem.ChatAlert.Size() / 2,
                new Vector2(scale).ToResolution(),
                default,
                default);
            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, TankGame.TextFontLarge, AlertText, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.625f), Color.Red, Color.White, new Vector2(0.4f).ToResolution(), 0f, Anchor.Center);
            _alertTime -= TankGame.DeltaTime;
        }
    }
}
