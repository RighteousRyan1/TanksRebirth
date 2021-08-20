using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.UI;

namespace WiiPlayTanksRemake.Internals.Common.GameUI
{
    public class UITextButton : UIPanel
    {
        public string Text
        {
            get; set;
        }

        public SpriteFont Font
        {
            get; set;
        }

        public float Scale
        {
            get; set;
        }

        public Color TextColor
        {
            get; set;
        }

        private byte baseAlpha;

        public UITextButton(string text, SpriteFont font, Color textColor, Color backgroundColor, float scale = 1f) {
            Text = text;
            Font = font;
            TextColor = textColor;
            BackgroundColor = backgroundColor;
            Scale = scale;
        }

        public override void Draw() {
            base.Draw();
            TankGame.spriteBatch.DrawString(Font, Text, InteractionBox.Center.ToVector2() - (Font.MeasureString(Text) / 2), TextColor, Rotation, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }

        public override void MouseOver() {
            base.MouseOver();
            baseAlpha = BackgroundColor.A;
            BackgroundColor.A = 100;
        }

        public override void MouseLeave() {
            base.MouseLeave();
            BackgroundColor.A = baseAlpha;
        }
    }
}
