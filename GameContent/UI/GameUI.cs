using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using System;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Net;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.Tanks;

namespace TanksRebirth.GameContent.UI;

// holy fucking shit. this class is beyond horrid. organize ts

#pragma warning disable 
public static class GameUI {
    public static bool InOptions { get; set; }

    public static Keybind Pause = new("Pause", Keys.Escape);

    public static UITextButton ResumeButton;
    public static UITextButton RestartButton;
    public static UITextButton OptionsButton;
    public static UITextButton QuitButton;

    public static UITextButton KeyboardPlayerButton;

    public static UITextButton VolumeButton;
    public static UITextButton GraphicsButton;
    public static UITextButton ControlsButton;
    public static UITextButton BackButton;

    public static UIElement[] menuElements;
    public static UIElement[] graphicsElements;

    public static bool Paused { get; set; } = false;

    static int _delay;

    static float _gpuSettingsOffset = 0f;

    // TODO: make rect scissor work -> get powerups to be pickupable
    static bool _initialized;

    internal static Vector2 QuitButtonSize = new(500, 150);
    internal static Vector2 OptionsButtonSize = new(500, 150);

    internal static Vector2 QuitButtonPos = new(700, 850);
    internal static Vector2 OptionsButtonPos = new(700, 600);

    internal static void Initialize() {
        if (_initialized) {
            foreach (var field in typeof(GameUI).GetFields()) {
                if (field.GetValue(null) is UIElement element) {
                    element.Remove();
                    field.SetValue(null, null);
                }
            }
        }
        _initialized = true;
        var ttColor = Color.LightGray;
        var font = FontGlobals.RebirthFont;

        ResumeButton = new(TankGame.GameLanguage.General.Resume, font, Color.WhiteSmoke) {
            IsVisible = false
        };
        ResumeButton.SetDimensions(() => new Vector2(700, 100).ToResolution(), () => new Vector2(500, 150).ToResolution());
        ResumeButton.OnLeftClick = (uiElement) => Pause.Fire();

        RestartButton = new(TankGame.GameLanguage.General.StartOver, font, Color.WhiteSmoke) {
            IsVisible = false,
        };
        RestartButton.SetDimensions(() => new Vector2(700, 350).ToResolution(), () => new Vector2(500, 150).ToResolution());

        OptionsButton = new(TankGame.GameLanguage.Menu.Options, font, Color.WhiteSmoke) {
            IsVisible = false
        };
        OptionsButton.SetDimensions(() => OptionsButtonPos.ToResolution(), () => OptionsButtonSize.ToResolution());
        OptionsButton.OnLeftClick = (uiElement) => {
            _delay = 1;
            InOptions = true;

            ResumeButton.IsVisible = false;
            RestartButton.IsVisible = false;
            QuitButton.IsVisible = false;
            OptionsButton.IsVisible = false;
            KeyboardPlayerButton.IsVisible = true;

            SetSettingsUIVisibility(true);

            MainMenuUI.MenuState = MainMenuUI.UIState.Settings;

            BackButton.Size.Y = 150;

            if (MainMenuUI.IsActive) {
                MainMenuUI.PlayButton.IsVisible = false;
            }
        };

        VolumeButton = new(TankGame.GameLanguage.Menu.Volume, font, Color.WhiteSmoke) {
            IsVisible = false
        };
        VolumeButton.SetDimensions(() => new Vector2(700, 100).ToResolution(), () => new Vector2(500, 150).ToResolution());
        VolumeButton.OnLeftClick = (uiElement) => {
            VolumeUI.BatchVisible = true;
            VolumeUI.ShowAll();
            VolumeUI.MusicVolume.IgnoreMouseInteractions = true;
            _delay = 1;
            VolumeButton.IsVisible = false;
            GraphicsButton.IsVisible = false;
            ControlsButton.IsVisible = false;
        };

        GraphicsButton = new(TankGame.GameLanguage.Menu.Graphics, font, Color.WhiteSmoke) {
            IsVisible = false
        };
        GraphicsButton.SetDimensions(() => new Vector2(700, 350).ToResolution(), () => new Vector2(500, 150).ToResolution());
        GraphicsButton.OnLeftClick = (uiElement) => {
            GraphicsUI.SetVisibility(true);
            GraphicsUI.VSyncBtn.IgnoreMouseInteractions = true;
            _delay = 1;
            VolumeButton.IsVisible = false;
            GraphicsButton.IsVisible = false;
            ControlsButton.IsVisible = false;
        };

        ControlsButton = new(TankGame.GameLanguage.Menu.Controls, font, Color.WhiteSmoke) {
            IsVisible = false
        };
        ControlsButton.SetDimensions(() => new Vector2(700, 600).ToResolution(), () => new Vector2(500, 150).ToResolution());
        ControlsButton.OnLeftClick = (uiElement) => {
            ControlsUI.BatchVisible = true;
            ControlsUI.ShowAll();
            VolumeButton.IsVisible = false;
            GraphicsButton.IsVisible = false;
            ControlsButton.IsVisible = false;
        };

        QuitButton = new(TankGame.GameLanguage.Menu.Quit, font, Color.WhiteSmoke) {
            IsVisible = false
        };
        QuitButton.SetDimensions(() => QuitButtonPos.ToResolution(), () => QuitButtonSize.ToResolution());
        QuitButton.OnLeftClick = (ui) => {
            QuitOut();
        };

        BackButton = new(TankGame.GameLanguage.Menu.Back, font, Color.WhiteSmoke) {
            IsVisible = false
        };
        BackButton.SetDimensions(() => new Vector2(700, 850).ToResolution(), () => new Vector2(500, 150).ToResolution());
        BackButton.OnLeftClick = (uiElement) => HandleBackButton();

        KeyboardPlayerButton = new($"{TankGame.GameLanguage.Settings.KeyboardPlayer}: {PlayerID.GetLocalizedPlayerColorName(PlayerTank.KbPlayer)}", font, Color.WhiteSmoke) {
            IsVisible = false
        };
        KeyboardPlayerButton.SetDimensions(() => new Vector2(WindowUtils.WindowWidth - 300.ToResolutionX(), 10.ToResolutionY()), () => new Vector2(250, 50).ToResolution());
        KeyboardPlayerButton.OnLeftClick = (ui) => {
            PlayerTank.KbPlayer++;
            if (PlayerTank.KbPlayer > InputUtils.NumGamepadsConnected)
                PlayerTank.KbPlayer = -1;

            KeyboardPlayerButton.Text = $"{TankGame.GameLanguage.Settings.KeyboardPlayer}: {PlayerID.GetLocalizedPlayerColorName(PlayerTank.KbPlayer)}";
        };

        // MainMenuUI.Initialize();

        GraphicsUI.Initialize();
        ControlsUI.Initialize();
        VolumeUI.Initialize();
        PostInitialize();
    }

