using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using System;
using TanksRebirth.GameContent.Systems;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Net;
using TanksRebirth.Achievements;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Graphics.Metrics;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.TankSystem;

namespace TanksRebirth.GameContent;

#pragma warning disable CS8618
public class GameHandler {

    public const int MAX_AI_TANKS = 50;
    public const int MAX_PLAYERS = 4;
    public const int MAX_PARTICLES = 15000;

    public delegate void PostRender();
    public static event PostRender? OnPostRender;
    public delegate void PostUpdate();
    public static event PostUpdate? OnPostUpdate;

    public static ParticleManager Particles { get; } = new(MAX_PARTICLES, () => CameraGlobals.GameView, () => CameraGlobals.GameProjection);
    public static XpBar ExperienceBar;

    public static AITank[] AllAITanks = new AITank?[MAX_AI_TANKS];
    public static PlayerTank[] AllPlayerTanks = new PlayerTank?[MAX_PLAYERS];
    public static Tank[] AllTanks = new Tank?[MAX_PLAYERS + MAX_AI_TANKS];

    internal static void MapEvents() {
        CampaignGlobals.OnMissionEnd += IntermissionHandler.DoEndMissionWorkload;
    }

    internal static void Initialize() {
        Client.ClientRandSeed = DateTime.Now.Millisecond;
        Client.ClientRandom = new(Client.ClientRandSeed);

        AllAITanks = new AITank[MAX_AI_TANKS];
        AllPlayerTanks = new PlayerTank[MAX_PLAYERS];
        AllTanks = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        ExperienceBar = new() {
            Level = (ushort)TankGame.SaveFile.ExpLevel
        };
        ExperienceBar.Value = TankGame.SaveFile.ExpLevel - ExperienceBar.Level;
        ExperienceBar.ApproachValue = ExperienceBar.Value;

        ExperienceBar.Alignment = Anchor.LeftCenter;
        ExperienceBar.FillColor = Color.Lime;
        ExperienceBar.EmptyColor = Color.Red;

        GameSceneUI.Initialize();
        CosmeticsUI.Initialize();
        RebirthMouse.Initialize();
    }
    internal static void UpdateAll(GameTime gameTime) {
        /*void doTestWithFont() {
            var str = 
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz" +
                "1234567890" +
                "`[]\\;',./~{}|:\"<>?";
            int longestId = -1;
            var longest = 0f;
            for (int i = 0; i < str.Length; i++) {
                var x = FontGlobals.RebirthFontLarge.MeasureString(str[i].ToString()).X;
                if (x > longest) {
                    longestId = i;
                    longest = x;
                }
            }
            Console.WriteLine($"{str[longestId]} is the longest. ({longest})");
        }
        if (InputUtils.KeyJustPressed(Keys.OemTilde))
            doTestWithFont();*/
        // ChatSystem.CurTyping = SoundPlayer.GetLengthOfSound("Content/Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg").ToString();

        ExperienceBar.Update();
        CosmeticsUI.Update();
        RoomScene.Update();

        Difficulties.GlobalManage();

        if (MainMenuUI.IsActive)
            PlayerTank.SetLives(999);
        // technically, level 0 in code is level 1, so we want to use that number (1) if the user is level 0.
        // ExperienceBar.Value = TankGame.GameData.ExpLevel - MathF.Floor(TankGame.GameData.ExpLevel);

        VanillaAchievements.Repository.UpdateCompletions(TankGame.VanillaAchievementPopupHandler);

        Client.SendLives();
        Client.SendKillCounts();

        /* uh, yeah. this is the decay-per-level calculation. people don't want it!
        var floor1 = MathF.Floor(TankGame.GameData.ExpLevel + 1f);
        var floor0 = MathF.Floor(TankGame.GameData.ExpLevel);
        GameData.UniversalExpMultiplier = floor1 - (GameData.DecayPerLevel * floor0);*/

        if (InputUtils.KeyJustPressed(Keys.I)) {
            new Shell(new Vector2(0, 100), -Vector2.UnitY * 2, ShellID.Standard, null);
            new Shell(new Vector2(MouseUtils.Test.X * 10, -100), Vector2.UnitY * 2, ShellID.Standard, null);
        }

        if (Difficulties.Types["InfiniteLives"])
            PlayerTank.SetLives(PlayerTank.StartingLives);

        for (int i = 0; i < Animator.Animators.Count; i++)
            Animator.Animators[i].PlayAnimation(gameTime);

        foreach (var ping in IngamePing.AllIngamePings)
            ping?.Update();

        if (!IntermissionSystem.IsAwaitingNewMission) {
            foreach (var pTank in AllPlayerTanks)
                pTank?.Update();

            AIManager.ProcessAITanks();

            foreach (var mine in Mine.AllMines)
                mine?.Update();

            foreach (var bullet in Shell.AllShells)
                bullet?.Update();

            foreach (var fp in TankFootprint.AllFootprints)
                fp?.Update();

            foreach (var p in Airplane.AllPlanes)
                p?.Update();
        }
        if (CampaignGlobals.InMission) {
            TankMusicSystem.Update();

            foreach (var crate in Crate.crates)
                crate?.Update();

            foreach (var pu in Powerup.Powerups)
                pu?.Update();
        }
        else {
            foreach (var audio in TankMusicSystem.Audio.ToList()) {
                audio.Value.Volume = 0;
                audio.Value.Stop();
            }
        }

        LevelEditorUI.Update();

        foreach (var expl in Explosion.Explosions)
            expl?.Update();

        if (CampaignGlobals.ShouldMissionsProgress && !MainMenuUI.IsActive)
            IntermissionHandler.HandleMissionChanging();

        foreach (var cube in Block.AllBlocks)
            cube?.OnUpdate();

        if ((DebugManager.DebuggingEnabled && DebugManager.DebugLevel == DebugManager.Id.LevelEditDebug && CameraGlobals.OverheadView) || LevelEditorUI.IsActive)
            foreach (var sq in PlacementSquare.Placements)
                sq?.Update();

        Particles.UpdateParticles();

        var mmActive = MainMenuUI.IsActive;
        if (mmActive)
            MainMenuUI.Update();
        // I AM PUTTING THIS HERE BECAUSE FUCK YOU.
        MainMenuUI.UpdateCampaignButton.IsVisible = MainMenuUI.MenuState == MainMenuUI.UIState.Campaigns && mmActive;

        // questioning as to why this is placed here
        if ((CameraGlobals.OverheadView || MainMenuUI.IsActive) && !LevelEditorUI.IsActive) {
            CampaignGlobals.InMission = false;
            IntermissionHandler.TankFunctionWait = 600;
        }

        SceneManager.HandleSceneVisuals();

        IntermissionHandler.Update();

        if (CameraGlobals.OverheadView)
            LevelEditorUI.HandleLevelEditorModifications();

        OnPostUpdate?.Invoke();
    }

