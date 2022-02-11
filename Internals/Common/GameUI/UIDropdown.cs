using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.UI;

namespace WiiPlayTanksRemake.Internals.Common.GameUI
{
    public class UIDropdown : UIElement
    {
        public string Text { get; set; }

        public SpriteFont Font { get; set; }

        public float Scale { get; set; }

        public Color Color { get; set; }

        public bool Dropped { get; set; } = false;

        private Rectangle wrapper;

        private Rectangle scroll;

        public UIDropdown(string text, SpriteFont font, Color color, float scale = 1f)
        {
            Text = text;
            Font = font;
            Color = color;
            Scale = scale;
        }

        public override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            Texture2D texture = UIPanelBackground;

            int border = 12;

            if (Dropped)
            {
                spriteBatch.Draw(TankGame.MagicPixel, wrapper, new Rectangle(0, 0, border, border), Color);
                spriteBatch.Draw(TankGame.MagicPixel, scroll, new Rectangle(0, 0, border, border), Color.Gray);
            }

            int middleX = Hitbox.X + border;
            int rightX = Hitbox.Right - border;

            int middleY = Hitbox.Y + border;
            int bottomY = Hitbox.Bottom - border;

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, border, border), new Rectangle(0, 0, border, border), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - border * 2, border), new Rectangle(border, 0, texture.Width - border * 2, border), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, border, border), new Rectangle(texture.Width - border, 0, border, border), MouseHovering ? Color.CornflowerBlue : Color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, border, Hitbox.Height - border * 2), new Rectangle(0, border, border, texture.Height - border * 2), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - border * 2, Hitbox.Height - border * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, border, Hitbox.Height - border * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), MouseHovering ? Color.CornflowerBlue : Color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, border, border), new Rectangle(0, texture.Height - border, border, border), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - border * 2, border), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, border, border), new Rectangle(texture.Width - border, texture.Height - border, border, border), MouseHovering ? Color.CornflowerBlue : Color);
            SpriteFontBase font = TankGame.TextFont;
            Vector2 drawOrigin = font.MeasureString(Text) / 2f;
            spriteBatch.DrawString(font, Text, Hitbox.Center.ToVector2(), Color.Black, new Vector2(Scale), 0, drawOrigin);
        }

        public override void OnInitialize()
        {
            wrapper = new Rectangle(Hitbox.X, Hitbox.Y, Hitbox.Width + 10, Hitbox.Height + 200);
            scroll = new Rectangle(Hitbox.X + Hitbox.Width, Hitbox.Y, 10, 40);
            foreach (UIImageButton child in Children)
            {
                child.Visible = Dropped;
                child.Scissor = wrapper;
            }
            OnLeftClick += UIDropdown_OnLeftClick;
        }

        private void UIDropdown_OnLeftClick(UIElement obj)
        {
            Dropped = !Dropped;
            foreach (UIImageButton child in Children)
            {
                child.Visible = Dropped;
            }
        }

        public override void DrawChildren(SpriteBatch spriteBatch)
        {
            if (!Dropped)
                return;

            int shift = 0;
            foreach (UIImageButton child in Children)
            {
                shift++;
                child.SetDimensions(Position.X, Position.Y + (shift * (Hitbox.Height)), Hitbox.Width, Hitbox.Height);
            }
            base.DrawChildren(spriteBatch);
        }
    }
}
