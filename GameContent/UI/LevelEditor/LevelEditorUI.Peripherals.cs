using FontStashSharp;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI.LevelEditor;

#pragma warning disable

// TODO: introduce localization to reduce garbage collection
public static partial class LevelEditorUI {
    public static UITextButton TestLevel;
    public static UITextButton Perspective;

    public static UITextButton TerrainCategory;
    public static UITextButton EnemyTanksCategory;
    public static UITextButton PlayerTanksCategory;

    public static UITextButton Properties;
    public static UITextButton LoadLevel;

    public static UITextButton AutoOrientTanks;

    public static UITextButton ReturnToEditor;

    public static UITextButton AddMissionBtn;
    public static UITextButton RemoveMissionBtn;
    public static UITextButton MoveMissionUp;
    public static UITextButton MoveMissionDown;
    public static Rectangle PlaceInfoRect => new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)500.ToResolutionY());
    public static Vector2 PlaceInfoStart;

    // 255 for now. no real need to make it bigger unless modders are ballin'
    public static ParticleManager EditorParticleSystem = new(255, () => CameraGlobals.ScreenView, () => CameraGlobals.ScreenProjOrthographic);
    // TODO: dynamically drawn 3d models on the UI.
    // TODO: rework scrollbar UI code, massively. my sanity is starting to taper off and achieve an all time low.
    // this will have the tanks at the bottom n stuff.
    // take advantage of DrawUtils.CenteredOrthoToScreen
    // this will include the block model and each tank type
    // then have texts n stuff under it.

    public static string AlertText;
    private static float _alertTime;
    public static float DefaultAlertDuration { get; set; } = 120;
    private static void OpenPeripherals() {
        // ensure particle trimming for blocks
        // spawn block model particle
        var bp = EditorParticleSystem.MakeParticle(Vector3.Zero, ModelGlobals.BlockStack.Duplicate(), GameScene.Assets["block.1"]);
        bp.Scale = Vector3.One;
        bp.Alpha = 1f;
        bp.UniqueBehavior = (p) => {
            bp.Position = DrawUtils.CenteredOrthoToScreen(PlaceInfoStart).Expand();
            bp.Roll = 0f;
            bp.Yaw = 0f;
            bp.Pitch = MathHelper.PiOver4;
            bp.Scale = new(2f); //new(2f + MathF.Sin(RuntimeData.RunTime / 10f) / 10f);
        };

        // block culler
        var bc = EditorParticleSystem.MakeParticle(Vector3.Zero, ModelGlobals.BlockStack.Duplicate(),
            /*TextureGlobals.Pixels[Color.Transparent]*/GameScene.Assets["block.1"]);
        bc.Alpha = 1f;
        bc.UniqueBehavior = (p) => {
            bc.Alpha = 1f;
            bc.Position = DrawUtils.CenteredOrthoToScreen(MouseUtils.MousePosition).Expand();
            bc.Roll = 0f;
            bc.Yaw = 0f;
            bc.Pitch = MathHelper.PiOver4;
            bc.Scale = new(2f);
        };
    }
    private static void ClosePeripherals() {
        EditorParticleSystem.Empty();
    }
    /// <summary>Displays an alert to the screen.</summary>
    /// <param name="alert">The text to show in the alert.</param>
    /// <param name="timeOverride">The amount of time to display the alert for. Defaults to <see cref="DefaultAlertDuration"/>.</param>
    public static void Alert(string alert, float timeOverride = 0f) {
        _alertTime = timeOverride != 0f ? timeOverride : DefaultAlertDuration;
        AlertText = alert;
        SoundPlayer.SoundError();
    }
    public static void DrawPlacementInfo() {
        // placement information
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], PlaceInfoRect, null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)40.ToResolutionY()), null, Color.White, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont,
            TankGame.GameLanguage.PlaceInfo,
            new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 3.ToResolutionY()),
            Color.Black,
            Vector2.One.ToResolution(),
            0f,
            Anchor.TopCenter.GetAnchor(FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.PlaceInfo)));

        var helpText = TankGame.GameLanguage.PlacementTeamInfo;
        PlaceInfoStart = new(WindowUtils.WindowWidth - 250.ToResolutionX(), 140.ToResolutionY());

        // draw tank placement info
        if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks) {
            // TODO: should be optimised. do later.
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.TankTeams, new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y - 80.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.TankTeams) / 2);

            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)PlaceInfoStart.X, (int)(PlaceInfoStart.Y - 40.ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, Color.Black, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.NoTeam, new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y - 40.ToResolutionY()), Color.Black, Vector2.One.ToResolution(), 0f, Vector2.Zero);
            for (int i = 0; i < TeamID.Collection.Count - 1; i++) {
                var color = TeamID.TeamColors[i + 1];

                TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, TeamColorsLocalized[i + 1], new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y + (i * 40).ToResolutionY()), color, Vector2.One.ToResolution(), 0f, Vector2.Zero);
                TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)PlaceInfoStart.X, (int)(PlaceInfoStart.Y + (i * 40).ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, color, 0f, Vector2.Zero, default, 0f);
            }

            // draw the visual that indicates to the user that they can press up and down arrows
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, ">", new Vector2(PlaceInfoStart.X - 25.ToResolutionX(), PlaceInfoStart.Y + ((SelectedTankTeam - 1) * 40).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFontLarge.MeasureString(">") / 2);

            if (SelectedTankTeam != TeamID.Magenta)
                TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, "v", new Vector2(PlaceInfoStart.X - 25.ToResolutionX(), PlaceInfoStart.Y + ((SelectedTankTeam - 1) * 40 + 50).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString("v") / 2);
            if (SelectedTankTeam != TeamID.NoTeam)
                TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont,
                    "v",
                    new Vector2(PlaceInfoStart.X - 25.ToResolutionX(), PlaceInfoStart.Y + ((SelectedTankTeam - 1) * 40 - 10).ToResolutionY()),
                    Color.White,
                    Vector2.One.ToResolution(),
                    MathHelper.Pi,
                    FontGlobals.RebirthFont.MeasureString("v") / 2);
        }
        // draw obstacle placement info
        else if (CurCategory == Category.Terrain) {
            helpText = "UP and DOWN to change stack.";
            // TODO: add static dict for specific types?
            var tex = SelectedBlockType != BlockID.Hole ? $"{BlockID.Collection.GetKey(SelectedBlockType)}_{BlockHeight}" : $"{BlockID.Collection.GetKey(SelectedBlockType)}";
            var size = RenderTextures[tex].Size();
            PlaceInfoStart = new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 450.ToResolutionY());
            TankGame.SpriteRenderer.Draw(RenderTextures[tex], PlaceInfoStart, null, Color.White, 0f, new Vector2(size.X / 2, size.Y), Vector2.One.ToResolution(), default, 0f);
            // TODO: reduce the hardcode for modders, yeah
            if (SelectedBlockType != BlockID.Teleporter && SelectedBlockType != BlockID.Hole) {
                TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, "v", new Vector2(PlaceInfoStart.X + 100.ToResolutionX(), PlaceInfoStart.Y - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFontLarge.MeasureString("v") / 2);
                TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFontLarge, "v", new Vector2(PlaceInfoStart.X - 100.ToResolutionX(), PlaceInfoStart.Y - 25.ToResolutionY()), Color.White, Vector2.One.ToResolution(), MathHelper.Pi, FontGlobals.RebirthFontLarge.MeasureString("v") / 2);
            }
        }
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, helpText, new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), WindowUtils.WindowHeight / 2 - 70.ToResolutionY()), Color.White, new Vector2(0.5f).ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString(helpText) / 2);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _curHoverRect, null, HoverBoxColor * 0.5f, 0f, Vector2.Zero, default, 0f);
    }
    public static void DrawLevelInfo() {
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 125).ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 40).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);

        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.LevelInfo, new Vector2(175, 3).ToResolution(), Color.Black, Vector2.One.ToResolution(), 0f, Anchor.TopCenter.GetAnchor(FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.LevelInfo)));
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"{TankGame.GameLanguage.EnemyTankTotal}: {AIManager.CountAll()}", new Vector2(10, 40).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        // localize later.
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"Total Terrain: {Block.AllBlocks.Count(x => x is not null)}", new Vector2(10, 60).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"{TankGame.GameLanguage.DifficultyRating}: {difficultyRating:0.00}", new Vector2(10, 80).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
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
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, AlertText, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.625f), Color.Red, Color.White, new Vector2(0.4f).ToResolution(), 0f, Anchor.Center);
            _alertTime -= RuntimeData.DeltaTime;
        }
    }

    public static void UpdateParticles() {
        EditorParticleSystem.UpdateParticles();
    }
    public static void RenderEditorParticles() {
        EditorParticleSystem.RenderParticles();
        EditorParticleSystem.RenderModelParticles();
    }
}
