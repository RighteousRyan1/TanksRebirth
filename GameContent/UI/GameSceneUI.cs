using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI;

public static class GameSceneUI {
    public static void Initialize() {
        // put any initialization logic here if needed
    }
    public static void DrawScores() {
        DrawScore(PlayerID.PlayerTankColors[NetPlay.GetMyClientId()].ToColor(), PlayerTank.KillCounts[NetPlay.GetMyClientId()], WindowUtils.WindowHeight * 0.9f);
    }
    public static void DrawMissionInfoBar() {
        var font = TankGame.TextFontLarge;
        var infoScale = 0.5f;
        var alpha = 1f;
        var bar = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/mission_info");
        var tnk = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank2d");
        var barPos = new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight - (bar.Height + 35).ToResolutionY());
        var missionInfo = LevelEditorUI.IsTestingLevel ? 
            LevelEditorUI.cachedMission.Name : $"{CampaignGlobals.LoadedCampaign.CurrentMission.Name ?? $"{TankGame.GameLanguage.Mission}"}";
        var infoMeasure = font.MeasureString(missionInfo) * infoScale;
        var infoScaling = 1f - ((float)missionInfo.Length / LevelEditorUI.MAX_MISSION_CHARS) + 0.4f;
        var tanksRemaining = $"x {AIManager.CountAll()}";

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, bar, barPos,
            Vector2.UnitY, IntermissionSystem.StripColor, Vector2.One.ToResolution(), alpha, Anchor.Center, shadowDistScale: 0.5f, shadowAlpha: 0.5f);

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, tnk, barPos + new Vector2(bar.Size().X * 0.25f, 0).ToResolution(),
            Vector2.One, IntermissionSystem.BackgroundColor, new Vector2(1.5f).ToResolution(), alpha, Anchor.Center, shadowDistScale: 0.5f, shadowAlpha: 0.5f);

        DrawUtils.DrawBorderedStringWithShadow(TankGame.SpriteRenderer, font, barPos - new Vector2(bar.Size().X / 6, 7.5f * infoScaling).ToResolution(),
            Vector2.One, missionInfo, IntermissionSystem.BackgroundColor, IntermissionSystem.ColorForBorders, new Vector2(infoScale * infoScaling).ToResolution(),
            1f, Anchor.BottomRight, shadowDistScale: 1.5f, origMeasureScale: infoScale, shadowAlpha: 0.5f, charSpacing: 10);

        DrawUtils.DrawBorderedStringWithShadow(TankGame.SpriteRenderer, font, barPos + new Vector2(bar.Size().X * 0.375f, -7.5f).ToResolution(),
            Vector2.One, tanksRemaining, IntermissionSystem.BackgroundColor, IntermissionSystem.ColorForBorders, new Vector2(infoScale).ToResolution(),
            alpha, Anchor.BottomRight, shadowDistScale: 1.5f, origMeasureScale: infoScale, shadowAlpha: 0.5f, charSpacing: 5);
    }

    // helpers

    private static void DrawScore(Color color, int score, float y, float scale = 1f, float pertrusion = 90) {

        color = ColorUtils.ChangeColorBrightness(color, 0.25f);
        var brighterColor = ColorUtils.ChangeColorBrightness(color, 0.5f);

        // draw the trailing part.

        // how much of the texture to crop and repeat
        var trim = 5;
        var inner = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard_inner");
        var outer = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard_outer");

        // invert pertrusion since anchor doesn't want to behave properly
        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, outer,
    new Vector2(-pertrusion.ToResolutionX(), y),
    shadowDir: Vector2.UnitY,
    color: brighterColor,
    scale: new Vector2(scale * 2 * (pertrusion / trim), scale * 2).ToResolution(),
    alpha: 0.8f,
    Anchor.LeftCenter,
    flip: false,
    rotation: 0f,
    shadowAlpha: 0.5f, shadowDistScale: 0.5f, srcRect: new(outer.Width - trim, 0, trim, outer.Height));

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, inner,
    new Vector2(-pertrusion.ToResolutionX(), y),
    shadowDir: Vector2.UnitY,
    color: Color.White,
    scale: new Vector2(scale * 2 * (pertrusion / trim), scale * 2).ToResolution(),
    alpha: 0.8f,
    Anchor.LeftCenter,
    flip: false,
    rotation: 0f,
    shadowAlpha: 0f, shadowDistScale: 0.5f, srcRect: new(inner.Width - trim, 0, trim, inner.Height));

        // draw the actual score thingy

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, outer,
    new Vector2(pertrusion.ToResolutionX(), y),
    shadowDir: Vector2.UnitY,
    color: brighterColor,
    scale: new Vector2(scale * 2).ToResolution(),
    alpha: 0.8f,
    Anchor.LeftCenter,
    flip: true,
    shadowAlpha: 0.5f, shadowDistScale: 0.5f);
        
        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, inner,
    new Vector2(pertrusion.ToResolutionX(), y),
    shadowDir: Vector2.UnitY,
    color: Color.White,
    scale: new Vector2(scale * 2).ToResolution(),
    alpha: 0.8f,
    Anchor.LeftCenter,
    flip: true,
    shadowAlpha: 0f, shadowDistScale: 0.5f);

        DrawUtils.DrawBorderedStringWithShadow(TankGame.SpriteRenderer, TankGame.TextFontLarge, new Vector2((pertrusion - 10).ToResolutionX(),
            y - 14f.ToResolutionY() * scale),
            Vector2.One, score.ToString(), brighterColor, color, new Vector2(0.675f * scale).ToResolution(), 1f, shadowAlpha: 0.5f);
    }

    private static Texture2D GenerateScoreboard(Color color) {
        var scoreboard = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard");

        var colors = new Color[scoreboard.Width * scoreboard.Height];
        scoreboard.GetData(colors);

        for (int i = 0; i < colors.Length; i++) {
            if (colors[i] == Color.Black) {
                colors[i] = color;
            }
        }

        var texture = new Texture2D(TankGame.Instance.GraphicsDevice, scoreboard.Width, scoreboard.Height);

        texture.SetData(colors);
        return texture;
    }
}
