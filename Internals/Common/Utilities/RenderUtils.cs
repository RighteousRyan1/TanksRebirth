using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class RenderUtils
{
    public static Vector2 Size(this Texture2D tex) => new(tex.Width, tex.Height);
    public static void DrawTextWithBorder(SpriteFontBase font, string text, Vector2 pos, Color color, Color borderColor, float rot, float scale, int borderSize)
    {
        var origin = font.MeasureString(text) / 2;
        int yOffset = 0;
        int xOffset = 0;
        for (int i = 0; i < borderSize + 3; i++)
        {
            if (i == 0)
                xOffset = -borderSize;
            if (i == 1)
                xOffset = borderSize;
            if (i == 2)
                yOffset = -borderSize;
            if (i == 3)
                yOffset = borderSize;

            TankGame.SpriteRenderer.DrawString(font, text, pos + new Vector2(xOffset, yOffset), borderColor, new Vector2(scale), 0f, origin);
        }
        TankGame.SpriteRenderer.DrawString(font, text, pos + new Vector2(xOffset, yOffset), color, new Vector2(scale), 0f, origin);
    }
    public static void DrawTextureWithBorder(Texture2D texture, Vector2 pos, Color color, Color borderColor, float rot, float scale, int borderSize)
    {
        var origin = texture.Size() / 2;
        int yOffset = 0;
        int xOffset = 0;
        for (int i = 0; i < borderSize + 3; i++)
        {
            if (i == 0)
                xOffset = -borderSize;
            if (i == 1)
                xOffset = borderSize;
            if (i == 2)
                yOffset = -borderSize;
            if (i == 3)
                yOffset = borderSize;


            TankGame.SpriteRenderer.Draw(texture, pos + new Vector2(xOffset, yOffset), null, borderColor, rot, origin, scale, default, 0f);
        }
        TankGame.SpriteRenderer.Draw(texture, pos, null, color, rot, origin, scale, default, 0f);
    }
    public static void DrawStringAtMouse(SpriteFontBase font, object text) => TankGame.SpriteRenderer.DrawString(font, text.ToString(), MouseUtils.MousePosition + new Vector2(25), Color.White, new Vector2(1f), 0f, Vector2.Zero);
}
