using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UITextButton : UIImage
    {
        public string Text { get; set; }

        public SpriteFontBase Font { get; set; }

        public Color Color { get; set; }

        public Color HoverColor { get; set; } = Color.CornflowerBlue;

        public float TextScale { get; set; }

        public UITextButton(string text, SpriteFontBase font, Color color, float textScale = 1) : base(null, 1, null)
        {
            Text = text;
            Font = font;
            Color = color;
            TextScale = textScale;
        }

        public override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D texture = UIPanelBackground;

            int border = 12;

            int middleX = Hitbox.X + border;
            int rightX = Hitbox.Right - border;

            int middleY = Hitbox.Y + border;
            int bottomY = Hitbox.Bottom - border;

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, border, border), new Rectangle(0, 0, border, border), MouseHovering ? HoverColor : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - border * 2, border), new Rectangle(border, 0, texture.Width - border * 2, border), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, border, border), new Rectangle(texture.Width - border, 0, border, border), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, border, Hitbox.Height - border * 2), new Rectangle(0, border, border, texture.Height - border * 2), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - border * 2, Hitbox.Height - border * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, border, Hitbox.Height - border * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, border, border), new Rectangle(0, texture.Height - border, border, border), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - border * 2, border), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, border, border), new Rectangle(texture.Width - border, texture.Height - border, border, border), MouseHovering ? HoverColor : Color, 0f, GameUtils.GetAnchor(Anchor, texture.Size()), default, 0f);
            SpriteFontBase font = TankGame.TextFont;
            Vector2 drawOrigin = font.MeasureString(Text) / 2f;
            spriteBatch.DrawString(font, Text, Hitbox.Center.ToVector2(), Color.Black, new Vector2(TextScale), 0, drawOrigin);
        }
    }
}