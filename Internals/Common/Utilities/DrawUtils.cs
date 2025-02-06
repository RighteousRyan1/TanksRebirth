using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.Internals.Common.Utilities;

public static class DrawUtils {
    public static void DrawBorderedText(SpriteBatch spriteBatch, SpriteFontBase font, string text, Vector2 position, Color textColor, Color borderColor, Vector2 scale, float rotation, Anchor anchoring, float borderThickness = 1f) {
        for (int i = 0; i < 4; i++)
            spriteBatch.DrawString(font, text, position + new Vector2(0, 2f * borderThickness).Rotate(MathHelper.PiOver2 * i + MathHelper.PiOver4),
                borderColor, scale, rotation, GameUtils.GetAnchor(anchoring, font.MeasureString(text)), 0f);
        spriteBatch.DrawString(font, text, position, textColor, scale, rotation, GameUtils.GetAnchor(anchoring, font.MeasureString(text)), 1f);
    }
    public static void DrawBorderedTexture(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color textureColor, Color borderColor, Vector2 scale, float rotation, Anchor anchoring, float borderThickness = 1f) {
        for (int i = 0; i < 4; i++)
            spriteBatch.Draw(texture, position + new Vector2(0, 2f * borderThickness).Rotate(MathHelper.PiOver2 * i + MathHelper.PiOver4),
                null, borderColor, rotation, GameUtils.GetAnchor(anchoring, texture.Size()), scale, default, 0f);
        spriteBatch.Draw(texture, position, null, textureColor, rotation, GameUtils.GetAnchor(anchoring, texture.Size()), scale, default, 1f);
    }
    public static void DrawShadowedString(SpriteFontBase font, Vector2 position, Vector2 shadowDir, string text, Color color, Vector2 scale, float alpha, Vector2 origin = default, float shadowDistScale = 1f) {
        TankGame.SpriteRenderer.DrawString(font, text, position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale), Color.Black * alpha * 0.75f, scale, 0f, origin == default ? TankGame.TextFontLarge.MeasureString(text) / 2 : origin, 0f);

        TankGame.SpriteRenderer.DrawString(font, text, position, color * alpha, scale, 0f, origin == default ? TankGame.TextFontLarge.MeasureString(text) / 2 : origin, 0f);
    }
    public static void DrawShadowedTexture(Texture2D texture, Vector2 position, Vector2 shadowDir, Color color, Vector2 scale, float alpha, Vector2 origin = default, bool flip = false, float shadowDistScale = 1f) {
        TankGame.SpriteRenderer.Draw(texture,
            position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale),
            null,
            Color.Black * alpha * 0.75f,
            0f,
            origin == default ? texture.Size() / 2 : origin,
            scale,
            flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            default);
        TankGame.SpriteRenderer.Draw(texture, position, null, color * alpha, 0f, origin == default ? texture.Size() / 2 : origin, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, default);
    }
}
