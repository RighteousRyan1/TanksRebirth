using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.UI;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using WiiPlayTanksRemake.Enums;
using System;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.GameContent.Systems;
using System.Collections.Generic;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.GameContent.UI;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common.Framework.Input;
using System.IO;
using System.Threading.Tasks;
using WiiPlayTanksRemake.GameContent.Systems.Coordinates;
using WiiPlayTanksRemake.Net;

namespace WiiPlayTanksRemake.GameContent
{
    public class GameHandler
    {
        public static Random GameRand = new();

        public static int timeUntilTankFunction = 180;

        public const int MAX_AI_TANKS = 1000;
        public const int MAX_PLAYERS = 100;
        public static AITank[] AllAITanks { get; } = new AITank[MAX_AI_TANKS];
        public static PlayerTank[] AllPlayerTanks { get; } = new PlayerTank[MAX_PLAYERS];
        public static Tank[] AllTanks { get; } = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        public static Campaign VanillaCampaign { get; private set; } = new();

        public static Logger ClientLog { get; } = new($"{TankGame.SaveDirectory}", "client");

        public static bool InMission { get; set; } = false;

        public static Matrix UIMatrix => Matrix.CreateOrthographicOffCenter(0, TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, 0, -1, 1);

        public delegate void MissionStartEvent();

        public static event MissionStartEvent OnMissionStart;


        private static bool _wasOverhead;

        internal static void Update()
        {

            if (InMission)
                TankMusicSystem.Update();

            foreach (var tank in AllPlayerTanks)
                tank?.Update();

            foreach (var tank in AllAITanks)
                tank?.Update();

            foreach (var mine in Mine.AllMines)
                mine?.Update();

            foreach (var bullet in Shell.AllShells)
                bullet?.Update();

            foreach (var expl in MineExplosion.explosions)
                expl?.Update();

            foreach (var crate in CrateDrop.crates)
                crate?.Update();

            foreach (var pu in Powerup.activePowerups)
                pu?.Update(); 

            foreach (var cube in Block.blocks)
                cube?.Update();

            if (TankGame.OverheadView)
                foreach (var sq in PlacementSquare.Placements)
                    sq?.Update();

            ParticleSystem.UpdateParticles();

            if (MainMenu.Active)
                MainMenu.Update();

            if (Input.KeyJustPressed(Keys.Insert))
                DebugUtils.DebuggingEnabled = !DebugUtils.DebuggingEnabled;

            if (Input.KeyJustPressed(Keys.Multiply))
                DebugUtils.DebugLevel++;
            if (Input.KeyJustPressed(Keys.Divide))
                DebugUtils.DebugLevel--;

            if (!TankGame.OverheadView && _wasOverhead)
                RestartMission(null);

            if (TankGame.OverheadView || MainMenu.Active)
            {
                InMission = false;
                timeUntilTankFunction = 180;
            }

            if (timeUntilTankFunction > 0)
                timeUntilTankFunction--;
            else
            {
                if (!InMission)
                {
                    InMission = true;
                    OnMissionStart?.Invoke();
                    TankMusicSystem.PlayAll();
                }
            }

            if (Input.KeyJustPressed(Keys.PageUp))
                SpawnTankPlethorae(true);
            if (Input.KeyJustPressed(Keys.PageDown))
                SpawnMe();

            if (Input.KeyJustPressed(Keys.NumPad7))
                tankToSpawnType--;
            if (Input.KeyJustPressed(Keys.NumPad9))
                tankToSpawnType++;

            if (Input.KeyJustPressed(Keys.NumPad1))
                tankToSpawnTeam--;
            if (Input.KeyJustPressed(Keys.NumPad3))
                tankToSpawnTeam++;

            if (Input.KeyJustPressed(Keys.OemPeriod))
                CubeHeight++;
            if (Input.KeyJustPressed(Keys.OemComma))
                CubeHeight--;

            if (Input.KeyJustPressed(Keys.Z))
                BlockType--;
            if (Input.KeyJustPressed(Keys.X))
                BlockType++;

            if (Input.KeyJustPressed(Keys.Home))
                SpawnTankAt(!TankGame.OverheadView ? GameUtils.GetWorldPosition(GameUtils.MousePosition) : PlacementSquare.CurrentlyHovered.Position, (TankTier)tankToSpawnType, (Team)tankToSpawnTeam);

            if (Input.KeyJustPressed(Keys.OemSemicolon))
                new Mine(null, GameUtils.GetWorldPosition(GameUtils.MousePosition).FlattenZ(), 400);
            if (Input.KeyJustPressed(Keys.OemQuotes))
                new Shell(GameUtils.GetWorldPosition(GameUtils.MousePosition) + new Vector3(0, 11, 0), Vector3.Zero, ShellTier.Standard, null, 0, playSpawnSound: false);
            if (Input.KeyJustPressed(Keys.End))
                SpawnCrateAtMouse();

            if (Input.KeyJustPressed(Keys.I))
                new Powerup(powerups[mode]) { Position = GameUtils.GetWorldPosition(GameUtils.MousePosition).FlattenZ() };

            CubeHeight = MathHelper.Clamp(CubeHeight, 1, 7);
            BlockType = MathHelper.Clamp(BlockType, 1, 3);

            _wasOverhead = TankGame.OverheadView;
        }

