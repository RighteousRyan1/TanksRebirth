using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.UI;

namespace WiiPlayTanksRemake.Internals.Common.GameUI
{
    public class UIPanel : UIElement
    {
        public Color BackgroundColor;

        private void DrawPanel(Texture2D texture, Color color) {
            SpriteBatch spriteBatch = TankGame.spriteBatch;
            Point other = new(InteractionBox.X + InteractionBox.Width - 12, InteractionBox.Y + InteractionBox.Height - 12);
            int width = other.X - InteractionBox.X - 12;
            int height = other.Y - InteractionBox.Y - 12;
            spriteBatch.Draw(texture, new Rectangle(InteractionBox.X, InteractionBox.Y, 12, 12), new Rectangle(0, 0, 12, 12), color);
            spriteBatch.Draw(texture, new Rectangle(other.X, InteractionBox.Y, 12, 12), new Rectangle(16, 0, 12, 12), color);
            spriteBatch.Draw(texture, new Rectangle(InteractionBox.X, other.Y, 12, 12), new Rectangle(0, 12 + 4, 12, 12), color);
            spriteBatch.Draw(texture, new Rectangle(other.X, other.Y, 12, 12), new Rectangle(12 + 4, 12 + 4, 12, 12), color);
            spriteBatch.Draw(texture, new Rectangle(InteractionBox.X + 12, InteractionBox.Y, width, 12), new Rectangle(12, 0, 4, 12), color);
            spriteBatch.Draw(texture, new Rectangle(InteractionBox.X + 12, other.Y, width, 12), new Rectangle(12, 12 + 4, 4, 12), color);
            spriteBatch.Draw(texture, new Rectangle(InteractionBox.X, InteractionBox.Y + 12, 12, height), new Rectangle(0, 12, 12, 4), color);
            spriteBatch.Draw(texture, new Rectangle(other.X, InteractionBox.Y + 12, 12, height), new Rectangle(12 + 4, 12, 12, 4), color);
            spriteBatch.Draw(texture, new Rectangle(InteractionBox.X + 12, InteractionBox.Y + 12, width, height), new Rectangle(12, 12, 4, 4), color);
        }

        public override void Draw() {
            DrawPanel(TankGame.UITextures.UIPanelBackground, BackgroundColor);
            DrawPanel(TankGame.UITextures.UIPanelBackgroundCorner, BackgroundColor);
        }
    }
}
