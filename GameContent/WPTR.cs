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

namespace WiiPlayTanksRemake.GameContent
{
    public class WPTR
    {
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);

        public static Logger BaseLogger { get; } = new($"{TankGame.ExePath}", "client_logger");

        private static UIElement lastElementClicked;

        private static UIParent parent;

        public static UITextButton panel;

        internal static void Update()
        {
            foreach (var music in Music.AllMusic)
                music?.Update();

            foreach (var tank in Tank.AllTanks)
                tank?.Update();

            foreach (var mine in Mine.AllMines)
                mine?.Update();

            foreach (var bullet in Bullet.AllBullets)
                bullet?.Update();

            if (TankGame.Instance.IsActive)
            {
                if (Input.MouseLeft && GameUtils.MouseOnScreenProtected)
                {
                    //shoot
                }
                if (!TextInput.trackingInput) {
                    TextInput.BeginInput();
                }
            }
        }

        internal static void Draw()
        {
            panel.Text = TextInput.InputtedText;
            foreach (var tank in Tank.AllTanks)
                tank?.Draw();

            foreach (var mine in Mine.AllMines)
                mine?.Draw();

            foreach (var bullet in Bullet.AllBullets)
                bullet?.Draw();

            foreach (var parent in UIParent.TotalParents)
                parent?.DrawElements();

            if (TankGame.Instance.IsActive) {
                foreach (var parent in UIParent.TotalParents.ToList()) {
                    foreach (var element in parent.Elements) {
                        if (!element.MouseHovering && element.InteractionBox.Contains(GameUtils.MousePosition)) {
                            element?.MouseOver();
                            element.MouseHovering = true;
                        }
                        else if (element.MouseHovering && !element.InteractionBox.Contains(GameUtils.MousePosition)) {
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
        private static void InitIngameMenu()
        {
            parent = new();
            panel = new("Resume", TankGame.Fonts.Font, Color.White, Color.Blue);
            panel.InteractionBox = new(100, 100, 200, 200);
            parent.AppendElement(panel);
        }

        public static void Initialize()
        {
            InitIngameMenu();
            MusicContent.LoadMusic();
            MusicContent.green1.Play();
        }
    }
}
