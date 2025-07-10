using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.Internals.Common.Utilities;

public static class DrawUtils {
    /// <summary>Converts a centered-orthographic coordinate to screen coordinates.</summary>
    public static Vector2 CenteredOrthoToScreen(Vector2 orthoPos) {
        // i think this works. probably not lol.
        var vector = orthoPos - WindowUtils.WindowBounds / 2;
        var negativeVector = new Vector2(vector.X, -vector.Y);
        return negativeVector;
    }
    public static void DrawTextWithBorder(SpriteBatch spriteBatch, SpriteFontBase font, string text, Vector2 position, 
        Color textColor, Color borderColor, Vector2 scale, float rotation, Anchor anchor = Anchor.Center, float borderThickness = 1f, float charSpacing = 0,
        float origMeasureScale = 1f) {

        DrawStringBorderOnly(spriteBatch, font, text, position, borderColor, scale, rotation, anchor, borderThickness, charSpacing, origMeasureScale);

        spriteBatch.DrawString(font, text, position, textColor, scale, rotation, GameUtils.GetAnchor(anchor, font.MeasureString(text) * origMeasureScale), 1f, characterSpacing: charSpacing);
    }
    public static void DrawTextureWithBorder(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color textureColor, Color borderColor, 
        Vector2 scale, float rotation, Anchor anchor = Anchor.Center, float borderThickness = 1f) {
        for (int i = 0; i < 4; i++)
            spriteBatch.Draw(texture, position + new Vector2(0, 2f * borderThickness).Rotate(MathHelper.PiOver2 * i + MathHelper.PiOver4),
                null, borderColor, rotation, GameUtils.GetAnchor(anchor, texture.Size()), scale, default, 0f);
        spriteBatch.Draw(texture, position, null, textureColor, rotation, GameUtils.GetAnchor(anchor, texture.Size()), scale, default, 1f);
    }
    public static void DrawStringWithShadow(SpriteBatch spriteBatch, SpriteFontBase font, Vector2 position, Vector2 shadowDir, 
        string text, Color color, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        float shadowDistScale = 1f, float shadowAlpha = 1f, float origMeasureScale = 1f, float charSpacing = 0) {

        DrawStringShadowOnly(spriteBatch, font, position, shadowDir, text, scale, alpha, anchor, shadowDistScale, shadowAlpha, origMeasureScale, charSpacing);

        spriteBatch.DrawString(font, text, position, color * alpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
    }
    public static void DrawTextureWithShadow(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Vector2 shadowDir, 
        Color color, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        SpriteEffects flip = SpriteEffects.None, float shadowDistScale = 1f, float shadowAlpha = 1f, float rotation = 0f, Rectangle? srcRect = null) {

        if (shadowAlpha > 0) {
            spriteBatch.Draw(texture,
                position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale),
                srcRect,
                Color.Black * alpha * shadowAlpha,
                rotation,
                anchor.GetAnchor(texture.Size()),
                scale,
                flip,
                default);
        }
        spriteBatch.Draw(texture, position, srcRect, color * alpha, rotation, anchor.GetAnchor(texture.Size()), scale, flip, default);
    }
    public static void DrawBorderedStringWithShadow(SpriteBatch spriteBatch, SpriteFontBase font, Vector2 position, Vector2 shadowDir, 
        string text, Color color, Color borderColor, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        float shadowDistScale = 1f, float shadowAlpha = 1f, float borderThickness = 1f, float charSpacing = 0, float origMeasureScale = 1f) {

        spriteBatch.DrawString(font, text, position + 
            Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale), 
            Color.Black * alpha * shadowAlpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);

        DrawTextWithBorder(spriteBatch, font, text, position, color * alpha, borderColor * alpha, scale, 0f, anchor, borderThickness, charSpacing: charSpacing, origMeasureScale: origMeasureScale); 
    }

    public static void DrawStringBorderOnly(SpriteBatch spriteBatch, SpriteFontBase font, string text, Vector2 position,
        Color borderColor, Vector2 scale, float rotation, Anchor anchor = Anchor.Center, float borderThickness = 1f, float charSpacing = 0,
        float origMeasureScale = 1f) {
        for (int i = 0; i < 4; i++)
            spriteBatch.DrawString(font, text, position + new Vector2(0, 2f * borderThickness).Rotate(MathHelper.PiOver2 * i + MathHelper.PiOver4).ToResolution(),
                borderColor, scale, rotation, GameUtils.GetAnchor(anchor, font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
    }
    public static void DrawStringShadowOnly(SpriteBatch spriteBatch, SpriteFontBase font, Vector2 position, Vector2 shadowDir,
    string text, Vector2 scale, float alpha, Anchor anchor = Anchor.Center,
    float shadowDistScale = 1f, float shadowAlpha = 1f, float origMeasureScale = 1f, float charSpacing = 0) {

        spriteBatch.DrawString(font, text, position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale),
            Color.Black * alpha * shadowAlpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
    }

    public static float GetTextXOffsetForSpacing(string text, float spacing) {
        var multiplicand = text.Length - 2;

        // nothing to do lol
        if (multiplicand < 0)
            multiplicand = 0;

        return spacing * multiplicand;
    }
    
    public static void DrawNineSliced(SpriteBatch spriteBatch, Texture2D texture, int border, Rectangle area, Color color, Vector2 origin) {
        Point useBorder = new Vector2(border, border).ToResolution().ToPoint();

        int middleX = area.X + useBorder.X;
        int rightX = area.Right - useBorder.X;

        int middleY = area.Y + useBorder.Y;
        int bottomY = area.Bottom - useBorder.Y;

        spriteBatch.Draw(texture, new Rectangle(area.X, area.Y, useBorder.X, useBorder.Y), new Rectangle(0, 0, border, border), color);
        spriteBatch.Draw(texture, new Rectangle(middleX, area.Y, area.Width - useBorder.X * 2, useBorder.Y), new Rectangle(border, 0, texture.Width - border * 2, border), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(rightX, area.Y, useBorder.X, useBorder.Y), new Rectangle(texture.Width - border, 0, border, border), color, 0f, origin, default, 0f);

        spriteBatch.Draw(texture, new Rectangle(area.X, middleY, useBorder.X, area.Height - useBorder.Y * 2), new Rectangle(0, border, border, texture.Height - border * 2), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(middleX, middleY, area.Width - useBorder.X * 2, area.Height - useBorder.Y * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(rightX, middleY, useBorder.X, area.Height - useBorder.Y * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), color, 0f, origin, default, 0f);

        spriteBatch.Draw(texture, new Rectangle(area.X, bottomY, useBorder.X, useBorder.Y), new Rectangle(0, texture.Height - border, border, border), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, area.Width - useBorder.X * 2, useBorder.Y), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, useBorder.X, useBorder.Y), new Rectangle(texture.Width - border, texture.Height - border, border, border), color, 0f, origin, default, 0f);
    }
}
