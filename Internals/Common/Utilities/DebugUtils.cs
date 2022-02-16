using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;

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

        private static readonly string[] DebuggingNames =
        {
            "General",
            "TankStats",
            "UIElements",
            "Powerups"
        };
        public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, int level = 0, float scaleOverride = 1f, bool centerIt = false, Color colorOverride = default, bool beginSb = false)
        {
            if (!DebuggingEnabled || DebugLevel != level)
                return;

            if (beginSb)
                sb.Begin();

            sb.DrawString(TankGame.TextFont, info.ToString(), position, colorOverride == default ? Color.White : colorOverride,  new Vector2(scaleOverride * 0.6f), 0f, centerIt ? TankGame.TextFont.MeasureString(info.ToString()) / 2 : default); 

            if (beginSb)
                sb.End();
        }
    }
}