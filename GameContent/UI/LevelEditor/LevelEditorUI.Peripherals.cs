using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.Systems.TankSystem.AI;
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
    public static ParticleManager EditorParticleSystem = new(() => CameraGlobals.ScreenView, () => CameraGlobals.ScreenProjOrthographic);
    // TODO: dynamically drawn 3d models on the UI.
    // TODO: rework scrollbar UI code, massively. my sanity is starting to taper off and achieve an all time low.
    // this will have the tanks at the bottom n stuff.
    // take advantage of DrawUtils.CenteredOrthoToScreen
    // this will include the block model and each tank type
    // then have texts n stuff under it.

    public static string AlertText;
    static float _alertTime;
    public static float DefaultAlertDuration { get; set; } = 120;
    
    // ordered by tier
    static List<Particle> _tankCategoryParticles;
    static List<Particle> _plrCategoryParticles;
    static List<Particle> _blockCategoryParticles;

    static Dictionary<EditorCategory, List<Particle>> _categoryParticles;
    static void OpenPeripherals() {
        _tankCategoryParticles ??= [];
        _plrCategoryParticles ??= [];
        _blockCategoryParticles ??= [];
        _categoryParticles ??= [];
        _tankCategoryParticles.Clear();
        _plrCategoryParticles.Clear();
        _blockCategoryParticles.Clear();
        _categoryParticles.Clear();
        // ensure particle trimming for blocks
        // spawn block model particle
        // this is for stacks
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

        for (int i = TankID.Brown; i < TankID.Collection.Count; i++) {
            var elem = TankID.Collection[i];

            // i fear there's no better way to do this?
            var tnkDummy = new AITank(i, false, true);
            var tnkPart = EditorParticleSystem.MakeParticle(Vector3.Zero, tnkDummy.DrawParamsTank.Model, Tank.Assets["tank_" + elem.Key.ToLower()]);
            tnkDummy.Remove(true); // eradicate it after dummy init

            tnkPart.MeshesToIgnore = ["Shadow"];
            tnkPart.Color = Color.Black;
            tnkPart.UniqueBehavior = (p) => {
                var t = RuntimeData.RunTime / 50;
                tnkPart.Yaw = t;
                tnkPart.Roll = 0; // MathF.Sin(t) * 0.25f; // barrel roll
                tnkPart.Pitch = 0; // forward/backward
            };

            _tankCategoryParticles.Add(tnkPart);
        }

        for (int i = 0; i < PlayerID.Collection.Count; i++) {
            var elem = PlayerID.Collection[i];

            // i fear there's no better way to do this?
            var tnkPart = EditorParticleSystem.MakeParticle(Vector3.Zero, ModelGlobals.TankPlayer.Duplicate(), Tank.Assets["plrtank_" + elem.Key.ToLower()]);

            tnkPart.MeshesToIgnore = ["Shadow"];
            tnkPart.Color = Color.Black;
            tnkPart.UniqueBehavior = (p) => {
                var t = RuntimeData.RunTime / 50;
                tnkPart.Yaw = t;
                tnkPart.Roll = 0; // MathF.Sin(t) * 0.25f; // barrel roll
                tnkPart.Pitch = 0; // forward/backward
            };

            _plrCategoryParticles.Add(tnkPart);
        }

        for (int i = 0; i < BlockID.Collection.Count; i++) {
            var elem = BlockID.Collection[i];

            // i fear there's no better way to do this?

            // var altModel = Block.WouldUseAlternateModel(BlockStack);

            // account for snowy?
            // var mdl = altModel ? ModelGlobals.BlockStackAlt : ModelGlobals.BlockStack;
            var block = new Block(i, BlockStack, new Vector2(float.MaxValue), true);
            var blPart = EditorParticleSystem.MakeParticle(Vector3.Zero, block.Model, block.Texture);
            block.Remove();

            // i still need to discover how 

            blPart.Color = Color.Black;
            blPart.UniqueBehavior = (p) => {
                var t = RuntimeData.RunTime / 50;
                blPart.Yaw = t;
                blPart.Roll = MathF.Sin(t) * 0.25f; // barrel roll
                blPart.Pitch = 0; // forward/backward
            };

            _blockCategoryParticles.Add(blPart);
        }

        _categoryParticles.Add(EditorCategory.EnemyTanks, _tankCategoryParticles);
        _categoryParticles.Add(EditorCategory.PlayerTanks, _plrCategoryParticles);
        _categoryParticles.Add(EditorCategory.Terrain, _blockCategoryParticles);
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
    public static void DrawPlacementInfo(SpriteBatch sb) {
        // placement information
        sb.Draw(TextureGlobals.Pixels[Color.White], PlaceInfoRect, null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)40.ToResolutionY()), null, Color.White, 0f, Vector2.Zero, default, 0f);
        sb.DrawString(FontGlobals.RebirthFont,
            TankGame.GameLanguage.PlaceInfo,
            new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 3.ToResolutionY()),
            Color.Black,
            Vector2.One.ToResolution(),
            0f,
            Anchor.TopCenter.GetAnchor(FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.PlaceInfo)));

        var helpText = TankGame.GameLanguage.PlacementTeamInfo;
        PlaceInfoStart = new(WindowUtils.WindowWidth - 250.ToResolutionX(), 140.ToResolutionY());

        // draw tank placement info
        if (CurCategory == EditorCategory.EnemyTanks || CurCategory == EditorCategory.PlayerTanks) {
            // TODO: should be optimised. do later.
            sb.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.TankTeams, new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y - 80.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.TankTeams) / 2);

            sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)PlaceInfoStart.X, (int)(PlaceInfoStart.Y - 40.ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, Color.Black, 0f, Vector2.Zero, default, 0f);
            sb.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.NoTeam, new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y - 40.ToResolutionY()), Color.Black, Vector2.One.ToResolution(), 0f, Vector2.Zero);
            for (int i = 0; i < TeamID.Collection.Count - 1; i++) {
                var color = TeamID.TeamColors[i + 1];

                sb.DrawString(FontGlobals.RebirthFont, TeamColorsLocalized[i + 1], new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y + (i * 40).ToResolutionY()), color, Vector2.One.ToResolution(), 0f, Vector2.Zero);
                sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)PlaceInfoStart.X, (int)(PlaceInfoStart.Y + (i * 40).ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, color, 0f, Vector2.Zero, default, 0f);
            }

            // draw the visual that indicates to the user that they can press up and down arrows
            sb.DrawString(FontGlobals.RebirthFontLarge, ">", new Vector2(PlaceInfoStart.X - 25.ToResolutionX(), PlaceInfoStart.Y + ((SelectedTankTeam - 1) * 40).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFontLarge.MeasureString(">") / 2);

            if (SelectedTankTeam != TeamID.Magenta)
                sb.DrawString(FontGlobals.RebirthFont, "v", new Vector2(PlaceInfoStart.X - 25.ToResolutionX(), PlaceInfoStart.Y + ((SelectedTankTeam - 1) * 40 + 50).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString("v") / 2);
            if (SelectedTankTeam != TeamID.NoTeam)
                sb.DrawString(FontGlobals.RebirthFont,
                    "v",
                    new Vector2(PlaceInfoStart.X - 25.ToResolutionX(), PlaceInfoStart.Y + ((SelectedTankTeam - 1) * 40 - 10).ToResolutionY()),
                    Color.White,
                    Vector2.One.ToResolution(),
                    MathHelper.Pi,
                    FontGlobals.RebirthFont.MeasureString("v") / 2);
        }
        // draw obstacle placement info
        else if (CurCategory == EditorCategory.Terrain) {
            helpText = TankGame.GameLanguage.BlockStackFlavor;
            // TODO: add static dict for specific types?
            PlaceInfoStart = new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 450.ToResolutionY());

            /*var tex = SelectedBlockType != BlockID.Hole ? $"{BlockID.Collection.GetKey(SelectedBlockType)}_{BlockStack}" : $"{BlockID.Collection.GetKey(SelectedBlockType)}";
            var size = RenderTextures[tex].Size();
            sb.Draw(RenderTextures[tex], PlaceInfoStart, null, Color.White, 0f, new Vector2(size.X / 2, size.Y), Vector2.One.ToResolution(), default, 0f);
            // TODO: reduce the hardcode for modders, yeah
            if (SelectedBlockType != BlockID.Teleporter && SelectedBlockType != BlockID.Hole) {
                sb.DrawString(FontGlobals.RebirthFontLarge, "v", new Vector2(PlaceInfoStart.X + 100.ToResolutionX(), PlaceInfoStart.Y - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFontLarge.MeasureString("v") / 2);
                sb.DrawString(FontGlobals.RebirthFontLarge, "v", new Vector2(PlaceInfoStart.X - 100.ToResolutionX(), PlaceInfoStart.Y - 25.ToResolutionY()), Color.White, Vector2.One.ToResolution(), MathHelper.Pi, FontGlobals.RebirthFontLarge.MeasureString("v") / 2);
            }*/
        }
        sb.DrawString(FontGlobals.RebirthFont, helpText, new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), WindowUtils.WindowHeight / 2 - 70.ToResolutionY()), Color.White, new Vector2(0.5f).ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString(helpText) / 2);
        sb.Draw(TextureGlobals.Pixels[Color.White], _curHoverRect, null, HoverBoxColor * 0.5f, 0f, Vector2.Zero, default, 0f);

        // ensure model draws over
        sb.End();
        
        // hacky ahh...
        TankGame.Instance.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);
        TankGame.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        RenderEditorParticles(sb);

        sb.Begin();
    }
    public static void DrawLevelInfo(SpriteBatch sb) {
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 125).ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 40).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);

        sb.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.LevelInfo, new Vector2(175, 3).ToResolution(), Color.Black, Vector2.One.ToResolution(), 0f, Anchor.TopCenter.GetAnchor(FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.LevelInfo)));
        sb.DrawString(FontGlobals.RebirthFont, $"{TankGame.GameLanguage.EnemyTankTotal}: {AIManager.CountAll()}", new Vector2(10, 40).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        // localize later.
        sb.DrawString(FontGlobals.RebirthFont, $"Total Terrain: {Block.AllBlocks.Count(x => x is not null)}", new Vector2(10, 60).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        sb.DrawString(FontGlobals.RebirthFont, $"{TankGame.GameLanguage.DifficultyRating}: {difficultyRating:0.00}", new Vector2(10, 80).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
    }
    public static void DrawAlerts(SpriteBatch sb) {
        if (_alertTime <= 0) return;

        var scale = 0.5f;
        sb.Draw(ChatSystem.ChatAlert,
            new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.625f - ChatSystem.ChatAlert.Size().Y.ToResolutionY() * scale),
            null,
            Color.White,
            0f,
            ChatSystem.ChatAlert.Size() / 2,
            new Vector2(scale).ToResolution(),
            default,
            default);
        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, AlertText, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.625f), Color.Red, Color.White, new Vector2(0.4f).ToResolution(), 0f, Anchor.Center);
        _alertTime -= RuntimeData.DeltaTime;
    }    

    public static void UpdateParticles() {
        EditorParticleSystem.UpdateParticles();
    }
    public static void RenderEditorParticles(SpriteBatch spriteBatch) {
        // EditorParticleSystem.RenderParticles(spriteBatch);
        EditorParticleSystem.RenderModelParticles();
    }
}
