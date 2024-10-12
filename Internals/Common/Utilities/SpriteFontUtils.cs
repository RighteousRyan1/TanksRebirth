using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.Internals.Common.Utilities;

public static class SpriteFontUtils
{
	public static void DrawBorderedText(SpriteBatch spriteBatch, SpriteFontBase font, string text, Vector2 position, Color textColor, Color borderColor, Vector2 scale, float rotation, Anchor anchoring, float borderThickness = 1f)
    {
        // pos + new Vector2(0, 2f).RotatedByRadians(MathHelper.PiOver2 * i + MathHelper.PiOver4)
        for (int i = 0; i < 4; i++)
            spriteBatch.DrawString(font, text, position + new Vector2(0, 2f * borderThickness).RotatedByRadians(MathHelper.PiOver2 * i + MathHelper.PiOver4), 
                borderColor, scale, rotation, GameUtils.GetAnchor(anchoring, font.MeasureString(text)), 0f);
        spriteBatch.DrawString(font, text, position, textColor, scale, rotation, GameUtils.GetAnchor(anchoring, font.MeasureString(text)), 1f);
    }
}
