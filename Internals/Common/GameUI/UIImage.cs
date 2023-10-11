using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UIImage : UIElement
    {
        public Texture2D Texture { get; set; }
        public Vector2 Scale { get; set; }
        public Color Color { get; set; } = Color.White;

        public Action<UIImage, SpriteBatch> UniqueDraw;

        public UIImage(Texture2D texture, Vector2 scale, Action<UIImage, SpriteBatch> uniqueDraw = null) {
            Texture = texture;
            Scale = scale;
            UniqueDraw = uniqueDraw;
        }

        public override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);

            if (UniqueDraw is null) {
                spriteBatch.Draw(Texture, Hitbox.Center.ToVector2(), null, Color, Rotation, new Vector2(Texture.Width, Texture.Height) / 2, Scale, SpriteEffects.None, 0f);
                return;
            }

            UniqueDraw.Invoke(this, spriteBatch);
        }
    }
}