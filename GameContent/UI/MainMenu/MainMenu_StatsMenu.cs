
using FontStashSharp;
using Microsoft.Xna.Framework;
using System.Linq;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    private static string[] _info;
    public static void RequestStats() {
        _info = [
            $"{TankGame.GameLanguage.TankKillsTotal}: {TankGame.GameData.TotalKills}",
            $"{TankGame.GameLanguage.TankKillsTotalBullets}: {TankGame.GameData.BulletKills}",
            $"{TankGame.GameLanguage.TankKillsTotalBulletsBounced}: {TankGame.GameData.BounceKills}",
            $"{TankGame.GameLanguage.TankKillsTotalMines}: {TankGame.GameData.MineKills}",
            $"{TankGame.GameLanguage.MissionsCompleted}: {TankGame.GameData.MissionsCompleted}",
            $"{TankGame.GameLanguage.CampaignsCompleted}: {TankGame.GameData.CampaignsCompleted}",
            $"{TankGame.GameLanguage.Deaths}: {TankGame.GameData.Deaths}",
            $"{TankGame.GameLanguage.Suicides}: {TankGame.GameData.Suicides}",
            $"{TankGame.GameLanguage.TimePlayedTotal}: {TankGame.GameData.TimePlayed.TotalHours:0.0} hrs",
            $"{TankGame.GameLanguage.TimePlayedCurrent}: {TankGame.CurrentSessionTimer.Elapsed.TotalMinutes:0.0} mins"
        ];
    }
    public static void RenderStatsMenu() {
        DrawStats(new Vector2(WindowUtils.WindowWidth * 0.3f, 200.ToResolutionY()), new Vector2(WindowUtils.WindowWidth * 0.7f, 40.ToResolutionY()), Anchor.TopCenter);
    }
    // probably GC collection here at like crazy amounts
    public static void DrawStats(Vector2 genericStatsPos, Vector2 tankKillsPos, Anchor aligning) {
        for (int i = 0; i < _info.Length; i++)
            DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, _info[i], genericStatsPos + Vector2.UnitY * (i * 25).ToResolutionY(), Color.White, Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
        //TankGame.SpriteRenderer.DrawString(TankGame.TextFont, _info[i], genericStatsPos + Vector2.UnitY * (i * 25).ToResolutionY(), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString(_info[i])), 0f);
        DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, TankGame.GameLanguage.TankKillsPerType + ":", tankKillsPos, Color.White, Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
        // GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString("Tanks Killed by Type:"))
        int count = 1;
        for (int i = 2; i < TankGame.GameData.TankKills.Count; i++) {
            var elem = TankGame.GameData.TankKills.ElementAt(i);
            if (elem.Value == 0)
                continue;
            count++;
            var split = TankID.Collection.GetKey(elem.Key)!.SplitByCamel();
            var display = $"{split}: {elem.Value}";
            DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, display, tankKillsPos + Vector2.UnitY * ((count - 1) * 25).ToResolutionY(), AITank.TankDestructionColors[elem.Key], Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
            //TankGame.SpriteRenderer.DrawString(TankGame.TextFont, display, tankKillsPos + Vector2.UnitY * ((i - 1) * 25).ToResolutionY(), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString(display)), 0f);
        }
        if (TankGame.GameData.ReadingOutdatedFile)
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Outdated save file ({TankGame.GameData.Name})! Delete the old one!", new Vector2(8, 8), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero, 0f);
        DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, "Press ESC to return", WindowUtils.WindowBottom - Vector2.UnitY * 40.ToResolutionY(), Color.White, Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
        // GameUtils.GetAnchor(aligning, TankGame.TextFont.MeasureString("Press ESC to return"))
    }
}
