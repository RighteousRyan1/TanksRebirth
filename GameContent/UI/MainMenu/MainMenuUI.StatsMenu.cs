
using FontStashSharp;
using Microsoft.Xna.Framework;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    private static string[] _info;
    public static void RequestStats() {
        _info = [
            $"{TankGame.GameLanguage.TankKillsTotal}: {TankGame.SaveFile.TotalKills}",
            $"{TankGame.GameLanguage.TankKillsTotalBullets}: {TankGame.SaveFile.BulletKills}",
            $"{TankGame.GameLanguage.TankKillsTotalBulletsBounced}: {TankGame.SaveFile.BounceKills}",
            $"{TankGame.GameLanguage.TankKillsTotalMines}: {TankGame.SaveFile.MineKills}",
            $"{TankGame.GameLanguage.MissionsCompleted}: {TankGame.SaveFile.MissionsCompleted}",
            $"{TankGame.GameLanguage.CampaignsCompleted}: {TankGame.SaveFile.CampaignsCompleted}",
            $"{TankGame.GameLanguage.Deaths}: {TankGame.SaveFile.Deaths}",
            $"{TankGame.GameLanguage.Suicides}: {TankGame.SaveFile.Suicides}",
            $"{TankGame.GameLanguage.TimePlayedTotal}: {TankGame.SaveFile.TimePlayed.TotalHours:0.0} hrs",
            $"{TankGame.GameLanguage.TimePlayedCurrent}: {TankGame.CurrentSessionTimer.Elapsed.TotalMinutes:0.0} mins"
        ];
    }
    public static void RenderStatsMenu() {
        DrawStats(new Vector2(WindowUtils.WindowWidth * 0.3f, 200.ToResolutionY()), new Vector2(WindowUtils.WindowWidth * 0.7f, 40.ToResolutionY()), Anchor.TopCenter);
    }
    // probably GC collection here at like crazy amounts
    public static void DrawStats(Vector2 genericStatsPos, Vector2 tankKillsPos, Anchor aligning) {
        for (int i = 0; i < _info.Length; i++)
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, _info[i], genericStatsPos + Vector2.UnitY * (i * 25).ToResolutionY(), Color.White, Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
        //TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, _info[i], genericStatsPos + Vector2.UnitY * (i * 25).ToResolutionY(), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(aligning, FontGlobals.RebirthFont.MeasureString(_info[i])), 0f);
        DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, TankGame.GameLanguage.TankKillsPerType + ":", tankKillsPos, Color.White, Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
        // GameUtils.GetAnchor(aligning, FontGlobals.RebirthFont.MeasureString("Tanks Killed by Type:"))
        int count = 1;
        for (int i = 2; i < TankGame.SaveFile.TankKills.Count; i++) {
            var elem = TankGame.SaveFile.TankKills.ElementAt(i);
            if (elem.Value == 0)
                continue;
            count++;
            var split = TankID.Collection.GetKey(elem.Key)!.SplitByCamel();
            var display = $"{split}: {elem.Value}";
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, display, tankKillsPos + Vector2.UnitY * ((count - 1) * 25).ToResolutionY(), AITank.TankDestructionColors[elem.Key], Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
            //TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, display, tankKillsPos + Vector2.UnitY * ((i - 1) * 25).ToResolutionY(), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(aligning, FontGlobals.RebirthFont.MeasureString(display)), 0f);
        }
        if (TankGame.SaveFile.ReadingOutdatedFile)
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"Outdated save file ({TankGame.SaveFile.Name})! Delete the old one!", new Vector2(8, 8), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero, 0f);
        DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, "Press ESC to return", WindowUtils.WindowBottom - Vector2.UnitY * 40.ToResolutionY(), Color.White, Color.Black, Vector2.One.ToResolution(), 0f, Anchor.Center);
        // GameUtils.GetAnchor(aligning, FontGlobals.RebirthFont.MeasureString("Press ESC to return"))
    }
}
