using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using WiiPlayTanksRemake.Internals.Core;

namespace WiiPlayTanksRemake.Internals.UI
{
    public abstract class UIElement
    {
        public bool HasScissor { get; set; }
        public Rectangle Scissor = new(-int.MaxValue, -int.MaxValue, 0, 0);

        public delegate void MouseEvent(UIElement affectedElement);

        private Vector2 InternalPosition;

        private Vector2 InternalSize;

        public static List<UIElement> AllUIElements { get; internal set; } = new();

        public UIElement Parent { get; private set; }

        protected IList<UIElement> Children { get; set; } = new List<UIElement>();

        public Rectangle Hitbox => new((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

        public Vector2 Position { get; set; }

        public Vector2 Size { get; set; }

        public Vector2 ScaleOrigin = new Vector2(0.5f);

        public bool Visible = true;

        public bool MouseHovering;

        public bool Initialized;

        public float Rotation { get; set; } = 0;

        public event MouseEvent OnMouseClick;

        public event MouseEvent OnMouseRightClick;

        public event MouseEvent OnMouseMiddleClick;

        public event MouseEvent OnMouseOver;

        public event MouseEvent OnMouseLeave;

        internal UIElement() {
            AllUIElements.Add(this);
        }

        public void SetDimensions(float x, float y, float width, float height) {
            InternalPosition = new Vector2(x, y);
            InternalSize = new Vector2(width, height);
            Recalculate();
        }
        public void SetDimensions(Rectangle rect)
        {
            InternalPosition = new Vector2(rect.X, rect.Y);
            InternalSize = new Vector2(rect.Width, rect.Height);
            Recalculate();
        }

        public void Recalculate() {
            Position = InternalPosition;
            Size = InternalSize;
        }

        public virtual void Draw(SpriteBatch spriteBatch) {
            if (!Visible)
                return;
            if (!HasScissor)
            {
                DrawSelf(spriteBatch);
                DrawChildren(spriteBatch);
            }
            else
            {
                var rastState = new RasterizerState
                {
                    ScissorTestEnable = true
                };

                TankGame.Instance.GraphicsDevice.RasterizerState = rastState;

                TankGame.Instance.GraphicsDevice.ScissorRectangle = Scissor;

                spriteBatch.Begin(rasterizerState: rastState);

                DrawSelf(spriteBatch);
                DrawChildren(spriteBatch);

                spriteBatch.End();
                // draw with schissor
            }
        }

        public void Initialize() {
            if (Initialized)
                return;

            OnInitialize();
            Initialized = true;
        }

        public virtual void OnInitialize() {
            foreach (UIElement child in Children) {
                child.Initialize();
            }
        }

        public virtual void DrawSelf(SpriteBatch spriteBatch) { }

        public virtual void DrawChildren(SpriteBatch spriteBatch) {
            foreach (UIElement child in Children) {
                child.Draw(spriteBatch);
            }
        }

        public virtual void Append(UIElement element) {
            element.Remove();
            element.Parent = this;
            Children.Add(element);
        }

        public virtual void Remove() {
            Parent?.Children.Remove(this);
            Parent = null;
        }

        public virtual void Remove(UIElement child) {
            Children.Remove(child);
            child.Parent = null;
        }

        public virtual void MouseClick() {
            if (Visible)
                OnMouseClick?.Invoke(this);
        }

        public virtual void MouseRightClick() {
            if (Visible)
                OnMouseRightClick?.Invoke(this);
        }

        public virtual void MouseMiddleClick() {
            if (Visible)
                OnMouseMiddleClick?.Invoke(this);
        }

        public virtual void MouseOver() {
            if (Visible)
            {
                OnMouseOver?.Invoke(this);
                MouseHovering = true;
            }
        }

        public virtual void MouseLeave() {
            if (Visible)
            {
                OnMouseLeave?.Invoke(this);
                MouseHovering = false;
            }
        }
    }
}