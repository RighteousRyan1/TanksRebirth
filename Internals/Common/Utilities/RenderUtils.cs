using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.Internals.Common.Utilities;

public static class RenderUtils
{
    public static Vector2 Size(this Texture2D tex) => new(tex.Width, tex.Height);
    public static Vector2 GetTextureAnchor(this Anchor a, Texture2D tex) {
        return a switch {
            Anchor.TopLeft => Vector2.Zero,
            Anchor.TopRight => new(tex.Width, 0),
            Anchor.BottomLeft => new(0, tex.Height),
            Anchor.BottomRight => new(tex.Width, tex.Height),
            Anchor.LeftCenter => new(0, tex.Height / 2),
            Anchor.RightCenter => new(tex.Width, tex.Height / 2),
            Anchor.Center => new(tex.Width / 2, tex.Height / 2),
            Anchor.TopCenter => new(tex.Width / 2, 0),
            Anchor.BottomCenter => new(tex.Width / 2, tex.Height),
            _ => default,
        };
    }
    public static void DrawStringAtMouse(SpriteFontBase font, object text) => TankGame.SpriteRenderer.DrawString(font, text.ToString(), MouseUtils.MousePosition + new Vector2(25), Color.White, new Vector2(1f), 0f, Vector2.Zero);
}
