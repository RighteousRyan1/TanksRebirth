using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.UI;

namespace WiiPlayTanksRemake.Internals.Common.GameUI
{
    public class UIImageButton : UIImage
    {
        public UIImageButton(Texture2D texture, float scale) : base(texture, scale) {
            Texture = texture;
            Scale = scale;
        }

        public override void Draw() {
            TankGame.spriteBatch.Draw(Texture, new Vector2(InteractionBox.X, InteractionBox.Y), null, Color.White * (MouseHovering ? 1.0f : 0.8f), Rotation, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}