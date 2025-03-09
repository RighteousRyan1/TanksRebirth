using TanksRebirth.Internals;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Common.GameUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using TanksRebirth.Enums;
using System;
using TanksRebirth.GameContent.Systems;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Framework.Audio;
using System.IO;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Net;
using TanksRebirth.Internals.Common.Framework;
using NativeFileDialogSharp;
using TanksRebirth.Achievements;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Speedrunning;
using FontStashSharp;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.Internals.Common.Framework.Animation;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.GameContent.RebirthUtils;
using System.Text;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Graphics.Metrics;

namespace TanksRebirth.GameContent;

public class GameHandler {
#pragma warning disable CS8618

    #region Non-test

    public const int MAX_AI_TANKS = 50;
    public const int MAX_PLAYERS = 4;
    public const int MAX_PARTICLES = 15000;

    public delegate void PostRender();
    public static event PostRender? OnPostRender;
    public delegate void PostUpdate();
    public static event PostUpdate? OnPostUpdate;

    private static int _randSeed;
    public static int GameRandSeed {
        get => _randSeed;
        set {
            _randSeed = value;
            GameRand = new(value);
        }

    }
    /// <summary>The randomizing behind the game's events. The seed can be modified if you change <see cref="GameRandSeed"/>.</summary>
    public static Random GameRand { get; private set; }
    public static ParticleSystem Particles { get; private set; }
    public static XpBar ExperienceBar;
    public static AITank[] AllAITanks = new AITank[MAX_AI_TANKS];
    public static PlayerTank[] AllPlayerTanks = new PlayerTank[MAX_PLAYERS];
    public static Tank[] AllTanks = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

    internal static void MapEvents() {
        CampaignGlobals.OnMissionEnd += IntermissionHandler.DoEndMissionWorkload;
    }

    internal static void Initialize() {
        GameRandSeed = DateTime.Now.Millisecond;
        GameRand = new(GameRandSeed);

        AllAITanks = new AITank[MAX_AI_TANKS];
        AllPlayerTanks = new PlayerTank[MAX_PLAYERS];
        AllTanks = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        ExperienceBar = new();
        Particles = new(MAX_PARTICLES);

        GameSceneUI.Initialize();
        CosmeticsUI.Initialize();
    }
    public static Graph RenderFpsGraph = new("Render", () => (float)TankGame.RenderFPS, 200, 50, 3, 0.35f);
    public static Graph LogicFpsGraph = new("Logic", () => (float)TankGame.LogicFPS, 200, 50, 3, 0.35f);

