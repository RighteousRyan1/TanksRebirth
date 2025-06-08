using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UIDropdown : UIElement
    {
        public string Text { get; set; }

        public SpriteFontBase Font { get; set; }

        public float Scale { get; set; }

        public Color Color { get; set; }

        public Color WrapperColor { get; set; }

        public Color ScrollBarColor { get; set; }

        public bool Dropped { get; set; } = false;

        private Rectangle wrapper;

        private Rectangle scroll;

        private static int _newScroll;
        private static int _oldScroll;
        private static float _gpuSettingsOffset = 0f;
        private static float _push = 0f;

        public UIDropdown(string text, SpriteFontBase font, Color color, float scale = 1f)
        {
            Text = text;
            Font = font;
            Color = color;
            Scale = scale;
            WrapperColor = Color.Gray;
            ScrollBarColor = Color.White;
        }

        public override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            _newScroll = InputUtils.CurrentMouseSnapshot.ScrollWheelValue;
            if (_newScroll != _oldScroll && wrapper.Contains(MouseUtils.MousePosition) && Dropped)
            {
                _gpuSettingsOffset = _newScroll - _oldScroll;
                _push += _gpuSettingsOffset;
                foreach (UIElement element in Children)
                {
                    element.Position = new(element.Position.X, element.Position.Y + _gpuSettingsOffset);
                    element.MouseHovering = false;
                }
                scroll = new(scroll.X, (int)(scroll.Y - _gpuSettingsOffset / Children.Count), scroll.Width, scroll.Height);
            }

            const int border = 12;

            if (Dropped)
            {
                spriteBatch.Draw(TextureGlobals.Pixels[Color.White], wrapper, new Rectangle(0, 0, border, border), WrapperColor);
                spriteBatch.Draw(TextureGlobals.Pixels[Color.White], scroll, new Rectangle(0, 0, border, border), ScrollBarColor);
            }

            _oldScroll = _newScroll;
        }

        public override void OnInitialize()
        {
            wrapper = new Rectangle(Hitbox.X, Hitbox.Y, Hitbox.Width + 10, Hitbox.Height + 200);
            scroll = new Rectangle(Hitbox.X + Hitbox.Width, Hitbox.Y, 10, 40);
            foreach (UIElement child in Children)
            {
                child.IsVisible = Dropped;
                child.HasScissor = true;
                child.Scissor = () => wrapper;
            }
            HasScissor = true;
            Scissor = () => wrapper;
            OnLeftClick = (uiElement) =>
            {
                Dropped = !Dropped;
                foreach (UIElement child in Children)
                {
                    child.IsVisible = Dropped;
                }
            };
            base.OnInitialize();
        }

        public override void DrawChildren(SpriteBatch spriteBatch)
        {
            if (Dropped)
            {
                int shift = 0;
                foreach (UIElement child in Children)
                {
                    shift++;
                    child.SetDimensions(Position.X, Position.Y + (_push / Children.Count) + (shift * Hitbox.Height), Hitbox.Width, Hitbox.Height);
                }
                base.DrawChildren(spriteBatch);
            }

            DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, MouseHovering ? Color.CornflowerBlue : Color, Vector2.Zero);
            SpriteFontBase font = TankGame.TextFont;
            Vector2 drawOrigin = font.MeasureString(Text) / 2f;
            spriteBatch.DrawString(font, Text, Hitbox.Center.ToVector2(), Color.Black, new Vector2(Scale), 0, drawOrigin);

        }
    }
}
