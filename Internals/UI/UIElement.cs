using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.Internals.UI
{
    public abstract class UIElement
    {
        public delegate void MouseEvent(UIElement affectedElement);

        public static List<UIElement> AllUIElements { get; internal set; } = new();

        public UIParent Parent { get; set; }

        public Rectangle InteractionBox;

        public bool MouseHovering;

        public float Rotation { get; set; } = 0;

        public event MouseEvent OnMouseClick;

        public event MouseEvent OnMouseRightClick;

        public event MouseEvent OnMouseMiddleClick;

        public event MouseEvent OnMouseOver;

        public event MouseEvent OnMouseLeave;

        internal UIElement() {
            AllUIElements.Add(this);
        }

        public virtual void Draw() {

        }

        public virtual void MouseClick() {
            OnMouseClick?.Invoke(this);
        }

        public virtual void MouseRightClick() {
            OnMouseRightClick?.Invoke(this);
        }

        public virtual void MouseMiddleClick() {
            OnMouseMiddleClick?.Invoke(this);
        }

        public virtual void MouseOver() {
            OnMouseOver?.Invoke(this);
            MouseHovering = true;
        }

        public virtual void MouseLeave() {
            OnMouseLeave?.Invoke(this);
            MouseHovering = false;
        }
    }
}