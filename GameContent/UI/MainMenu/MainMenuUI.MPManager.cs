using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    private static Vector2 _panelPosition;
    private static float _panelWidth;
    private static float _panelHeaderHeight;
    private static float _panelHeight;

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

    public static Vector2 PlayersGraphicOrigin = new Vector2(94.374275f, -94.35968f);
    public static Vector3 PlayersGraphicRotationOrigin = new Vector3(0f, 0.03470005f, 0.3459305f);

    public static UITextButton CreateServerButton;
    public static UITextButton ConnectToServerButton;
    public static UITextInput UsernameInput;
    public static UITextInput IPInput;
    public static UITextInput PortInput;
    public static UITextInput PasswordInput;
    public static UITextInput ServerNameInput;
    public static UITextButton DisconnectButton;
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
        StartMPGameButton.IsVisible = visible && Client.IsHost() && Client.IsConnected();
    }
    public static void InitializeMP(SpriteFontBase font) {
        var uiColor = Color.LightGray;
        UsernameInput = new(font, uiColor, 1f, 15) {
            IsVisible = false,
            DefaultString = "Username"
        };
        UsernameInput.SetDimensions(() => _panelPosition + new Vector2(0, _panelHeaderHeight / 4), () => new Vector2(240.ToResolutionX(), _panelHeaderHeight / 2));

        IPInput = new(font, uiColor, 1f, 15) {
            IsVisible = false,
            DefaultString = "Server IP address"
        };
        IPInput.SetDimensions(() => UsernameInput.Position + new Vector2(UsernameInput.Size.X, 0), () => UsernameInput.Size);

        PortInput = new(font, uiColor, 1f, 5) {
            IsVisible = false,
            DefaultString = "Server Port"
        };
        PortInput.SetDimensions(() => IPInput.Position + new Vector2(UsernameInput.Size.X, 0), () => UsernameInput.Size);

        PasswordInput = new(font, uiColor, 1f, 10) {
            IsVisible = false,
            DefaultString = "Server Password",
            Tooltip = "Empty = none"
        };
        PasswordInput.SetDimensions(() => PortInput.Position + new Vector2(UsernameInput.Size.X, 0), () => UsernameInput.Size);
        DisconnectButton = new("Disconnect", font, uiColor, 1f) {
            IsVisible = false,
            OnLeftClick = (arg) => {
                Client.SendDisconnect(NetPlay.CurrentClient.Id, NetPlay.CurrentClient.Name, "User left.");
                Client.NetClient.Disconnect();

                NetPlay.CurrentClient = null;
                NetPlay.CurrentServer = null;

                Server.ConnectedClients = null;
                Server.NetManager = null;

                NetPlay.UnmapClientNetworking();
                NetPlay.UnmapServerNetworking();

                ShouldServerButtonsBeVisible = true;
            }
        };
        DisconnectButton.SetDimensions(
            () => _panelPosition + new Vector2(_panelWidth / 3 * 2 - DisconnectButton.Size.X / 2, _panelHeaderHeight / 4),
            () => UsernameInput.Size);

        ServerNameInput = new(font, uiColor, 1f, 10) {
            IsVisible = false,
            DefaultString = "Server Name"
        };
        ServerNameInput.SetDimensions(() => PasswordInput.Position + new Vector2(UsernameInput.Size.X, 0), () => UsernameInput.Size);

        ConnectToServerButton = new(TankGame.GameLanguage.ConnectToServer, font, uiColor) {
            IsVisible = false,
            Tooltip = "Connect to the written IP and Port in the form of ip:port"
        };
        ConnectToServerButton.SetDimensions(() => ServerNameInput.Position + new Vector2(UsernameInput.Size.X, 0), () => UsernameInput.Size);
        ConnectToServerButton.OnLeftClick = (uiButton) => {
            if (UsernameInput.IsEmpty()) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("Your username is empty!", Color.Red);
                return;
            }
            if (PortInput.IsEmpty()) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("The port is empty!", Color.Red);
                return;
            }
            if (IPInput.IsEmpty()) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("The IP address is not valid.", Color.Red);
                return;
            }

            if (int.TryParse(PortInput.GetRealText(), out var port)) {
                Client.CreateClient(UsernameInput.GetRealText());
                Client.AttemptConnectionTo(IPInput.GetRealText(), port, PasswordInput.GetRealText());
            }
            else {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("That is not a valid port.", Color.Red);

                //Client.CreateClient("client");
                //Client.AttemptConnectionTo("localhost", 7777, string.Empty);
            }
        };

        CreateServerButton = new(TankGame.GameLanguage.CreateServer, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = "Create a server with the written IP and Port in the form of ip:port"
        };
        CreateServerButton.SetDimensions(
            () => new Vector2(_panelPosition.X + _panelWidth / 2 - CreateServerButton.Size.X / 2, _panelPosition.Y + _panelHeaderHeight + _panelHeight - 30.ToResolutionY()),
            () => UsernameInput.Size);
        CreateServerButton.OnLeftClick = (uiButton) => {
            if (UsernameInput.IsEmpty()) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("Your username is empty!", Color.Red);
                return;
            }
            if (PortInput.IsEmpty()) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("The port is empty!", Color.Red);
                return;
            }
            if (IPInput.IsEmpty()) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("The IP address is not valid.", Color.Red);
                return;
            }

            if (int.TryParse(PortInput.GetRealText(), out var port)) {
                Server.CreateServer();

                NetPlay.ServerName = ServerNameInput.GetRealText() == string.Empty ? "Unnamed" : ServerNameInput.GetRealText();
                Server.StartServer(NetPlay.ServerName, port, IPInput.GetRealText(), PasswordInput.GetRealText());

                Client.CreateClient(UsernameInput.GetRealText());
                Client.AttemptConnectionTo(IPInput.GetRealText(), port, PasswordInput.GetRealText());

                Server.ConnectedClients[0] = NetPlay.CurrentClient;

                StartMPGameButton.IsVisible = true;
            }
            else {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("That is not a valid port.", Color.Red);
                /*Server.CreateServer();

                Server.StartServer("test_name", 7777, "localhost", string.Empty);

                NetPlay.ServerName = ServerNameInput.GetRealText();

                Client.CreateClient("host");
                Client.AttemptConnectionTo("localhost", 7777, string.Empty);

                Server.ConnectedClients[0] = NetPlay.CurrentClient;*/
            }

        };
        StartMPGameButton = new(TankGame.GameLanguage.Play, font, uiColor) {
            IsVisible = false,
            Tooltip = "Start the game with every client that is connected"
        };
        StartMPGameButton.OnLeftClick = (uiButton) => {
            PlayButton_SinglePlayer.OnLeftClick?.Invoke(null); // starts the game

            SetPlayButtonsVisibility(false);

            MenuState = UIState.Campaigns;
        };
        StartMPGameButton.SetDimensions(
            () => _panelPosition + new Vector2(_panelWidth / 3 - DisconnectButton.Size.X / 2, _panelHeaderHeight / 4),
            () => UsernameInput.Size);
    }
    public static void UpdateMP() {

        var plrOffset = -10f;
        if (!Client.IsConnected()) {
            if (PlayerTank.ClientTank is null) {
                var p = new PlayerTank(PlayerID.Blue);
                p.Physics.Position = (PlayersGraphicOrigin + new Vector2(0, plrOffset)) / Tank.UNITS_PER_METER;
                p.TankRotation = PlayersGraphicRotationOrigin.Z;
                p.Dead = false;
            } else {
                if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.K))
                    PlayerTank.ClientTank.Remove(true);
            }
            return;
        }
        for (int i = 0; i < Server.CurrentClientCount; i++) {
            var client = Server.ConnectedClients[i];
            if (client is null) continue;
            // TODO: UHHH??????
            //if (client.IsOperatedByPlayer) continue;

            if (GameHandler.AllPlayerTanks[i] is not null) continue;

            var p = new PlayerTank(client.Id);
            p.Physics.Position = (PlayersGraphicOrigin + new Vector2(0, plrOffset).Rotate(MathHelper.PiOver2 / 2 * i)) / Tank.UNITS_PER_METER;
            p.TankRotation = PlayersGraphicRotationOrigin.Z;
            p.Dead = false;
        }
    }
    public static void RenderMP() {
        if (Server.ConnectedClients is null) {
            Server.ConnectedClients = new Client[4];
            NetPlay.ServerName = "ServerName";
            for (int i = 0; i < 4; i++) {
                Server.ConnectedClients[i] = new(i, "Client" + i);
            }
        }

        float divisor = 8;
        float initialX = WindowUtils.WindowWidth / divisor;
        _panelWidth = initialX * (divisor - 2);
        _panelHeaderHeight = 50f.ToResolutionY();
        _panelHeight = 300f.ToResolutionY();
        _panelPosition = new Vector2(initialX, 50);

        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _panelPosition, null,
            Color.White, 0f, Vector2.Zero, new Vector2(_panelWidth, _panelHeaderHeight), default, 0f);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _panelPosition + new Vector2(0, _panelHeaderHeight), null,
            Color.Gray, 0f, Vector2.Zero, new Vector2(_panelWidth, _panelHeight), default, 0f);

        Vector2 serverNamePos = new(_panelPosition.X + _panelWidth / 2, _panelPosition.Y + _panelHeaderHeight + 20.ToResolutionY());

        /*DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, $"Connected Players:", initialPosition + new Vector2(0, 40),
            Color.White, Color.Black, new Vector2(0.6f).ToResolution(), 0f, Anchor.TopLeft, 0.8f);*/
        var numClients = Server.CurrentClientCount;

        var divisions = numClients + 1;
        var panelXCut = _panelWidth / divisions;

        var borderColor = Color.Black;

        DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, numClients > 0 ? $"\"{NetPlay.ServerName}\"" : "N/A", serverNamePos,
            Color.White, Color.Black, new Vector2(0.6f).ToResolution(), 0f, Anchor.Center, 0.8f);

        for (int i = 0; i < numClients; i++) {
            var client = Server.ConnectedClients[i];
            // TODO: when u work on this again be sure to like, re-enable this code, cuz like, if u dont, u die.
            Color textCol = PlayerID.PlayerTankColors[client.Id];

            var clientNamePos = new Vector2(_panelPosition.X + (panelXCut * (i + 1)), _panelPosition.Y + _panelHeaderHeight + _panelHeight / 3);

            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, $"{client.Name}",
                clientNamePos, textCol, borderColor, new Vector2(0.3f).ToResolution(), 0f, Anchor.Center, 0.8f);
            DrawUtils.DrawTextureWithBorder(TankGame.SpriteRenderer, GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank2d"),
                clientNamePos + new Vector2(0, 35).ToResolution(), textCol, borderColor, new Vector2(1f), 0f);
        }
    }
}
