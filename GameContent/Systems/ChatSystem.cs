using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    public static class ChatSystem
    {
        public static List<ChatMessage> ChatMessages { get; } = new();

        public static ChatMessageCorner Corner { get; set; } = ChatMessageCorner.TopLeft;

        public static ChatMessage Send(object text, Color color)
        {
            var msg = new ChatMessage(text.ToString(), color);

            ChatMessages.Add(msg);

            return msg;
        }

        public static void DrawMessages()
        {
            var basePosition = new Vector2();
            var offset = 0f;
            for (int i = 0; i < ChatMessages.Count; i++)
            {
                var msg = ChatMessages[i];

                var drawOrigin = new Vector2();

                var measure = ChatMessage.Font.MeasureString(msg.Content);

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

                ChatMessages[i].lifeTime--;

                if (ChatMessages[i].lifeTime <= 0)
                    ChatMessages.RemoveAt(i);

                sb.DrawString(ChatMessage.Font, msg.Content, basePosition + new Vector2(0, i * offset), msg.Color, 0f, drawOrigin, 0.8f, default, default);
            }
        }
    }
    public class ChatMessage
    {
        public string Content { get; }
        public Color Color { get; set; }
        public static SpriteFont Font => TankGame.Fonts.Default;

        public int lifeTime = 150;

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
