using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UIPanel : UIElement
    {
        public Color BackgroundColor = Color.White;

        public Action<UIPanel, SpriteBatch> UniqueDraw;

        public UIPanel(Action<UIPanel, SpriteBatch> uniqueDraw = null) {
            UniqueDraw = uniqueDraw;
        }

        public override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);

            var texture = UIPanelBackground;
            const int PANEL_BORDER = 12;
            
            // X
            var middleX = Hitbox.X + PANEL_BORDER;
            var rightX = Hitbox.Right - PANEL_BORDER;

            // Y
            var middleY = Hitbox.Y + PANEL_BORDER;
            var bottomY = Hitbox.Bottom - PANEL_BORDER;

            // hit box (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, PANEL_BORDER, PANEL_BORDER), new Rectangle(0, 0, PANEL_BORDER, PANEL_BORDER), BackgroundColor);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - PANEL_BORDER * 2, PANEL_BORDER), new Rectangle(PANEL_BORDER, 0, texture.Width - PANEL_BORDER * 2, PANEL_BORDER), BackgroundColor);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, PANEL_BORDER, PANEL_BORDER), new Rectangle(texture.Width - PANEL_BORDER, 0, PANEL_BORDER, PANEL_BORDER), BackgroundColor);

            // Middle (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, PANEL_BORDER, Hitbox.Height - PANEL_BORDER * 2), new Rectangle(0, PANEL_BORDER, PANEL_BORDER, texture.Height - PANEL_BORDER * 2), BackgroundColor);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - PANEL_BORDER * 2, Hitbox.Height - PANEL_BORDER * 2), new Rectangle(PANEL_BORDER, PANEL_BORDER, texture.Width - PANEL_BORDER * 2, texture.Height - PANEL_BORDER * 2), BackgroundColor);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, PANEL_BORDER, Hitbox.Height - PANEL_BORDER * 2), new Rectangle(texture.Width - PANEL_BORDER, PANEL_BORDER, PANEL_BORDER, texture.Height - PANEL_BORDER * 2), BackgroundColor);

            // Bottom (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, PANEL_BORDER, PANEL_BORDER), new Rectangle(0, texture.Height - PANEL_BORDER, PANEL_BORDER, PANEL_BORDER), BackgroundColor);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - PANEL_BORDER * 2, PANEL_BORDER), new Rectangle(PANEL_BORDER, texture.Height - PANEL_BORDER, texture.Width - PANEL_BORDER * 2, PANEL_BORDER), BackgroundColor);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, PANEL_BORDER, PANEL_BORDER), new Rectangle(texture.Width - PANEL_BORDER, texture.Height - PANEL_BORDER, PANEL_BORDER, PANEL_BORDER), BackgroundColor);

            UniqueDraw?.Invoke(this, spriteBatch);
        }
    }
}
