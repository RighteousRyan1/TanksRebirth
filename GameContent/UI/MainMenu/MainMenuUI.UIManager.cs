using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.GameContent.Systems;
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

    private static readonly string tanksMessage =
    $"Tanks Rebirth ALPHA v{RuntimeData.GameVersion}" +
    $"\nOriginal game and assets developed by Nintendo" +
    $"\nProgrammed by RighteousRyan" +
    $"\nArt and graphics by BigKitty1011" +
    $"\nTANKS to all our contributors!";
    // not always properly set, fix later
    // this code is becoming so shit i want to vomit but i don't know any better
    public enum UIState {
        LoadingMods,
        PrimaryMenu,
        PlayList,
        Campaigns,
        Mulitplayer,
        Cosmetics,
        Difficulties,
        Settings,
        StatsMenu
    }

    private static UIState _menuState;
    public static UIState MenuState {
        get => _menuState;
        set {
            _menuState = value;

            if (MenuCameraManipulations.ContainsKey(value)) {
                CameraPositionAnimator = Animator.Create()
                    .WithFrame(new(position3d: CameraGlobals.RebirthFreecam.Position, duration: CameraTransitionTime, easing: CameraEasingFunction))
                    .WithFrame(new(position3d: MenuCameraManipulations[value].Position));
                CameraRotationAnimator = Animator.Create()
                    .WithFrame(new(position3d: CameraGlobals.RebirthFreecam.Rotation, duration: CameraTransitionTime, easing: CameraEasingFunction))
                    .WithFrame(new(position3d: MenuCameraManipulations[value].Rotation));
            }
            // if it doesn't have a proper camera position, just go to the regular one.
            else {
                CameraPositionAnimator = Animator.Create()
                    .WithFrame(new(position3d: CameraGlobals.RebirthFreecam.Position, duration: CameraTransitionTime, easing: CameraEasingFunction))
                    .WithFrame(new(position3d: CamPosMain));
                CameraRotationAnimator = Animator.Create()
                    .WithFrame(new(position3d: CameraGlobals.RebirthFreecam.Rotation, duration: CameraTransitionTime, easing: CameraEasingFunction))
                    .WithFrame(new(position3d: CamPosMainRotation));
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

    private static UIElement[] _menuElements;

    internal static List<UIElement> campaignNames = [];

    public static UITextButton CosmeticsMenuButton;
    public static UITextButton StatsMenu;

    public static void InitializeMain(SpriteFontBase font) {
        var presentationParams = TankGame.Instance.GraphicsDevice.PresentationParameters;
        TextTarget = new(TankGame.Instance.GraphicsDevice, 420, 140, false, 
            presentationParams.BackBufferFormat, presentationParams.DepthStencilFormat, 0, RenderTargetUsage.DiscardContents);
        
        PlayButton = new(TankGame.GameLanguage.Play, font, Color.WhiteSmoke) {
            IsVisible = true,
        };
        PlayButton.SetDimensions(() => new Vector2(700, 550).ToResolution(), () => new Vector2(500, 50).ToResolution());
        PlayButton.OnLeftClick = (uiElement) => {
            GameUI.BackButton.IsVisible = true;
            MenuState = UIState.PlayList;
        };

        PlayButton_Multiplayer = new(TankGame.GameLanguage.Multiplayer, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.MultiplayerFlavor
        };
        PlayButton_Multiplayer.SetDimensions(() => new Vector2(700, 750).ToResolution(), () => new Vector2(500, 50).ToResolution());

        PlayButton_Multiplayer.OnLeftClick = (uiElement) => {
            SetPlayButtonsVisibility(false);
            SetMPButtonsVisibility(true);
            MenuState = UIState.Mulitplayer;
        };

        DifficultiesButton = new(TankGame.GameLanguage.Difficulties, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.DifficultiesFlavor
        };
        DifficultiesButton.SetDimensions(() => new Vector2(700, 550).ToResolution(), () => new Vector2(500, 50).ToResolution());
        DifficultiesButton.OnLeftClick = (element) => {
            MenuState = UIState.Difficulties;
        };

        PlayButton_SinglePlayer = new(TankGame.GameLanguage.SinglePlayer, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.SinglePlayerFlavor
        };
        PlayButton_SinglePlayer.SetDimensions(() => new Vector2(700, 450).ToResolution(), () => new Vector2(500, 50).ToResolution());

        PlayButton_SinglePlayer.OnLeftClick = (uiElement) => {
            SetCampaignDisplay();
            MenuState = UIState.Campaigns;
        };
        InitializeDifficultyButtons();

        PlayButton_LevelEditor = new(TankGame.GameLanguage.LevelEditor, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.LevelEditFlavor
        };
        PlayButton_LevelEditor.SetDimensions(() => new Vector2(700, 650).ToResolution(), () => new Vector2(500, 50).ToResolution());
        PlayButton_LevelEditor.OnLeftClick = (b) => {
            LevelEditorUI.Initialize();
            LevelEditorUI.Open();
        };
        CosmeticsMenuButton = new(TankGame.GameLanguage.CosmeticsMenu, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = TankGame.GameLanguage.CosmeticsFlavor
        };
        CosmeticsMenuButton.SetDimensions(() => new Vector2(50, 50).ToResolution(), () => new Vector2(300, 50).ToResolution());
        CosmeticsMenuButton.OnLeftClick += (elem) => {
            CosmeticsMenuButton.IsVisible = false;
            SetPlayButtonsVisibility(false);
            SetMPButtonsVisibility(false);
            SetPrimaryMenuButtonsVisibility(false);

            MenuState = UIState.Cosmetics;
        };
        StatsMenu = new(TankGame.GameLanguage.GameStats, font, Color.WhiteSmoke) {
            IsVisible = false,
            OnLeftClick = (a) => { MenuState = UIState.StatsMenu; },
            Tooltip = "View your all-time statistics for this game!"
        };
        StatsMenu.OnLeftClick = (a) => {
            RequestStats();
            MenuState = UIState.StatsMenu;
        };
        StatsMenu.SetDimensions(() => new Vector2(WindowUtils.WindowWidth / 2 - 90.ToResolutionX(), WindowUtils.WindowHeight - 100.ToResolutionY()), () => new Vector2(180, 50).ToResolution());
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

        StatsMenu.IsVisible = visible;
    }

    // the code is horrid but at least it's separated now
    // TODO: make rainbow work, ask lolxd or someone knowledgeable
    public static void PrepareTextBuffers(GraphicsDevice device, SpriteBatch spriteBatch) {
        device.SetRenderTarget(TextTarget);

        device.Clear(RenderGlobals.BackBufferColor);

        spriteBatch.Begin();
        DrawUtils.DrawTextWithBorder(spriteBatch, FontGlobals.RebirthFont, tanksMessage, Vector2.Zero,
            Color.White, Color.Black, new Vector2(0.8f).ToResolution(), 0f, Anchor.TopLeft, 0.5f);
        spriteBatch.End();

        device.SetRenderTarget(null);
    }
    public static void RenderGeneralUI() {
        if (SteamworksUtils.IsInitialized)
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"STEAM LAUNCH!\nLogged in as '{SteamworksUtils.MyUsername}'\n" +
                $"You have {SteamworksUtils.FriendsCount} friends.", Vector2.One * 8, Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);

        TankGame.SpriteRenderer.End();

        TankGame.SpriteRenderer.Begin(effect: GameShaders.AnimatedRainbow);
        TankGame.SpriteRenderer.Draw(TextTarget, new Vector2(10, WindowUtils.WindowHeight - 10), null, Color.White, 0f, Anchor.BottomLeft.GetTextureAnchor(TextTarget), 1f, default, 0f);
        TankGame.SpriteRenderer.End();

        TankGame.SpriteRenderer.Begin();

        if (MenuState == UIState.PrimaryMenu || MenuState == UIState.PlayList) {
            var size = FontGlobals.RebirthFont.MeasureString(TankGame.Instance.MOTD);
            var MotdPos = new Vector2(WindowUtils.WindowWidth / 2, 10);
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, TankGame.Instance.MOTD, MotdPos, Color.White, Vector2.One * 0.5f, 0f, Anchor.TopCenter.GetAnchor(size));
        }
    }
    public static void UpdateUI() {
        // quite unfortunate hardcode. fix later.
        if (MenuState == UIState.StatsMenu)
            if (InputUtils.KeyJustPressed(Keys.Escape))
                MenuState = UIState.PrimaryMenu;
        // todo: do transitions
        SetPlayButtonsVisibility(MenuState == UIState.PlayList);
        SetMPButtonsVisibility(MenuState == UIState.Mulitplayer);
        SetPrimaryMenuButtonsVisibility(MenuState == UIState.PrimaryMenu);
        SetDifficultiesButtonsVisibility(MenuState == UIState.Difficulties);

    }
    public static void OpenUI() {
        if (!Speedrun.AreSpeedrunsFetched) {
            Speedrun.AreSpeedrunsFetched = true;
            Speedrun.GetSpeedruns();
        }

        plrsConfirmed = 0;
        _musicFading = false;
        MenuState = UIState.PrimaryMenu;
        Active = true;
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
    }
    public static void LeaveUI() {
        SetMPButtonsVisibility(false);
        SetPlayButtonsVisibility(false);
        SetPrimaryMenuButtonsVisibility(false);
        Active = false;
        GraphicsUI.BatchVisible = false;
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