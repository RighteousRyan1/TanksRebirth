using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI
{
    // ok are rendertargets just a death sentence???
    public static RenderTarget2D TextTarget;

    static readonly string tanksMessage = $"Tanks Rebirth ALPHA v{RuntimeData.ShortVersion}";
    // not always properly set, fix later
    // this code is becoming so shit i want to vomit but i don't know any better

    public enum UIState {
        /// <summary>The "Loading Mods" menu state.</summary>
        LoadingMods,
        /// <summary>The first menu the player sees after loading into the game.</summary>
        PrimaryMenu,
        /// <summary>Where all of the buttons for controling the game are.</summary>
        PlayList,
        /// <summary>Where the player's campaigns are.</summary>
        Campaigns,
        /// <summary>The multiplayer menu.</summary>
        Multiplayer,
        /// <summary>Where the player unlocks and manages their cosmetic items.</summary>
        Cosmetics,
        /// <summary>Where the player tweaks their gameplay experiences.</summary>
        Modifiers,
        /// <summary>Where the player changes their audio, graphics, and control settings.</summary>
        Settings,
        /// <summary>How the player views their all-time stats.</summary>
        StatsMenu,
        /// <summary>The menu where the player manages their mods.</summary>
        ModsMenu,
        /// <summary>The credits scene.</summary>
        Credits
    }
    static UIState _menuState;
    public static UIState MenuState {
        get => _menuState;
        set {
            _menuState = value;

            if (MenuGraphicsStates.ContainsKey(value)) {
                _goalBlur = MenuGraphicsStates[value].GaussianBlurFactor;
                CameraPositionAnimator = Animator.Create()
                    .WithFrame(new(position: CameraGlobals.RebirthFreecam.Position))
                    .WithFrame(new(position: MenuGraphicsStates[value].Position, duration: CameraTransitionTime, easing: CameraEasingFunction));
                CameraRotationAnimator = Animator.Create()
                    .WithFrame(new(position: CameraGlobals.RebirthFreecam.Rotation))
                    .WithFrame(new(position: MenuGraphicsStates[value].Rotation, duration: CameraTransitionTime, easing: CameraEasingFunction));
            }
            // if it doesn't have a proper camera position, just go to the regular one.
            else {
                _goalBlur = DEFAULT_BLUR;
                CameraPositionAnimator = Animator.Create()
                    .WithFrame(new(position: CameraGlobals.RebirthFreecam.Position))
                    .WithFrame(new(position: CamPosMain, duration: CameraTransitionTime, easing: CameraEasingFunction));
                CameraRotationAnimator = Animator.Create()
                    .WithFrame(new(position: CameraGlobals.RebirthFreecam.Rotation))
                    .WithFrame(new(position: CamPosMainRotation, duration: CameraTransitionTime, easing: CameraEasingFunction));
            }
            CameraPositionAnimator.Restart();
            CameraPositionAnimator.Run();
            CameraRotationAnimator.Restart();
            CameraRotationAnimator.Run();
        }
    }
    public static UITextButton PlayButton;
    public static UITextButton PlayButton_SinglePlayer;
    public static UITextButton PlayButton_LevelEditor;
    public static UITextButton PlayButton_Multiplayer;
    public static UITextButton StartMPGameButton;
    public static UITextButton DifficultiesButton;
    
    static UIElement[] _menuElements;

    internal static List<UIElement> campaignNames = [];

    public static UITextButton CosmeticsMenuButton;
    public static UITextButton StatsMenu;
    public static UITextButton CreditsButton;

    public static void InitializeMain(SpriteFontBase font) {
        PlayButton = new(TankGame.GameLanguage.Menu.Play, font, Color.WhiteSmoke) {
            IsVisible = true,
        };
        PlayButton.SetDimensions(() => new Vector2(700, 550).ToResolution(), () => new Vector2(500, 50).ToResolution());
        PlayButton.OnLeftClick = (uiElement) => {
            GameUI.BackButton.IsVisible = true;
            MenuState = UIState.PlayList;
        };

        PlayButton_Multiplayer = new(TankGame.GameLanguage.Menu.Multiplayer, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.Menu.MultiplayerFlavor
        };
        PlayButton_Multiplayer.SetDimensions(() => new Vector2(700, 750).ToResolution(), () => new Vector2(500, 50).ToResolution());

        PlayButton_Multiplayer.OnLeftClick = (uiElement) => {
            SetPlayButtonsVisibility(false);
            SetMPButtonsVisibility(true);
            MenuState = UIState.Multiplayer;
        };

        DifficultiesButton = new(TankGame.GameLanguage.Menu.Difficulties, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.Menu.DifficultiesFlavor
        };
        DifficultiesButton.SetDimensions(() => new Vector2(700, 550).ToResolution(), () => new Vector2(500, 50).ToResolution());
        DifficultiesButton.OnLeftClick = (element) => {
            ArrangeDifficultyButtons();
            MenuState = UIState.Modifiers;
        };

        PlayButton_SinglePlayer = new(TankGame.GameLanguage.Menu.SinglePlayer, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.Menu.SinglePlayerFlavor
        };
        PlayButton_SinglePlayer.SetDimensions(() => new Vector2(700, 450).ToResolution(), () => new Vector2(500, 50).ToResolution());

        PlayButton_SinglePlayer.OnLeftClick = (uiElement) => {
            SetCampaignDisplay();
            MenuState = UIState.Campaigns;
        };
        InitializeDifficultyButtons();

        PlayButton_LevelEditor = new(TankGame.GameLanguage.Menu.LevelEditor, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.Menu.LevelEditFlavor
        };
        PlayButton_LevelEditor.SetDimensions(() => new Vector2(700, 650).ToResolution(), () => new Vector2(500, 50).ToResolution());
        PlayButton_LevelEditor.OnLeftClick = (b) => {
            LevelEditorUI.Initialize();
            LevelEditorUI.TryOpen();
        };
        CosmeticsMenuButton = new(TankGame.GameLanguage.Menu.CosmeticsMenu, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.Menu.CosmeticsMenu,
            OnLeftClick = (a) => {
                CosmeticsUI.EnterMenu();

                CosmeticsMenuButton.IsVisible = false;
                SetPlayButtonsVisibility(false);
                SetMPButtonsVisibility(false);
                SetPrimaryMenuButtonsVisibility(false);

                MenuState = UIState.Cosmetics;
            }
        };
        CosmeticsMenuButton.SetDimensions(() => new Vector2(50, 50).ToResolution(), () => new Vector2(300, 50).ToResolution());

        StatsMenu = new(TankGame.GameLanguage.Menu.GameStats, font, Color.WhiteSmoke) {
            IsVisible = false,
            OnLeftClick = (a) => {
                RequestStats();
                MenuState = UIState.StatsMenu; 
            },
            Tooltip = TankGame.GameLanguage.Menu.GameStatsFlavor
        };
        StatsMenu.SetDimensions(() => new Vector2(WindowUtils.WindowWidth / 2 - 200.ToResolutionX(), WindowUtils.WindowHeight - 100.ToResolutionY()), () => new Vector2(180, 50).ToResolution());

        CreditsButton = new(TankGame.GameLanguage.Credits.Credits, font, Color.WhiteSmoke) {
            IsVisible = false,
            OnLeftClick = (a) => {
                CreditsHandler.Load(TankGame.GameLanguage);
                MenuState = UIState.Credits;
            },
            // Tooltip = TankGame.GameLanguage.Menu.GameStatsFlavor
        };
        CreditsButton.SetDimensions(() => new Vector2(WindowUtils.WindowWidth / 2 + 20.ToResolutionX(), WindowUtils.WindowHeight - 100.ToResolutionY()), () => new Vector2(180, 50).ToResolution());

        InitModsMenu(font);
    }
    private static void HideAll() {
        PlayButton.IsVisible = false;
        PlayButton_SinglePlayer.IsVisible = false;
        PlayButton_Multiplayer.IsVisible = false;
        PlayButton_LevelEditor.IsVisible = false;

        GameUI.BackButton.IsVisible = false;
    }
    internal static void SetPlayButtonsVisibility(bool visible) {
        PlayButton_SinglePlayer.IsVisible = visible;
        PlayButton_LevelEditor.IsVisible = visible;
        PlayButton_Multiplayer.IsVisible = visible;
        DifficultiesButton.IsVisible = visible;
        CosmeticsMenuButton.IsVisible = visible;
    }
    internal static void SetPrimaryMenuButtonsVisibility(bool visible) {
        GameUI.OptionsButton.IsVisible = visible;
        GameUI.QuitButton.IsVisible = visible;
        GameUI.BackButton.Size.Y = 50;

        PlayButton.IsVisible = visible;
        ModsMenuButton.IsVisible = visible;
        StatsMenu.IsVisible = visible;
        CreditsButton.IsVisible = visible;
    }

    // the code is horrid but at least it's separated now
    // TODO: make rainbow work, ask lolxd or someone knowledgeable
    public static void PrepareTextBuffers(GraphicsDevice device, SpriteBatch spriteBatch) {
        RenderGlobals.EnsureRenderTargetOK(ref TextTarget, device, (int)500.ToResolutionX(), (int)300.ToResolutionY());

        device.SetRenderTarget(TextTarget);

        device.Clear(RenderGlobals.BackBufferColor);

        if (MenuState == UIState.PrimaryMenu) {

            var bottomLeft = Anchor.BottomLeft.GetTextureAnchor(TextTarget);
            var messageScale = new Vector2(0.8f).ToResolution();
            var font = FontGlobals.RebirthFont;

            spriteBatch.Begin();
            DrawUtils.DrawStringBorderOnly(spriteBatch, font, tanksMessage, bottomLeft, Color.Black, messageScale, 0f, Anchor.BottomLeft, borderThickness: 0.75f);
            spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.DrawString(font, tanksMessage, bottomLeft, Color.White, messageScale, origin: Anchor.BottomLeft.GetAnchor(font.MeasureString(tanksMessage)));
            spriteBatch.End();
        }

        device.SetRenderTarget(null);
    }
    public static void RenderGeneralUI(SpriteBatch spriteBatch) {

        // this is scary to have here... oh well.

        if (MenuState != UIState.ModsMenu) {
            spriteBatch.End();
            // uhhhhhhh.
            GameShaders.AnimatedRainbow.Parameters["oMinLum"].SetValue(0.5f);
            spriteBatch.Begin(effect: GameShaders.AnimatedRainbow);
            spriteBatch.Draw(TextTarget, new Vector2(10, WindowUtils.WindowHeight - 10), null, Color.White, 0f, Anchor.BottomLeft.GetTextureAnchor(TextTarget), 1f, default, 0f);
            spriteBatch.End();

            spriteBatch.Begin();
        }

        if (MenuState is UIState.PrimaryMenu or UIState.PlayList) {
            var size = FontGlobals.RebirthFont.MeasureString(TankGame.Instance.MOTD);
            var motdPos = new Vector2(WindowUtils.WindowWidth / 2, 10);
            spriteBatch.DrawString(FontGlobals.RebirthFont, TankGame.Instance.MOTD, motdPos, Color.White, Vector2.One * 0.5f, 0f, Anchor.TopCenter.GetAnchor(size));
        }
    }
    public static void UpdateUI() {
        // quite unfortunate hardcode. fix later.
        if (InputUtils.KeyJustPressed(Keys.Escape)) {
            switch (MenuState) {
                case UIState.StatsMenu:
                    MenuState = UIState.PrimaryMenu;
                    break;
                case UIState.Credits:
                    MenuState = UIState.PrimaryMenu;
                    CreditsHandler.Unload();
                    break;
            }
        }
        // todo: do transitions
        SetPlayButtonsVisibility(MenuState == UIState.PlayList);
        SetMPButtonsVisibility(MenuState == UIState.Multiplayer);
        SetPrimaryMenuButtonsVisibility(MenuState == UIState.PrimaryMenu);
        SetDifficultiesButtonsVisibility(MenuState == UIState.Modifiers);

    }
    public static void OpenUI() {
        if (!Speedrun.AreSpeedrunsFetched) {
            Speedrun.AreSpeedrunsFetched = true;
            Speedrun.GetSpeedruns();
        }

        plrsConfirmed = 0;
        _musicFading = false;
        MenuState = UIState.PrimaryMenu;
        IsActive = true;
        GameUI.Paused = false;
        CameraGlobals.OverheadView = false;
        CameraGlobals.OrthoRotationVector.Y = CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        CameraGlobals.AddativeZoom = 1f;
        CameraGlobals.CameraFocusOffset.Y = 0f;

        // this manipulates the "back" button to be properly shown in the main menu
        // this will obviously be nuked during the UI rework

        GameUI.QuitButtonPos.Y -= 50;
        GameUI.OptionsButtonPos.Y += 75;

        SetPrimaryMenuButtonsVisibility(true);
        SetPlayButtonsVisibility(false);
        SetMPButtonsVisibility(false);

        GameUI.ResumeButton.IsVisible = false;
        GameUI.RestartButton.IsVisible = false;
        GameUI.QuitButtonSize.Y = 50;
        GameUI.OptionsButtonSize.Y = 50;
        GameUI.QuitButton.IsVisible = true;
        GameUI.OptionsButton.IsVisible = true;
        GameUI.KeyboardPlayerButton.IsVisible = false;
    }
    public static void LeaveUI() {
        SetMPButtonsVisibility(false);
        SetPlayButtonsVisibility(false);
        SetPrimaryMenuButtonsVisibility(false);
        IsActive = false;
        GraphicsUI.IsVisible = false;
        ControlsUI.BatchVisible = false;
        VolumeUI.BatchVisible = false;
        GameUI.InOptions = false;
        GameUI.OptionsButtonSize.Y = 150;
        GameUI.QuitButtonSize.Y = 150;
        GameUI.QuitButtonPos.Y += 50;
        GameUI.OptionsButtonPos.Y -= 75;
        HideAll();

        // invoked last.
        OnMenuClose?.Invoke();
    }
}