    internal static void RenderAll() {
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        if (!MainMenuUI.IsActive && !LevelEditorUI.IsEditing) {
            ExperienceBar.Position = new(WindowUtils.WindowWidth / 2 - ExperienceBar.Scale.X / 2, 50);
            ExperienceBar.Scale = new(600, 20);
            ExperienceBar.Alignment = Anchor.LeftCenter;
            ExperienceBar.FillColor = Color.Green;
            ExperienceBar.EmptyColor = Color.Red;
            ExperienceBar.GainedColor = Color.Lime;
            ExperienceBar.Render(TankGame.SpriteRenderer);
        }
        
        // CHECK: move this back if necessary
        GameScene.RenderWorldModels();
        // TankFootprint.Draw();

        foreach (var tank in AllTanks)
            tank?.Render();

        foreach (var cube in Block.AllBlocks)
            cube?.OnRender();

        foreach (var mine in Mine.AllMines)
            mine?.Render();

        foreach (var bullet in Shell.AllShells)
            bullet?.Render();

        foreach (var mark in TankDeathMark.deathMarks)
            mark?.Render();

        foreach (var ping in IngamePing.AllIngamePings)
            ping?.Render();

        foreach (var p in Airplane.AllPlanes)
            p?.Render();

        //foreach (var print in TankFootprint.footprints)
        //print?.Render();

        foreach (var crate in Crate.crates)
            crate?.Render();

        foreach (var powerup in Powerup.Powerups)
            powerup?.Render();

        Particles.RenderModelParticles();

        if ((DebugManager.DebugLevel == DebugManager.Id.LevelEditDebug && CameraGlobals.OverheadView) || LevelEditorUI.IsActive)
            foreach (var sq in PlacementSquare.Placements)
                sq?.Render();

        TankGame.Instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        Particles.RenderParticles();

        // only render the level editor if it's active
        // change depth stencil...?
        if (LevelEditorUI.IsActive) {
            LevelEditorUI.Render();
        }

        // only draw if ingame + is multiplayer.
        if (CampaignGlobals.ShouldMissionsProgress 
            && !MainMenuUI.IsActive
            && Client.IsConnected()) {
            PingMenu.DrawPingHUD();
        }

        IntermissionHandler.RenderCountdownGraphics();

        OnPostRender?.Invoke();
    }

    public static void RenderUI() {
        foreach (var element in UIElement.AllUIElements.ToList()) {
            // element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));
            if (element.Parent != null)
                continue;

            if (element.HasScissor)
                TankGame.SpriteRenderer.End();

            element?.Draw(TankGame.SpriteRenderer);

            if (element!.HasScissor)
                TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: RenderGlobals.DefaultRasterizer);
        }
        foreach (var element in UIElement.AllUIElements)
            element?.DrawTooltips(TankGame.SpriteRenderer);

        CameraGlobals.SetMatrices();
    }
    public static void SetupGraphics() {
        GameShaders.Initialize();
        GameScene.InitializeRenderers();
        SceneManager.LoadGameScene();
        DebugManager.InitDebugUI();
        PlacementSquare.InitializeLevelEditorSquares();
    }
}