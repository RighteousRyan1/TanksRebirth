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

namespace WiiPlayTanksRemake.GameContent
{
    public class WPTR
    {
        public static int timeUntilTankFunction;

        public const int MAX_AI_TANKS = 1000;
        public const int MAX_PLAYERS = 100;

        // public static List<AITank> AllAITanks { get; } = new();
        public static AITank[] AllAITanks { get; } = new AITank[MAX_AI_TANKS];

        // public static List<PlayerTank> AllPlayerTanks { get; } = new();
        public static PlayerTank[] AllPlayerTanks { get; } = new PlayerTank[MAX_PLAYERS];

        // public static List<Tank> AllTanks { get; } = new();

        public static Tank[] AllTanks { get; } = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        public static Campaign VanillaCampaign { get; private set; } = new();

        public static Logger ClientLog { get; } = new($"{TankGame.ExePath}", "client");

        private static UIElement lastElementClicked;

        public static bool WindowBorderless { get; set; }

        public static bool InMission { get; set; } = false;

        public static Matrix UIMatrix => Matrix.CreateOrthographicOffCenter(0, TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, 0, -1, 1);

        public delegate void MissionStartEvent();

        public static event MissionStartEvent OnMissionStart;

        internal static void Update()
        {
            if (InMission)
                TankMusicSystem.Update();

            foreach (var bind in Keybind.AllKeybinds)
                bind?.Update();

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

            IngameUI.UpdateButtons();

            GameShaders.UpdateShaders();

            if (Input.KeyJustPressed(Keys.Insert))
                DebugUtils.DebuggingEnabled = !DebugUtils.DebuggingEnabled;

            if (Input.AreKeysJustPressed(Keys.RightAlt, Keys.Enter))
                WindowBorderless = !WindowBorderless;

            if (Input.KeyJustPressed(Keys.Add))
                DebugUtils.DebugLevel++;
            if (Input.KeyJustPressed(Keys.Subtract))
                DebugUtils.DebugLevel--;

            if (timeUntilTankFunction > 0)
                timeUntilTankFunction--;
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
            // TODO: Fix translation
            // TODO: Scaling with screen size.

            foreach (var element in UIElement.AllUIElements) {
                //element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));

                element?.Draw(TankGame.spriteBatch);
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

            if (TankGame.Instance.IsActive) {
                foreach (var element in UIElement.AllUIElements.ToList()) {
                    DebugUtils.DrawDebugString(TankGame.spriteBatch, element.Hitbox, new(200, 200), 2);
                    DebugUtils.DrawDebugString(TankGame.spriteBatch, GameUtils.MousePosition, new(200, 250), 2);
                    if (!element.MouseHovering && element.Hitbox.Contains(GameUtils.MousePosition)) {
                        element?.MouseOver();
                        element.MouseHovering = true;
                    }
                    else if (element.MouseHovering && !element.Hitbox.Contains(GameUtils.MousePosition)) {
                        element?.MouseLeave();
                        element.MouseHovering = false;
                    }
                    if (Input.MouseLeft && GameUtils.MouseOnScreenProtected && element != lastElementClicked && element.Hitbox.Contains(GameUtils.MousePosition)) {
                        element?.MouseClick();
                        lastElementClicked = element;
                    }
                    if (Input.MouseRight && GameUtils.MouseOnScreenProtected && element != lastElementClicked && element.Hitbox.Contains(GameUtils.MousePosition)) {
                        element?.MouseRightClick();
                        lastElementClicked = element;
                    }
                    if (Input.MouseMiddle && GameUtils.MouseOnScreenProtected && element != lastElementClicked && element.Hitbox.Contains(GameUtils.MousePosition)) {
                        element?.MouseMiddleClick();
                        lastElementClicked = element;
                    }
                }
                if (!Input.MouseLeft && !Input.MouseRight && !Input.MouseMiddle) {
                    lastElementClicked = null;
                }
            }
        }

        public static PlayerTank myTank;

        public static Mission ExampleMission1 = new(
                new Tank[]
                {
                    new AITank(TankTier.White) { Team = Team.Red },
                    new AITank(TankTier.Amethyst) { Team = Team.Blue },
                    new AITank(TankTier.Gold) { Team = Team.Green },
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
                    new(Cube.BlockType.Wood, 7),
                    new(Cube.BlockType.Wood, 7),
                    new(Cube.BlockType.Wood, 5),
                    new(Cube.BlockType.Wood, 5),
                    new(Cube.BlockType.Wood, 3),
                    new(Cube.BlockType.Wood, 3),
                    new(Cube.BlockType.Wood, 2),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),

                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),

                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1),
                    new(Cube.BlockType.Wood, 1)
                },
                new CubeMapPosition[]
                {
                    new(0, 10),
                    new(1, 10),
                    new(2, 10),
                    new(3, 10),
                    new(4, 10),
                    new(5, 10),
                    new(6, 10),
                    new(7, 10),
                    new(8, 10),
                    new(9, 10),

                    new(9, 11),
                    new(9, 12),
                    new(9, 13),

                    new(9, 19),
                    new(9, 18),
                    new(9, 17),
                    new(9, 16)
                });
        // fix shitty mission init

        public static void Initialize()
        {
            // 26 x 18
            InitDebugUi();
            GameShaders.Initialize();

            DebugUtils.DebuggingEnabled = true;
            MapRenderer.InitializeRenderers();

            VanillaCampaign.LoadMission(ExampleMission1);

            IngameUI.Initialize();

            TankMusicSystem.LoadMusic();
          
            BeginIntroSequence();
        }

        public static PlayerTank SpawnMe()
        {
            myTank = new PlayerTank(PlayerType.Blue)
            {
                Team = Team.Red,
                position = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 21))
            };
            return myTank;
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
                    var s = new Shell(new(i, 11, miny), default);
                    s.INTERNAL_ignoreCollisions = true;
                    s.INTERNAL_doRender = false;

                    var p = new Shell(new(i, 11, maxy), default);
                    p.INTERNAL_ignoreCollisions = true;
                    p.INTERNAL_doRender = false;
                }
            }
            for (int j = miny; j < maxy; j++)
            {
                if (j % 10 == 0)
                {
                    var s = new Shell(new(minx, 11, j), default);
                    s.INTERNAL_ignoreCollisions = true;
                    s.INTERNAL_doRender = false;

                    var p = new Shell(new(maxx, 11, j), default);
                    p.INTERNAL_ignoreCollisions = true;
                    p.INTERNAL_doRender = false;
                }
            }
            // for ai tanks avoiding walls lol
        }
        public static AITank SpawnTank(TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var random = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 20));

            return new AITank(tier)
            {
                TankRotation = rot,
                TurretRotation = rot,
                Team = team,
                position = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 20))
            };
        }
        public static AITank SpawnTankAtMouse(TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var pos = GameUtils.GetWorldPosition(GameUtils.MousePosition);


            return new AITank(tier)
            {
                TankRotation = rot,
                TurretRotation = rot,
                Team = team,
                position = pos
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
                    position = random
                };

                t.Team = (Team)new Random().Next(0, Enum.GetValues<Team>().Length);

                // t.Team = (Team)new Random().Next(1, Enum.GetNames<Team>().Length);
            }
        }

        public static UIImageButton ClearTracks;
        public static UIImageButton ClearChecks;

        public static UIImageButton SetupMissionAgain;

        public static void InitDebugUi()
        {
            ClearTracks = new(null, 1f, (uiPanel, spriteBatch) => IngameUI.QuickButton(uiPanel, TankGame.spriteBatch, "Clear Tracks", Color.LightBlue, 0.5f));
            ClearTracks.SetDimensions(250, 25, 100, 50);

            ClearTracks.OnMouseClick += ClearTankTracks;

            ClearChecks = new(null, 1f, (uiPanel, spriteBatch) => IngameUI.QuickButton(uiPanel, TankGame.spriteBatch, "Clear Checks", Color.LightBlue, 0.5f));
            ClearChecks.SetDimensions(250, 95, 100, 50);

            ClearChecks.OnMouseClick += ClearTankDeathmarks;

            SetupMissionAgain = new(null, 1f, (uiPanel, spriteBatch) => IngameUI.QuickButton(uiPanel, TankGame.spriteBatch, "Restart\n Mission", Color.LightBlue, 0.5f));
            SetupMissionAgain.SetDimensions(250, 165, 100, 50);

            SetupMissionAgain.OnMouseClick += RestartMission;
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
        }

        private static void ClearTankTracks(UIElement affectedElement)
        {
            for (int i = 0; i < TankFootprint.footprints.Length; i++)
                TankFootprint.footprints[i] = null;
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
