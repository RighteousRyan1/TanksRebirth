using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common;
using System;
using TanksRebirth.Internals.UI;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;
using System.IO;
using System.Diagnostics;

using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals;
using TanksRebirth.Net;

using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Localization;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.Graphics;

namespace TanksRebirth.GameContent.UI.MainMenu;

// unfortunately godclassed asf
public static partial class MainMenuUI
{
    public static bool Active { get; private set; } = true;

    public static Animator CameraPositionAnimator;
    public static Animator CameraRotationAnimator;

    public static OggMusic Theme;
    private static bool _musicFading;

    private static Matrix View;

    public static Matrix ProjectionOrtho;
    public static Matrix ProjectionPerspective;

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
        [UIState.PlayList] = (new Vector3(242.30f, 42.34f, 193.49f), new Vector3(0f, -0.364f, 1.35f)),
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

        var font = TankGame.TextFont;
        InitializeMain(font);
        InitializeMPMenu(font);

        _menuElements = [PlayButton, PlayButton_SinglePlayer, PlayButton_LevelEditor, PlayButton_Multiplayer, ConnectToServerButton,
            CreateServerButton, UsernameInput, IPInput, PortInput, PasswordInput, ServerNameInput,
            DifficultiesButton ];

        foreach (var e in _menuElements) {
            e.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect, rememberMe: true); };
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

        ProjectionOrtho = Matrix.CreateOrthographic(TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, -2000f, 5000f);
        ProjectionPerspective = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), TankGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.1f, 10000f);
        View = Matrix.CreateLookAt(Vector3.Backward, Vector3.Zero, Vector3.Up) * Matrix.CreateTranslation(0, 0, -500);
    }
    public static void RenderModels() {

        UpdateModels();
        // TODO: change this to world view/world projection...? i think it would look better if the crate existed in world space
        // it would give reason to have the camera move over for the player.

        if (!Active) return;
        //RebirthLogo.Rotation = new(MathF.Sin(TankGame.RunTime / 100) * MathHelper.PiOver4, 0, 0);
        RebirthLogo.Scale = 0.8f.ToResolutionF();
        RebirthLogo.View = View;
        RebirthLogo.Projection = ProjectionOrtho;
        if (MenuState == UIState.PrimaryMenu || MenuState == UIState.PlayList)
            RebirthLogo.Render();
    }
    private static void SetCampaignDisplay() {
        SetPlayButtonsVisibility(false);

        foreach (var elem in campaignNames)
            elem?.Remove();
        // get all the campaign folders from the SaveDirectory + Campaigns
        var path = Path.Combine(TankGame.SaveDirectory, "Campaigns");
        Directory.CreateDirectory(path);
        // add a new UIElement for each campaign folder

        var campaignFiles = Directory.GetFiles(path).Where(file => file.EndsWith(".campaign")).ToArray();

        for (int i = 0; i < campaignFiles.Length; i++) {
            int offset = i * 60;
            var name = campaignFiles[i];

            int numTanks = 0;
            var campaign = Campaign.Load(name);

            var missions = campaign.CachedMissions;

            foreach (var mission in missions) {
                // load the mission file, then count each tank, then add that to the total
                numTanks += mission.Tanks.Count(x => !x.IsPlayer);
            }

            var elem = new UITextButton(Path.GetFileNameWithoutExtension(name), TankGame.TextFont, Color.White, 0.8f) {
                IsVisible = true,
                Tooltip = missions.Length + " missions" +
                $"\n{numTanks} tanks total" +
                $"\n\nName: {campaign.MetaData.Name}" +
                $"\nDescription: {campaign.MetaData.Description}" +
                $"\nVersion: {campaign.MetaData.Version}" +
                $"\nStarting Lives: {campaign.MetaData.StartingLives}" +
                $"\nBonus Life Count: {campaign.MetaData.ExtraLivesMissions.Length}" +
                // display all tags in a string
                $"\nTags: {string.Join(", ", campaign.MetaData.Tags)}" +
                $"\n\nRight click to DELETE ME."
            };
            elem.SetDimensions(() => new Vector2(700, 100 + offset).ToResolution(), () => new Vector2(300, 40).ToResolution());
            //elem.HasScissor = true;
            //elem.
            elem.OnLeftClick += (el) => {
                if (Client.IsConnected() && !Client.IsHost()) {
                    ChatSystem.SendMessage("You cannot initiate a game as you are not the host!", Color.Red);
                    SoundPlayer.SoundError();
                    return;
                }
                var noExt = Path.GetFileNameWithoutExtension(name);
                PrepareGameplay(noExt, !Client.IsConnected() || Server.CurrentClientCount == 1, false); // switch second param to !Client.IsConnected() when it should check first.
                OnCampaignSelected?.Invoke(CampaignGlobals.LoadedCampaign);
            };
            elem.OnRightClick += (el) => {
                var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", elem.Text);

                File.Delete(path + ".campaign");
                SetCampaignDisplay();
            };
            elem.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect); };
            campaignNames.Add(elem);
        }
        var extra = new UITextButton("Freeplay", TankGame.TextFont, Color.White, 0.8f) {
            IsVisible = true,
            Tooltip = "Play without a campaign!",
        };
        extra.SetDimensions(() => new Vector2(1150, 100).ToResolution(), () => new Vector2(300, 40).ToResolution());
        extra.OnMouseOver = (uiElement) => { SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect); };
        //elem.HasScissor = true;
        //elem.
        extra.OnLeftClick += (el) => {
            foreach (var elem in campaignNames)
                elem.Remove();

            CampaignGlobals.ShouldMissionsProgress = false;

            IntermissionSystem.TimeBlack = 150;
        };
        campaignNames.Add(extra);
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
            IntermissionSystem.StripColor = campaign.MetaData.MissionStripColor;
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

            TransitionToGame();
        }

        return true;
    }
    public static void TransitionToGame() {
        foreach (var elem in campaignNames)
            elem?.Remove();

        IntermissionSystem.TimeBlack = 240;

        CampaignGlobals.ShouldMissionsProgress = true;

        _musicFading = true;

        IntermissionSystem.InitializeCountdowns();

        IntermissionSystem.BeginOperation(600);
    }
    internal static void SetPlayButtonsVisibility(bool visible) {
        PlayButton_SinglePlayer.IsVisible = visible;
        PlayButton_LevelEditor.IsVisible = visible;
        PlayButton_Multiplayer.IsVisible = visible;
        DifficultiesButton.IsVisible = visible;
        CosmeticsMenuButton.IsVisible = visible;
    }
    internal static void SetDifficultiesButtonsVisibility(bool visible) {
        TanksAreCalculators.IsVisible = visible;
        PieFactory.IsVisible = visible;
        UltraMines.IsVisible = visible;
        BulletHell.IsVisible = visible;
        AllInvisible.IsVisible = visible;
        AllStationary.IsVisible = visible;
        Armored.IsVisible = visible;
        AllHoming.IsVisible = visible;
        BumpUp.IsVisible = visible;
        Monochrome.IsVisible = visible;
        InfiniteLives.IsVisible = visible;
        MasterModBuff.IsVisible = visible;
        MarbleModBuff.IsVisible = visible;
        MachineGuns.IsVisible = visible;
        RandomizedTanks.IsVisible = visible;
        ThunderMode.IsVisible = visible;
        POVMode.IsVisible = visible;
        AiCompanion.IsVisible = visible;
        Shotguns.IsVisible = visible;
        Predictions.IsVisible = visible;
        RandomizedPlayer.IsVisible = visible;
        BulletBlocking.IsVisible = visible;
        FFA.IsVisible = visible;
        LanternMode.IsVisible = visible;
        DisguiseMode.IsVisible = visible;
    }
    internal static void SetPrimaryMenuButtonsVisibility(bool visible) {
        GameUI.OptionsButton.IsVisible = visible;

        GameUI.QuitButton.IsVisible = visible;

        GameUI.BackButton.Size.Y = 50;

        PlayButton.IsVisible = visible;

        StatsMenu.IsVisible = visible;
    }

    private static bool _ssbbv = true;
    public static bool ShouldServerButtonsBeVisible {
        get => _ssbbv;
        set {
            _ssbbv = value;
            ConnectToServerButton.IsVisible = value;
            CreateServerButton.IsVisible = value;
            ConnectToServerButton.IsVisible = value;
            CreateServerButton.IsVisible = value;
            UsernameInput.IsVisible = value;
            IPInput.IsVisible = value;
            PasswordInput.IsVisible = value;
            PortInput.IsVisible = value;
            ServerNameInput.IsVisible = value && !Client.IsConnected();
        }
    }
    internal static void SetMPButtonsVisibility(bool visible) {
        if (ShouldServerButtonsBeVisible) {
            ConnectToServerButton.IsVisible = visible;
            CreateServerButton.IsVisible = visible;
            UsernameInput.IsVisible = visible;
            IPInput.IsVisible = visible;
            PasswordInput.IsVisible = visible;
            PortInput.IsVisible = visible;
            ServerNameInput.IsVisible = visible && !Client.IsConnected();
        }
        DisconnectButton.IsVisible = visible && Client.IsConnected();
        StartMPGameButton.IsVisible = visible && Client.IsHost();
    }

    public static float VolumeMultiplier = 1f;

    public static void Update() {
        if (!_initialized || !_diffButtonsInitialized)
            return;
        UpdateUI();
        UpdateDifficulties();
        UpdateGameplay();
        UpdateMusic();
    }
    public static void Open() {
        OpenUI();
        OpenAudio();
        OpenGP();
        // there used to be stuff that manually removed entities. i hope we didn't need that
        OnMenuOpen?.Invoke();
    }

    private static int _oldwheel;
    public static void Render() {
        if (!_initialized || !_diffButtonsInitialized)
            return;
        if (Active) {
            if (MenuState == UIState.StatsMenu)
                RenderStatsMenu();
            else if (MenuState == UIState.LoadingMods)
                ModLoader.DrawModLoading();

            DrawMPMenu();

            RenderGeneralUI();
            if (MenuState == UIState.Campaigns)
                DrawCampaignsUI();
            else if (MenuState == UIState.Cosmetics)
                RenderCosmeticsUI();
        }
        _oldwheel = InputUtils.DeltaScrollWheel;
    }
    public static void DrawMPMenu() {
        if (Server.ConnectedClients is null) {
            Server.ConnectedClients = new Client[4];
            NetPlay.ServerName = "ServerName";
            for (int i = 0; i < 4; i++) {
                Server.ConnectedClients[i] = new(i, "Client" + i);
            }
        }
        // TODO: rework this very rudimentary ui
        if (NetPlay.CurrentServer is not null && (Server.ConnectedClients is not null || NetPlay.ServerName is not null) || Client.IsConnected() && Client.LobbyDataReceived) {
            Vector2 initialPosition = new(WindowUtils.WindowWidth * 0.75f, WindowUtils.WindowHeight * 0.25f);
            DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"\"{NetPlay.ServerName}\"", initialPosition - new Vector2(0, 40),
                Color.White, Color.Black, new Vector2(0.6f).ToResolution(), 0f, Anchor.TopLeft, 0.8f);
            DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"Connected Players:", initialPosition,
                Color.White, Color.Black, new Vector2(0.6f).ToResolution(), 0f, Anchor.TopLeft, 0.8f);

            for (int i = 0; i < Server.ConnectedClients.Count(x => x is not null); i++) {
                var client = Server.ConnectedClients[i];
                // TODO: when u work on this again be sure to like, re-enable this code, cuz like, if u dont, u die.
                Color textCol = PlayerID.PlayerTankColors[client.Id].ToColor();
                //if (NetPlay.CurrentClient.Id == i)
                //textCol = Color.Green;

                DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"{client.Name}" + $" ({PlayerID.Collection.GetKey(client.Id)} tank)",
                    initialPosition + new Vector2(0, 20) * (i + 1), textCol, Color.Black, new Vector2(0.6f).ToResolution(), 0f, Anchor.TopLeft, 0.8f);
            }
        }
    }
    public static void BotherUserForNotHavingVanillaCampaign() {
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"You are missing the vanilla campaign!" +
    $"\nTry downloading the Vanilla campaign by pressing 'Enter'." +
    $"\nCampaign files belong in '{Path.Combine(TankGame.SaveDirectory, "Campaigns").Replace(Environment.UserName, "%UserName%")}' (press TAB to open on Windows)", new Vector2(12, 12).ToResolution(), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);

        if (Client.IsConnected() && Client.IsHost())
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"The people who are connected to you MUST own this\ncampaign, and it MUST have the same file name.\nOtherwise, the campaign will not load.", new(12, WindowUtils.WindowHeight / 2), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);

        if (InputUtils.KeyJustPressed(Keys.Tab)) {
            if (Directory.Exists(Path.Combine(TankGame.SaveDirectory, "Campaigns")))
                Process.Start("explorer.exe", Path.Combine(TankGame.SaveDirectory, "Campaigns"));
            // do note that this fails on windows lol
        }
        if (InputUtils.KeyJustPressed(Keys.Enter)) {
            try {
                var bytes = WebUtils.DownloadWebFile("https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/Vanilla.campaign?raw=true", out var filename);
                var path = Path.Combine(TankGame.SaveDirectory, "Campaigns", filename);
                File.WriteAllBytes(path, bytes);

                SetCampaignDisplay();

            }
            catch (Exception e) {
                TankGame.ReportError(e);
            }
        }
    }
    public static void DrawCampaignMenuExtras() {
        if (_oldwheel != InputUtils.DeltaScrollWheel)
            MissionCheckpoint += InputUtils.DeltaScrollWheel - _oldwheel;
        if (MissionCheckpoint < 0)
            MissionCheckpoint = 0;

        DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"You can scroll with your mouse to skip to a certain mission." +
            $"\nCurrently, you will skip to mission {MissionCheckpoint + 1}." +
            $"\nYou will be alerted if that mission does not exist.", new Vector2(12, 200).ToResolution(),
            Color.White, Color.Black, new Vector2(0.75f).ToResolution(), 0f, Anchor.TopLeft);
        //TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"You can scroll with your mouse to skip to a certain mission." +
        //$"\nCurrently, you will skip to mission {MissionCheckpoint + 1}." +
        //$"\nYou will be alerted if that mission does not exist.", new Vector2(12, 200).ToResolution(), Color.White, new Vector2(0.75f).ToResolution(), 0f, Vector2.Zero);

        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/trophy");
        var defPos = new Vector2(60, 380);
        TankGame.SpriteRenderer.Draw(tex, defPos.ToResolution(), null, Color.White, 0f, new Vector2(tex.Size().X, tex.Size().Y / 2), new Vector2(0.1f).ToResolution(), default, default);
        var text = $"Top {Speedrun.LoadedSpeedruns.Length} speedruns:\n" + string.Join(Environment.NewLine, Speedrun.LoadedSpeedruns);
        DrawUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, text, defPos.ToResolution(), Color.White, Color.Black, new Vector2(0.75f).ToResolution(), 0f, Anchor.LeftCenter);
    }
}