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
        Vector2 velXZ = Vector2.UnitY.RotatedByRadians(pl.TurretRotation) / 4;
        Vector3 initialVelocity = new(-velXZ.X, 1 + pl.Velocity.Length() / 3, velXZ.Y);
        Vector3 velocity = initialVelocity;
        int hits = 5;
        float timer = 0f;
        bool startTimer = false;
        bool isSmokeDestroyed = false;
        Vector3 oldPosition = p.Position;
        float shadowPos = 0f;

        p.UniqueBehavior = (a) => {
            shadowPos = 0.1f;
            p.Scale = new(125);
            p.IsIn2DSpace = false;

            p.Position += velocity;
            velocity.Y -= gravity;

            if (hits > 0) {
                p.Roll += 0.07f * velocity.Length() * TankGame.DeltaTime;
                p.Pitch += 0.07f * velocity.Length() * TankGame.DeltaTime;
            }

            // bounce off walls
            if (p.Position.Y <= 80) {
                if ((p.Position.X <= MapRenderer.MIN_X && p.Position.X >= MapRenderer.MIN_X - 6) || (p.Position.X >= MapRenderer.MAX_X && p.Position.X <= MapRenderer.MAX_X + 6)) {
                    velocity.X = -velocity.X * 0.75f;
                }
                if ((p.Position.Z <= MapRenderer.MIN_Y && p.Position.Z >= MapRenderer.MIN_Y - 6) || (p.Position.Z >= MapRenderer.MAX_Y && p.Position.Z <= MapRenderer.MAX_Y + 6)) {
                    velocity.Z = -velocity.Z * 0.75f;
                }
            }
            // block collision
            for (int i = 0; i < Block.AllBlocks.Length; i++) {
                var block = Block.AllBlocks[i];
                if (block is null) continue;
                if (block.Hitbox.Contains(p.Position.FlattenZ())) {
                    shadowPos = block.HeightFromGround;
                    if (p.Position.Y < block.HeightFromGround) {
                        if (oldPosition.X > block.Hitbox.X + block.Hitbox.Width
                        || oldPosition.X < block.Hitbox.X)
                            velocity.X = -velocity.X * 0.75f;
                        else if (oldPosition.Z > block.Hitbox.Y + block.Hitbox.Height
                        || oldPosition.Z < block.Hitbox.Y)
                            velocity.Z = -velocity.Z * 0.75f;
                        if (oldPosition.Y >= block.HeightFromGround) {
                            // less bounces on blocks!
                            if (hits <= 1) {
                                velocity = Vector3.Zero;
                                p.Position.Y = block.HeightFromGround;
                                startTimer = true;
                            }
                            hits--;
                            velocity.Y = initialVelocity.Y * hits / 5;
                        }
                    }
                }
            }

            if (p.Position.Y < 7) {
                if (hits > 0) {
                    hits--;
                    velocity.Y = initialVelocity.Y * hits / 5;
                }
                else if (hits <= 0) {
                    velocity = Vector3.Zero;
                    p.Position.Y = 7;
                    startTimer = true;
                }
            }

            if (startTimer) timer += TankGame.DeltaTime;

            if (timer > 60 && !exploded) {
                exploded = true;
                SoundPlayer.PlaySoundInstance("Assets/sounds/smoke_hiss.ogg", SoundContext.Effect, 0.3f, gameplaySound: true);
                for (int i = 0; i < 8; i++) {
                    var c = Particles.MakeParticle(p.Position,
                        GameResources.GetGameResource<Model>("Assets/smoke"),
                        GameResources.GetGameResource<Texture2D>("Assets/textures/smoke/smoke"));
                    var randDir = new Vector3(Server.ServerRandom.NextFloat(-35, 35), 0, Server.ServerRandom.NextFloat(-35, 35));
                    c.Position += randDir;
                    var randSize = Server.ServerRandom.NextFloat(5, 10);
                    c.Scale.X = randSize;
                    c.Scale.Z = randSize;
                    c.UniqueBehavior = (b) => {
                        c.Pitch += 0.005f * TankGame.DeltaTime;
                        if (c.Scale.Y < randSize && c.LifeTime < 600)
                            c.Scale.Y += 0.1f * TankGame.DeltaTime;
                        if (c.LifeTime >= 600) {
                            c.Scale.Y -= 0.06f * TankGame.DeltaTime;
                            c.Alpha -= 0.06f / randSize * TankGame.DeltaTime;

                            if (c.Scale.Y <= 0) {
                                c.Destroy();
                            }
                        }
                    };
                }
                isSmokeDestroyed = true;
                p.Destroy();
            }
            oldPosition = p.Position;
        };

        var shadow = Particles.MakeParticle(pl.Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow"));
        shadow.Scale = new(0.8f);
        shadow.Color = Color.Black;
        shadow.HasAddativeBlending = false;
        shadow.Roll = -MathHelper.PiOver2;

        shadow.UniqueBehavior = (a) => {
            if (isSmokeDestroyed) {
                shadow.Destroy();
            }
            shadow.Position.Y = shadowPos;
            shadow.Position.X = p.Position.X;
            shadow.Position.Z = p.Position.Z;

            shadow.Alpha = MathUtils.InverseLerp(150, 7, p.Position.Y, true);
        };
    }

    internal static void UpdateAll(GameTime gameTime) {
        // ChatSystem.CurTyping = SoundPlayer.GetLengthOfSound("Content/Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg").ToString();
        if (DebugManager.DebuggingEnabled) {
            if (InputUtils.KeyJustPressed(Keys.H)) {
                var plane = new Plane(new Vector3(0, 50, 0), Vector3.Zero, new() {
                    Roll = 0f,
                    Pitch = 0f,
                    Yaw = 0f
                }, 300f);
            }
            if (InputUtils.AreKeysJustPressed(Keys.J, Keys.K))
                Server.SyncSeeds();
            if (InputUtils.KeyJustPressed(Keys.M))
                SmokeNadeDebug();
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

            foreach (var p in Plane.AllPlanes)
                p?.Update();
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

        foreach (var p in Plane.AllPlanes)
            p?.Render();

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

        if (GameProperties.ShouldMissionsProgress && !MainMenu.Active)
            PingMenu.DrawPingHUD();

        IntermissionHandler.RenderCountdownGraphics();

        if (!MainMenu.Active && !LevelEditor.Active) {
            if (IntermissionSystem.IsAwaitingNewMission) {
                // uhhh... what was i doing here?
            }
            // this draws the amount of kills a player has.
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
        // TODO: MissionInfoBar can be much better.
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
    public static void SetupGraphics() {
        GameShaders.Initialize();
        MapRenderer.InitializeRenderers();
        SceneManager.LoadGameScene();
        DebugManager.InitDebugUI();
        PlacementSquare.InitializeLevelEditorSquares();
    }
    #endregion
}