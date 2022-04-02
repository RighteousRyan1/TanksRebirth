using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UIImageButton : UIImage
    {
        public UIImageButton(Texture2D texture, float scale, Action<UIImage, SpriteBatch> uniqueDraw = null) : base(texture, scale, uniqueDraw)
        {
            Texture = texture;
            Scale = scale;
            UniqueDraw = uniqueDraw;
        }

        public override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (UniqueDraw is null)
            {
                spriteBatch.Draw(Texture, Position, null, Color.White * (MouseHovering ? 1.0f : 0.8f), Rotation, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            }
        }
    }
}