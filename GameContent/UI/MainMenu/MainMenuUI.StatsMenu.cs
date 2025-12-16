using FontStashSharp;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    private static string[] _info;
    private static float _statsOpenProgress = 0f; // 0 to 1 for animation
    private static double _statsOpenTime = 0;

    public static void RequestStats() {
        _statsOpenProgress = 0f;
        _statsOpenTime = RuntimeData.RunTime;

        _info = [
            $"{TankGame.GameLanguage.MissionsCompleted}: {TankGame.SaveFile.MissionsCompleted}",
            $"{TankGame.GameLanguage.CampaignsCompleted}: {TankGame.SaveFile.CampaignsCompleted}",
            $"{TankGame.GameLanguage.TankKillsTotal}: {TankGame.SaveFile.TotalKills}",
            $"{TankGame.GameLanguage.Deaths}: {TankGame.SaveFile.Deaths}",
            $"{TankGame.GameLanguage.Suicides}: {TankGame.SaveFile.Suicides}",
            $"{TankGame.GameLanguage.TankKillsTotalBullets}: {TankGame.SaveFile.BulletKills}",
            $"{TankGame.GameLanguage.TankKillsTotalBulletsBounced}: {TankGame.SaveFile.BounceKills}",
            $"{TankGame.GameLanguage.TankKillsTotalMines}: {TankGame.SaveFile.MineKills}",
            $"{TankGame.GameLanguage.TimePlayedTotal}: {TankGame.SaveFile.TimePlayed.TotalHours:0.0} hrs",
            $"{TankGame.GameLanguage.TimePlayedCurrent}: {TankGame.CurrentSessionTimer.Elapsed.TotalMinutes:0.0} mins"
        ];
    }

    public static void DrawStatsMenu(GameTime gameTime) {
        _statsOpenProgress += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f;
        if (_statsOpenProgress > 1f) _statsOpenProgress = 1f;

        DrawStatsPanel();
    }

    private static void DrawStatsPanel() {
        var renderer = TankGame.SpriteRenderer;
        var font = FontGlobals.RebirthFont;
        var largeFont = FontGlobals.RebirthFontLarge;

        float smoothProgress = Easings.OutExpo(_statsOpenProgress);
        float slideOffset = (1f - smoothProgress) * 50f;

        // dims the bg
        renderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight), Color.Black * 0.7f * smoothProgress);

        // main panel
        var panelWidth = WindowUtils.WindowWidth * 0.85f;
        var panelHeight = WindowUtils.WindowHeight * 0.8f;
        var panelRect = new Rectangle(
            (int)((WindowUtils.WindowWidth - panelWidth) / 2),
            (int)((WindowUtils.WindowHeight - panelHeight) / 2 + slideOffset),
            (int)panelWidth,
            (int)panelHeight
        );

        // panel bg
        DrawUtils.DrawBoxWithOutline(renderer, panelRect, new Color(20, 20, 25) * smoothProgress, Color.Gray * smoothProgress, 2);

        // header
        var headerText = "SERVICE RECORD";
        var headerScale = new Vector2(.75f).ToResolution();
        var headerSize = largeFont.MeasureString(headerText) * headerScale;
        var headerPos = new Vector2(panelRect.Center.X, panelRect.Y + 30.ToResolutionY());

        DrawUtils.DrawStringWithBorder(renderer, largeFont, headerText, headerPos, Color.Goldenrod * smoothProgress, Color.White * smoothProgress, headerScale, 0f, Anchor.Center);

        // content columns
        float colStartY = panelRect.Y + 100.ToResolutionY();
        float leftColX = panelRect.X + panelWidth * 0.25f;
        float rightColX = panelRect.X + panelWidth * 0.75f;

        // localize... soon.
        DrawUtils.DrawStringWithBorder(renderer, font, $"- General Statistics -", new Vector2(leftColX, colStartY), Color.LightGray * smoothProgress, Color.Black * smoothProgress, Vector2.One.ToResolution(), 0f, Anchor.Center);

        float spacing = 35.ToResolutionY();
        for (int i = 0; i < _info.Length; i++) {
            // staggered animation for list items
            float itemProgress = Easings.OutCubic(MathHelper.Clamp(_statsOpenProgress * 2f - (i * 0.05f), 0f, 1f));
            if (itemProgress <= 0f) continue;

            var parts = _info[i].Split(':');
            var label = parts[0];
            var value = parts.Length > 1 ? parts[1] : string.Empty;

            var pos = new Vector2(leftColX, colStartY + 50.ToResolutionY() + (i * spacing));

            // label
            // still unsure if a border should be used here
            renderer.DrawString(font, label, pos - new Vector2(20, 0).ToResolution(), Color.White * itemProgress, new Vector2(0.9f).ToResolution(), 0f, new Vector2(font.MeasureString(label).X, 0), 0f);

            // value
            renderer.DrawString(font, value, pos + new Vector2(20, 0).ToResolution(), Color.Yellow * itemProgress, new Vector2(0.9f).ToResolution(), 0f, Vector2.Zero, 0f);
        }

        // tank kill dict display
        DrawUtils.DrawStringWithBorder(renderer, font, "- Combat Efficiency -", new Vector2(rightColX, colStartY), Color.LightGray * smoothProgress, Color.Black * smoothProgress, Vector2.One.ToResolution(), 0f, Anchor.Center);

        int count = 0;
        // only use tanks that have kills attached to them to not spoil anything
        var activeKills = TankGame.SaveFile.TankKills.Where(x => x.Value > 0).ToList();

        foreach (var kill in activeKills) {
            count++;
            // creates the smooth fade in for different lines
            float itemProgress = Easings.OutCubic(MathHelper.Clamp(_statsOpenProgress * 2f - (count * 0.05f + 0.5f), 0f, 1f));
            if (itemProgress <= 0f) continue;

            var split = TankID.Collection.GetKey(kill.Key)!.SplitByCamel();
            var color = AITank.TankDestructionColors.ContainsKey(kill.Key) ? AITank.TankDestructionColors[kill.Key] : Color.Gray;

            var pos = new Vector2(rightColX, colStartY + 50.ToResolutionY() + ((count - 1) * spacing));

            // tank names
            DrawUtils.DrawStringWithBorder(renderer, font, split, pos - new Vector2(20, 0).ToResolution(), color * itemProgress, ColorUtils.WhiteBlack(color) * itemProgress, new Vector2(0.9f).ToResolution(), 0f, Anchor.TopRight, 0.25f);

            // rolling number effect
            int displayedValue = (int)(kill.Value * itemProgress);
            renderer.DrawString(font, displayedValue.ToString(), pos + new Vector2(20, 0).ToResolution(), Color.White * itemProgress, new Vector2(0.9f).ToResolution(), 0f, Vector2.Zero, 0f);
        }

        // footer
        if (TankGame.SaveFile.ReadingOutdatedFile) {
            var warning = $"Warning: Outdated save file ({TankGame.SaveFile.Name})!";
            DrawUtils.DrawStringWithBorder(renderer, font, warning, new Vector2(panelRect.X + 20, panelRect.Bottom - 40), Color.Red * smoothProgress, Color.Black, Vector2.One.ToResolution(), 0f, Anchor.BottomLeft);
        }

        DrawUtils.DrawStringWithBorder(renderer, font, "Press ESC to return", new Vector2(panelRect.Center.X, panelRect.Bottom - 20), Color.Gray * smoothProgress, Color.Black * smoothProgress, new Vector2(0.8f).ToResolution(), 0f, Anchor.BottomCenter);
    }
}