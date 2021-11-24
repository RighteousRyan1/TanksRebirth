using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WiiPlayTanksRemake.Internals.Common.Utilities
{
    public static class DebugUtils
    {
        public static bool DebuggingEnabled { get; set; }
        public static int DebugLevel { get; set; }
        public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, int level = 0, float scaleOverride = 1f, bool beginSb = false)
        {
            if (!DebuggingEnabled || DebugLevel != level)
                return;

            if (beginSb)
                sb.Begin();

            sb.DrawString(TankGame.Fonts.Default, info.ToString(), position, Color.White, 0f, default, scaleOverride * 0.6f, default, default); 

            if (beginSb)
                sb.End();
        }
    }
}