using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems.CommandsSystem;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems;

/// <summary>A system for handling chat.</summary>
public sealed record ChatSystem {
    // TODO: add more here.
    public struct ChatTag {
        // TODO: start sometime eventually?
        // [HEX###:text]
        // [r,g,b: text]

        public static readonly char[] Hexadecimals = [
            'A', 'B', 'C', 'D', 'E', 'F',
            '0','1','2','3','4','5','6','7','8','9'
        ];

        /// <summary>The color for each character in the string array.</summary>
        public readonly Color[] Colors;
        public readonly string ParsedString;
        public ChatTag(string tag) {
            ParsedString = string.Empty;
            Colors = new Color[tag.Length];

            var lastOpenBracket = -1;

            for (int i = 0; i < tag.Length; i++) {
                var c = tag[i];

                if (c == '[') {
                    lastOpenBracket = i;
                    bool success = true;
                    // check all characters within 6 spaces ahead of the open bracket to see if it is hexadecimal
                    for (int j = 1; j <= 6; j++) {
                        if (!Hexadecimals.Contains(c)) {
                            success = false;
                            break;
                        }
                    }
                    if (!success)
                        continue;
                    // check one space after the final hexadecimal value, since the check was successful.
                    if (tag[i + 7] != ':')
                        continue;
                    // now read the rest of the string as intended.
                    bool endRead = false;
                    for (int k = i + 8; k < tag.Length; k++) {
                        var v = tag[k];

                        if (v == ']') {
                            endRead = true;
                            break;
                        }
                        else {
                            ParsedString += v;
                        }
                        // no clue if ts works :sob:
                    }
                }
            }
        }

        private void Colorize(int from, int to) {

        }
    }

    public delegate void OnMessageAddedDelegate(string message);
    public static event OnMessageAddedDelegate? OnMessageAdded;

    public static List<ChatMessage> ChatMessages { get; private set; } = [];
    public static int Alerts;
    public static bool IsOpen;
    public static ChatMessageCorner Corner { get; set; } = ChatMessageCorner.TopLeft;

    public static Vector2 OpenOrigin = new(8, 8);

    public static Vector2 Scale = new(0.8f);

    public static int MessagesAtOnce = 10;

    public static string CurTyping = string.Empty;
    public static bool ActiveHandle;
    public static int MaxLength = 100;
    public static int BoxWidth = 750;

    public static bool ChatBoxHover;

    public static Texture2D ChatAlert;

    public static void Initialize() {
        ChatAlert = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/chatalert");
    }
    /// <summary>
    /// Sends a new <see cref="ChatMessage"/> to the chat.
    /// </summary>
    /// <param name="contents">The content of the <see cref="ChatMessage"/>.</param>
    /// <param name="color">The color in which to render the content of the <see cref="ChatMessage"/>.</param>
    /// <param name="sender">The sender of the message, e.g: a player</param>
    /// <param name="netSend">If true, will send the message to the server in a multiplayer context.</param>
    public static void SendMessage(object contents, Color color, string? sender = null, bool netSend = false)
    {
        var message = contents.ToString()!;
        if (message.Length > 0 && message[0] == CommandGlobals.ExpectedPrefix) {
            var cmdSplit = message.Remove(0, 1).Split(' ');
            var cmdName = cmdSplit[0];
            var index = CommandGlobals.Commands.Keys.ToList().FindIndex(cmd => cmd.Name == cmdName);
            if (index > -1) {
                try {
                    var value = CommandGlobals.Commands.ElementAt(index).Value;

                    // if the length is equal to zero, the user has provided no arguments.
                    var args = cmdSplit.Length == 0 ? [] : cmdSplit[1..];

                    /*if (args.Length == 0 && !cmdSplit[0].Contains("help")) {
                        SendMessage("Invalid command syntax! Arguments missing.", Color.Red);
                        return;
                    }*/
                    
                    if (value.NetSync && Client.IsConnected()) {
                        if (sender != "cmd_sync" && !Client.IsHost()) {
                            SendMessage("You cannot use this command as you are not the host of the server.", Color.Red);
                            return;
                        }
                        if (Client.IsHost())
                            Client.SendCommandUsage(message);
                    }
                    if (value.RequireCheats) {
                        if (CommandGlobals.AreCheatsEnabled)
                            value.ActionToPerform?.Invoke(args);
                        else
                            SendMessage("In order to use this command, cheats must be enabled.", Color.Red, "CMD");
                    }
                    else
                        value.ActionToPerform?.Invoke(args);
                    return;
                }
                catch(Exception e) {
                    TankGame.ReportError(e);
                    SendMessage("Error with command.", Color.Orange);
                }
            }
        }

        List<ChatMessage> msgs = [];

        if (sender is not null)
        {
            SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect);
            if (Client.IsConnected() && !netSend)
                Client.SendMessage(message.ToString(), color, sender.ToString());
        }

