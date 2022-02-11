using Microsoft.Xna.Framework;
using System;
using WiiPlayTanksRemake.Internals.Common.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.GameUI;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class ControlsUI
    {
        public static UITextButton UpKeybindButton;

        public static UITextButton LeftKeybindButton;

        public static UITextButton RightKeybindButton;

        public static UITextButton DownKeybindButton;

        public static UITextButton MineKeybindButton;

        public static bool BatchVisible { get; set; }

        public static void Initialize()
        {
            var pressKey = TankGame.GameLanguage.PressAKey;
            UpKeybindButton = new("Up: " + PlayerTank.controlUp.AssignedKey.ParseKey(), TankGame.TextFont, Color.WhiteSmoke)
            {
                Visible = false
            };
            UpKeybindButton.SetDimensions(550, 200, 300, 150);
            UpKeybindButton.OnLeftClick = (uiElement) =>
            {
                UpKeybindButton.Text = pressKey;
                PlayerTank.controlUp.OnKeyReassigned = (key) =>
                {
                    UpKeybindButton.Text = "Up: " + key.ParseKey();
                    TankGame.Settings.UpKeybind = key;
                    PlayerTank.controlUp.OnKeyReassigned = null;
                };
                PlayerTank.controlUp.PendKeyReassign = true;
            };

            LeftKeybindButton = new("Left: " + PlayerTank.controlLeft.AssignedKey.ParseKey(), TankGame.TextFont, Color.WhiteSmoke)
            {
                Visible = false
            };
            LeftKeybindButton.SetDimensions(1050, 200, 300, 150);
            LeftKeybindButton.OnLeftClick = (uiElement) =>
            {
                LeftKeybindButton.Text = pressKey;
                PlayerTank.controlLeft.OnKeyReassigned = (key) =>
                {
                    LeftKeybindButton.Text = "Left: " + key.ParseKey();
                    TankGame.Settings.LeftKeybind = key;
                    PlayerTank.controlLeft.OnKeyReassigned = null;
                };
                PlayerTank.controlLeft.PendKeyReassign = true;
            };

            RightKeybindButton = new("Right: " + PlayerTank.controlRight.AssignedKey.ParseKey(), TankGame.TextFont, Color.WhiteSmoke)
            {
                Visible = false
            };
            RightKeybindButton.SetDimensions(550, 400, 300, 150);
            RightKeybindButton.OnLeftClick = (uiElement) =>
            {
                RightKeybindButton.Text = pressKey;
                PlayerTank.controlRight.OnKeyReassigned = (key) =>
                {
                    RightKeybindButton.Text = "Right: " + key.ParseKey();
                    TankGame.Settings.RightKeybind = key;
                    PlayerTank.controlRight.OnKeyReassigned = null;
                };
                PlayerTank.controlRight.PendKeyReassign = true;
            };

            DownKeybindButton = new("Down: " + PlayerTank.controlDown.AssignedKey.ParseKey(), TankGame.TextFont, Color.WhiteSmoke)
            {
                Visible = false
            };
            DownKeybindButton.SetDimensions(1050, 400, 300, 150);
            DownKeybindButton.OnLeftClick = (uiElement) =>
            {
                DownKeybindButton.Text = pressKey;
                PlayerTank.controlDown.OnKeyReassigned = (key) =>
                {
                    DownKeybindButton.Text = "Down: " + key.ParseKey();
                    TankGame.Settings.DownKeybind = key;
                    PlayerTank.controlDown.OnKeyReassigned = null;
                };
                PlayerTank.controlDown.PendKeyReassign = true;
            };

            MineKeybindButton = new("Mine: " + PlayerTank.controlMine.AssignedKey.ParseKey(), TankGame.TextFont, Color.WhiteSmoke)
            {
                Visible = false
            };
            MineKeybindButton.SetDimensions(800, 600, 300, 150);
            MineKeybindButton.OnLeftClick = (uiElement) =>
            {
                MineKeybindButton.Text = pressKey;
                PlayerTank.controlMine.OnKeyReassigned = (key) =>
                {
                    MineKeybindButton.Text = "Mine: " + key.ParseKey();
                    TankGame.Settings.MineKeybind = key;
                    PlayerTank.controlMine.OnKeyReassigned = null;
                };
                PlayerTank.controlMine.PendKeyReassign = true;
            };
        }

        public static void HideAll()
        {
            UpKeybindButton.Visible = false;
            LeftKeybindButton.Visible = false;
            RightKeybindButton.Visible = false;
            DownKeybindButton.Visible = false;
            MineKeybindButton.Visible = false;
        }

        public static void ShowAll()
        {
            UpKeybindButton.Visible = true;
            LeftKeybindButton.Visible = true;
            RightKeybindButton.Visible = true;
            DownKeybindButton.Visible = true;
            MineKeybindButton.Visible = true;
        }
    }
}