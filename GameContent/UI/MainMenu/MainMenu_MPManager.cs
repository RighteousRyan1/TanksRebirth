using FontStashSharp;
using Microsoft.Xna.Framework;
using System.Linq;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
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
        StartMPGameButton.IsVisible = visible && Client.IsHost();
    }
    public static void InitializeMP(SpriteFontBase font) {
        UsernameInput = new(font, Color.WhiteSmoke, 1f, 20) {
            IsVisible = false,
            DefaultString = "Username"
        };
        UsernameInput.SetDimensions(() => new Vector2(100, 400).ToResolution(), () => new Vector2(500, 50).ToResolution());

        IPInput = new(font, Color.WhiteSmoke, 1f, 15) {
            IsVisible = false,
            DefaultString = "Server IP address"
        };
        IPInput.SetDimensions(() => new Vector2(100, 500).ToResolution(), () => new Vector2(500, 50).ToResolution());

        PortInput = new(font, Color.WhiteSmoke, 1f, 5) {
            IsVisible = false,
            DefaultString = "Server Port"
        };
        PortInput.SetDimensions(() => new Vector2(100, 600).ToResolution(), () => new Vector2(500, 50).ToResolution());

        PasswordInput = new(font, Color.WhiteSmoke, 1f, 10) {
            IsVisible = false,
            DefaultString = "Server Password (Empty = None)"
        };
        PasswordInput.SetDimensions(() => new Vector2(100, 700).ToResolution(), () => new Vector2(500, 50).ToResolution());
        DisconnectButton = new("Disconnect", font, Color.WhiteSmoke, 1f) {
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
        DisconnectButton.SetDimensions(() => new Vector2(100, 800).ToResolution(), () => new Vector2(500, 50).ToResolution());

        ServerNameInput = new(font, Color.WhiteSmoke, 1f, 10) {
            IsVisible = false,
            DefaultString = "Server Name (Server Creation)"
        };
        ServerNameInput.SetDimensions(() => new Vector2(100, 800).ToResolution(), () => new Vector2(500, 50).ToResolution());

        ConnectToServerButton = new(TankGame.GameLanguage.ConnectToServer, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = "Connect to the written IP and Port in the form of ip:port"
        };
        ConnectToServerButton.SetDimensions(() => new Vector2(700, 100).ToResolution(), () => new Vector2(500, 50).ToResolution());
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
        CreateServerButton.SetDimensions(() => new Vector2(700, 350).ToResolution(), () => new Vector2(500, 50).ToResolution());
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
        StartMPGameButton = new(TankGame.GameLanguage.Play, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = "Start the game with every client that is connected"
        };
        StartMPGameButton.OnLeftClick = (uiButton) => {
            PlayButton_SinglePlayer.OnLeftClick?.Invoke(null); // starts the game

            SetPlayButtonsVisibility(false);

            MenuState = UIState.Campaigns;
        };
        StartMPGameButton.SetDimensions(() => new Vector2(700, 600).ToResolution(), () => new Vector2(500, 50).ToResolution());
    }
    public static void UpdateMP() {
        var plrOffset = -10f;
        if (!Client.IsConnected()) {
            if (PlayerTank.ClientTank is null) {
                var p = new PlayerTank(PlayerID.Blue);
                p.Body.Position = (PlayersGraphicOrigin + new Vector2(0, plrOffset)) / Tank.UNITS_PER_METER;
                p.TankRotation = PlayersGraphicRotationOrigin.Z;
                p.Dead = false;
            }else {
                if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.K))
                    PlayerTank.ClientTank.Remove(true);
            }
            return;
        }
        for (int i = 0; i < Server.ConnectedClients.Length; i++) {
            var client = Server.ConnectedClients[i];
            if (client is null) continue;
            // TODO: UHHH??????
            if (client.IsOperatedByPlayer) continue;

            if (GameHandler.AllPlayerTanks.Any(x => x is not null && x.PlayerId == i))
                continue;

            var p = new PlayerTank(client.Id);
            p.Body.Position = (PlayersGraphicOrigin + new Vector2(0, plrOffset).Rotate(MathHelper.PiOver2 / 2 * i)) / Tank.UNITS_PER_METER;
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
        // TODO: rework this very rudimentary ui
        if (NetPlay.CurrentServer is not null && 
            (Server.ConnectedClients is not null || NetPlay.ServerName is not null) || 
            Client.IsConnected() && Client.LobbyDataReceived) {
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
}
