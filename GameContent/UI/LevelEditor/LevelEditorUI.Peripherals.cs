using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.Tanks.AI;
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
    public static ParticleManager EditorParticleSystem = new(() => mtxvw(), () => CameraGlobals.ScreenProjOrthographic);

    static Matrix mtxvw() {
        return CameraGlobals.ScreenView * Matrix.CreateRotationX(CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE); // used to be pi/8
    }
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

    // the floor plane the block stack sinks into
    static Particle _stackFloor;
    // what the stack preview currently shows, so it's only rebuilt when the selection changes
    static int _previewType = -1, _previewStack = -1;
    static MapTheme _previewTheme;
    static readonly BlendState _depthOnly = new() {
        Name = "LevelEditor.DepthOnly",
        ColorWriteChannels = ColorWriteChannels.None,
    };
    // tracks if the latest placement preview block is stackable or not
    static bool _latestStackable = true;
    public static float ElementRotation => RuntimeData.RunTime / 100;
    // this is causing sometimes for block shadows to not be destroyed
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
        // make this a field too?
        var bp = EditorParticleSystem.MakeParticle(Vector3.Zero, ModelGlobals.BlockStack.Asset, GameScene.Assets["block.1"]);
        bp.Scale = Vector3.One * 2;
        bp.Alpha = 1f;
        bp.Color = Color.Black;
        bp.MeshesToIgnore = ["Shadow"];
        _previewType = -1;
        bp.UpdateCallback = (p) => {
            UpdatePlacementInfo(bp);
            // bp.Model = (Block.WouldUseAlternateModel(BlockStack) ? ModelGlobals.BlockStackAlt : ModelGlobals.BlockStack).Asset;
            bp.Position = DrawUtils.CenteredOrthoToScreen(PlaceInfoStart).Expand();
            bp.Yaw = MathHelper.PiOver4;
            bp.Alpha = CurCategory == EditorCategory.Terrain ? 1 : 0;
        };

        _stackFloor = EditorParticleSystem.MakeParticle(Vector3.Zero, ModelGlobals.FlatFace.Asset, TextureGlobals.Pixels[Color.Gray]);

        // most of these values are magical. the particle system is truly fucked
        _stackFloor.Scale = Vector3.One * 10;
        _stackFloor.Alpha = 1f;
        _stackFloor.Yaw = MathHelper.PiOver4;
        _stackFloor.LightPower = 0.775f;
        _stackFloor.Color = Color.Black;
        _stackFloor.ApplyGameLight = false;
        _stackFloor.UpdateCallback = (p) => {
            // true cuz there's no need to lower the Y
            var curHeight = Block.GetOffsetY(BlockStack, true) / Block.BLOCK_DEF_SCALING;
            _stackFloor.Position = bp.Position + Vector3.Up * curHeight * bp.Scale;
            _stackFloor.Alpha = _latestStackable ? bp.Alpha * 0.25f : 0f;
        };

        for (int i = TankID.Brown; i < TankID.Collection.Count; i++) {
            var elem = TankID.Collection[i];

            // i fear there's no better way to do this?
            var tnkDummy = new AITank(i, true, true);
            var tnkPart = EditorParticleSystem.MakeParticle(Vector3.Zero, tnkDummy.DrawParamsTank.Model, tnkDummy.DrawParamsTank.TankTexture);

            // at some point
            /*tnkPart.DrawCallback = (p) => {
                GameShaders.DrawTankMesh(mesh, tnkPart.po, EditorParticleSystem.SystemView, EditorParticleSystem.SystemProjection,
                    tnkDummy.DrawParamsTank.TankTexture, tnkDummy.DrawParamsTank.TankAlpha, true);
            };*/
            tnkDummy.Remove(true); // eradicate it after dummy init

            tnkPart.MeshesToIgnore = ["Shadow"];
            tnkPart.Color = Color.Black;
            tnkPart.Yaw = ElementRotation;

            _tankCategoryParticles.Add(tnkPart);
        }

        for (int i = 0; i < PlayerID.Collection.Count; i++) {
            var elem = PlayerID.Collection[i];

            var tnkPart = EditorParticleSystem.MakeParticle(Vector3.Zero, ModelGlobals.TankPlayer.Duplicate(), Tank.Assets["plrtank_" + elem.Key.ToLower()]);

            tnkPart.MeshesToIgnore = ["Shadow"];
            tnkPart.Color = Color.Black;
            tnkPart.Yaw = ElementRotation;

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
            blPart.Yaw = ElementRotation;

            // i still need to discover how 

            blPart.MeshesToIgnore = ["Shadow"];
            blPart.Texturing = block.TextureMap;

            block.Remove();

            blPart.Color = Color.Black;

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
    // this needs to be written much neater
    public static void DrawPlacementInfo(SpriteBatch sb) {
        // placement information
        sb.Draw(TextureGlobals.Pixels[Color.White], PlaceInfoRect, null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)40.ToResolutionY()), null, Color.White, 0f, Vector2.Zero, default, 0f);
        sb.DrawString(FontGlobals.RebirthFont,
            TankGame.GameLanguage.LevelEdit.PlaceInfo,
            new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 3.ToResolutionY()),
            Color.Black,
            Vector2.One.ToResolution(),
            0f,
            Anchor.TopCenter.GetAnchor(FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.LevelEdit.PlaceInfo)));

        var helpText = TankGame.GameLanguage.LevelEdit.PlacementTeamInfo;
        PlaceInfoStart = new(WindowUtils.WindowWidth - 250.ToResolutionX(), 140.ToResolutionY());

        // draw tank placement info
        if (CurCategory == EditorCategory.EnemyTanks || CurCategory == EditorCategory.PlayerTanks) {
            // TODO: should be optimised. do later.
            sb.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.LevelEdit.TankTeams, new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y - 80.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.LevelEdit.TankTeams) / 2);

            sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)PlaceInfoStart.X, (int)(PlaceInfoStart.Y - 40.ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, Color.Black, 0f, Vector2.Zero, default, 0f);
            sb.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.Teams.NoTeam, new Vector2(PlaceInfoStart.X + 45.ToResolutionX(), PlaceInfoStart.Y - 40.ToResolutionY()), Color.Black, Vector2.One.ToResolution(), 0f, Vector2.Zero);
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
        sb.DrawString(FontGlobals.RebirthFont, helpText, new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), WindowUtils.WindowHeight / 2 - 70.ToResolutionY()), Color.White, new Vector2(0.5f).ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString(helpText) / 2);
        sb.Draw(TextureGlobals.Pixels[Color.White], _curHoverRect, null, HoverBoxColor * 0.5f, 0f, Vector2.Zero, default, 0f);

        // ensure model draws over
        sb.End();
        
        // hacky ahh...
        TankGame.Instance.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);
        TankGame.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        TankGame.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        // write the floor's depth before anything is drawn, so the part of the stack below the floor is hidden
        if (_stackFloor is { Destroyed: false, Alpha: > 0f })
            DrawFloorDepth(_stackFloor);

        RenderEditorParticles(sb);

        sb.Begin();
        // draw obstacle placement info
        // this is after RenderEditorParticles because the text needs to draw over the model
        // i love doing hacky shit cuz idk a better way (and am too lazy to find a better way)
        if (CurCategory == EditorCategory.Terrain) {
            helpText = TankGame.GameLanguage.LevelEdit.BlockStackFlavor;
            // TODO: add static dict for specific types?
            PlaceInfoStart = new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 350.ToResolutionY());

            // TODO: reduce the hardcode for modders, yeah
            if (_latestStackable) {
                // var stackPos = new Vector2(PlaceInfoStart.X + 100.ToResolutionX(), PlaceInfoStart.Y - 75.ToResolutionY());
                var stackPos = PlaceInfoStart + new Vector2(25, -10).ToResolution();
                var blst = BlockStack.ToString();
                var meas = FontGlobals.RebirthFontLarge.MeasureString(blst) / 2;
                DrawUtils.DrawStringWithBorderAndShadow(sb, FontGlobals.RebirthFontLarge, stackPos, Vector2.One, BlockStack.ToString(),
                    Color.White, Color.Black, Vector2.One, 1f, shadowAlpha: 0.5f);

            }
        }

        sb.End();

        sb.Begin();
    }

    static void UpdatePlacementInfo(Particle preview) {
        if (_previewType == SelectedBlockType && _previewStack == BlockStack && _previewTheme == GameScene.Theme)
            return;
        _previewType = SelectedBlockType;
        _previewStack = BlockStack;
        _previewTheme = GameScene.Theme;

        // a dummy block to scrape da assets
        var block = new Block(SelectedBlockType, BlockStack, new Vector2(float.MaxValue), true);
        _latestStackable = block.Properties.CanStack;
        preview.Texture = block.Texture;
        preview.Model = block.Model;
        preview.Texturing = block.TextureMap;
        block.Remove();
    }
    // draws a _stackFloor particle to the depth buffer to give the illusion of the block changing stack, just like ingame
    static void DrawFloorDepth(Particle floor) {
        var device = TankGame.Instance.GraphicsDevice;
        var blend = device.BlendState;
        var scale = floor.Scale;
        device.BlendState = _depthOnly;

        // wider than the visible floor so the stack can't show past its edges
        floor.Scale = scale * 1.5f;
        floor.DrawModel();
        floor.Scale = scale;
        device.BlendState = blend;
    }
    public static void DrawLevelInfo(SpriteBatch sb) {
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 125).ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(0, 0, 350, 40).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);

        sb.DrawString(FontGlobals.RebirthFont, TankGame.GameLanguage.LevelEdit.LevelInfo, new Vector2(175, 3).ToResolution(), Color.Black, Vector2.One.ToResolution(), 0f, Anchor.TopCenter.GetAnchor(FontGlobals.RebirthFont.MeasureString(TankGame.GameLanguage.LevelEdit.LevelInfo)));
        UpdateLevelInfoText();
        sb.DrawString(FontGlobals.RebirthFont, _enemyCountText, new Vector2(10, 40).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        // localize later.
        sb.DrawString(FontGlobals.RebirthFont, _terrainCountText, new Vector2(10, 60).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        sb.DrawString(FontGlobals.RebirthFont, _ratingText, new Vector2(10, 80).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
    }

    static string _enemyCountText, _terrainCountText, _ratingText;
    static int _shownEnemyCount = -1, _shownTerrainCount = -1;
    static float _shownRating = float.NaN;
    static object _levelInfoLanguage;

    static void UpdateLevelInfoText() {
        var language = TankGame.GameLanguage;
        bool languageChanged = !ReferenceEquals(_levelInfoLanguage, language);
        _levelInfoLanguage = language;

        var enemies = AIManager.CountAll();
        if (languageChanged || enemies != _shownEnemyCount) {
            _shownEnemyCount = enemies;
            _enemyCountText = $"{language.LevelEdit.EnemyTankTotal}: {enemies}";
        }

        var blocks = Block.AllBlocks;
        int terrain = 0;
        for (int i = 0; i < blocks.Length; i++)
            if (blocks[i] is not null)
                terrain++;
        if (terrain != _shownTerrainCount) {
            _shownTerrainCount = terrain;
            _terrainCountText = $"Total Terrain: {terrain}";
        }

        if (languageChanged || difficultyRating != _shownRating) {
            _shownRating = difficultyRating;
            _ratingText = $"{language.LevelEdit.DifficultyRating}: {difficultyRating:0.00}";
        }
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
        _alertTime -= RuntimeData.DeltaFrames;
    }    

    public static void UpdateParticles() {
        EditorParticleSystem.UpdateParticles();
    }
    public static void RenderEditorParticles(SpriteBatch spriteBatch) {
        // EditorParticleSystem.RenderParticles(spriteBatch);
        EditorParticleSystem.RenderModelParticles();
    }
}
