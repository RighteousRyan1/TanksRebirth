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
        public static List<AITank> AllAITanks { get; } = new();

        public static List<PlayerTank> AllPlayerTanks { get; } = new();

        public static List<Tank> AllTanks { get; } = new();

        public static float FloatForTesting;

        public static Logger BaseLogger { get; } = new($"{TankGame.ExePath}", "client_logger");

        private static UIElement lastElementClicked;

        public static bool WindowBorderless { get; set; }

        public static TankMusicSystem tankMusicHandler;

        public static bool InMission { get; set; } = false;

        public static Matrix UIMatrix => Matrix.CreateOrthographicOffCenter(0, TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, 0, -1, 1);

        internal static void Update()
        {
            if (InMission)
                tankMusicHandler.Update();

            foreach (var bind in Keybind.AllKeybinds)
                bind?.Update();

            foreach (var tank in AllPlayerTanks)
                tank.Update();
            foreach (var tank in AllAITanks)
                tank.Update();

            foreach (var mine in Mine.AllMines)
                mine?.Update();

            foreach (var bullet in Shell.AllShells)
                bullet?.Update();

            foreach (var cube in Cube.cubes)
                cube?.Update();

            FloatForTesting = MathHelper.Clamp(FloatForTesting, -1, 1);

            /*if (Input.MouseRight)
            {
                if (TankGame.GameUpdateTime % 5 == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var treadPlaceSfx = treadPlace.CreateInstance();
                    treadPlaceSfx.Play();
                    treadPlaceSfx.Volume = 0.2f;
                    treadPlaceSfx.Pitch = FloatForTesting;
                }
            }*/

            if (Input.KeyJustPressed(Keys.Insert))
                DebugUtils.DebuggingEnabled = !DebugUtils.DebuggingEnabled;

            if (Input.AreKeysJustPressed(Keys.RightAlt, Keys.Enter))
            {
                WindowBorderless = !WindowBorderless;
            }

            if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Left))
            {
                FloatForTesting -= 0.01f;
            }
            if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Right))
            {
                FloatForTesting += 0.01f;
            }

            if (Input.KeyJustPressed(Keys.Up))
                DebugUtils.DebugLevel++;
            if (Input.KeyJustPressed(Keys.Down))
                DebugUtils.DebugLevel--;

            if (timeUntilTankFunction > 0)
                timeUntilTankFunction--;
            else
            {
                if (!InMission)
                {
                    tankMusicHandler.LoadMusic();
                    InMission = true;
                }
            }

            if (Input.KeyJustPressed(Keys.PageUp))
                SpawnTankPlethorae();
        }
        internal static void Draw()
        {
            MapRenderer.DrawWorldModels();

            foreach (var tank in AllPlayerTanks)
               tank.DrawBody();
            foreach (var tank in AllAITanks)
                tank.DrawBody();

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

            // TODO: Fix translation
            // TODO: Scaling with screen size.

            foreach (var element in UIElement.AllUIElements) {
                //element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));

                element?.Draw(TankGame.spriteBatch);
            }

            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"TestFloat: {FloatForTesting}" +
                $"\nHighestTier: {AITank.GetHighestTierActive()}" +
                $"\n", new(10, GameUtils.WindowHeight / 3));

            for (int i = 0; i < AllAITanks.Count; i++)
            {
                var t = AllAITanks[i];
                DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{t}", new(10, 15 * i), 1);
            }

            for (int i = 0; i < Enum.GetNames<TankTier>().Length; i++)
            {
                DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{Enum.GetNames<TankTier>()[i]}: {AITank.GetTankCountOfType((TankTier)i)}", new(10, GameUtils.WindowHeight * 0.6f + (i * 20)));
            }

            if (TankGame.Instance.IsActive) {
                foreach (var element in UIElement.AllUIElements.ToList()) {
                    DebugUtils.DrawDebugString(TankGame.spriteBatch, element.Hitbox, new(200, 200));
                    DebugUtils.DrawDebugString(TankGame.spriteBatch, GameUtils.MousePosition, new(200, 250));
                    if (!element.MouseHovering && element.Hitbox.Contains(GameUtils.MousePosition)) {
                        element?.MouseOver();
                        element.MouseHovering = true;
                    }
                    else if (element.MouseHovering && !element.Hitbox.Contains(GameUtils.MousePosition)) {
                        element?.MouseLeave();
                        element.MouseHovering = false;
                    }
                    if (Input.MouseLeft && GameUtils.MouseOnScreenProtected && element != lastElementClicked) {
                        element?.MouseClick();
                        lastElementClicked = element;
                    }
                    if (Input.MouseRight && GameUtils.MouseOnScreenProtected && element != lastElementClicked) {
                        element?.MouseRightClick();
                        lastElementClicked = element;
                    }
                    if (Input.MouseMiddle && GameUtils.MouseOnScreenProtected && element != lastElementClicked) {
                        element?.MouseMiddleClick();
                        lastElementClicked = element;
                    }
                }
                if (!Input.MouseLeft && !Input.MouseRight && !Input.MouseMiddle) {
                    lastElementClicked = null;
                }
            }

            DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{Input.CurrentGamePadSnapshot.ThumbSticks.Left.X}\n{Input.CurrentGamePadSnapshot.ThumbSticks.Left.Y}", new Vector2(10, 10), 2);

            MouseRenderer.DrawMouse();
        }
        public static PlayerTank myTank;

        public static void Initialize()
        {
            GameShaders.Initialize();

            DebugUtils.DebuggingEnabled = true;
            MapRenderer.InitializeRenderers();

            new Cube(Vector3.Zero);

            tankMusicHandler = new();
            myTank = new PlayerTank(new Vector3(new Random().Next(-200, 201), 0, new Random().Next(-500, 600)), playerType: PlayerType.Blue);
            myTank.Team = Team.Green;

            //SpawnTankPlethorae();
            SpawnTank(TankTier.Black, Team.Red);
            SpawnTank(TankTier.Black, Team.Blue);

            IngameUI.Initialize();

            BeginIntroSequence();
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
            return new AITank(new Vector3(new Random().Next(-200, 201), 0, new Random().Next(-500, 600)), tier)
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
                var rot = GeometryUtils.GetPiRandom();
                var t = new AITank(new Vector3(new Random().Next(-200, 201), 0, new Random().Next(-500, 600)), AITank.PICK_ANY_THAT_ARE_IMPLEMENTED())
                {
                    TankRotation = rot,
                    TurretRotation = rot
                };
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
                TankGame.spriteBatch.Draw(MouseTexture, mousePos, null, Color.Blue, MathHelper.PiOver2 * i, MouseTexture.Size(), 1f, default, default);
            }

            /*
             * if pixel sampled at (x, y) is rgb(1, 1, 1) -- ignore
             * else, apply shader
             */
        }
    }
    public class GameShaders
    {
        public static Effect MouseShader { get; set; }

        public static void Initialize()
        {
            MouseShader = GameResources.GetGameResource<Effect>("Assets/Shaders/MouseShader");
        }

        public void UpdateShader()
        {
        }
    }
}
