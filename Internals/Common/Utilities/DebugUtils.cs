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
        public static string GetSafeFontText(SpriteFont font, string text)
		{
            string safeText = "";

            foreach (char letter in text)
                if (letter == '\n' || letter == '\r' || font.Characters.Contains(letter))
                    safeText += letter;
                else
                    safeText += "?";

            return safeText;
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

            string text = GetSafeFontText(TankGame.Fonts.Default, info.ToString());
            sb.DrawString(TankGame.Fonts.Default, text, position, Color.White, 0f, centerIt ? TankGame.Fonts.Default.MeasureString( text ) / 2 : default, scaleOverride * 0.6f, default, default); 

            if (beginSb)
                sb.End();
        }
    }
}