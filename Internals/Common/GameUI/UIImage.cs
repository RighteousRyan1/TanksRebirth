using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.UI;

namespace WiiPlayTanksRemake.Internals.Common.GameUI
{
    public class UIImage : UIElement
    {
        public Texture2D Texture { get; set; }

        public float Scale { get; set; }

        public UIImage(Texture2D texture, float scale) {
            Texture = texture;
            Scale = scale;
        }

        public override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);
            
            spriteBatch.Draw(Texture, Hitbox.Center.ToVector2(), null, Color.White, Rotation, new Vector2(Texture.Width, Texture.Height) / 2, Scale, SpriteEffects.None, 0f);
        }
    }
}