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
            if (OnMouseClick != null)
                OnMouseClick(this);
        }

        public virtual void MouseRightClick() {
            if (OnMouseRightClick != null)
                OnMouseRightClick(this);
        }

        public virtual void MouseMiddleClick() {
            if (OnMouseMiddleClick != null)
                OnMouseMiddleClick(this);
        }

        public virtual void MouseOver() {
            if (OnMouseOver != null)
                OnMouseOver(this);
        }

        public virtual void MouseLeave() {
            if (OnMouseLeave != null)
                OnMouseLeave(this);
        }
    }
}