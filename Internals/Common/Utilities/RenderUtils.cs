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
    public static void DrawStringAtMouse(SpriteFontBase font, object text) => TankGame.SpriteRenderer.DrawString(font, text.ToString(), MouseUtils.MousePosition + new Vector2(25), Color.White, new Vector2(1f), 0f, Vector2.Zero);
}
