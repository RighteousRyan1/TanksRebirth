using Microsoft.Xna.Framework;
using System;
using TanksRebirth.GameContent.Globals;
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
            UpKeybindButton = new("Up: " + PlayerTank.MoveUp.Assigned.KeyAsString(), FontGlobals.RebirthFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            UpKeybindButton.SetDimensions(() => new Vector2(550, 200).ToResolution(), () => new Vector2(300, 150).ToResolution());
            UpKeybindButton.OnLeftClick = (uiElement) =>
            {
                UpKeybindButton.Text = pressKey;
                PlayerTank.MoveUp.OnReassign = (key) =>
                {
                    UpKeybindButton.Text = "Up: " + key.KeyAsString();
                    TankGame.Settings.UpKeybind = key;
                    PlayerTank.MoveUp.OnReassign = null;
                };
                PlayerTank.MoveUp.PendReassign = true;
            };

            LeftKeybindButton = new("Left: " + PlayerTank.MoveLeft.Assigned.KeyAsString(), FontGlobals.RebirthFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            LeftKeybindButton.SetDimensions(() => new Vector2(1050, 200).ToResolution(), () => new Vector2(300, 150).ToResolution());
            LeftKeybindButton.OnLeftClick = (uiElement) =>
            {
                LeftKeybindButton.Text = pressKey;
                PlayerTank.MoveLeft.OnReassign = (key) =>
                {
                    LeftKeybindButton.Text = "Left: " + key.KeyAsString();
                    TankGame.Settings.LeftKeybind = key;
                    PlayerTank.MoveLeft.OnReassign = null;
                };
                PlayerTank.MoveLeft.PendReassign = true;
            };

            RightKeybindButton = new("Right: " + PlayerTank.MoveRight.Assigned.KeyAsString(), FontGlobals.RebirthFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            RightKeybindButton.SetDimensions(() => new Vector2(550, 400).ToResolution(), () => new Vector2(300, 150).ToResolution());
            RightKeybindButton.OnLeftClick = (uiElement) =>
            {
                RightKeybindButton.Text = pressKey;
                PlayerTank.MoveRight.OnReassign = (key) =>
                {
                    RightKeybindButton.Text = "Right: " + key.KeyAsString();
                    TankGame.Settings.RightKeybind = key;
                    PlayerTank.MoveRight.OnReassign = null;
                };
                PlayerTank.MoveRight.PendReassign = true;
            };

            DownKeybindButton = new("Down: " + PlayerTank.MoveDown.Assigned.KeyAsString(), FontGlobals.RebirthFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            DownKeybindButton.SetDimensions(() => new Vector2(1050, 400).ToResolution(), () => new Vector2(300, 150).ToResolution());
            DownKeybindButton.OnLeftClick = (uiElement) =>
            {
                DownKeybindButton.Text = pressKey;
                PlayerTank.MoveDown.OnReassign = (key) =>
                {
                    DownKeybindButton.Text = "Down: " + key.KeyAsString();
                    TankGame.Settings.DownKeybind = key;
                    PlayerTank.MoveDown.OnReassign = null;
                };
                PlayerTank.MoveDown.PendReassign = true;
            };

            MineKeybindButton = new("Mine: " + PlayerTank.PlaceMine.Assigned.KeyAsString(), FontGlobals.RebirthFont, Color.WhiteSmoke)
            {
                IsVisible = false
            };
            MineKeybindButton.SetDimensions(() => new Vector2(800, 600).ToResolution(), () => new Vector2(300, 150).ToResolution());
            MineKeybindButton.OnLeftClick = (uiElement) =>
            {
                MineKeybindButton.Text = pressKey;
                PlayerTank.PlaceMine.OnReassign = (key) =>
                {
                    MineKeybindButton.Text = "Mine: " + key.KeyAsString();
                    TankGame.Settings.MineKeybind = key;
                    PlayerTank.PlaceMine.OnReassign = null;
                };
                PlayerTank.PlaceMine.PendReassign = true;
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