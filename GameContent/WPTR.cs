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
        public const int MAX_AI_TANKS = 1000;
        public const int MAX_PLAYERS = 8;

        // public static List<AITank> AllAITanks { get; } = new();
        public static AITank[] AllAITanks { get; } = new AITank[MAX_AI_TANKS];

        // public static List<PlayerTank> AllPlayerTanks { get; } = new();
        public static PlayerTank[] AllPlayerTanks { get; } = new PlayerTank[MAX_PLAYERS];

        // public static List<Tank> AllTanks { get; } = new();

        public static Tank[] AllTanks { get; } = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        public static float FloatForTesting;

        public static Logger BaseLogger { get; } = new($"{TankGame.ExePath}", "client_logger");

        private static UIElement lastElementClicked;

        public static bool WindowBorderless { get; set; }

        public static bool InMission { get; set; } = false;

        public static Matrix UIMatrix => Matrix.CreateOrthographicOffCenter(0, TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, 0, -1, 1);

        public delegate void MissionStartEvent();

        public static event MissionStartEvent OnMissionStart;

        public static void StartMission() { }

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

            foreach (var expl in MineExplosion.explosions.Where(expl => expl is not null))
                expl.Update();

            IngameUI.UpdateButtons();

            GameShaders.UpdateShaders();

            FloatForTesting = MathHelper.Clamp(FloatForTesting, -1, 1);

            if (Input.KeyJustPressed(Keys.Insert))
                DebugUtils.DebuggingEnabled = !DebugUtils.DebuggingEnabled;

            if (Input.AreKeysJustPressed(Keys.RightAlt, Keys.Enter))
                WindowBorderless = !WindowBorderless;

            if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Left))
                FloatForTesting -= 0.01f;
            if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Right))
                FloatForTesting += 0.01f;

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
        }

        public static int tankToSpawnType;
        public static int tankToSpawnTeam;

        internal static void Draw()
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

            foreach (var mark in TankDeathMark.deathMarks.Where(mk => mk is not null))
                mark?.Render();

            foreach (var print in TankFootprint.footprints.Where(prnt => prnt is not null))
                print?.Render();

            foreach (var expl in MineExplosion.explosions.Where(expl => expl is not null))
                expl.Render();

            // TODO: Fix translation
            // TODO: Scaling with screen size.

            foreach (var element in UIElement.AllUIElements) {
                //element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));

                element?.Draw(TankGame.spriteBatch);
            }
            tankToSpawnType = MathHelper.Clamp(tankToSpawnType, 1, Enum.GetValues<TankTier>().Length - 1);
            tankToSpawnTeam = MathHelper.Clamp(tankToSpawnTeam, 0, Enum.GetValues<Team>().Length - 1);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, "Spawn Tank With Info:", GameUtils.WindowTop + new Vector2(0, 8), 1, centerIt: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"Tier: {Enum.GetNames<TankTier>()[tankToSpawnType]}", GameUtils.WindowTop + new Vector2(0, 24), 1, centerIt: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"Team: {Enum.GetNames<Team>()[tankToSpawnTeam]}", GameUtils.WindowTop + new Vector2(0, 40), 1, centerIt: true);

            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"TestFloat: {FloatForTesting}" +
                $"\nHighestTier: {AITank.GetHighestTierActive()}" +
                $"\n", new(10, GameUtils.WindowHeight * 0.1f));

            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"HighestTier: {AITank.GetHighestTierActive()}", new(10, GameUtils.WindowHeight / 2));
            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.volume == 0.5f).Name : "N/A")}", new(10, GameUtils.WindowHeight - 100));

            /*for (int i = 0; i < AllAITanks.Length; i++)
            {
                var t = AllAITanks[i];
                DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{t}", new(10, 15 * i), 1);
            }*/

            ChatSystem.DrawMessages();

            for (int i = 0; i < Enum.GetNames<TankTier>().Length; i++)
            {
                DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{Enum.GetNames<TankTier>()[i]}: {AITank.GetTankCountOfType((TankTier)i)}", new(10, GameUtils.WindowHeight * 0.6f + (i * 20)));
            }

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

            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{Input.CurrentGamePadSnapshot.ThumbSticks.Left.X}\n{Input.CurrentGamePadSnapshot.ThumbSticks.Left.Y}", new Vector2(10, 10), 2);
        }

        public static PlayerTank myTank;

        public static void Initialize()
        {
            GameShaders.Initialize();

            DebugUtils.DebuggingEnabled = true;
            MapRenderer.InitializeRenderers();

            // 26 max x
            // 20 max y

            var dirX = 0;
            var dirY = 0;

            for (int i = 0; i < 20; i++)
            {
                dirX += new Random().Next(0, 2) == 0 ? 1 : -1;
                dirY += new Random().Next(0, 2) == 0 ? 1 : -1;

                dirX = MathHelper.Clamp(dirX, 0, CubeMapPosition.MAP_WIDTH);
                dirY = MathHelper.Clamp(dirY, 0, CubeMapPosition.MAP_HEIGHT);

                var pos = new CubeMapPosition(dirX, dirY);

                new Cube(pos, (Cube.BlockType)new Random().Next(1, 3), 2);
            }

            SpawnMe();
            SpawnTankPlethorae();

            IngameUI.Initialize();

            TankMusicSystem.LoadMusic();
          
            BeginIntroSequence();
        }

        public static void SpawnMe()
        {
            myTank = new PlayerTank(new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 21)), playerType: PlayerType.Blue);
            myTank.Team = Team.Red;
        }

        internal static int timeUntilTankFunction;

        public static void BeginIntroSequence()
        {
            timeUntilTankFunction = 180;
            var tune = GameResources.GetGameResource<SoundEffect>("Assets/fanfares/mission_snare");

            SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);
        }
        public static AITank SpawnTank(TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var random = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 20));

            return new AITank(new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 20)), tier)
            {
                TankRotation = rot,
                TurretRotation = rot,
                Team = team
            };
        }
        public static AITank SpawnTankAtMouse(TankTier tier, Team team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var pos = GameUtils.GetWorldPosition(GameUtils.MousePosition);


            return new AITank(pos, tier)
            {
                TankRotation = rot,
                TurretRotation = rot,
                Team = team
            };
        }
        public static void SpawnTankPlethorae()
        {
            for (int i = 0; i < 5; i++)
            {
                var random = new CubeMapPosition(new Random().Next(0, 27), new Random().Next(0, 20));
                var rot = GeometryUtils.GetPiRandom();
                var t = new AITank(random, AITank.PICK_ANY_THAT_ARE_IMPLEMENTED())
                {
                    TankRotation = rot,
                    TurretRotation = rot
                };

                t.Team = Team.Orange;

                // t.Team = (Team)new Random().Next(1, Enum.GetNames<Team>().Length);
            }
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