        var split = message.Split('\n');

        for (int i = 0; i < split.Length; i++)
        {
            if (i == 0 && sender is not null)
                msgs.Add(new ChatMessage($"<{sender}> {split[i]}", color));
            else
                msgs.Add(new ChatMessage($"{split[i]}", color));
        }
        Alerts++;

        ChatMessages.AddRange(msgs);

        OnMessageAdded?.Invoke(message);
        // return msgs.ToArray();
    }
    /// <summary>Sends a message in the color white.</summary>
    public static void SendMessage(object contents, string? sender = null, bool netSend = false) {
        SendMessage(contents, Color.White, sender, netSend);
    }

    private static void DrawChatBox(out Rectangle chatBox, out Rectangle typeBox)
    {
        // tallest letter i'm guessing?
        var measureY = (ChatMessage.Font.MeasureString("X").Y * Scale.Y).ToResolutionY();
        // draw it out of view if not open chat box.
        var chatRect = new Rectangle((int)OpenOrigin.X, IsOpen ? (int)OpenOrigin.Y : -5000, (int)BoxWidth.ToResolutionX(), (int)(measureY.ToResolutionY() * MessagesAtOnce));

        var typeRect = new Rectangle(chatRect.X, chatRect.Y + chatRect.Height + (int)8.ToResolutionY(), chatRect.Width, (int)(ChatMessage.Font.MeasureString(CurTyping).Y.ToResolutionY() + (CurTyping.Length == 0 ? 32.ToResolutionY() : 0)));

        // TODO: do it. do it.

        // one box for the chat which is bigger, one box for the text, which scales to text size.

        chatBox = chatRect;
        typeBox = typeRect;
    }
    public static Keybind ToggleChat = new("Toggle Chat", Keys.F2) {
        OnPress = () => IsOpen = !IsOpen
    };
    public static void DrawMessages()
    {
        #region Draw Chat

        TankGame.SpriteRenderer.Begin();

        DrawChatBox(out var chatRect, out var typeRect);

        var crc = chatRect.Contains(MouseUtils.MousePosition);
        var trc = typeRect.Contains(MouseUtils.MousePosition);

        var alpha1 = crc ? 0.9f : 0.45f;
        var alpha2 = trc ? 0.9f : 0.45f;

        ChatBoxHover = crc || trc;

        // var alpha = shiftAlpha ? 0.9f : 0.45f;

        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], chatRect, Color.Gray * alpha1);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], typeRect, Color.Gray * alpha2);
        TankGame.SpriteRenderer.DrawString(ChatMessage.Font, CurTyping, new Vector2(typeRect.X, typeRect.Y), Color.White, Scale.ToResolution());

        TankGame.SpriteRenderer.End();

        var boxRasterizer = new RasterizerState()
        {
            ScissorTestEnable = true,
        };

        TankGame.SpriteRenderer.Begin(rasterizerState: boxRasterizer);

        TankGame.Instance.GraphicsDevice.ScissorRectangle = chatRect;

        var basePosition = new Vector2(chatRect.X + 8.ToResolutionX(), chatRect.Y + chatRect.Height - 8.ToResolutionY());
        var offset = 20f;

        var drawOrigin = new Vector2();

        var measure = ChatMessage.Font.MeasureString(CurTyping).X * Scale.X;

        if (measure > typeRect.Width)
        {
            var lastChar = CurTyping[^1];
            CurTyping = CurTyping.Remove(CurTyping.Length - 1);
            CurTyping += '\n';
            CurTyping += lastChar;
        }

        if (InputUtils.CanDetectClick())
        {
            if (trc && !ActiveHandle) { 
                TankGame.Instance.Window.TextInput += HandleInput;
                ActiveHandle = true;
            }
            else if (!trc && ActiveHandle) {
                TankGame.Instance.Window.TextInput -= HandleInput;
                ActiveHandle = false;
            }
        }

        if (ActiveHandle) {
            if (InputUtils.AreKeysJustPressed(Keys.LeftControl, Keys.V)) {
                CurTyping += TextCopy.ClipboardService.GetText();
            }
            if (InputUtils.AreKeysJustPressed(Keys.LeftControl, Keys.C)) {
                TextCopy.ClipboardService.SetText(CurTyping);
            }
            if (ToggleChat.JustPressed) {
                TankGame.Instance.Window.TextInput -= HandleInput;
                ActiveHandle = false;
            }
        }

        if (ChatMessages.Count >= MessagesAtOnce)
        {
            ChatMessages[0] = default;
            var arr = ChatMessages.ToArray();
            arr = ArrayUtils.Shift(arr, -1);
            Array.Resize(ref arr, arr.Length - 1);
            ChatMessages = [.. arr];
        }

        for (int i = ChatMessages.Count - 1; i >= 0; i--)
        {
            var pos = basePosition - new Vector2(0, ((ChatMessages.Count - i) * offset).ToResolutionY());
            TankGame.SpriteRenderer.DrawString(ChatMessage.Font, ChatMessages[i].Content, pos, ChatMessages[i].Color, Scale.ToResolution(), 0f, drawOrigin);
        }

        TankGame.SpriteRenderer.End();

        if (IsOpen) {
            Alerts = 0;
        }
        else {
            TankGame.SpriteRenderer.Begin();
            var scale = new Vector2(0.2f);
            if (Alerts > 0) {
                TankGame.SpriteRenderer.Draw(ChatAlert, OpenOrigin.ToResolution(), null, Color.White, 0f, Vector2.Zero, scale, default, default);
                TankGame.SpriteRenderer.DrawString(ChatMessage.Font, Alerts.ToString(), OpenOrigin.ToResolution() + (ChatAlert.Size() * scale) - new Vector2(12, 12).ToResolution(), Color.White, scale * 3f);
                TankGame.SpriteRenderer.DrawString(ChatMessage.Font, TankGame.GameLanguage.Press + $" [{ToggleChat.Assigned}] " + TankGame.GameLanguage.ToToggleChat, OpenOrigin.ToResolution() + new Vector2(ChatAlert.Size().X * scale.X + 10.ToResolutionX(), 0), Color.White, scale * 3f);
            }
            else
                TankGame.SpriteRenderer.DrawString(ChatMessage.Font, TankGame.GameLanguage.Press + $" [{ToggleChat.Assigned}] " + TankGame.GameLanguage.ToToggleChat, OpenOrigin.ToResolution(), Color.White, scale * 3f);
            // TODO: draw an alertbox saying "!1" or something similar.
            TankGame.SpriteRenderer.End();
        }

        #endregion
    }
    private static void HandleInput(object sender, TextInputEventArgs e)
    {
        if (TankGame.Instance.IsActive) {

            if (e.Key == Keys.Back) {
                if (CurTyping.Length > 0)
                    CurTyping = CurTyping.Remove(CurTyping.Length - 1);
            }
            else if (e.Key == Keys.Escape) {
                CurTyping = string.Empty;
                TankGame.Instance.Window.TextInput -= HandleInput;
                ActiveHandle = false;
            }
            else if (e.Key == Keys.Tab)
                CurTyping += "   ";
            else if (e.Key == Keys.Enter) {

                if (CurTyping == string.Empty) {
                    TankGame.Instance.Window.TextInput -= HandleInput;
                    ActiveHandle = false;
                    return;
                }

                /*if (CurTyping.Contains('\n'))
                {
                    var split = CurTyping.Split('\n');

                    for (int i = 0; i < split.Length; i++)
                    {
                        string sender1 = null;

                        if (Client.IsConnected() && i == 0)
                            sender1 = NetPlay.CurrentClient.Name;

                        SendMessage(split[i], Color.White, sender1);
                    }
                }
                else
                {
                    string sender1 = null;

                    if (Client.IsConnected())
                        sender1 = NetPlay.CurrentClient.Name;

                    SendMessage(CurTyping, Color.White, sender1);
                }*/
                string sender1 = null;

                if (Client.IsConnected())
                    sender1 = NetPlay.CurrentClient.Name;
                SendMessage(CurTyping, Color.White, sender1);
                CurTyping = string.Empty;
            }
            else
            {
                if (CurTyping.Length < MaxLength)
                    CurTyping += e.Character;
            }
        }
    }
}

// TODO: perhaps struct.
/// <summary>Represents a system used to store messages and their contents in use with the <see cref="ChatSystem"/>.</summary>
public struct ChatMessage
{
    /// <summary>The content of this <see cref="ChatMessage"/>.</summary>
    public string Content;

    /// <summary>The color of the content of this <see cref="ChatMessage"/>.</summary>
    public Color Color;

    /// <summary>The <see cref="SpriteFont"/> in which to use to render the content of this <see cref="ChatMessage"/>.</summary>
    public static SpriteFontBase Font = FontGlobals.RebirthFont;

    /// <summary>
    /// Creates a new <see cref="ChatMessage"/>.
    /// </summary>
    /// <param name="content">The content of the <see cref="ChatMessage"/>.</param>
    /// <param name="color">The color in which to render the content of the <see cref="ChatMessage"/>.</param>
    public ChatMessage(string content, Color color)
    {
        Content = content;
        Color = color;
    }
}

public enum ChatMessageCorner
{
    TopLeft     = 0,
    TopRight    = 1,
    BottomLeft  = 2,
    BottomRight = 3 
}
