using Microsoft.Xna.Framework;
using System;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI
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
                IsVisible = false
            };
            UpKeybindButton.SetDimensions(() => new Vector2(550, 200).ToResolution(), () => new Vector2(300, 150).ToResolution());
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
                IsVisible = false
            };
            LeftKeybindButton.SetDimensions(() => new Vector2(1050, 200).ToResolution(), () => new Vector2(300, 150).ToResolution());
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
                IsVisible = false
            };
            RightKeybindButton.SetDimensions(() => new Vector2(550, 400).ToResolution(), () => new Vector2(300, 150).ToResolution());
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
                IsVisible = false
            };
            DownKeybindButton.SetDimensions(() => new Vector2(1050, 400).ToResolution(), () => new Vector2(300, 150).ToResolution());
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
                IsVisible = false
            };
            MineKeybindButton.SetDimensions(() => new Vector2(800, 600).ToResolution(), () => new Vector2(300, 150).ToResolution());
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
            UpKeybindButton.IsVisible = false;
            LeftKeybindButton.IsVisible = false;
            RightKeybindButton.IsVisible = false;
            DownKeybindButton.IsVisible = false;
            MineKeybindButton.IsVisible = false;
        }

        public static void ShowAll()
        {
            UpKeybindButton.IsVisible = true;
            LeftKeybindButton.IsVisible = true;
            RightKeybindButton.IsVisible = true;
            DownKeybindButton.IsVisible = true;
            MineKeybindButton.IsVisible = true;
        }
    }
}