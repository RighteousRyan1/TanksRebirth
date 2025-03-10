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
            UpKeybindButton = new("Up: " + PlayerTank.controlUp.Assigned.KeyAsString(), TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            UpKeybindButton.SetDimensions(() => new Vector2(550, 200).ToResolution(), () => new Vector2(300, 150).ToResolution());
            UpKeybindButton.OnLeftClick = (uiElement) =>
            {
                UpKeybindButton.Text = pressKey;
                PlayerTank.controlUp.OnReassign = (key) =>
                {
                    UpKeybindButton.Text = "Up: " + key.KeyAsString();
                    TankGame.Settings.UpKeybind = key;
                    PlayerTank.controlUp.OnReassign = null;
                };
                PlayerTank.controlUp.PendReassign = true;
            };

            LeftKeybindButton = new("Left: " + PlayerTank.controlLeft.Assigned.KeyAsString(), TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            LeftKeybindButton.SetDimensions(() => new Vector2(1050, 200).ToResolution(), () => new Vector2(300, 150).ToResolution());
            LeftKeybindButton.OnLeftClick = (uiElement) =>
            {
                LeftKeybindButton.Text = pressKey;
                PlayerTank.controlLeft.OnReassign = (key) =>
                {
                    LeftKeybindButton.Text = "Left: " + key.KeyAsString();
                    TankGame.Settings.LeftKeybind = key;
                    PlayerTank.controlLeft.OnReassign = null;
                };
                PlayerTank.controlLeft.PendReassign = true;
            };

            RightKeybindButton = new("Right: " + PlayerTank.controlRight.Assigned.KeyAsString(), TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            RightKeybindButton.SetDimensions(() => new Vector2(550, 400).ToResolution(), () => new Vector2(300, 150).ToResolution());
            RightKeybindButton.OnLeftClick = (uiElement) =>
            {
                RightKeybindButton.Text = pressKey;
                PlayerTank.controlRight.OnReassign = (key) =>
                {
                    RightKeybindButton.Text = "Right: " + key.KeyAsString();
                    TankGame.Settings.RightKeybind = key;
                    PlayerTank.controlRight.OnReassign = null;
                };
                PlayerTank.controlRight.PendReassign = true;
            };

            DownKeybindButton = new("Down: " + PlayerTank.controlDown.Assigned.KeyAsString(), TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            DownKeybindButton.SetDimensions(() => new Vector2(1050, 400).ToResolution(), () => new Vector2(300, 150).ToResolution());
            DownKeybindButton.OnLeftClick = (uiElement) =>
            {
                DownKeybindButton.Text = pressKey;
                PlayerTank.controlDown.OnReassign = (key) =>
                {
                    DownKeybindButton.Text = "Down: " + key.KeyAsString();
                    TankGame.Settings.DownKeybind = key;
                    PlayerTank.controlDown.OnReassign = null;
                };
                PlayerTank.controlDown.PendReassign = true;
            };

            MineKeybindButton = new("Mine: " + PlayerTank.controlMine.Assigned.KeyAsString(), TankGame.TextFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            MineKeybindButton.SetDimensions(() => new Vector2(800, 600).ToResolution(), () => new Vector2(300, 150).ToResolution());
            MineKeybindButton.OnLeftClick = (uiElement) =>
            {
                MineKeybindButton.Text = pressKey;
                PlayerTank.controlMine.OnReassign = (key) =>
                {
                    MineKeybindButton.Text = "Mine: " + key.KeyAsString();
                    TankGame.Settings.MineKeybind = key;
                    PlayerTank.controlMine.OnReassign = null;
                };
                PlayerTank.controlMine.PendReassign = true;
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