using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common;
using System;
using TanksRebirth.Internals.UI;
using System.Collections.Generic;
using System.IO;

using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Net;

using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.GameContent.UI.MainMenu;

// unfortunately godclassed asf
public static partial class MainMenuUI
{
    public static bool Active { get; private set; } = true;

    public static Animator CameraPositionAnimator;
    public static Animator CameraRotationAnimator;

    public static OggAudio TickSound;

    public static OggMusic Theme;
    private static bool _musicFading;

    public delegate void MenuOpenDelegate();
    public delegate void MenuCloseDelegate();
    public delegate void CampaignSelectedDelegate(Campaign campaign);
    public static event MenuOpenDelegate OnMenuOpen;
    public static event MenuCloseDelegate OnMenuClose;
    public static event CampaignSelectedDelegate OnCampaignSelected;

    public static int MissionCheckpoint = 0;

    #region MenuCameraPositions

    public static EasingFunction CameraEasingFunction = EasingFunction.InOutQuad;
    public static TimeSpan CameraTransitionTime = TimeSpan.FromSeconds(2);

    // (most of) these magical vectors were found from flying around ingame.

    // if this dict does not contain the UIState we want, we just default to CamPosMain
    // cosmetics menu in the future can be just a simple transition over to a post where a player tank can render in front of the camera at
    // the camera's position plus the Camera's world forward matrix times a certain amount for distance
    public static Dictionary<UIState, (Vector3 Position, Vector3 Rotation)> MenuCameraManipulations = new() {
        [UIState.Campaigns] = (new(330f, 204f, 879f), new(0, -0.18f, 0.29f)), // seat headrest
        [UIState.PlayList] = (new Vector3(247.031f, 59.885f, 204.935f), new Vector3(0f, -0.404f, 1.397f)),
        [UIState.Mulitplayer] = (new(51f, 34f, -140f), new(0, -0.36f, 3.7f)), // behind game scene
        [UIState.Settings] = (new(1461f, 928f, 623f), new(0, -0.33f, 0.53f)), // near grandfather clock
        [UIState.StatsMenu] = (new(-1121f, 176f, 439f), new(0, -0.231f, -0.67f)), // near sheet music
        [UIState.Difficulties] = (new(-1189f, 288f, 2583f), new(0f, -0.25f, -2.27f)), // near books
        [UIState.LoadingMods] = (new(-3443f, 2088f, 3183f), new(0, -0.6307f, -0.91f)),
        [UIState.Cosmetics] = (new(-953f, 1078f, 2753f), new(0f, -0.226f, -2.56f))
    };

    public static Vector3 CamPosMain = new(0, 150, GameScene.MAX_Z + 100); // this is in front of the game scene, viewing it
    public static Vector3 CamPosMainRotation = new(0, -0.5f, 0);

    #endregion

    internal static Mission curMenuMission;
    private static List<Mission> _cachedMissions = [];

    private static bool _initialized;