    public static void SetSettingsUIVisibility(bool visible) {
        VolumeButton.IsVisible = visible;
        GraphicsButton.IsVisible = visible;
        ControlsButton.IsVisible = visible;
        BackButton.IsVisible = visible;
    }

    public static void QuitOut(bool netSent = false) {
        if (!MainMenuUI.IsActive) {
            // if a non-host client tries quitting, deny them
            if (!netSent && Client.IsConnected() && !Client.IsHost()) {
                ChatSystem.SendMessage("Only the host can quit the campaign!", Color.Red);
                SoundPlayer.SoundError();
                return;
            }
            if (!netSent) Client.SendQuit();

            foreach (var elem in MainMenuUI.campaignNames) elem?.Remove();
            MainMenuUI.campaignNames.Clear();

            // LOL: fuck it it's a funny bug, leave it in.
            IntermissionSystem.ShouldDrawBanner = false;
            IntermissionSystem.ShouldDrawBonusBanner = false;
            IntermissionSystem.BlackAlpha = 0f;
            IntermissionSystem.Alpha = 0f;

            IntermissionSystem.BonusLifeAnimator?.Restart();
            IntermissionSystem.IntermissionAnimator?.Restart();
            IntermissionSystem.BonusLifeAnimator?.Stop();
            IntermissionSystem.BonusLifeAnimator?.Stop();

            MainMenuUI.Open();
            if (LevelEditorUI.IsActive) {
                LevelEditorUI.Close(true);
                LevelEditorUI.IsEditing = false;
            }
        }
        else {
            TankGame.Quit();
        }
    }

