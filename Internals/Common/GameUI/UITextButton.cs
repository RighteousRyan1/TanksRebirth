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

        public Func<Vector2> TextScale { get; set; }

        public float TextRotation;

        public static bool AutoResolutionHandle = true;
        public bool DrawText = true;
        public UITextButton(string text, SpriteFontBase font, Color color, Func<Vector2> textScale) : base(null, 1, null)
        {
            Text = text;
            Font = font;
            Color = color;
            TextScale = textScale;
        }
        public UITextButton(string text, SpriteFontBase font, Color color, float textScale = 1f) : base(null, 1, null)
        {
            Text = text;
            Font = font;
            Color = color;
            TextScale = () => new(textScale);
        }

        public override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D texture = UIPanelBackground;

            const int TEXT_BUTTON_BORDER = 12;

            // Font
            var font = TankGame.TextFont;
            var drawOrigin = font.MeasureString(Text) / 2f;

            // X
            var middleX = Hitbox.X + TEXT_BUTTON_BORDER;
            var rightX = Hitbox.Right - TEXT_BUTTON_BORDER;

            // Y
            var middleY = Hitbox.Y + TEXT_BUTTON_BORDER;
            var bottomY = Hitbox.Bottom - TEXT_BUTTON_BORDER;

            // hit box
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), new Rectangle(0, 0, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), MouseHovering ? HoverColor : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - TEXT_BUTTON_BORDER * 2, TEXT_BUTTON_BORDER), new Rectangle(TEXT_BUTTON_BORDER, 0, texture.Width - TEXT_BUTTON_BORDER * 2, TEXT_BUTTON_BORDER), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), new Rectangle(texture.Width - TEXT_BUTTON_BORDER, 0, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);

            // Middle (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, TEXT_BUTTON_BORDER, Hitbox.Height - TEXT_BUTTON_BORDER * 2), new Rectangle(0, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER, texture.Height - TEXT_BUTTON_BORDER * 2), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - TEXT_BUTTON_BORDER * 2, Hitbox.Height - TEXT_BUTTON_BORDER * 2), new Rectangle(TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER, texture.Width - TEXT_BUTTON_BORDER * 2, texture.Height - TEXT_BUTTON_BORDER * 2), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, TEXT_BUTTON_BORDER, Hitbox.Height - TEXT_BUTTON_BORDER * 2), new Rectangle(texture.Width - TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER, texture.Height - TEXT_BUTTON_BORDER * 2), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);
            
            // Bottom (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), new Rectangle(0, texture.Height - TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - TEXT_BUTTON_BORDER * 2, TEXT_BUTTON_BORDER), new Rectangle(TEXT_BUTTON_BORDER, texture.Height - TEXT_BUTTON_BORDER, texture.Width - TEXT_BUTTON_BORDER * 2, TEXT_BUTTON_BORDER), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), new Rectangle(texture.Width - TEXT_BUTTON_BORDER, texture.Height - TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER, TEXT_BUTTON_BORDER), MouseHovering ? HoverColor : Color, 0f, Anchor.GetAnchor(texture.Size()), default, 0f);

            if (TextScale != null && DrawText)
                spriteBatch.DrawString(font, Text, Hitbox.Center.ToVector2(), Color.Black, AutoResolutionHandle ? TextScale.Invoke().ToResolution() : TextScale.Invoke(), TextRotation, drawOrigin);
        }
    }
}