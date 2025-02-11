using FontStashSharp;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.LevelEditor; 
public static partial class LevelEditorUI {
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
}