    public static Graph RenderTimeGraph = new("Render Time", () => TankGame.RenderTime.Milliseconds, 50, 50, 3, 0.35f);
    public static Graph LogicTimeGraph = new("Logic Time", () => TankGame.LogicTime.Milliseconds, 50, 50, 3, 0.35f);
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
                var x = TankGame.TextFontLarge.MeasureString(str[i].ToString()).X;
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
        CosmeticsUI.Update();
        RoomScene.Update();
        if (DebugManager.DebuggingEnabled) {
            if (/*InputUtils.KeyJustPressed(Keys.H)*/ DebugManager.DebugLevel == -2 && CampaignGlobals.InMission) {
                if (TankGame.RunTime % 300 <= TankGame.DeltaTime) {
                    if (Server.ServerRandom.Next(2) == 0) {
                        var pos = Airplane.ChooseRandomXZPosition(Server.ServerRandom);
                        var vel = Airplane.ChooseRandomFlightTarget(Server.ServerRandom, pos, 0.5f, 0.5f);
                        var plane = new Airplane(new Vector3(pos.X, 100, pos.Y), vel, 400f);
                        plane.WhileTrapDoorsOpened = () => {
                            /*if (TankGame.RunTime % 10 <= TankGame.DeltaTime) {
                                var t = new AITank(TankMusicSystem.TierHighest);
                                t.Body.Position = plane.Position.FlattenZ() / Tank.UNITS_PER_METER;
                            }*/
                                //new Mine(null, plane.Position.FlattenZ(), 180);
                                //new Explosion(plane.Position.FlattenZ(), 10f);
                            
                            if (TankGame.RunTime % 30 <= TankGame.DeltaTime) {
                                ParticleGameplay.CreateSmokeGrenade(Particles, plane.Position, Vector3.Down + new Vector3(plane.Velocity.X, 0, plane.Velocity.Y) * 0.5f/* * GameRand.NextFloat(0.5f, 1.1f)*/);
                            }
                        };
                    }
                }
            }
            if (InputUtils.AreKeysJustPressed(Keys.Q, Keys.E))
                Server.SyncSeeds();
            if (InputUtils.KeyJustPressed(Keys.M))
                if (PlayerTank.ClientTank is not null)
                    ParticleGameplay.CreateSmokeGrenade(Particles, PlayerTank.ClientTank.Position3D + new Vector3(0, 10, 0), Vector3.Up);
            if (InputUtils.KeyJustPressed(Keys.G)) {
                TankGame.VanillaAchievementPopupHandler.SummonOrQueue(GameRand.Next(VanillaAchievements.Repository.GetAchievements().Count));
            }
            if (InputUtils.KeyJustPressed(Keys.OemPipe)) {
                var expl = new Explosion(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition).FlattenZ(), 5f);
            }
        }
        if (MainMenuUI.Active)
            PlayerTank.SetLives(999);
        // technically, level 0 in code is level 1, so we want to use that number (1) if the user is level 0.
        ExperienceBar.Value = TankGame.GameData.ExpLevel - MathF.Floor(TankGame.GameData.ExpLevel);

        VanillaAchievements.Repository.UpdateCompletions(TankGame.VanillaAchievementPopupHandler);

        Client.SendLives();
        /* uh, yeah. this is the decay-per-level calculation. people don't want it!
        var floor1 = MathF.Floor(TankGame.GameData.ExpLevel + 1f);
        var floor0 = MathF.Floor(TankGame.GameData.ExpLevel);
        GameData.UniversalExpMultiplier = floor1 - (GameData.DecayPerLevel * floor0);*/

        if (Difficulties.Types["InfiniteLives"])
            PlayerTank.SetLives(PlayerTank.StartingLives);

        for (int i = 0; i < Animator.Animators.Count; i++)
            Animator.Animators[i].PlayAnimation(gameTime);

        foreach (var ping in IngamePing.AllIngamePings)
            ping?.Update();

        if (!IntermissionSystem.IsAwaitingNewMission) {
            foreach (var tank in AllTanks)
                tank?.Update();

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
        else if (!CampaignGlobals.InMission)
            if (TankMusicSystem.Audio is not null)
                foreach (var song in TankMusicSystem.Audio.ToList())
                    song.Value.Volume = 0;
        LevelEditorUI.Update();

        foreach (var expl in Explosion.Explosions)
            expl?.Update();

        if (CampaignGlobals.ShouldMissionsProgress && !MainMenuUI.Active)
            IntermissionHandler.HandleMissionChanging();

        foreach (var cube in Block.AllBlocks)
            cube?.OnUpdate();

        if ((DebugManager.DebuggingEnabled && DebugManager.DebugLevel == DebugManager.Id.LevelEditDebug && TankGame.OverheadView) || LevelEditorUI.Active)
            foreach (var sq in PlacementSquare.Placements)
                sq?.Update();

        Particles.UpdateParticles();

        if (MainMenuUI.Active)
            MainMenuUI.Update();

        if ((TankGame.OverheadView || MainMenuUI.Active) && !LevelEditorUI.Active) {
            CampaignGlobals.InMission = false;
            IntermissionHandler.TankFunctionWait = 600;
        }

        SceneManager.HandleSceneVisuals();

        IntermissionHandler.Update();

        if (TankGame.OverheadView)
            LevelEditorUI.HandleLevelEditorModifications();

        OnPostUpdate?.Invoke();
    }

    internal static void RenderAll() {
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        if (!MainMenuUI.Active && !LevelEditorUI.Editing)
            ExperienceBar?.Render(TankGame.SpriteRenderer, new(WindowUtils.WindowWidth / 2, 50.ToResolutionY()), new Vector2(100, 20).ToResolution(), Anchor.Center, Color.Red, Color.Lime);
        // CHECK: move this back if necessary
        GameScene.RenderWorldModels();

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

        if ((DebugManager.DebugLevel == DebugManager.Id.LevelEditDebug && TankGame.OverheadView) || LevelEditorUI.Active)
            foreach (var sq in PlacementSquare.Placements)
                sq?.Render();
        if (DebugManager.DebuggingEnabled) {

            var posOffset = new Vector2(0, 80);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, "Spawn Tank With Info:", WindowUtils.WindowTop + posOffset, 3, centered: true);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"Tier: {TankID.Collection.GetKey(DebugManager.tankToSpawnType)}", WindowUtils.WindowTop + posOffset + new Vector2(0, 16), 3, centered: true);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"Team: {TeamID.Collection.GetKey(DebugManager.tankToSpawnTeam)}", WindowUtils.WindowTop + posOffset + new Vector2(0, 32), 3, centered: true);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"BlockStack: {DebugManager.blockHeight} | BlockType: {BlockID.Collection.GetKey(DebugManager.blockType)}", WindowUtils.WindowBottom - new Vector2(0, 20), 3, centered: true);

            DebugManager.tankToSpawnType = MathHelper.Clamp(DebugManager.tankToSpawnType, 2, TankID.Collection.Count - 1);
            DebugManager.tankToSpawnTeam = MathHelper.Clamp(DebugManager.tankToSpawnTeam, 0, TeamID.Collection.Count - 1);


            if (DebugManager.DebuggingEnabled) {
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"Logic Time: {TankGame.LogicTime.TotalMilliseconds:0.00}ms" +
                                                           $"\nLogic FPS: {TankGame.LogicFPS}" +
                                                           $"\n\nRender Time: {TankGame.RenderTime.TotalMilliseconds:0.00}ms" +
                                                           $"\nRender FPS: {TankGame.RenderFPS}" +
                                                           $"\nKeys Q + W: Localhost Connect for Multiplayer Debug" +
                                                           $"\nKeys U + I: Unload All Mods" +
                                                           $"\nKeys O + P: Reload All Mods" +
                                                           $"\nKeys Q + E: Resynchronize Randoms", new(10, 500));

                DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"Current Mission: {CampaignGlobals.LoadedCampaign.CurrentMission.Name}\nCurrent Campaign: {CampaignGlobals.LoadedCampaign.MetaData.Name}", WindowUtils.WindowBottomLeft - new Vector2(-4, 60), 3, centered: false);
            }

            DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"HighestTier: {AIManager.GetHighestTierActive()}", new(10, WindowUtils.WindowHeight * 0.26f), 1);
            // DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f).Name : "N/A")}", new(10, WindowUtils.WindowHeight - 100), 1);

            for (int i = 0; i < TankID.Collection.Count; i++)
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"{TankID.Collection.GetKey(i)}: {AIManager.GetTankCountOfType(i)}", new(10, WindowUtils.WindowHeight * 0.3f + (i * 20)), 1);
        }

        TankGame.Instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        Particles.RenderParticles();

        // only draw if ingame + is multiplayer.
        if (CampaignGlobals.ShouldMissionsProgress 
            && !MainMenuUI.Active
            && Client.IsConnected()) {
            PingMenu.DrawPingHUD();
        }

        IntermissionHandler.RenderCountdownGraphics();

        var shouldSeeInfo = !MainMenuUI.Active && !LevelEditorUI.Active && !CampaignCompleteUI.IsViewingResults;
        if (shouldSeeInfo) {
            GameSceneUI.DrawScores();
            GameSceneUI.DrawMissionInfoBar();
        }
        // TODO: MissionInfoBar can be much better.

        OnPostRender?.Invoke();
    }

    #endregion

    #region Extra
    public static void RenderUI() {
        foreach (var element in UIElement.AllUIElements) {
            // element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));
            if (element.Parent != null)
                continue;

            if (element.HasScissor)
                TankGame.SpriteRenderer.End();

            element?.Draw(TankGame.SpriteRenderer);

            if (element.HasScissor)
                TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: TankGame.DefaultRasterizer);
        }
        foreach (var element in UIElement.AllUIElements)
            element?.DrawTooltips(TankGame.SpriteRenderer);

        if (DebugManager.DebugLevel == DebugManager.Id.SceneMetrics) {
            if (TankGame.RunTime % 10f <= TankGame.DeltaTime) {
                RenderFpsGraph.Update();
                LogicFpsGraph.Update();

                RenderTimeGraph.Update();
                LogicTimeGraph.Update();
            }
            RenderFpsGraph.Draw(TankGame.SpriteRenderer, new Vector2(100, 200), 2);
            LogicFpsGraph.Draw(TankGame.SpriteRenderer, new Vector2(100, 400), 2);
            RenderTimeGraph.Draw(TankGame.SpriteRenderer, new Vector2(500, 200), 2);
            LogicTimeGraph.Draw(TankGame.SpriteRenderer, new Vector2(500, 400), 2);
        }
    }
    public static void SetupGraphics() {
        GameShaders.Initialize();
        GameScene.InitializeRenderers();
        SceneManager.LoadGameScene();
        DebugManager.InitDebugUI();
        PlacementSquare.InitializeLevelEditorSquares();
    }
    #endregion
}