using System;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems
{
    public sealed record ChatSystem
    {
        public struct ChatTag {
            // TODO: start
        }

        public delegate void OnMessageAddedDelegate(string message);
        public static event OnMessageAddedDelegate OnMessageAdded;

        public static List<ChatMessage> ChatMessages { get; private set; } = new();
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
        /// <summary>
        /// Sends a new <see cref="ChatMessage"/> to the chat.
        /// </summary>
        /// <param name="message">The content of the <see cref="ChatMessage"/>.</param>
        /// <param name="color">The color in which to render the content of the <see cref="ChatMessage"/>.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="netSend">If true, will not send the message to the server in a multiplayer context.</param>
        /// <returns>The <see cref="ChatMessage"/> sent to the chat.</returns>
        public static ChatMessage[] SendMessage(string message, Color color, string sender = null, bool netSend = false)
        {

            List<ChatMessage> msgs = new();

            if (sender is not null)
            {
                SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick", SoundContext.Effect);
                if (Client.IsConnected() && !netSend)
                    Client.SendMessage(message.ToString(), color, sender.ToString());
            }

            var split = message.Split('\n');

            for (int i = 0; i < split.Length; i++)
            {
                if (sender is not null)
                    msgs.Add(new ChatMessage($"<{sender}> {message}", color));
                else
                    msgs.Add(new ChatMessage($"{message}", color));
            }
            Alerts++;
            ChatMessages.AddRange(msgs);

            OnMessageAdded?.Invoke(message);
            return msgs.ToArray();
        }

        private static void DrawChatBox(out Rectangle chatBox, out Rectangle typeBox)
        {
            var measureY = ChatMessage.Font.MeasureString("X").Y * Scale.Y;
            // draw it out of view if not open chat box.
            var chatRect = new Rectangle((int)OpenOrigin.X, IsOpen ? (int)OpenOrigin.Y : -400, BoxWidth, (int)(measureY * MessagesAtOnce)).ToResolution();

            var typeRect = new Rectangle(chatRect.X, chatRect.Y + chatRect.Height + (int)8.ToResolutionY(), chatRect.Width, (int)(ChatMessage.Font.MeasureString(CurTyping).Y + (CurTyping.Length == 0 ? 32.ToResolutionY() : 0)));

            // TODO: do it. do it.

            // one box for the chat which is bigger, one box for the text, which scales to text size.

            chatBox = chatRect;
            typeBox = typeRect;
        }
        public static Keybind ToggleChat = new("Toggle Chat", Keys.F2);
        public static void DrawMessages()
        {
            if (ToggleChat.JustPressed)
                IsOpen = !IsOpen;
            #region Draw Chat

            TankGame.SpriteRenderer.Begin();

            DrawChatBox(out var chatRect, out var typeRect);

            var crc = chatRect.Contains(MouseUtils.MousePosition);
            var trc = typeRect.Contains(MouseUtils.MousePosition);

            var alpha1 = crc ? 0.9f : 0.45f;
            var alpha2 = trc ? 0.9f : 0.45f;

            ChatBoxHover = crc || trc;

            // var alpha = shiftAlpha ? 0.9f : 0.45f;

            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, chatRect, Color.Gray * alpha1);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, typeRect, Color.Gray * alpha2);
            TankGame.SpriteRenderer.DrawString(ChatMessage.Font, CurTyping, new Vector2(typeRect.X, typeRect.Y), Color.White, Scale.ToResolution());

            TankGame.SpriteRenderer.End();

            var boxRasterizer = new RasterizerState()
            {
                ScissorTestEnable = true,
            };

            TankGame.SpriteRenderer.Begin(rasterizerState: boxRasterizer);

            TankGame.Instance.GraphicsDevice.ScissorRectangle = chatRect;

            var basePosition = new Vector2(chatRect.X + 8.ToResolutionX(), chatRect.Y + 8.ToResolutionY());
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
                if (trc && !ActiveHandle)
                {
                    TankGame.Instance.Window.TextInput += HandleInput;
                    ActiveHandle = true;
                }
            }

            if (ChatMessages.Count >= MessagesAtOnce)
            {
                ChatMessages[0] = default;
                var arr = ChatMessages.ToArray();
                arr = ArrayUtils.Shift(arr, -1);
                Array.Resize(ref arr, arr.Length - 1);
                ChatMessages = arr.ToList();
            }

            for (int i = 0; i < ChatMessages.Count; i++)
            {
                var pos = basePosition + new Vector2(0, (i * offset).ToResolutionY());
                TankGame.SpriteRenderer.DrawString(ChatMessage.Font, ChatMessages[i].Content, pos, ChatMessages[i].Color, Scale, 0f, drawOrigin);
            }

            TankGame.SpriteRenderer.End();

            #endregion

            if (IsOpen) {
                Alerts = 0;
            }
            else {
                TankGame.SpriteRenderer.Begin();
                var scale = new Vector2(0.2f);
                if (Alerts > 0) {
                    var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/chatalert");
                    TankGame.SpriteRenderer.Draw(tex, OpenOrigin.ToResolution(), null, Color.White, 0f, Vector2.Zero, scale, default, default);
                    TankGame.SpriteRenderer.DrawString(ChatMessage.Font, Alerts.ToString(), OpenOrigin.ToResolution() + (tex.Size() * scale) - new Vector2(12, 12).ToResolution(), Color.White, scale * 3f);
                    TankGame.SpriteRenderer.DrawString(ChatMessage.Font, $"Press [{ToggleChat.AssignedKey}] to open chat.", OpenOrigin.ToResolution() + new Vector2(tex.Size().X * scale.X + 10.ToResolutionX(), 0), Color.White, scale * 3f);
                }
                else
                    TankGame.SpriteRenderer.DrawString(ChatMessage.Font, $"Press [{ToggleChat.AssignedKey}] to open chat.", OpenOrigin.ToResolution(), Color.White, scale * 3f);
                // TODO: draw an alertbox saying "!1" or something similar.
                TankGame.SpriteRenderer.End();
            }
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
                    TankGame.Instance.Window.TextInput -= HandleInput;
                    ActiveHandle = false;

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
        public static SpriteFontBase Font = TankGame.TextFont;

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
}
