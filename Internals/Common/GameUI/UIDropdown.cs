using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
                foreach (var element in Children) {
                    element.Position = new(element.Position.X, element.Position.Y + _gpuSettingsOffset);
                    element.MouseHovering = false;
                }
                scroll = new(scroll.X, (int)(scroll.Y - _gpuSettingsOffset / Children.Count), scroll.Width, scroll.Height);
            }

            const int border = 12;

            if (Dropped) {
                spriteBatch.Draw(TankGame.WhitePixel, wrapper, new Rectangle(0, 0, border, border), WrapperColor);
                spriteBatch.Draw(TankGame.WhitePixel, scroll, new Rectangle(0, 0, border, border), ScrollBarColor);
            }

            _oldScroll = _newScroll;
        }

        public override void OnInitialize()
        {
            wrapper = new Rectangle(Hitbox.X, Hitbox.Y, Hitbox.Width + 10, Hitbox.Height + 200);
            scroll = new Rectangle(Hitbox.X + Hitbox.Width, Hitbox.Y, 10, 40);
            HasScissor = true;
            
            Scissor = () => wrapper;

            foreach (var child in Children) {
                child.IsVisible = Dropped;
                child.HasScissor = true;
                child.Scissor = () => wrapper;
            }
            
            OnLeftClick = _ => {
                Dropped = !Dropped;
                foreach (var child in Children) {
                    child.IsVisible = Dropped;
                }
            };

            base.OnInitialize();
        }

        public override void DrawChildren(SpriteBatch spriteBatch)
        {
            if (Dropped) {
                for (var i = 0; i < Children.Count; i++) {
                    var child = Children[i];
                    child.SetDimensions(Position.X, Position.Y + (_push / Children.Count) + (i * Hitbox.Height),
                        Hitbox.Width, Hitbox.Height);
                }

                base.DrawChildren(spriteBatch);
            }

            var texture = UIPanelBackground;

            // Font
            var font = TankGame.TextFont;
            var drawOrigin = font.MeasureString(Text) / 2f;
            
            const int DROPDOWN_BORDER = 12;

            // X
            var middleX = Hitbox.X + DROPDOWN_BORDER;
            var rightX = Hitbox.Right - DROPDOWN_BORDER;

            // Y
            var middleY = Hitbox.Y + DROPDOWN_BORDER;
            var bottomY = Hitbox.Bottom - DROPDOWN_BORDER;

            // hit box (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, DROPDOWN_BORDER, DROPDOWN_BORDER), new Rectangle(0, 0, DROPDOWN_BORDER, DROPDOWN_BORDER), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - DROPDOWN_BORDER * 2, DROPDOWN_BORDER), new Rectangle(DROPDOWN_BORDER, 0, texture.Width - DROPDOWN_BORDER * 2, DROPDOWN_BORDER), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, DROPDOWN_BORDER, DROPDOWN_BORDER), new Rectangle(texture.Width - DROPDOWN_BORDER, 0, DROPDOWN_BORDER, DROPDOWN_BORDER), MouseHovering ? Color.CornflowerBlue : Color);

            // Middle (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, DROPDOWN_BORDER, Hitbox.Height - DROPDOWN_BORDER * 2), new Rectangle(0, DROPDOWN_BORDER, DROPDOWN_BORDER, texture.Height - DROPDOWN_BORDER * 2), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - DROPDOWN_BORDER * 2, Hitbox.Height - DROPDOWN_BORDER * 2), new Rectangle(DROPDOWN_BORDER, DROPDOWN_BORDER, texture.Width - DROPDOWN_BORDER * 2, texture.Height - DROPDOWN_BORDER * 2), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, DROPDOWN_BORDER, Hitbox.Height - DROPDOWN_BORDER * 2), new Rectangle(texture.Width - DROPDOWN_BORDER, DROPDOWN_BORDER, DROPDOWN_BORDER, texture.Height - DROPDOWN_BORDER * 2), MouseHovering ? Color.CornflowerBlue : Color);

            // Bottom (?)
            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, DROPDOWN_BORDER, DROPDOWN_BORDER), new Rectangle(0, texture.Height - DROPDOWN_BORDER, DROPDOWN_BORDER, DROPDOWN_BORDER), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - DROPDOWN_BORDER * 2, DROPDOWN_BORDER), new Rectangle(DROPDOWN_BORDER, texture.Height - DROPDOWN_BORDER, texture.Width - DROPDOWN_BORDER * 2, DROPDOWN_BORDER), MouseHovering ? Color.CornflowerBlue : Color);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, DROPDOWN_BORDER, DROPDOWN_BORDER), new Rectangle(texture.Width - DROPDOWN_BORDER, texture.Height - DROPDOWN_BORDER, DROPDOWN_BORDER, DROPDOWN_BORDER), MouseHovering ? Color.CornflowerBlue : Color);
            
            spriteBatch.DrawString(font, Text, Hitbox.Center.ToVector2(), Color.Black, new Vector2(Scale), 0, drawOrigin);

        }
    }
}