        public static int BlockType = 1;
        public static int CubeHeight = 1;
        public static int tankToSpawnType;
        public static int tankToSpawnTeam;

        internal static void RenderAll()
        {
            TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

            if (!MainMenu.Active)
                MapRenderer.RenderWorldModels();

            foreach (var cube in Block.blocks)
                cube?.Render();

            foreach (var tank in AllPlayerTanks)
                tank?.DrawBody();

            foreach (var tank in AllAITanks)
                tank?.DrawBody();

            foreach (var mine in Mine.AllMines)
                mine?.Render();

            foreach (var bullet in Shell.AllShells)
                bullet?.Render();

            foreach (var expl in MineExplosion.explosions)
                expl?.Render();

            foreach (var mark in TankDeathMark.deathMarks)
                mark?.Render();

            foreach (var print in TankFootprint.footprints)
                print?.Render();

            foreach (var crate in CrateDrop.crates)
                crate?.Render();

            foreach (var powerup in Powerup.activePowerups)
                powerup?.Render();

            if (TankGame.OverheadView)
                foreach (var sq in PlacementSquare.Placements)
                    sq?.Render();

            ParticleSystem.RenderParticles();
            MainMenu.Render();

            // TODO: Fix translation
            // TODO: Scaling with screen size.

            foreach (var element in UIElement.AllUIElements.ToList()) {
                //element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));
                if (element.Parent != null)
                    continue;

                if (element.HasScissor)
                    TankGame.spriteBatch.End();

                element?.Draw(TankGame.spriteBatch);

                if (element.HasScissor)
                    TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            }
            tankToSpawnType = MathHelper.Clamp(tankToSpawnType, 1, Enum.GetValues<TankTier>().Length - 1);
            tankToSpawnTeam = MathHelper.Clamp(tankToSpawnTeam, 0, Enum.GetValues<Team>().Length - 1);

            #region TankInfo
            DebugUtils.DrawDebugString(TankGame.spriteBatch, "Spawn Tank With Info:", GameUtils.WindowTop + new Vector2(0, 8), 1, centered: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"Tier: {Enum.GetNames<TankTier>()[tankToSpawnType]}", GameUtils.WindowTop + new Vector2(0, 24), 1, centered: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"Team: {Enum.GetNames<Team>()[tankToSpawnTeam]}", GameUtils.WindowTop + new Vector2(0, 40), 1, centered: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"CubeStack: {CubeHeight} | CubeType: {Enum.GetNames<Block.BlockType>()[BlockType - 1]}", GameUtils.WindowBottom - new Vector2(0, 20), 3, centered: true);

            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"HighestTier: {AITank.GetHighestTierActive()}", new(10, GameUtils.WindowHeight * 0.26f), 1);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.volume == 0.5f).Name : "N/A")}", new(10, GameUtils.WindowHeight - 100), 1);
            for (int i = 0; i < Enum.GetNames<TankTier>().Length; i++)
            {
                DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{Enum.GetNames<TankTier>()[i]}: {AITank.GetTankCountOfType((TankTier)i)}", new(10, GameUtils.WindowHeight * 0.3f + (i * 20)), 1);
            }
            #endregion

            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"Logic Time: {TankGame.LogicTime}" +
                $"\nLogic FPS: {TankGame.LogicFPS}" +
                $"\n\nRender Time: {TankGame.RenderTime}" +
                $"\nRender FPS: {TankGame.RenderFPS}", new(10, 500));

            ChatSystem.DrawMessages();

            if (!MainMenu.Active)
            {
                ClearTracks.IsVisible = DebugUtils.DebuggingEnabled;
                ClearChecks.IsVisible = DebugUtils.DebuggingEnabled;
                SetupMissionAgain.IsVisible = DebugUtils.DebuggingEnabled;
                MovePULeft.IsVisible = DebugUtils.DebuggingEnabled;
                MovePURight.IsVisible = DebugUtils.DebuggingEnabled;
                Display.IsVisible = DebugUtils.DebuggingEnabled;
            }
            GameUI.MissionInfoBar.IsVisible = DebugUtils.DebuggingEnabled;
        }

        public static PlayerTank myTank;

        // fix shitty mission init (innit?)

        private static readonly PowerupTemplate[] powerups =
        {
             new(1000, 50f, (tnk) => { tnk.MaxSpeed *= 2; }, (tnk) => { tnk.MaxSpeed /= 2; }) { Name = "Speed" },
             new(1000, 50f, (tnk) => { tnk.Invisible = !tnk.Invisible; }, (tnk) => tnk.Invisible = !tnk.Invisible) { Name = "InvisSwap" },
             new(1000, 50f, (tnk) => { tnk.ShellHoming.radius = 150f; tnk.ShellHoming.speed = tnk.ShellSpeed; tnk.ShellHoming.power = 1f; }, (tnk) => tnk.ShellHoming = new()) { Name = "Homing" },
             new(1000, 50f, (tnk) => { if (tnk.MaxSpeed > 0) tnk.Stationary = true; }, (tnk) => { if (tnk.MaxSpeed > 0) tnk.Stationary = !tnk.Stationary; }) { Name = "Stationary" }
        };

        public static void StartTnkScene()
        {
            DebugUtils.DebuggingEnabled = false;
            MapRenderer.InitializeRenderers();

            LoadTnkScene();

            var brighter = new Lighting.DayState()
            {
                color = new(150, 150, 170),// color = Color.DarkGray,
                brightness = 0.71f,
            };

            brighter.Apply(false);

            MainMenu.isLoadingScene = false;

            BeginIntroSequence();

            PlacementSquare.InitializeLevelEditorSquares();

            InitDebugUi();
        }

        public static void SetupGraphics()
        {
            GameUI.Initialize();
            MainMenu.Initialize();
            GameShaders.Initialize();
        }

        public static void LoadTnkScene()
        {
            TankMusicSystem.LoadMusic();

            TankMusicSystem.LoadAmbienceTracks();
        }

        public static PlayerTank SpawnMe()
        {
            var pos = GameUtils.GetWorldPosition(GameUtils.MousePosition);
            myTank = new PlayerTank(PlayerType.Blue)
            {
                Team = Team.Red,
                // Position = pos.FlattenZ(),
                Dead = false
            };
            myTank.Body.Position = pos.FlattenZ();
            return myTank;
        }

        public static void SpawnTankInCrate(TankTier tierOverride = default, Team teamOverride = default, bool createEvenDrop = false)
        {
            var random = new CubeMapPosition(GameRand.Next(0, 26), GameRand.Next(0, 20));

            var drop = CrateDrop.SpawnCrate(new(CubeMapPosition.Convert3D(random).X, 500 + (createEvenDrop ? 0 : GameRand.Next(-300, 301)), CubeMapPosition.Convert3D(random).Z), 2f);
            drop.scale = 1.25f;
            drop.TankToSpawn = new AITank(tierOverride == default ? AITank.PICK_ANY_THAT_ARE_IMPLEMENTED() : tierOverride)
            {
                Team = teamOverride == default ? GameUtils.PickRandom<Team>() : teamOverride
            };
        }

        public static void SpawnCrateAtMouse()
        {
            var pos = GameUtils.GetWorldPosition(GameUtils.MousePosition);

            var drop = CrateDrop.SpawnCrate(new(pos.X, 200, pos.Z), 2f);
            drop.scale = 1.25f;
            drop.TankToSpawn = new AITank(AITank.PICK_ANY_THAT_ARE_IMPLEMENTED())
            {
                Dead = true,
                Team = Team.NoTeam
            };
        }

        public static void BeginIntroSequence()
        {
            timeUntilTankFunction = 180;
            var tune = GameResources.GetGameResource<SoundEffect>("Assets/fanfares/mission_snare");

            SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);

            foreach (var tank in AllTanks)
                if (tank is not null)
                    tank.Velocity = Vector2.Zero;

            foreach (var song in TankMusicSystem.songs)
                song?.Stop();

            for (int i = 0; i < Mine.AllMines.Length; i++)
                Mine.AllMines[i] = null;

            for (int i = 0; i < Shell.AllShells.Length; i++)
                Shell.AllShells[i]?.Destroy(false);

            InMission = false;
        }
        public static AITank SpawnTank(TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();
            
            var t = new AITank(tier)
            {
                TankRotation = rot,
                TurretRotation = rot,
                Team = team,
                Dead = false
            };
            t.Body.Position = new CubeMapPosition(GameRand.Next(0, 27), GameRand.Next(0, 20));

            return t;
        }
        public static AITank SpawnTankAt(Vector3 position, TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var x = new AITank(tier)
            {
                TankRotation = rot,
                targetTankRotation = rot - MathHelper.Pi,
                TurretRotation = -rot,
                Team = team,
                Dead = false,
            };
            x.Body.Position = position.FlattenZ();
            return x;
        }
        public static void SpawnTankPlethorae(bool useCurTank = false)
        {
            for (int i = 0; i < 5; i++)
            {
                var random = new CubeMapPosition(GameRand.Next(0, 23),GameRand.Next(0, 18));
                var rot = GeometryUtils.GetPiRandom();
                var t = new AITank(useCurTank ? (TankTier)tankToSpawnType : AITank.PICK_ANY_THAT_ARE_IMPLEMENTED())
                {
                    TankRotation = rot,
                    TurretRotation = rot,
                    Position = random,
                    Dead = false,
                    Team = useCurTank ? (Team)tankToSpawnTeam : Team.NoTeam
                };
            }
        }

        public static UITextButton ClearTracks;
        public static UITextButton ClearChecks;

        public static UITextButton SetupMissionAgain;

        public static UITextButton MovePURight;
        public static UITextButton MovePULeft;

        public static UITextButton Display;

        private static int mode;

        public static void InitDebugUi()
        {
            ClearTracks = new("Clear Tracks", TankGame.TextFont, Color.LightBlue, 0.5f);
            ClearTracks.SetDimensions(250, 25, 100, 50);

            ClearTracks.OnLeftClick += ClearTankTracks;

            ClearChecks = new("Clear Checks", TankGame.TextFont, Color.LightBlue, 0.5f);
            ClearChecks.SetDimensions(250, 95, 100, 50);

            ClearChecks.OnLeftClick += ClearTankDeathmarks;

            SetupMissionAgain = new("Restart\nMission", TankGame.TextFont, Color.LightBlue, 0.5f);
            SetupMissionAgain.SetDimensions(250, 165, 100, 50);

            SetupMissionAgain.OnLeftClick += RestartMission;

            MovePULeft = new("<", TankGame.TextFont, Color.LightBlue, 0.5f);
            MovePULeft.SetDimensions(GameUtils.WindowWidth / 2 - 100, 25, 50, 50);

            MovePURight = new(">", TankGame.TextFont, Color.LightBlue, 0.5f);
            MovePURight.SetDimensions(GameUtils.WindowWidth / 2 + 100, 25, 50, 50);

            Display = new(powerups[mode].Name, TankGame.TextFont, Color.LightBlue, 0.5f);
            Display.SetDimensions(GameUtils.WindowWidth / 2 - 35, 25, 125, 50);

            MovePULeft.OnLeftClick += MovePULeft_OnLeftClick;
            MovePURight.OnLeftClick += MovePURight_OnLeftClick;
        }

        private static void MovePURight_OnLeftClick(UIElement obj)
        {
            if (mode < powerups.Length - 1)
                mode++;
            Display.Text = powerups[mode].Name;
        }

        private static void MovePULeft_OnLeftClick(UIElement obj)
        {
            if (mode > 0)
                mode--;
            Display.Text = powerups[mode].Name;
        }

        private static void RestartMission(UIElement affectedElement)
        {
            BeginIntroSequence();
        }

        private static void ClearTankDeathmarks(UIElement affectedElement)
        {
            for (int i = 0; i < TankDeathMark.deathMarks.Length; i++)
            {
                if (TankDeathMark.deathMarks[i] != null)
                    TankDeathMark.deathMarks[i].check?.Destroy();
                TankDeathMark.deathMarks[i] = null;
            }

            TankDeathMark.total_death_marks = 0;
        }

        private static void ClearTankTracks(UIElement affectedElement)
        {
            for (int i = 0; i < TankFootprint.footprints.Length; i++)
            {
                if (TankFootprint.footprints[i] != null)
                    TankFootprint.footprints[i].track?.Destroy();
                TankFootprint.footprints[i] = null;
            }

            TankFootprint.total_treads_placed = 0;
        }
    }

    public static class MouseRenderer
    {
        public static Texture2D MouseTexture { get; private set; }

        public static int numDots = 10;

        private static float _sinScale;

        public static void DrawMouse()
        {
            if (GameHandler.myTank is not null)
            {
                // mwvar tankPos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, GameHandler.myTank.World, TankGame.GameView, TankGame.GameProjection);

                // var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes");

                // GameHandler.ClientLog.Write("One Loop:", LogType.Info);

                /*for (int i = 0; i < numDots; i++)
                {
                    var ii = 1f / i;

                    var pos = (GameUtils.MousePosition - tankPos) * (ii * i) + tankPos;

                    GameHandler.ClientLog.Write(pos, LogType.Info);

                    TankGame.spriteBatch.Draw(tex, pos, null, Color.White, 0f, tex.Size() / 2, 1f, default, default);
                }*/

                // lata
            }

            _sinScale = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds) / 8;

            MouseTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/cursor_1");

            for (int i = 0; i < 4; i++)
            {
                TankGame.spriteBatch.Draw(MouseTexture, GameUtils.MousePosition, null, Color.White, MathHelper.PiOver2 * i, MouseTexture.Size(), 1f + _sinScale, default, default);
            }
        }
    }
    public class GameShaders
    {
        public static Effect MouseShader { get; set; }

        public static void Initialize()
        {
            MouseShader = GameResources.GetGameResource<Effect>("Assets/Shaders/MouseShader");
        }

        public static void UpdateShaders()
        {
            MouseShader.Parameters["oGlobalTime"].SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
            MouseShader.Parameters["oColor"].SetValue(new Vector3(0f, 0f, 1f));
            MouseShader.Parameters["oSpeed"].SetValue(-20f);
            MouseShader.Parameters["oSpacing"].SetValue(10f);
        }
    }
}