    static void PostInitialize() {
        Pause.OnPress = () => {
            if (CampaignCompleteUI.IsViewingResults)
                return;
            if (InOptions) {
                HandleBackButton();
                return;
            }
            else if (!MainMenuUI.IsActive) {
                Paused = !Paused;

                if (CampaignGlobals.InMission) {
                    if (Paused)
                        TankMusicSystem.PauseAll();
                    else
                        TankMusicSystem.ResumeAll();
                }

                if (Paused)
                    IntermissionSystem.TryPauseAll();
                else
                    IntermissionSystem.TryResumeAll();
            }

            ResumeButton.IsVisible = Paused;
            RestartButton.IsVisible = Paused;
            QuitButton.IsVisible = Paused;
            OptionsButton.IsVisible = Paused;
        };

        menuElements =
        [
            ResumeButton,
            RestartButton,
            QuitButton,
            OptionsButton,
            VolumeButton,
            GraphicsButton,
            ControlsButton,
            BackButton,

            GraphicsUI.VSyncBtn,
            GraphicsUI.PPLButton,
            GraphicsUI.WinKindBtn,
            GraphicsUI.ResBtn,
            GraphicsUI.MenuGameplayBtn,
            GraphicsUI.FadeTracksBtn,
            VolumeUI.MusicVolume,
            VolumeUI.EffectsVolume,
            VolumeUI.AmbientVolume
        ];
        graphicsElements =
        [
            GraphicsUI.VSyncBtn,
            GraphicsUI.PPLButton,
            GraphicsUI.WinKindBtn,
            GraphicsUI.ResBtn
        ];
        foreach (UIElement button in graphicsElements) {
            // button.HasScissor = true;
            // button.Scissor = () => new(0, (int)(WindowUtils.WindowHeight * 0.05f), WindowUtils.WindowWidth, (int)(WindowUtils.WindowHeight * 0.7f));
            button.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect); };
        }
        foreach (var e in menuElements)
            e.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect); };

        // UIElement.ResizeAndRelocate();
    }

    // 7/7/25 - WHAT THE FUCK IS THIS SHIT.
    // TODO: pls get arctan to finish the rewrite
    // TODO: arctan is fucking ignoring me

    // todo: ui states/screens/panels/whatever
    // anti-sigma code
    static void HandleBackButton() {
        if (!_initialized)
            return;

        if (MainMenuUI.MenuState == MainMenuUI.UIState.Cosmetics) {
            MainMenuUI.MenuState = MainMenuUI.UIState.PlayList;
            CosmeticsUI.LeaveMenu();
        }
        if (MainMenuUI.MenuState == MainMenuUI.UIState.StatsMenu)
            MainMenuUI.MenuState = MainMenuUI.UIState.PrimaryMenu;

        // We are on the main menu and in the settings menu.
        if (MainMenuUI.MenuState == MainMenuUI.UIState.Settings && MainMenuUI.IsActive && VolumeButton.IsVisible) {
            // Set to main menu, we are going back to it after all.
            MainMenuUI.MenuState = MainMenuUI.UIState.PrimaryMenu;

            // Hide Options buttons and load MMenu buttons.
            MainMenuUI.PlayButton.IsVisible = true;
            OptionsButton.IsVisible = true;
            QuitButton.IsVisible = true;

            ResumeButton.IsVisible = false;
            RestartButton.IsVisible = false;
            BackButton.IsVisible = false;
            ControlsButton.IsVisible = false;
            GraphicsButton.IsVisible = false;
            VolumeButton.IsVisible = false;
            KeyboardPlayerButton.IsVisible = false;
        }


        if (VolumeButton.IsVisible && !MainMenuUI.IsActive) {
            ResumeButton.IsVisible = true;
            OptionsButton.IsVisible = true;
            RestartButton.IsVisible = true;
            QuitButton.IsVisible = true;

            BackButton.IsVisible = false;
            ControlsButton.IsVisible = false;
            GraphicsButton.IsVisible = false;
            VolumeButton.IsVisible = false;
            KeyboardPlayerButton.IsVisible = false;
        }
        else if (VolumeUI.BatchVisible) {
            VolumeUI.BatchVisible = false;
            VolumeUI.HideAll();
            VolumeButton.IsVisible = true;
            GraphicsButton.IsVisible = true;
            ControlsButton.IsVisible = true;
        }
        else if (GraphicsUI.IsVisible) {
            GraphicsUI.SetVisibility(false);
            VolumeButton.IsVisible = true;
            GraphicsButton.IsVisible = true;
            ControlsButton.IsVisible = true;

            KeyboardPlayerButton.IsVisible = true;

            if (TankGame.Settings.WindowKind == WindowKind.Windowed) {
                TankGame.Settings.ResWidth = GraphicsUI.CurrentRes.Key;
                TankGame.Settings.ResHeight = GraphicsUI.CurrentRes.Value;

                TankGame.Instance.Graphics.PreferredBackBufferWidth = TankGame.Settings.ResWidth;
                TankGame.Instance.Graphics.PreferredBackBufferHeight = TankGame.Settings.ResHeight;

                TankGame.Instance.Graphics.ApplyChanges();
            }

            // FIXME: acts weird
            // TankGame.Instance.CalculateProjection();
        }
        else if (ControlsUI.BatchVisible) {
            ControlsUI.BatchVisible = false;
            ControlsUI.HideAll();
            VolumeButton.IsVisible = true;
            GraphicsButton.IsVisible = true;
            ControlsButton.IsVisible = true;
        }
        else {
            // WHAT THE HELL IS THIS CODE????
            if (MainMenuUI.IsActive) {
                if (MainMenuUI.PlayButton.IsVisible)
                    return;
                if (MainMenuUI.campaignNames.Count > 0) {
                    foreach (var elem in MainMenuUI.campaignNames)
                        elem.Remove();
                    MainMenuUI.MenuState = MainMenuUI.UIState.PlayList;

                    MainMenuUI.campaignNames.Clear();
                }
                else if (MainMenuUI.PlayButton_SinglePlayer.IsVisible) {
                    MainMenuUI.PlayButton.IsVisible = true;
                    OptionsButton.IsVisible = true;
                    QuitButton.IsVisible = true;

                    BackButton.IsVisible = false;

                    MainMenuUI.MenuState = MainMenuUI.UIState.PrimaryMenu;
                }

                else if (GraphicsButton.IsVisible) {
                    BackButton.IsVisible = false;
                    VolumeButton.IsVisible = false;
                    GraphicsButton.IsVisible = false;
                    ControlsButton.IsVisible = false;
                    OptionsButton.IsVisible = true;
                    QuitButton.IsVisible = true;
                    GraphicsUI.IsVisible = false;
                    VolumeUI.BatchVisible = false;

                    MainMenuUI.MenuState = MainMenuUI.UIState.PrimaryMenu;
                }
                else if (MainMenuUI.ConnectToServerButton.IsVisible || MainMenuUI.DisconnectButton.IsVisible) {
                    MainMenuUI.MenuState = MainMenuUI.UIState.PlayList;
                }
                if (MainMenuUI.TanksAreCalculators.IsVisible) {
                    MainMenuUI.MenuState = MainMenuUI.UIState.PlayList;
                }
            }
            else {
                InOptions = false;
                VolumeUI.HideAll();
                BackButton.IsVisible = false;
                VolumeButton.IsVisible = false;
                GraphicsButton.IsVisible = false;
                ControlsButton.IsVisible = false;
            }
        }

        // UIElement.ResizeAndRelocate();
    }

    // i swear these are present elsewhere in the codebase

    public static void UpdateButtons() {
        if (!_initialized)
            return;

        TankGame.Settings.MusicVolume = VolumeUI.MusicVolume.Value;
        TankGame.Settings.EffectsVolume = VolumeUI.EffectsVolume.Value;
        TankGame.Settings.AmbientVolume = VolumeUI.AmbientVolume.Value;

        if (VolumeUI.MusicVolume.Value <= 0.01f)
            VolumeUI.MusicVolume.Value = 0f;

        if (VolumeUI.EffectsVolume.Value <= 0.01f)
            VolumeUI.EffectsVolume.Value = 0f;

        if (VolumeUI.AmbientVolume.Value <= 0.01f)
            VolumeUI.AmbientVolume.Value = 0f;

        if (!MainMenuUI.IsActive)
            TankMusicSystem.UpdateVolume();

        if (_delay > 0 && !InputUtils.MouseLeft)
            _delay--;
        if (_delay <= 0) {
            VolumeUI.MusicVolume.IgnoreMouseInteractions = false;
            GraphicsUI.VSyncBtn.IgnoreMouseInteractions = false;
        }
        VolumeUI.MusicVolume.Tooltip = $"{Math.Round(TankGame.Settings.MusicVolume * 100, 1)}%";
        VolumeUI.EffectsVolume.Tooltip = $"{Math.Round(TankGame.Settings.EffectsVolume * 100, 1)}%";
        VolumeUI.AmbientVolume.Tooltip = $"{Math.Round(TankGame.Settings.AmbientVolume * 100, 1)}%";
    }
}