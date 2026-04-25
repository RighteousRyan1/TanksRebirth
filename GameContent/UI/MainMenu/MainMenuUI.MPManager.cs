using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Tanks;

using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework;
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

    private static float _mpOpenProgress = 0f;

    private static bool _ssbbv = true;
    public static bool ShouldServerButtonsBeVisible {
        get => _ssbbv;
        set {
            _ssbbv = value;
            ConnectToServerButton.IsVisible = value;
            CreateServerButton.IsVisible = value;
            UsernameInput.IsVisible = value;
            IPInput.IsVisible = value;
            PasswordInput.IsVisible = value;
            PortInput.IsVisible = value;
            ServerNameInput.IsVisible = value && !Client.IsConnected();

            // resets animation on open
            if (value) _mpOpenProgress = 0f;
        }
    }

    public static Vector2 PlayersGraphicOrigin = new Vector2(94.374275f, -220);
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

        if (!visible) _mpOpenProgress = 0f;
    }

    // comment bs cleanup
    private static Func<Vector2> GetButtonSize(float padding) {
        return () => new Vector2((_panelWidth - padding * 7) / 6, _panelHeaderHeight - padding * 2);
    }

    private static Func<Vector2> GetFirstButtonPosition(float padding) {
        return () => _panelPosition + new Vector2(padding, padding);
    }

    private static Func<Vector2> GetNextButtonPosition(dynamic previousButton, float padding) {
        return () => previousButton.Position + new Vector2(previousButton.Size.X + padding, 0);
    }

    public static void InitializeMP(SpriteFontBase font) {
        var uiColor = Color.LightGray;
        var padding = 10f.ToResolutionX();
        var buttonSize = GetButtonSize(padding);
        
        UsernameInput = new(font, uiColor, 1f, 15) {
            IsVisible = false,
            DefaultString = "Username"
        };
        UsernameInput.SetDimensions(GetFirstButtonPosition(padding), buttonSize);

        IPInput = new(font, uiColor, 1f, 15) {
            IsVisible = false,
            DefaultString = "Server IP address"
        };
        IPInput.SetDimensions(GetNextButtonPosition(UsernameInput, padding), buttonSize);

        PortInput = new(font, uiColor, 1f, 5) {
            IsVisible = false,
            DefaultString = "Server Port"
        };
        PortInput.SetDimensions(GetNextButtonPosition(IPInput, padding), buttonSize);

        PasswordInput = new(font, uiColor, 1f, 10) {
            IsVisible = false,
            DefaultString = "Server Password",
            Tooltip = "Empty = none"
        };
        PasswordInput.SetDimensions(GetNextButtonPosition(PortInput, padding), buttonSize);
        DisconnectButton = new("Disconnect", font, uiColor, 1f) {
            IsVisible = false,
            OnLeftClick = (arg) => {
                Client.Disconnect();
            }
        };
        DisconnectButton.SetDimensions(
            () => _panelPosition + new Vector2(_panelWidth / 3 * 2 - DisconnectButton.Size.X / 2, padding),
            () => UsernameInput.Size);

        ServerNameInput = new(font, uiColor, 1f, 10) {
            IsVisible = false,
            DefaultString = "Server Name"
        };
        ServerNameInput.SetDimensions(GetNextButtonPosition(PasswordInput, padding), buttonSize);

        ConnectToServerButton = new(TankGame.GameLanguage.Menu.ConnectToServer, font, uiColor) {
            IsVisible = false,
            Tooltip = "Connect to the written IP and Port in the form of ip:port"
        };
        ConnectToServerButton.SetDimensions(GetNextButtonPosition(ServerNameInput, padding), buttonSize);
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
            }
        };

        CreateServerButton = new(TankGame.GameLanguage.Menu.CreateServer, font, Color.WhiteSmoke) {
            IsVisible = false,
            Tooltip = "Create a server with the written IP and Port in the form of ip:port"
        };
        CreateServerButton.SetDimensions(
            () => new Vector2(_panelPosition.X + _panelWidth / 2 - CreateServerButton.Size.X / 2, _panelPosition.Y + _panelHeaderHeight + _panelHeight - 45.ToResolutionY()),
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
            }

        };
        StartMPGameButton = new(TankGame.GameLanguage.Menu.Play, font, uiColor) {
            IsVisible = false,
            Tooltip = "Start the game with every client that is connected"
        };
        StartMPGameButton.OnLeftClick = (uiButton) => {
            PlayButton_SinglePlayer.OnLeftClick?.Invoke(null); // starts the game

            SetPlayButtonsVisibility(false);

            MenuState = UIState.Campaigns;
        };
        StartMPGameButton.SetDimensions(
            () => _panelPosition + new Vector2(_panelWidth / 3 - DisconnectButton.Size.X / 2, padding),
            () => UsernameInput.Size);
    }

    public static void UpdateMP() {
        var plrOffset = -10f;
        if (!Client.IsConnected()) {
            if (PlayerTank.ClientTank is null) {
                var p = new PlayerTank(PlayerID.Blue);
                p.Physics.Position = (PlayersGraphicOrigin + new Vector2(0, plrOffset)) / Tank.UNITS_PER_METER;
                p.ChassisRotation = PlayersGraphicRotationOrigin.Z;
                p.IsDestroyed = false;
            }
            else {
                if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.K))
                    PlayerTank.ClientTank.Remove(true);
            }
            return;
        }
        for (int i = 0; i < Server.CurrentClientCount; i++) {
            var client = Server.ConnectedClients[i];
            if (client is null) continue;
            if (GameHandler.AllPlayerTanks[i] is not null) continue;

            var p = new PlayerTank(client.Id);
            p.Physics.Position = (PlayersGraphicOrigin + new Vector2(0, plrOffset).RotatedBy(MathHelper.PiOver2 / 2 * i)) / Tank.UNITS_PER_METER;
            p.ChassisRotation = PlayersGraphicRotationOrigin.Z;
            p.IsDestroyed = false;
        }
    }

    // also localize eventually
    public static void DrawMPMenu(GameTime gameTime) {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (Server.ConnectedClients is null) {
            Server.ConnectedClients = new Client[4];
            NetPlay.ServerName = "ServerName";
            for (int i = 0; i < 4; i++) Server.ConnectedClients[i] = new(i, "Client" + i);
        }

        _mpOpenProgress += dt;
        _mpOpenProgress = MathHelper.Clamp(_mpOpenProgress, 0f, 1f);

        float alpha = _mpOpenProgress;
        float slide = Easings.OutQuint(_mpOpenProgress);

        // move up smoooothly
        float yOffset = (1f - slide) * 50f;

        // dimensions/positioning
        float divisor = 8;
        float initialX = WindowUtils.WindowWidth / divisor;
        _panelWidth = initialX * (divisor - 2);
        _panelHeaderHeight = 55f.ToResolutionY();
        _panelHeight = 200f.ToResolutionY();
        _panelPosition = new Vector2(initialX, 50);

        var renderer = TankGame.SpriteRenderer;
        var whiteTex = TextureGlobals.Pixels[Color.White];

        // bg dimming
        renderer.Draw(whiteTex, new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight), Color.Black * 0.5f * alpha);

        // header panel
        var headerRect = new Rectangle((int)_panelPosition.X, (int)_panelPosition.Y, (int)_panelWidth, (int)_panelHeaderHeight);
        DrawUtils.DrawBoxWithOutline(renderer, headerRect, new Color(30, 30, 30) * alpha, Color.Gray * alpha, 2);

        // body + player list
        var bodyRect = new Rectangle((int)_panelPosition.X, (int)(_panelPosition.Y + _panelHeaderHeight), (int)_panelWidth, (int)_panelHeight);
        DrawUtils.DrawBoxWithOutline(renderer, bodyRect, new Color(20, 20, 25) * alpha, Color.Gray * alpha, 2);

        // server name
        Vector2 serverNamePos = new(_panelPosition.X + _panelWidth / 2, _panelPosition.Y + _panelHeaderHeight + 20.ToResolutionY());
        string sName = Server.CurrentClientCount > 0 ? $"Lobby: \"{NetPlay.ServerName}\"" : "Multiplayer Setup";

        DrawUtils.DrawStringWithBorder(renderer, FontGlobals.RebirthFontLarge, sName, serverNamePos,
            Color.Goldenrod * alpha, Color.Black * alpha, new Vector2(0.6f).ToResolution(), 0f, Anchor.Center, 0.8f);

        // player slots
        int maxSlots = GameHandler.MAX_PLAYERS;
        float slotPadding = 20.ToResolutionX();
        float totalPadding = slotPadding * (maxSlots + 1);
        float slotWidth = (_panelWidth - totalPadding) / maxSlots;
        float slotHeight = _panelHeight * 0.6f;
        float startY = _panelPosition.Y + _panelHeaderHeight + (_panelHeight - slotHeight) / 2 + 10.ToResolutionY();

        for (int i = 0; i < maxSlots; i++) {
            // allows for a staggered entry animation based on slot index
            float cardProgress = Easings.OutElastic(MathHelper.Clamp(_mpOpenProgress * 1.5f - (i * 0.1f), 0f, 1f));
            float cardScale = cardProgress;

            // slot positioning per-player
            float slotX = _panelPosition.X + slotPadding + (i * (slotWidth + slotPadding));
            var slotRect = new Rectangle((int)slotX, (int)startY, (int)slotWidth, (int)slotHeight);

            // scale rect from center
            var animRect = MathUtils.ScaleRect(slotRect, cardScale);

            bool hasPlayer = i < Server.CurrentClientCount;
            Client client = hasPlayer ? Server.ConnectedClients[i] : null;

            // card/slot bg
            var cardColor = hasPlayer ? new Color(40, 40, 50) : new Color(30, 30, 30);
            var borderColor = hasPlayer ? PlayerID.PlayerTankColors[client.Id] : Color.DarkGray * 0.5f;

            renderer.Draw(whiteTex, animRect, cardColor * alpha);
            DrawUtils.DrawBoxWithOutline(renderer, animRect, Color.Transparent, borderColor * alpha, 2);

            if (hasPlayer) {
                var namePos = new Vector2(animRect.Center.X, animRect.Y + 20.ToResolutionY());
                DrawUtils.DrawStringWithBorder(renderer, FontGlobals.RebirthFontLarge, client.Name, namePos,
                    Color.White * alpha, Color.Black * alpha, new Vector2(0.4f).ToResolution() * cardScale, 0f, Anchor.Center);

                // icon + pulsing
                var tankTex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank2d");
                var iconPos = new Vector2(animRect.Center.X, animRect.Center.Y);

                float pulse = 1f + 0.05f * (float)Math.Sin(RuntimeData.RunTime * 0.1f + i);

                renderer.Draw(tankTex, iconPos, null, borderColor * alpha, 0f,
                    tankTex.Size() / 2, new Vector2(1.2f).ToResolution() * cardScale * pulse, SpriteEffects.None, 0f);

                // ping, if host
                if (Client.IsHost()) {
                    var ping = Server.NetManager.ConnectedPeerList.Count > i ? Server.NetManager.ConnectedPeerList[NetPlay.ReversePeerMap[i]].Ping : 0;
                    var badPing = 250;
                    var sColor = new StatisticalColor<int>(Color.Lime, Color.Red, 30, ping, badPing);

                    Vector2 pingPos = new Vector2(animRect.Center.X, animRect.Bottom - 20.ToResolutionY());

                    // ping dot + color coded by horribleness
                    renderer.Draw(whiteTex, new Rectangle((int)pingPos.X - 30, (int)pingPos.Y - 5, 10, 10), sColor.FinalColor * alpha);

                    DrawUtils.DrawStringWithBorder(renderer, FontGlobals.RebirthFont, $"{ping}ms", pingPos,
                        Color.LightGray * alpha, Color.Black * alpha, new Vector2(0.6f).ToResolution() * cardScale, 0f, Anchor.Center);
                }
            }
            else {
                // drawn if it's an empty slot
                Vector2 textPos = new Vector2(animRect.Center.X, animRect.Center.Y);
                DrawUtils.DrawStringWithBorder(renderer, FontGlobals.RebirthFont, "Empty", textPos,
                    Color.Gray * alpha * 0.5f, Color.Black * alpha * 0.5f, new Vector2(0.5f).ToResolution() * cardScale, 0f, Anchor.Center);
            }
        }
    }
}