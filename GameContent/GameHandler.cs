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

namespace WiiPlayTanksRemake.GameContent
{
    public class GameHandler
    {

        public static int timeUntilTankFunction;

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

            foreach (var cube in Cube.cubes)
                cube?.Update();

            foreach (var expl in MineExplosion.explosions)
                expl?.Update();

            foreach (var crate in CrateDrop.crates)
                crate?.Update();

            foreach (var pu in Powerup.activePowerups)
                pu?.Update();

            if (Input.KeyJustPressed(Keys.Insert))
                DebugUtils.DebuggingEnabled = !DebugUtils.DebuggingEnabled;

            if (Input.KeyJustPressed(Keys.Add))
                DebugUtils.DebugLevel++;
            if (Input.KeyJustPressed(Keys.Subtract))
                DebugUtils.DebugLevel--;

            if (timeUntilTankFunction > 0)
            {
                timeUntilTankFunction--;
            }
            else
            {
                if (!InMission)
                {
                    InMission = true;
                    OnMissionStart?.Invoke();
                    TankMusicSystem.PlayMusic();
                }
            }

            if (Input.KeyJustPressed(Keys.PageUp))
                SpawnTankPlethorae();
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

            if (Input.KeyJustPressed(Keys.Home))
            {
                SpawnTankAtMouse((TankTier)tankToSpawnType, (Team)tankToSpawnTeam);
                // new Cube(CubeMapPosition.ConvertFromVector3(GameUtils.GetWorldPosition(GameUtils.MousePosition)), Cube.BlockType.Wood, 3);
            }

            if (Input.KeyJustPressed(Keys.OemSemicolon))
            {
                var m = new Mine(null, GameUtils.GetWorldPosition(GameUtils.MousePosition), 400);
            }
            if (Input.KeyJustPressed(Keys.OemQuotes))
            {
                var m = new Shell(GameUtils.GetWorldPosition(GameUtils.MousePosition) + new Vector3(0, 11, 0), default, 0);
            }
            if (Input.KeyJustPressed(Keys.L))
            {
                var c = new Cube(Cube.BlockType.Wood, 1)
                {
                    position = GameUtils.GetWorldPosition(GameUtils.MousePosition)
                };
            }
            if (Input.KeyJustPressed(Keys.End))
            {
                SpawnCrateAtMouse();
            }

            if (Input.KeyJustPressed(Keys.I))
                new Powerup(powerups[mode]) { position = GameUtils.GetWorldPosition(GameUtils.MousePosition) };

        }

        public static int tankToSpawnType;
        public static int tankToSpawnTeam;

        internal static void DoRender()
        {
            MapRenderer.DrawWorldModels();

            foreach (var tank in AllPlayerTanks)
                tank?.DrawBody();

            foreach (var tank in AllAITanks)
                tank?.DrawBody();

            foreach (var cube in Cube.cubes)
                cube?.Draw();

            foreach (var mine in Mine.AllMines)
                mine?.Draw();

            foreach (var bullet in Shell.AllShells)
                bullet?.Render();

            foreach (var mark in TankDeathMark.deathMarks)
                mark?.Render();

            foreach (var print in TankFootprint.footprints)
                print?.Render();

            foreach (var expl in MineExplosion.explosions)
                expl?.Render();

            foreach (var crate in CrateDrop.crates)
                crate?.Render();

            foreach (var pu in Powerup.activePowerups)
                pu?.Render();

            // TODO: Fix translation
            // TODO: Scaling with screen size.

            foreach (var element in UIElement.AllUIElements.ToList()) {
                //element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));

                if (element.HasScissor)
                    TankGame.spriteBatch.End();

                element?.Draw(TankGame.spriteBatch);

                if (element.HasScissor)
                    TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            }
            tankToSpawnType = MathHelper.Clamp(tankToSpawnType, 1, Enum.GetValues<TankTier>().Length - 1);
            tankToSpawnTeam = MathHelper.Clamp(tankToSpawnTeam, 0, Enum.GetValues<Team>().Length - 1);

            #region TankInfo
            DebugUtils.DrawDebugString(TankGame.spriteBatch, "Spawn Tank With Info:", GameUtils.WindowTop + new Vector2(0, 8), 1, centerIt: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"Tier: {Enum.GetNames<TankTier>()[tankToSpawnType]}", GameUtils.WindowTop + new Vector2(0, 24), 1, centerIt: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"Team: {Enum.GetNames<Team>()[tankToSpawnTeam]}", GameUtils.WindowTop + new Vector2(0, 40), 1, centerIt: true);

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

            List<UIElement> focusedElements = UIElement.GetElementAt(GameUtils.MousePosition, true);

            foreach (UIElement el in focusedElements)
            {
                if (el != null)
                {
                    el.LeftClick();
                    el.LeftDown();
                    el.LeftUp();

                    el.RightClick();
                    el.RightDown();
                    el.RightUp();

                    el.MiddleClick();
                    el.MiddleDown();
                    el.MiddleUp();

                    el.MouseOver();
                }
            }

            foreach (UIElement element in UIElement.AllUIElements)
            {
                if (element.MouseHovering)
                    element.MouseOut();
            }
        }

