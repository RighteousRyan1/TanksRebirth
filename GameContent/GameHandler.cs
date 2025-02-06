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

        CosmeticsUI.Initialize();
    }

    internal static void UpdateAll(GameTime gameTime) {
        void doTestWithFont() {
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
            doTestWithFont();
        // ChatSystem.CurTyping = SoundPlayer.GetLengthOfSound("Content/Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg").ToString();
        if (MainMenu.Active)
        CosmeticsUI.Update();
        if (DebugManager.DebuggingEnabled) {
            if (/*InputUtils.KeyJustPressed(Keys.H)*/ DebugManager.DebugLevel == -2 && CampaignGlobals.InMission) {
                if (TankGame.RunTime % 300 <= TankGame.DeltaTime) {
                    if (Server.ServerRandom.Next(2) == 0) {
                        var pos = Airplane.ChooseRandomXZPosition(Server.ServerRandom);
                        var vel = Airplane.ChooseRandomFlightTarget(Server.ServerRandom, pos, 0.5f, 0.5f);
                        var plane = new Airplane(new Vector3(pos.X, 100, pos.Y), vel, 400f);
                        plane.WhileTrapDoorsOpened = () => {
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
        if (MainMenu.Active)
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
        LevelEditor.Update();

        foreach (var expl in Explosion.Explosions)
            expl?.Update();

        if (CampaignGlobals.ShouldMissionsProgress && !MainMenu.Active)
            IntermissionHandler.HandleMissionChanging();

        foreach (var cube in Block.AllBlocks)
            cube?.OnUpdate();

        if ((DebugManager.DebuggingEnabled && DebugManager.DebugLevel == DebugManager.Id.LevelEditDebug && TankGame.OverheadView) || LevelEditor.Active)
            foreach (var sq in PlacementSquare.Placements)
                sq?.Update();

        Particles.UpdateParticles();

        if (MainMenu.Active)
            MainMenu.Update();

        if ((TankGame.OverheadView || MainMenu.Active) && !LevelEditor.Active) {
            CampaignGlobals.InMission = false;
            IntermissionHandler.TankFunctionWait = 600;
        }

        SceneManager.HandleSceneVisuals();

        IntermissionHandler.Update();

        if (TankGame.OverheadView)
            LevelEditor.HandleLevelEditorModifications();

        OnPostUpdate?.Invoke();
    }

    internal static void RenderAll() {
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        if (!MainMenu.Active && !LevelEditor.Editing)
            ExperienceBar?.Render(TankGame.SpriteRenderer, new(WindowUtils.WindowWidth / 2, 50.ToResolutionY()), new Vector2(100, 20).ToResolution(), Anchor.Center, Color.Red, Color.Lime);
        // CHECK: move this back if necessary
        GameSceneRenderer.RenderWorldModels();

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

        if ((DebugManager.DebugLevel == DebugManager.Id.LevelEditDebug && TankGame.OverheadView) || LevelEditor.Active)
            foreach (var sq in PlacementSquare.Placements)
                sq?.Render();
        if (DebugManager.DebuggingEnabled) {

            DebugManager.DrawDebugString(TankGame.SpriteRenderer, "Spawn Tank With Info:", WindowUtils.WindowTop + new Vector2(0, 8), 3, centered: true);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"Tier: {TankID.Collection.GetKey(DebugManager.tankToSpawnType)}", WindowUtils.WindowTop + new Vector2(0, 24), 3, centered: true);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"Team: {TeamID.Collection.GetKey(DebugManager.tankToSpawnTeam)}", WindowUtils.WindowTop + new Vector2(0, 40), 3, centered: true);
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
            && !MainMenu.Active
            && Client.IsConnected()) {
            PingMenu.DrawPingHUD();
        }

        IntermissionHandler.RenderCountdownGraphics();

        if (!MainMenu.Active && !LevelEditor.Active) {
            if (IntermissionSystem.IsAwaitingNewMission) {
                // uhhh... what was i doing here?
            }
            // this draws the amount of kills a player has.
            for (int i = -4; i < 10; i++) {
                DrawUtils.DrawShadowedTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard"),
                    new Vector2((i * 14).ToResolutionX(), WindowUtils.WindowHeight * 0.9f),
                    Vector2.UnitY,
                    Color.White,
                    new Vector2(2f).ToResolution(),
                    1f,
                    new(0, GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard").Size().Y / 2),
                    true);
            }
            DrawUtils.DrawShadowedString(TankGame.TextFontLarge, new Vector2(80.ToResolutionX(), WindowUtils.WindowHeight * 0.9f - 14f.ToResolutionY()), Vector2.One, $"{PlayerTank.KillCount}", new(119, 190, 238), new Vector2(0.675f).ToResolution(), 1f);
        }
        // TODO: put this code elsewhere... idk where rn.
        var shouldSeeInfo = !MainMenu.Active && !LevelEditor.Active && !CampaignCompleteUI.IsViewingResults;
        if (shouldSeeInfo) {
            var font = TankGame.TextFontLarge;
            var infoScale = 0.5f;
            var alpha = 1f;
            var bar = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/mission_info");
            var tnk = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank2d");
            var barPos = new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight - (bar.Height + 35).ToResolutionY());
            var missionInfo = $"{CampaignGlobals.LoadedCampaign.CurrentMission.Name ?? $"{TankGame.GameLanguage.Mission}"}";
            var infoMeasure = font.MeasureString(missionInfo) * infoScale;
            var infoScaling = 1f - ((float)missionInfo.Length / LevelEditor.MAX_MISSION_CHARS) + 0.4f;
            var tanksRemaining = $"x {AIManager.CountAll()}";
            var tanksRemMeasure = font.MeasureString(tanksRemaining) * infoScale;

            DrawUtils.DrawShadowedTexture(bar, barPos,
                Vector2.UnitY, IntermissionSystem.StripColor * 1.5f, Vector2.One.ToResolution(), alpha, bar.Size() / 2, shadowDistScale: 0.5f);

            DrawUtils.DrawShadowedTexture(tnk, barPos + new Vector2(bar.Size().X * 0.25f, 0).ToResolution(),
                Vector2.One, IntermissionSystem.BackgroundColor, new Vector2(1.5f).ToResolution(), alpha, tnk.Size() / 2, shadowDistScale: 0.5f);

            DrawUtils.DrawShadowedString(font, barPos - new Vector2(bar.Size().X / 6, 7.5f * infoScaling).ToResolution(),
                Vector2.One, missionInfo, IntermissionSystem.BackgroundColor, new Vector2(infoScale * infoScaling).ToResolution(),
                1f, infoMeasure, shadowDistScale: 1.5f);

            DrawUtils.DrawShadowedString(font, barPos + new Vector2(bar.Size().X * 0.375f, -7.5f).ToResolution(),
                Vector2.One, tanksRemaining, IntermissionSystem.BackgroundColor, new Vector2(infoScale).ToResolution(),
                alpha, tanksRemMeasure, shadowDistScale: 1.5f);
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
    }
    public static void SetupGraphics() {
        GameShaders.Initialize();
        GameSceneRenderer.InitializeRenderers();
        SceneManager.LoadGameScene();
        DebugManager.InitDebugUI();
        PlacementSquare.InitializeLevelEditorSquares();
    }
    #endregion
}