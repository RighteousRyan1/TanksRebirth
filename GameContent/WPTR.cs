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

namespace WiiPlayTanksRemake.GameContent
{
    public class WPTR
    {
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);

        public static Logger BaseLogger { get; } = new($"{TankGame.ExePath}", "client_logger");

        private static UIElement lastElementClicked;

        public static bool WindowBorderless { get; set; }

        internal static void Update()
        {
            foreach (var bind in Keybind.AllKeybinds)
                bind?.Update();

            foreach (var music in Music.AllMusic)
                music?.Update();

            foreach (var tank in Tank.AllTanks)
                tank?.Update();

            foreach (var mine in Mine.AllMines)
                mine?.Update();

            foreach (var bullet in Bullet.AllBullets)
                bullet?.Update();

            if (Input.MouseLeft)
            {
                if (TankGame.GameUpdateTime.TotalGameTime.Ticks % 5 == 0)
                {
                    var treadPlace = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var treadPlaceSfx = treadPlace.CreateInstance();
                    treadPlaceSfx.Play();
                    treadPlaceSfx.Volume = 0.2f;
                    treadPlaceSfx.Pitch = -0.2f;
                }
            }

            if (Input.AreKeysJustPressed(Keys.LeftShift, Keys.Enter))
            {
                WindowBorderless = !WindowBorderless;
            }
        }

        internal static void Draw()
        {
            foreach (var tank in Tank.AllTanks)
                tank?.DrawBody();

            foreach (var mine in Mine.AllMines)
                mine?.Draw();

            foreach (var bullet in Bullet.AllBullets)
                bullet?.Draw();

            foreach (var parent in UIParent.TotalParents)
                parent?.DrawElements();

            if (TankGame.Instance.IsActive) {
                foreach (var parent in UIParent.TotalParents.ToList()) {
                    foreach (var element in parent.Elements) {
                        if (!element.MouseHovering && element.InteractionBox.ToRectangle().Contains(GameUtils.MousePosition)) {
                            element?.MouseOver();
                            element.MouseHovering = true;
                        }
                        else if (element.MouseHovering && !element.InteractionBox.ToRectangle().Contains(GameUtils.MousePosition)) {
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
                }
                if (!Input.MouseLeft && !Input.MouseRight && !Input.MouseMiddle) {
                    lastElementClicked = null;
                }
            }
        }

       
        public static void Initialize()
        {
            new Tank(new Vector3(0, 0, 0), playerType: PlayerType.Blue)
            {
                scale = 2
            };

            /*new Tank(new Vector3(100, 100, 0), true, TankTier.Bubblegum)
            {
                scale = 5
            };

            new Tank(new Vector3(-300, 300, 0), true, TankTier.Marble)
            {
                scale = 5,
                tankTreadPitch = -0.25f
            };*/

            // UI.PauseMenu.Initialize();
            MusicContent.LoadMusic();
            //MusicContent.green1.Play();
        }
    }
}
