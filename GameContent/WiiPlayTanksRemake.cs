using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.UI;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;

namespace WiiPlayTanksRemake.GameContent
{
    class WiiPlayTanksRemake
    {
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);

        public static Logger BaseLogger { get; } = new($"{TankGame.ExePath}", "client_logger");

        private static UIElement lastElementClicked;

        internal static void Update()
        {
            foreach (var music in Music.AllMusic)
                music?.Update();
            foreach (var tank in Tank.AllTanks)
            {
                tank?.Update();
                foreach (Mine mine in tank.minesLaid)
                    mine?.Update();
                foreach (Bullet bullet in tank.bulletsFired)
                    bullet?.Update();
            }
            if (TankGame.Instance.IsActive)
            {
                if (Input.MouseLeft && GameUtils.MouseOnScreenProtected)
                {
                    //shoot
                }
            }
        }

        internal static void Draw()
        {
            UIParent parente = new();
            UIText text = new("Test", TankGame.Fonts.Font, Color.White, 2f);
            parente.AppendElement(text);
            foreach (var tank in Tank.AllTanks)
            {
                tank?.Draw();
                foreach (Mine mine in tank.minesLaid)
                    mine?.Draw();
                foreach (Bullet bullet in tank.bulletsFired)
                    bullet?.Draw();
            }
            foreach (var parent in UIParent.TotalParents)
                parent?.DrawElements();
            if (TankGame.Instance.IsActive) {
                if (Input.MouseLeft && GameUtils.MouseOnScreenProtected) {
                    foreach (var parent in UIParent.TotalParents) {
                        foreach (var element in parent.Elements.FindAll(el => el.InteractionBox.Contains(GameUtils.MousePosition) && el != lastElementClicked)) {
                            element?.MouseClick();
                            lastElementClicked = element;
                        }
                    }
                }
                if (Input.MouseRight && GameUtils.MouseOnScreenProtected) {
                    foreach (var parent in UIParent.TotalParents) {
                        foreach (var element in parent.Elements.FindAll(el => el.InteractionBox.Contains(GameUtils.MousePosition) && el != lastElementClicked)) {
                            element?.MouseRightClick();
                            lastElementClicked = element;
                        }
                    }
                }
                if (Input.MouseMiddle && GameUtils.MouseOnScreenProtected) {
                    foreach (var parent in UIParent.TotalParents) {
                        foreach (var element in parent.Elements.FindAll(el => el.InteractionBox.Contains(GameUtils.MousePosition) && el != lastElementClicked)) {
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
    }
}
