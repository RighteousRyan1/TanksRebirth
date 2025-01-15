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
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Speedrunning;
using FontStashSharp;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.Internals.Common.Framework.Animation;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.GameContent.RebirthUtils;
using System.Text;

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

    public static XpBar ExperienceBar;
    public static ParticleSystem Particles { get; private set; }

    // TODO: convert to lists.
    public static AITank[] AllAITanks = new AITank[MAX_AI_TANKS];
    public static PlayerTank[] AllPlayerTanks = new PlayerTank[MAX_PLAYERS];
    public static Tank[] AllTanks = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

    internal static void MapEvents() {
        GameProperties.OnMissionEnd += IntermissionHandler.DoEndMissionWorkload;
    }

    internal static void Initialize() {
        GameRandSeed = DateTime.Now.Millisecond;
        GameRand = new(GameRandSeed);

        AllAITanks = new AITank[MAX_AI_TANKS];
        AllPlayerTanks = new PlayerTank[MAX_PLAYERS];
        AllTanks = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        ExperienceBar = new();
        Particles = new(MAX_PARTICLES);
    }
    // todo: balls
    private static void SmokeNadeDebug() {

        var pl = AllPlayerTanks.FirstOrDefault(x => x is not null);

        if (pl is null)
            return;
        var p = Particles.MakeParticle(pl.Position3D + new Vector3(0, 10, 0), GameResources.GetGameResource<Model>("Assets/smokenade"), GameResources.GetGameResource<Texture2D>("Assets/textures/smoke/smokenade"));
        bool exploded = false;

        float gravity = 0.01f;
        Vector3 initialVelocity = new(0, 1, 0);
        Vector3 velocity = initialVelocity;
        int hits = 5;

        p.UniqueBehavior = (a) => {
            p.Scale = new(125);
            p.IsIn2DSpace = false;

            p.Position += velocity;
            velocity.Y -= gravity;

            if (p.Position.Y < 10 && hits > 0) {
                hits--;
                velocity.Y = initialVelocity.Y * hits / 5;
            }
            else if (hits <= 0)
                p.Position.Y = 10;

            if (p.LifeTime >= 180 && !exploded) {
                exploded = true;
                SoundPlayer.PlaySoundInstance("Assets/sounds/smoke_hiss.ogg", SoundContext.Effect, 0.3f, gameplaySound: true);
                for (int i = 0; i < 8; i++) {
                    var c = Particles.MakeParticle(p.Position,
                        GameResources.GetGameResource<Model>("Assets/smoke"),
                        GameResources.GetGameResource<Texture2D>("Assets/textures/smoke/smoke"));
                    var randDir = new Vector3(GameRand.NextFloat(-60, 60), 0, GameRand.NextFloat(-60, 60));
                    c.Position += randDir;
                    var randSize = GameRand.NextFloat(5, 10);
                    c.Scale.X = randSize;
                    c.Scale.Z = randSize;
                    c.UniqueBehavior = (b) => {
                        c.Pitch += 0.005f * TankGame.DeltaTime;
                        if (c.Scale.Y < randSize && c.LifeTime < 600)
                            c.Scale.Y += 0.1f * TankGame.DeltaTime;
                        if (c.LifeTime >= 600) {
                            c.Scale.Y -= 0.06f * TankGame.DeltaTime;

                            if (c.Scale.Y <= 0) {
                                c.Destroy();
                            }
                        }
                    };
                }
                p.Destroy();
            }
        };
    }

    internal static void UpdateAll(GameTime gameTime) {
        if (InputUtils.AreKeysJustPressed(Keys.J, Keys.K))
            Server.SyncSeeds();
        //if (InputUtils.KeyJustPressed(Keys.M))
        //SmokeNadeDebug();
        // ChatSystem.CurTyping = SoundPlayer.GetLengthOfSound("Content/Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg").ToString();
        if (DebugManager.DebuggingEnabled) {
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

            foreach (var fp in TankFootprint.footprints)
                fp?.Update();
        }
        if (GameProperties.InMission) {
            TankMusicSystem.Update();

            foreach (var crate in Crate.crates)
                crate?.Update();

            foreach (var pu in Powerup.Powerups)
                pu?.Update();
        }
        else if (!GameProperties.InMission)
            if (TankMusicSystem.Audio is not null)
                foreach (var song in TankMusicSystem.Audio.ToList())
                    song.Value.Volume = 0;
        LevelEditor.Update();

        foreach (var expl in Explosion.Explosions)
            expl?.Update();

        if (GameProperties.ShouldMissionsProgress && !MainMenu.Active)
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
            GameProperties.InMission = false;
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
        MapRenderer.RenderWorldModels();

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

        //foreach (var print in TankFootprint.footprints)
        //print?.Render();

        foreach (var crate in Crate.crates)
            crate?.Render();

        foreach (var powerup in Powerup.Powerups)
            powerup?.Render();

        Particles.RenderModelParticles();

        if ((DebugManager.DebuggingEnabled && DebugManager.DebugLevel == DebugManager.Id.LevelEditDebug && TankGame.OverheadView) || LevelEditor.Active) {
            foreach (var sq in PlacementSquare.Placements)
                sq?.Render();

        }

        TankGame.Instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        Particles.RenderParticles();

        IntermissionHandler.RenderCountdownGraphics();

        if (!MainMenu.Active && !LevelEditor.Active) {
            if (IntermissionSystem.IsAwaitingNewMission) {
                // uhhh... what was i doing here?
            }
            for (int i = -4; i < 10; i++) {
                IntermissionSystem.DrawShadowedTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard"),
                    new Vector2((i * 14).ToResolutionX(), WindowUtils.WindowHeight * 0.9f),
                    Vector2.UnitY,
                    Color.White,
                    new Vector2(2f).ToResolution(),
                    1f,
                    new(0, GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard").Size().Y / 2),
                    true);
            }
            IntermissionSystem.DrawShadowedString(TankGame.TextFontLarge, new Vector2(80.ToResolutionX(), WindowUtils.WindowHeight * 0.9f - 14f.ToResolutionY()), Vector2.One, $"{PlayerTank.KillCount}", new(119, 190, 238), new Vector2(0.675f).ToResolution(), 1f);
        }
        GameUI.MissionInfoBar.IsVisible = !MainMenu.Active && !LevelEditor.Active && !CampaignCompleteUI.IsViewingResults;

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

    // fix shitty mission init (innit?)

    public static void SetupGraphics() {
        GameShaders.Initialize();
        MapRenderer.InitializeRenderers();
        SceneManager.LoadGameScene();
        DebugManager.InitDebugUI();
        PlacementSquare.InitializeLevelEditorSquares();
    }

    #endregion
}