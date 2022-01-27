using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WiiPlayTanksRemake.Internals.Common.Utilities
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

        /// <summary>
        /// Remove non-valid font characters from the given text to be safe-to-draw
        /// </summary>
        /// <param name="font">Font to check valid characters</param>
        /// <param name="text">Text to valid</param>
        /// <returns>Safe-to-draw text</returns>
        public static void GetSafeFontText(SpriteFont font, string defaultText, out string text)
        {
            text = "";

            foreach (char letter in defaultText)
			{
                if (letter == '\n' || letter == '\r' || font.Characters.Contains(letter))
                {
                    text += letter;
                }
                else
                { 
                    text += "?";
				}
			}
        }

        private static readonly string[] DebuggingNames =
        {
            "General",
            "TankStats",
            "UIElements",
            "Powerups"
        };
        public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, int level = 0, float scaleOverride = 1f, bool centerIt = false, bool beginSb = false)
        {
            if (!DebuggingEnabled || DebugLevel != level)
                return;

            if (beginSb)
                sb.Begin();

            GetSafeFontText(TankGame.Fonts.Default, info.ToString(), out string text);
            sb.DrawString(TankGame.Fonts.Default, text, position, Color.White, 0f, centerIt ? TankGame.Fonts.Default.MeasureString(text) / 2 : default, scaleOverride * 0.6f, default, default); 

            if (beginSb)
                sb.End();
        }
    }
}