        public static PlayerTank myTank;

        public static Mission ExampleMission1 = new(
                new Tank[]
                {
                    new AITank(TankTier.Ash) { Team = Team.NoTeam },
                    new AITank(TankTier.Marine) { Team = Team.NoTeam },
                    new AITank(TankTier.Pink) { Team = Team.NoTeam },
                    new AITank(TankTier.Yellow) { Team = Team.NoTeam }
                },
                new Vector3[]
                {
                    new CubeMapPosition(4, 4),
                    new CubeMapPosition(CubeMapPosition.MAP_WIDTH - 4, 4),
                    new CubeMapPosition(CubeMapPosition.MAP_WIDTH - 4, CubeMapPosition.MAP_HEIGHT - 4),
                    new CubeMapPosition(4, CubeMapPosition.MAP_HEIGHT - 4)
                },
                new float[]
                {
                    GeometryUtils.GetQuarterRotation(1),
                    GeometryUtils.GetQuarterRotation(0),
                    GeometryUtils.GetQuarterRotation(3),
                    GeometryUtils.GetQuarterRotation(2)
                },
                new Cube[]
                {
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),

                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),

                                        new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                },
                new CubeMapPosition[]
                {
                    new(7, 0),
                    new(7, 1),
                    new(7, 2),
                    new(7, 3),
                    new(7, 4),
                    new(7, 5),
                    new(7, 6),
                    new(7, 7),
                    new(7, 8),
                    new(7, 9),

                    new(11, 0),
                    new(11, 1),
                    new(11, 2),
                    new(11, 3),
                    new(11, 4),
                    new(11, 5),
                    new(11, 6),
                    new(11, 7),
                    new(11, 8),
                    new(11, 9),
                    new(11, 8),
                    new(11, 9),
                });
        // fix shitty mission init (innit?)

        private static readonly PowerupTemplate[] powerups = 
        {
             new(1000, 50f, (tnk) => { tnk.MaxSpeed *= 2; }) { Name = "Speed" },
             new(1000, 50f, (tnk) => { tnk.Invisible = true; }) { Name = "Invis" },
             new(1000, 50f, (tnk) => { tnk.MaximalTurn = 6.28f; tnk.TurningSpeed = 100f; }) { Name = "BigTurn" },
             new(1000, 50f, (tnk) => { tnk.ShellHoming.radius = 150f; tnk.ShellHoming.speed = tnk.ShellSpeed; tnk.ShellHoming.power = 1f; }) { Name = "Homing" },
             new(1000, 50f, (tnk) => { tnk.Stationary = true; }) { Name = "Stationary" }
        };

        public static void Initialize()
        {
            // TankGame.Instance.IsFixedTimeStep = false;
            // 26 x 18 (technically 27 x 19)
            InitDebugUi();
            GameShaders.Initialize();

            DebugUtils.DebuggingEnabled = true;
            MapRenderer.InitializeRenderers();

            VanillaCampaign.LoadMission(ExampleMission1);

            IngameUI.Initialize();

            LoadTnkScene();

            // Lighting.Midnight.Apply(false);

            var brighter = new Lighting.DayState()
            {
                color = Color.White,
                brightness = 0.5f
            };

            brighter.Apply(false);

            /*for (int i = 0; i < 3; i++)
                SpawnTankInCrate(TankTier.Black, Team.Red, true);

            for (int i = 0; i < 2; i++)
                SpawnTankInCrate(TankTier.Obsidian, Team.Blue, true);*/


            BeginIntroSequence();
        }

        public static void LoadTnkScene()
        {
            TankMusicSystem.LoadMusic();

            TankMusicSystem.LoadAmbienceTracks();
        }

        public static PlayerTank SpawnMe()
        {
            myTank = new PlayerTank(PlayerType.Blue)
            {
                Team = Team.Red,
                position = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 21)),
                Dead = false
            };
            return myTank;
        }

        public static void SpawnTankInCrate(TankTier tierOverride = default, Team teamOverride = default, bool createEvenDrop = false)
        {
            var random = new CubeMapPosition(new Random().Next(0, 26), new Random().Next(0, 20));

            var drop = CrateDrop.SpawnCrate(new(CubeMapPosition.Convert3D(random).X, 500 + (createEvenDrop ? 0 : new Random().Next(-300, 301)), CubeMapPosition.Convert3D(random).Z), 2f);
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
                Team = Team.NoTeam
            };
        }

        public static void BeginIntroSequence()
        {
            timeUntilTankFunction = 180;
            var tune = GameResources.GetGameResource<SoundEffect>("Assets/fanfares/mission_snare");

            SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);

            VanillaCampaign.SetupLoadedMission();

            foreach (var tank in AllTanks.Where(tnk => tnk is not null))
                tank.velocity = Vector3.Zero;

            foreach (var song in TankMusicSystem.songs)
                song?.Stop();

            for (int i = 0; i < Mine.AllMines.Length; i++)
                Mine.AllMines[i] = null;

            for (int i = 0; i < Shell.AllShells.Length; i++)
                Shell.AllShells[i] = null;

            InMission = false;

            int minx = (int)MapRenderer.MIN_X - 12;
            int miny = (int)MapRenderer.MIN_Y - 12;

            int maxx = (int)MapRenderer.MAX_X + 12;
            int maxy = (int)MapRenderer.MAX_Y + 12;

            for (int i = minx; i < maxx; i++)
            {
                if (i % 10 == 0)
                {
                    var s = new Shell(new(i, 11, miny), default)
                    {
                        INTERNAL_ignoreCollisions = true,
                        INTERNAL_doRender = false
                    };

                    var p = new Shell(new(i, 11, maxy), default)
                    {
                        INTERNAL_ignoreCollisions = true,
                        INTERNAL_doRender = false
                    };
                }
            }
            for (int j = miny; j < maxy; j++)
            {
                if (j % 10 == 0)
                {
                    var s = new Shell(new(minx, 11, j), default)
                    {
                        INTERNAL_ignoreCollisions = true,
                        INTERNAL_doRender = false
                    };

                    var p = new Shell(new(maxx, 11, j), default)
                    {
                        INTERNAL_ignoreCollisions = true,
                        INTERNAL_doRender = false
                    };
                }
            }
            // for ai tanks avoiding walls lol
        }
        public static AITank SpawnTank(TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();

            return new AITank(tier)
            {
                TankRotation = rot,
                TurretRotation = rot,
                Team = team,
                position = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 20)),
                Dead = false
            };
        }
        public static AITank SpawnTankAtMouse(TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var pos = GameUtils.GetWorldPosition(GameUtils.MousePosition);


            return new AITank(tier)
            {
                TankRotation = rot,
                targetTankRotation = rot - MathHelper.Pi,
                TurretRotation = rot - MathHelper.TwoPi,
                Team = team,
                position = pos,
                Dead = false
            };
        }
        public static void SpawnTankPlethorae()
        {
            for (int i = 0; i < 5; i++)
            {
                var random = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 20));
                var rot = GeometryUtils.GetPiRandom();
                var t = new AITank(AITank.PICK_ANY_THAT_ARE_IMPLEMENTED())
                {
                    TankRotation = rot,
                    TurretRotation = rot,
                    position = random,
                    Dead = false
                };

                t.Team = (Team)new Random().Next(0, Enum.GetValues<Team>().Length);

                // t.Team = (Team)new Random().Next(1, Enum.GetNames<Team>().Length);
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
            VanillaCampaign.LoadMission(new Mission(
                new Tank[]
                {
                    new AITank(TankTier.Sapphire) { Team = Team.Red },
                    new AITank(TankTier.Amethyst) { Team = Team.Blue },
                    new AITank(TankTier.Sapphire) { Team = Team.Green },
                    new AITank(TankTier.Amethyst) { Team = Team.Yellow }
                },
                new Vector3[]
                {
                    new CubeMapPosition(4, 4),
                    new CubeMapPosition(CubeMapPosition.MAP_WIDTH - 4, 4),
                    new CubeMapPosition(CubeMapPosition.MAP_WIDTH - 4, CubeMapPosition.MAP_HEIGHT - 4),
                    new CubeMapPosition(4, CubeMapPosition.MAP_HEIGHT - 4)
                },
                new float[]
                {
                    GeometryUtils.GetQuarterRotation(1),
                    GeometryUtils.GetQuarterRotation(0),
                    GeometryUtils.GetQuarterRotation(3),
                    GeometryUtils.GetQuarterRotation(2)
                },
                new Cube[]
                {
                },
                new CubeMapPosition[]
                {
                }));
            BeginIntroSequence();
        }

        private static void ClearTankDeathmarks(UIElement affectedElement)
        {
            for (int i = 0; i < TankDeathMark.deathMarks.Length; i++)
                TankDeathMark.deathMarks[i] = null;

            TankDeathMark.total_death_marks = 0;
        }

        private static void ClearTankTracks(UIElement affectedElement)
        {
            for (int i = 0; i < TankFootprint.footprints.Length; i++)
                TankFootprint.footprints[i] = null;

            TankFootprint.total_treads_placed = 0;
        }
    }

    public class MouseRenderer
    {
        public static Texture2D MouseTexture { get; private set; }

        public static void DrawMouse()
        {
            MouseTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/cursor_1");

            var mousePos = GameUtils.MousePosition;

            for (int i = 0; i < 4; i++)
            {
                TankGame.spriteBatch.Draw(MouseTexture, mousePos, null, Color.White, MathHelper.PiOver2 * i, MouseTexture.Size(), 1f, default, default);
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
            // MouseShader.Parameters["oColor"].SetValue(new Vector4(0, 0, 1, 1));
        }
    }
}
