using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems
{
    public sealed record ChatSystem
    {
        public const int CHAT_MESSAGE_CACHE_CAPACITY = 10000;
        public static List<ChatMessage> ChatMessages { get; } = new();

        public static List<ChatMessage> MessageCache { get; } = new(CHAT_MESSAGE_CACHE_CAPACITY);

        public static ChatMessageCorner Corner { get; set; } = ChatMessageCorner.TopLeft;

        /// <summary>
        /// Sends a new <see cref="ChatMessage"/> to the chat.
        /// </summary>
        /// <param name="text">The content of the <see cref="ChatMessage"/>.</param>
        /// <param name="color">The color in which to render the content of the <see cref="ChatMessage"/>.</param>
        /// <returns>The <see cref="ChatMessage"/> sent to the chat.</returns>
        public static ChatMessage SendMessage(object text, Color color, object sender = null)
        {
            ChatMessage msg;
            if (sender is not null)
                msg = new ChatMessage($"{sender}: {text}", color);
            else
                msg = new ChatMessage($"{text}", color);

            ChatMessages.Add(msg);
            MessageCache.Add(msg);

            if (sender is not null)
            {
                SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/sounds/menu/menu_tick"), SoundContext.Effect);
                if (Client.IsConnected())
                    Client.SendMessage(text.ToString(), color, sender.ToString());
            }

            return msg;
        }

        public static void DrawMessages()
        {
            var basePosition = new Vector2();
            var offset = 0f;
            for (int i = 0; i < ChatMessages.Count; i++)
            {
                var drawOrigin = new Vector2();

                var measure = ChatMessage.Font.MeasureString(ChatMessages[i].Content);

                var sb = TankGame.spriteBatch;

                switch (Corner)
                {
                    case ChatMessageCorner.TopLeft:
                        basePosition = new(20);
                        drawOrigin = new(0, measure.Y / 2);
                        offset = 15f;

                        break;
                    case ChatMessageCorner.TopRight:
                        basePosition = new(GameUtils.WindowWidth - 20, 20);
                        drawOrigin = new(measure.X, measure.Y / 2);
                        offset = 15f;

                        break;
                    case ChatMessageCorner.BottomLeft:
                        basePosition = new(20, GameUtils.WindowHeight - 20);
                        drawOrigin = new(0, measure.Y / 2);
                        offset = -15f;

                        break;
                    case ChatMessageCorner.BottomRight:
                        basePosition = new(GameUtils.WindowWidth - 20, GameUtils.WindowHeight - 20);
                        drawOrigin = new(measure.X, measure.Y / 2);
                        offset = -15f;

                        break;
                }

                if (i > 5)
                {
                    ChatMessages[4].lifeTime = 0;
                }

                sb.DrawString(ChatMessage.Font, ChatMessages[i].Content, basePosition + new Vector2(0, i * offset), ChatMessages[i].Color, new Vector2(0.8f), 0f, drawOrigin);

                ChatMessages[i].lifeTime--;

                if (ChatMessages[i].lifeTime <= 0)
                    ChatMessages.RemoveAt(i);
            }
        }
    }

    /// <summary>Represents a system used to store messages and their contents in use with the <see cref="ChatSystem"/>.</summary>
    public sealed class ChatMessage
    {
        /// <summary>The content of this <see cref="ChatMessage"/>.</summary>
        public string Content;

        /// <summary>The color of the content of this <see cref="ChatMessage"/>.</summary>
        public Color Color;

        /// <summary>The <see cref="SpriteFont"/> in which to use to render the content of this <see cref="ChatMessage"/>.</summary>
        public static SpriteFontBase Font = TankGame.TextFont;

        /// <summary>The duration this <see cref="ChatMessage"/> will persist for.</summary>
        public int lifeTime = 150;

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
