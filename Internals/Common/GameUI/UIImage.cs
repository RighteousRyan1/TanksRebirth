using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.UI;

namespace WiiPlayTanksRemake.Internals.Common.GameUI
{
    public class UIImage : UIElement
    {
        public Texture2D Texture { get; set; }

        public float Scale { get; set; }

        public Action<UIImage, SpriteBatch> UniqueDraw;

        public UIImage(Texture2D texture, float scale, Action<UIImage, SpriteBatch> uniqueDraw = null) {
            Texture = texture;
            Scale = scale;
            UniqueDraw = uniqueDraw;
        }

        public override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);

            if (UniqueDraw is null) {
                spriteBatch.Draw(Texture, Hitbox.Center.ToVector2(), null, Color.White, Rotation, new Vector2(Texture.Width, Texture.Height) / 2, Scale, SpriteEffects.None, 0f);
                return;
            }

            UniqueDraw.Invoke(this, spriteBatch);
        }
    }
}