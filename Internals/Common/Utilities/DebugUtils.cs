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
                    return "Unknown";
                else
                    return DebuggingNames[DebugLevel];
            }
        }
        public static bool DebuggingEnabled { get; set; }
        public static int DebugLevel { get; set; }

        private static readonly string[] DebuggingNames =
        {
            "General",
            "TankTeams",
            "UIElements"
        };
        public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, int level = 0, float scaleOverride = 1f, bool centerIt = false, bool beginSb = false)
        {
            if (!DebuggingEnabled || DebugLevel != level)
                return;

            if (beginSb)
                sb.Begin();

            sb.DrawString(TankGame.Fonts.Default, info.ToString(), position, Color.White, 0f, centerIt ? TankGame.Fonts.Default.MeasureString(info.ToString()) / 2 : default, scaleOverride * 0.6f, default, default); 

            if (beginSb)
                sb.End();
        }
    }
}