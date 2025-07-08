using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;
using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.GameContent.UI
{
    public class XpBar
    {
        public float Value;
        public float MaxValue;
        public void Render(SpriteBatch sb, Vector2 position, Vector2 scale, Anchor aligning, Color emptyColor, Color fillColor)
        {

            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"Level: {MathF.Floor(TankGame.GameData.ExpLevel)} | {MathF.Floor(Value / MaxValue * 100)}%", position - (Vector2.UnitY * 20).ToResolution(), Color.White, new Vector2(0.6f).ToResolution(), 0f, FontGlobals.RebirthFont.MeasureString($"Level: {MathF.Floor(TankGame.GameData.ExpLevel)} | {MathF.Floor(Value / MaxValue * 100)}%") / 2, 0f);
            //TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"Level: {MathF.Floor(TankGame.GameData.ExpLevel)} | {MathF.Floor(Value / MaxValue * 100)}%", position + new Vector2(0, 20), Color.White, 0f, FontGlobals.RebirthFont.MeasureString($"Level: {MathF.Floor(TankGame.GameData.ExpLevel)} | {MathF.Floor(Value / MaxValue * 100)}%") / 2, 1f, 0f);

            sb.Draw(TextureGlobals.Pixels[Color.White], position, null, emptyColor, 0f, GameUtils.GetAnchor(aligning, TextureGlobals.Pixels[Color.White].Size()), new Vector2(scale.X, scale.Y), default, 0f);

            sb.Draw(TextureGlobals.Pixels[Color.White], position, null, fillColor, 0f, GameUtils.GetAnchor(aligning, TextureGlobals.Pixels[Color.White].Size()), new Vector2(scale.X * Value, scale.Y), default, 0f);
        }
    }
}
