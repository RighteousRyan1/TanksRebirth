using FontStashSharp;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    public static void InitializeMPMenu(SpriteFontBase font) {
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
}
