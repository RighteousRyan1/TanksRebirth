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
        for (int i = 0; i < 4; i++)
            spriteBatch.DrawString(font, text, position + new Vector2(0, 2f * borderThickness).Rotate(MathHelper.PiOver2 * i + MathHelper.PiOver4),
                borderColor, scale, rotation, GameUtils.GetAnchor(anchor, font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
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

        spriteBatch.DrawString(font, text, position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale), 
            Color.Black * alpha * shadowAlpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);

        spriteBatch.DrawString(font, text, position, color * alpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
    }
    public static void DrawTextureWithShadow(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Vector2 shadowDir, 
        Color color, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        bool flip = false, float shadowDistScale = 1f, float shadowAlpha = 1f, float rotation = 0f, Rectangle? srcRect = null) {

        if (shadowAlpha > 0) {
            spriteBatch.Draw(texture,
                position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale),
                srcRect,
                Color.Black * alpha * shadowAlpha,
                rotation,
                anchor.GetAnchor(texture.Size()),
                scale,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                default);
        }
        spriteBatch.Draw(texture, position, srcRect, color * alpha, rotation, anchor.GetAnchor(texture.Size()), scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, default);
    }
    public static void DrawBorderedStringWithShadow(SpriteBatch spriteBatch, SpriteFontBase font, Vector2 position, Vector2 shadowDir, 
        string text, Color color, Color borderColor, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        float shadowDistScale = 1f, float shadowAlpha = 1f, float borderThickness = 1f, float charSpacing = 0, float origMeasureScale = 1f) {

        spriteBatch.DrawString(font, text, position + 
            Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale), 
            Color.Black * alpha * shadowAlpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);

        DrawTextWithBorder(spriteBatch, font, text, position, color * alpha, borderColor * alpha, scale, 0f, anchor, borderThickness, charSpacing: charSpacing, origMeasureScale: origMeasureScale); 
    }


    public static float GetTextXOffsetForSpacing(string text, float spacing) {
        var multiplicand = text.Length - 2;

        // nothing to do lol
        if (multiplicand < 0)
            multiplicand = 0;

        return spacing * multiplicand;
    }
}
