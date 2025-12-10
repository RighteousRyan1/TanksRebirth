using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI;

public static class GameSceneUI {
    public static bool DrawingEnabled = true;
    static GameSceneUI() {
        // any init logic
    }
    public static void DrawAll() {
        if (!DrawingEnabled) return;

        if (!MainMenuUI.IsActive && !LevelEditorUI.IsEditing) {
            var xpBar = GameHandler.ExpBar;
            xpBar.Position = new(WindowUtils.WindowWidth / 2 - xpBar.Scale.X / 2, 50);
            xpBar.Scale = new(600, 20);
            xpBar.Alignment = Anchor.LeftCenter;
            xpBar.FillColor = Color.Green;
            xpBar.EmptyColor = Color.Red;
            xpBar.GainedColor = Color.Lime;
            xpBar.Render(TankGame.SpriteRenderer);
        }

        DrawScores();
        DrawMissionInfoBar();
    }

    public static void DrawScores() {
        // TODO: probably make it where single player can use controllers
        // make a ui for that later xd im too FUCKING TIRED rn
        var drawCount = Client.IsConnected() ? Server.CurrentClientCount : InputUtils.NumConnectedInputs;
        for (int i = 0; i < drawCount; i++) {

            float y = WindowUtils.WindowHeight * 0.9f;
            bool flip = i % 2 != 0;

            if (i >= 2) y -= WindowUtils.WindowHeight * 0.1f;

            DrawScore(PlayerID.PlayerTankColors[i], PlayerTank.KillCounts[i], y, flipSide: flip, scale: 2f);
        }
    }
    public static void DrawMissionInfoBar() {
        var font = FontGlobals.RebirthFontLarge;
        var infoScale = 0.5f;
        var alpha = 1f;
        var bar = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/mission_info");
        var tnk = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank2d");
        var barPos = new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight - (bar.Height + 35).ToResolutionY());
        var missionInfo = LevelEditorUI.IsTestingLevel ? 
            LevelEditorUI.cachedMission.Name : $"{CampaignGlobals.LoadedCampaign.CurrentMission.Name ?? $"{TankGame.GameLanguage.Mission}"}";
        var infoMeasure = font.MeasureString(missionInfo) * infoScale;
        var infoScaling = 1f - ((float)missionInfo.Length / LevelEditorUI.MAX_MISSION_CHARS) + 0.4f;
        var tanksRemaining = $"× {AIManager.CountAll()}";

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, bar, barPos,
            Vector2.UnitY, IntermissionSystem.BannerColor, Vector2.One.ToResolution(), alpha, Anchor.Center, shadowDistScale: 0.5f, shadowAlpha: 0.5f);

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, tnk, barPos + new Vector2(bar.Size().X * 0.25f, 0).ToResolution(),
            Vector2.One, IntermissionSystem.BackgroundColor, new Vector2(1.5f).ToResolution(), alpha, Anchor.Center, shadowDistScale: 0.5f, shadowAlpha: 0.5f);

        DrawUtils.DrawStringWithBorderAndShadow(TankGame.SpriteRenderer, font, barPos - new Vector2(bar.Size().X / 6, 7.5f * infoScaling).ToResolution(),
            Vector2.One, missionInfo, IntermissionSystem.BackgroundColor, IntermissionSystem.ColorForBorders, new Vector2(infoScale * infoScaling).ToResolution(),
            1f, Anchor.BottomRight, shadowDistScale: 1.5f, origMeasureScale: infoScale, shadowAlpha: 0.5f, charSpacing: 10);

        DrawUtils.DrawStringWithBorderAndShadow(TankGame.SpriteRenderer, font, barPos + new Vector2(bar.Size().X * 0.375f, -7.5f).ToResolution(),
            Vector2.One, tanksRemaining, IntermissionSystem.BackgroundColor, IntermissionSystem.ColorForBorders, new Vector2(infoScale).ToResolution(),
            alpha, Anchor.BottomRight, shadowDistScale: 1.5f, origMeasureScale: infoScale, shadowAlpha: 0.5f, charSpacing: 5);
    }

    // helpers
    static void DrawScore(Color color, int score, float y, bool flipSide = false, float scale = 1f, float pertrusion = 90) {
        color = ColorUtils.ChangeColorBrightness(color, 0.25f);
        var brighterColor = ColorUtils.ChangeColorBrightness(color, 0.5f);

        // draw the trailing part.

        // how much of the texture to crop and repeat
        var trim = 1;
        var inner = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard_inner");
        var outer = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard_outer");

        var pertrusionReal = flipSide ? WindowUtils.WindowWidth - pertrusion : pertrusion;
        var outerPadding = flipSide ? WindowUtils.WindowWidth - pertrusion : 0;

        scale = scale.ToResolutionY();

        var pertrusionScaling = new Vector2(pertrusion, scale);

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, outer,
    new Vector2(outerPadding, y),
    shadowDir: Vector2.UnitY,
    color: brighterColor,
    scale: pertrusionScaling,
    alpha: 0.9f,
    Anchor.LeftCenter,
    flip: !flipSide ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
    rotation: 0f,
    shadowAlpha: 0.5f, shadowDistScale: 0.5f, 
    srcRect: new(outer.Width - trim, 0, trim, outer.Height));

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, inner,
    new Vector2(outerPadding, y),
    shadowDir: Vector2.UnitY,
    color: Color.White,
    scale: pertrusionScaling,
    alpha: 0.9f,
    Anchor.LeftCenter,
    flip: !flipSide ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
    rotation: 0f,
    shadowAlpha: 0f, shadowDistScale: 0.5f, 
    srcRect: new(inner.Width - trim, 0, trim, inner.Height));

        // draw the actual score thingy

        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, outer,
    new Vector2(pertrusionReal, y),
    shadowDir: Vector2.UnitY,
    color: brighterColor,
    scale: new Vector2(scale),
    alpha: 0.9f,
    flipSide ? Anchor.RightCenter : Anchor.LeftCenter,
    flip: !flipSide ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
    shadowAlpha: 0.5f, shadowDistScale: 0.5f);
        
        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, inner,
    new Vector2(pertrusionReal, y),
    shadowDir: Vector2.UnitY,
    color: Color.White,
    scale: new Vector2(scale),
    alpha: 0.9f,
    flipSide ? Anchor.RightCenter : Anchor.LeftCenter,
    flip: !flipSide ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
    shadowAlpha: 0f, shadowDistScale: 0.5f);

        DrawUtils.DrawStringWithBorderAndShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, 
            // draws the text on the right side of the screen
            new Vector2(flipSide ? pertrusionReal + 10 : pertrusionReal - 10,
            y - 7f * scale),
            Vector2.One, score.ToString(), brighterColor, color, new Vector2(0.375f * scale), 1f, shadowAlpha: 0.5f);
    }
}
