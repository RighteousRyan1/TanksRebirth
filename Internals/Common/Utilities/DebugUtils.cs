using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WiiPlayTanksRemake.Internals.Common.Utilities
{
    public static class DebugUtils
    {
        public static bool DebuggingEnabled { get; set; }
        public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, float scaleOverride = 1f, bool beginSb = false)
        {
            if (!DebuggingEnabled)
                return;

            if (beginSb)
                sb.Begin();

            sb.DrawString(TankGame.Fonts.Default, info.ToString(), position, Color.White, 0f, default, scaleOverride, default, default); 

            if (beginSb)
                sb.End();
        }
    }
}