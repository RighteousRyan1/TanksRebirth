using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;

namespace TanksRebirth.Internals.Common.Utilities
{
    public static class DebugUtils
    {
        public static string CurDebugLabel
        {
            get
            {
                if (DebugLevel < 0 || DebugLevel >= DebuggingNames.Length)
                    return $"Unknown - {DebugLevel}";
                else
                    return DebuggingNames[DebugLevel];
            }
        }
        public static bool DebuggingEnabled { get; set; }
        public static int DebugLevel { get; set; }

        private static readonly string[] DebuggingNames =
        {
            "General",
            "Entity Data",
            "Player Data",
            "Level Edit Debug",
            "Powerups"
        };
        public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, int level = 0, float scale = 1f, bool centered = false, Color color = default, bool beginSb = false)
        {
            if (!DebuggingEnabled || DebugLevel != level)
                return;

            if (beginSb)
                sb.Begin();

            var sizeAdjust = new Vector2(scale * 0.15f * (float)(GameUtils.WindowWidth / 1920f), scale * 0.15f * (float)(GameUtils.WindowHeight / 1080f));

            sb.DrawString(TankGame.TextFontLarge, info.ToString(), position, color == default ? Color.White : color,  sizeAdjust, 0f, centered ? TankGame.TextFont.MeasureString(info.ToString()) / 2 : default); 

            if (beginSb)
                sb.End();
        }

        public static void DrawDebugTexture(SpriteBatch sb, Texture2D texture, Vector2 position, int level = 0, float scale = 1f, Color color = default, bool centered = false, bool beginSb = false)
        {
            if (!DebuggingEnabled || DebugLevel != level)
                return;

            if (beginSb)
                sb.Begin();

            sb.Draw(texture, position, null, color == default ? Color.White : color, 0f, centered ? texture.Size() / 2 : default, scale, default, 0f);

            if (beginSb)
                sb.End();
        }
    }
}