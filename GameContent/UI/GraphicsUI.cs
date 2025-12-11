using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI;

// rename?
public static class GraphicsUI {
    public static UITextButton PPLButton;
    public static UITextButton VSyncBtn;
    public static UITextButton WinKindBtn;
    public static UITextButton ResBtn;
    public static UITextButton FadeTracksBtn;

    public static UITextButton MenuGameplayBtn;
    static bool _initialized;

    static int _idxPair;

    public static KeyValuePair<int, int> CurrentRes = new(TankGame.Settings.ResWidth, TankGame.Settings.ResHeight);

    static KeyValuePair<int, int>[] _commonResolutions =
    [
        new(640, 480),
        new(1280, 720),
        new(1920, 1080),
        new(2560, 1440),
        new(3840, 2160),
        new(7680, 4320)
    ];

    public static bool IsVisible;

    public static void DrawBooleanIndicator(SpriteBatch spriteBatch, Rectangle hitbox, bool active) {
        spriteBatch.Draw(TextureGlobals.Pixels[Color.White], hitbox, active ? Color.Green : Color.Red);
    }

    // uninitialize after leaving the menu to save resources.... yes. please.
    public static void Initialize() {
        if (_initialized) {
            foreach (var field in typeof(GraphicsUI).GetFields()) {
                if (field.GetValue(null) is UIElement element) {
                    element.Remove();
                    field.SetValue(null, null);
                }
            }
        }
        _initialized = true;
        // Per-Pixel Lighting

        PPLButton = new(TankGame.GameLanguage.PerPxLight + ": " + TankGame.GameLanguage.GetEnablement(TankGame.Settings.PerPixelLighting), FontGlobals.RebirthFont, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.PerPxLightDesc
        };
        PPLButton.SetDimensions(() => new Vector2(240, 150).ToResolution(), () => new Vector2(400, 100).ToResolution());
        PPLButton.OnLeftClick = (uiElement) => {
            TankGame.Settings.PerPixelLighting = !TankGame.Settings.PerPixelLighting;
            PPLButton.Text = $"{TankGame.GameLanguage.PerPxLight}: {TankGame.GameLanguage.GetEnablement(TankGame.Settings.PerPixelLighting)}";
        };

        // Vsync
        VSyncBtn = new(TankGame.GameLanguage.VSync + ": " + TankGame.GameLanguage.GetEnablement(TankGame.Settings.Vsync), FontGlobals.RebirthFont, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.VSyncDesc
        };
        VSyncBtn.SetDimensions(() => new Vector2(240, 275).ToResolution(), () => new Vector2(400, 100).ToResolution());
        VSyncBtn.OnLeftClick = (uiElement) => {
            TankGame.Instance.Graphics.SynchronizeWithVerticalRetrace = TankGame.Settings.Vsync = !TankGame.Settings.Vsync;
            TankGame.Instance.Graphics.ApplyChanges();
            VSyncBtn.Text = TankGame.GameLanguage.VSync + ": " + TankGame.GameLanguage.GetEnablement(TankGame.Settings.Vsync);
        };

        WinKindBtn = new($"{TankGame.GameLanguage.WindowKind}: {StringUtils.SplitByCamel(Enum.GetName(TankGame.Settings.WindowKind)!)}", FontGlobals.RebirthFont, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.WindowKindDesc
        };
        WinKindBtn.SetDimensions(() => new Vector2(240, 400).ToResolution(), () => new Vector2(400, 100).ToResolution());
        WinKindBtn.OnLeftClick = (uiElement) => {
            if (TankGame.Settings.WindowKind == WindowKind.Fullscreen) {
                TankGame.Instance.Graphics.PreferredBackBufferHeight -= 50;
            }
            else {
                TankGame.Instance.Graphics.PreferredBackBufferHeight += 50;
            }
            TankGame.Settings.WindowKind++;
            if (TankGame.Settings.WindowKind > WindowKind.FullscreenBorderless) {
                TankGame.Settings.WindowKind = WindowKind.Windowed;
            }

            WinKindBtn.Text = $"{TankGame.GameLanguage.WindowKind}: {StringUtils.SplitByCamel(Enum.GetName(TankGame.Settings.WindowKind)!)}";
            WindowUtils.ChangeWindowKind(TankGame.Settings.WindowKind);
        };

        // Resolution
        ResBtn = new($"{TankGame.GameLanguage.Resolution}: {CurrentRes.Key}x{CurrentRes.Value}", FontGlobals.RebirthFont, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.ResolutionDesc
        };
        ResBtn.SetDimensions(() => new Vector2(240, 525).ToResolution(), () => new Vector2(400, 100).ToResolution());
        ResBtn.OnLeftClick = (uiElement) => {
            var tryFind = _commonResolutions.FirstOrDefault(x => x.Key == CurrentRes.Key);

            if (Array.IndexOf(_commonResolutions, tryFind) > -1)
                _idxPair = Array.IndexOf(_commonResolutions, tryFind);

            _idxPair++;

            if (_idxPair >= _commonResolutions.Length)
                _idxPair = 0;

            CurrentRes = _commonResolutions[_idxPair];

            ResBtn.Text = $"{TankGame.GameLanguage.Resolution}: {CurrentRes.Key}x{CurrentRes.Value}";
        };
        ResBtn.OnRightClick = (uiElement) => {
            var tryFind = _commonResolutions.FirstOrDefault(x => x.Key == CurrentRes.Key);

            if (Array.IndexOf(_commonResolutions, tryFind) > -1) {
                _idxPair = Array.IndexOf(_commonResolutions, tryFind);
            }

            _idxPair--;

            if (_idxPair < 0)
                _idxPair = _commonResolutions.Length - 1;

            CurrentRes = _commonResolutions[_idxPair];

            ResBtn.Text = $"{TankGame.GameLanguage.Resolution}: {CurrentRes.Key}x{CurrentRes.Value}";
        };

        FadeTracksBtn = new(TankGame.GameLanguage.FadeTracks + ": " + TankGame.GameLanguage.GetEnablement(TankGame.Settings.FadeFootprints), FontGlobals.RebirthFont, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.FadeTracksDesc
        };
        FadeTracksBtn.SetDimensions(() => new Vector2(240, 650).ToResolution(), () => new Vector2(400, 100).ToResolution());

        FadeTracksBtn.OnLeftClick = (uiElement) => {
            TankGame.Settings.FadeFootprints = !TankGame.Settings.FadeFootprints;
            TankFootprint.ShouldTracksFade = TankGame.Settings.FadeFootprints;
            FadeTracksBtn.Text = TankGame.GameLanguage.FadeTracks + ": " + TankGame.GameLanguage.GetEnablement(TankGame.Settings.FadeFootprints);
        };

        MenuGameplayBtn = new(TankGame.GameLanguage.MenuGameplay + ": " + TankGame.GameLanguage.GetEnablement(TankGame.Settings.MenuGameplayEnabled), FontGlobals.RebirthFont, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.MenuGameplayDesc
        };
        MenuGameplayBtn.SetDimensions(() => new Vector2(660, 150).ToResolution(), () => new Vector2(400, 100).ToResolution());

        MenuGameplayBtn.OnLeftClick = (uiElement) => {
            TankGame.Settings.MenuGameplayEnabled = !TankGame.Settings.MenuGameplayEnabled;
            MenuGameplayBtn.Text = TankGame.GameLanguage.MenuGameplay + ": " + TankGame.GameLanguage.GetEnablement(TankGame.Settings.MenuGameplayEnabled);

            if (!MainMenuUI.IsActive) return;
            foreach (var tank in GameHandler.AllTanks) {
                tank?.Remove(true);
            }
        };
    }

    public static void SetVisibility(bool visibility) {
        IsVisible = visibility;
        PPLButton.IsVisible = visibility;
        VSyncBtn.IsVisible = visibility;
        WinKindBtn.IsVisible = visibility;
        ResBtn.IsVisible = visibility;
        WinKindBtn.IsVisible = visibility;
        FadeTracksBtn.IsVisible = visibility;
        MenuGameplayBtn.IsVisible = visibility;
    }
}