    public static RebirthLogoModel RebirthLogo;
    public static void InitializeBasics() {
        TickSound = new OggAudio("Content/Assets/sounds/menu/menu_tick.ogg");
        CameraPositionAnimator = Animator.Create();
        CameraRotationAnimator = Animator.Create();
        MenuState = UIState.PrimaryMenu;
        RebirthLogo = new() {
            Position = new(0, 500, 250),
        };

        // LogoTexture = SteamworksUtils.GetAvatar(Steamworks.SteamUser.GetSteamID());
    }
    public static void InitializeUI() {
        if (_initialized) {
            foreach (var field in typeof(MainMenuUI).GetFields()) {
                if (field.GetValue(null) is UIElement uielem) {
                    uielem.Remove();
                    uielem = null;
                }
            }
        }
        _initialized = true;

        var font = FontGlobals.RebirthFont;
        InitializeMain(font);
        InitializeMP(font);

        _menuElements = [PlayButton, PlayButton_SinglePlayer, PlayButton_LevelEditor, PlayButton_Multiplayer, ConnectToServerButton,
            CreateServerButton, UsernameInput, IPInput, PortInput, PasswordInput, ServerNameInput,
            DifficultiesButton ];

        foreach (var e in _menuElements) {
            e.OnMouseOver = (uiElement) => SoundPlayer.PlaySoundInstance(TickSound, SoundContext.Effect);
        }
    }
    public static void Leave() {
        // call partially implemented methods
        LeaveUI();
        LeaveGP();
    }
    private static void UpdateModels() {
        RebirthLogo.Position = new Vector3(0, 300.ToResolutionY(), 0);
        //var testX = MouseUtils.MousePosition.X / WindowUtils.WindowWidth;
        //var testY = MouseUtils.MousePosition.Y / WindowUtils.WindowHeight;

        var scalar = 0.4f;
        var rotX = -((MouseUtils.MousePosition.X - WindowUtils.WindowWidth / 2) / (WindowUtils.WindowWidth / 2)) * scalar;
        var rotY = MathHelper.PiOver2 + (MouseUtils.MousePosition.Y - WindowUtils.WindowHeight / 4) / (WindowUtils.WindowHeight / 4) * scalar * 0.5f;
        RebirthLogo.Rotation.X = rotX;
        RebirthLogo.Rotation.Y = rotY;
    }
    public static void RenderModels() {

        UpdateModels();
        // TODO: change this to world view/world projection...? i think it would look better if the crate existed in world space
        // it would give reason to have the camera move over for the player.

        if (!Active) return;
        //RebirthLogo.Rotation = new(MathF.Sin(RuntimeData.RunTime / 100) * MathHelper.PiOver4, 0, 0);
        RebirthLogo.Scale = 0.8f.ToResolutionF();
        RebirthLogo.View = CameraGlobals.ScreenView;
        RebirthLogo.Projection = CameraGlobals.ScreenProjOrthographic;
        if (MenuState == UIState.PrimaryMenu || MenuState == UIState.PlayList)
            RebirthLogo.Render();
    }
    // this is super workaround-y.
    internal static int plrsConfirmed;
    public static bool PrepareGameplay(string name, bool wasConfirmed = true, bool netRecieved = false, int? missionId = null) {
        if (missionId.HasValue) {
            ChatSystem.SendMessage("We have received a custom mission identifier", Color.Green);
            MissionCheckpoint = missionId.Value;
        }

        // FIXME: find the feedback loop that causes the recieving client to start a mission anyway...

        var path = Path.Combine("Campaigns", name + ".campaign");

        var checkPath = Path.Combine(TankGame.SaveDirectory, "Campaigns", $"{name}.campaign");

        void completePreparation(Campaign campaign) {
            CampaignGlobals.LoadedCampaign = campaign;

            CampaignGlobals.LoadedCampaign.LoadMission(MissionCheckpoint); // loading the mission specified

            PlayerTank.StartingLives = campaign.MetaData.StartingLives;
            IntermissionSystem.BannerColor = campaign.MetaData.MissionStripColor;
            IntermissionSystem.BackgroundColor = campaign.MetaData.BackgroundColor;
        }

        if (!wasConfirmed && netRecieved) {
            var ret = File.Exists(checkPath);
            if (ret)
                completePreparation(Campaign.Load(path));
            return ret;
        }

        if (!netRecieved && !wasConfirmed) { // when switch, do !wasConfirmed && !netRecieved
            if (Client.IsConnected()) {
                //Client.SendCampaignBytes(camp);
                Client.SendCampaignByName(name, MissionCheckpoint);
                return true;
            }
        }
        if (wasConfirmed) {
            var camp = Campaign.Load(path);

            Client.RequestStartGame(MissionCheckpoint, true);

            // if campaign verification across clients fails, RequestStartGame should come before this branch.
            // check if the CampaignCheckpoint number is less than the number of missions in the array
            if (MissionCheckpoint >= camp.CachedMissions.Length) {
                // if it is, notify the user that the checkpoint is too high via the chat, and play the error sound
                ChatSystem.SendMessage($"Campaign '{name}' has no mission {MissionCheckpoint + 1}.", Color.Red);
                SoundPlayer.SoundError();
                return false;
            }

            // if it is, load the mission
            completePreparation(camp);

            SetPlayButtonsVisibility(false);
            SetPrimaryMenuButtonsVisibility(false);
            SetMPButtonsVisibility(false);
            SetDifficultiesButtonsVisibility(false);

            TransitionToGame();
        }

        return true;
    }
    public static void TransitionToGame() {
        foreach (var elem in campaignNames)
            elem?.Remove();

        IntermissionSystem.TimeBlack = 280;

        CampaignGlobals.ShouldMissionsProgress = true;

        _musicFading = true;

        IntermissionSystem.InitializeCountdowns();

        IntermissionSystem.BeginOperation(600);
    }

    public static float VolumeMultiplier = 1f;

    public static void Update() {
        if (!_initialized || !_diffButtonsInitialized)
            return;
        UpdateUI();
        UpdateDifficulties();
        UpdateGameplay();
        UpdateMusic();
        UpdateMP();

        if (!Active) {
            UpdateCampaignButton.IsVisible = false;
        }
    }
    public static void Open() {
        OpenUI();
        OpenAudio();
        OpenGP();
        // there used to be stuff that manually removed entities. i hope we didn't need that
        OnMenuOpen?.Invoke();
    }

    private static int _oldwheel;
    public static void Render(SpriteBatch spriteBatch) {
        if (!_initialized || !_diffButtonsInitialized)
            return;

        if (Active) {
            RenderGeneralUI(spriteBatch);
            if (MenuState == UIState.StatsMenu)
                RenderStatsMenu();
            else if (MenuState == UIState.LoadingMods)
                ModLoader.DrawModLoading();

            else if (MenuState == UIState.Mulitplayer)
                RenderMP();
            else if (MenuState == UIState.Campaigns)
                DrawCampaignsUI();
            else if (MenuState == UIState.Cosmetics)
                RenderCosmeticsUI();
        }

        // why does this need to be here?????????? doesn't work in Update()
        _oldwheel = InputUtils.DeltaScrollWheel;
    }
}