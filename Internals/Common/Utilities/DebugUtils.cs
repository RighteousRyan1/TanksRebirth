using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;

namespace TanksRebirth.Internals.Common.Utilities;

public static class DebugUtils
{
    public readonly struct Id {
        public const int General = 0;
        public const int EntityData = 1;
        public const int PlayerData = 2;
        public const int LevelEditDebug = 3;
        public const int PowerUps = 4;
        public const int AchievementData = 5;
    }
    public static string CurDebugLabel {
        get {
            if (DebugLevel < 0 || DebugLevel >= DebuggingNames.Length)
                return $"Unknown - {DebugLevel}";
            return DebuggingNames[DebugLevel];
        }
    }
    public static bool DebuggingEnabled { get; set; }
    public static int DebugLevel { get; set; }

    private static readonly string[] DebuggingNames = {
        "General",
        "Entity Data",
        "Player Data",
        "Level Edit Debug",
        "Powerups",
        "Achievement Data"
    };
    public static void DrawDebugString(this SpriteBatch sb, object info, Vector2 position, int level = Id.General, float scale = 1f, bool centered = false, Color color = default, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        var sizeAdjust = new Vector2(scale * 0.6f * (float)(WindowUtils.WindowWidth / 1920f), scale * 0.6f * (float)(WindowUtils.WindowHeight / 1080f));

        sb.DrawString(TankGame.TextFont, info.ToString(), position, color == default ? Color.White : color, sizeAdjust, 0f, centered ? TankGame.TextFont.MeasureString(info.ToString()) / 2 : default);

        if (beginSb)
            sb.End();
    }
    public static void DrawDebugString(this SpriteFontBase font, SpriteBatch sb, object info, Vector2 position, int level = Id.General, float scale = 1f, bool centered = false, Color color = default, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        var sizeAdjust = new Vector2(scale * 0.6f * (float)(WindowUtils.WindowWidth / 1920f), scale * 0.6f * (float)(WindowUtils.WindowHeight / 1080f));

        sb.DrawString(font, info.ToString(), position, color == default ? Color.White : color, sizeAdjust, 0f, centered ? TankGame.TextFont.MeasureString(info.ToString()) / 2 : default);

        if (beginSb)
            sb.End();
    }
    public static void DrawDebugTexture(this SpriteBatch sb, Texture2D texture, Vector2 position, int level = Id.General, float scale = 1f, Color color = default, bool centered = false, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        sb.Draw(texture, position, null, color == default ? Color.White : color, 0f, centered ? texture.Size() / 2 : default, scale, default, 0f);

        if (beginSb)
            sb.End();
    }
}
