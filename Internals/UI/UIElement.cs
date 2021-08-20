using Microsoft.Xna.Framework;
using System.Collections.Generic;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.Internals.UI
{
    public abstract class UIElement
    {
        public delegate void MouseEvent(UIElement affectedElement);

        public static List<UIElement> AllUIElements { get; internal set; } = new();

        public UIParent Parent { get; set; }

        public OuRectangle InteractionBox;

        public OuRectangle InteractionBoxRelative;

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
            if (InteractionBoxRelative.X != default) {
                InteractionBox.X += GameUtils.WindowTopLeft.X + (GameUtils.WindowWidth * InteractionBoxRelative.X);
            }
            if (InteractionBoxRelative.Y != default) {
                InteractionBox.Y += GameUtils.WindowTopLeft.Y + (GameUtils.WindowHeight * InteractionBoxRelative.Y);
            }
            if (InteractionBoxRelative.Width != default) {
                InteractionBox.Width += GameUtils.WindowWidth * InteractionBoxRelative.Width;
            }
            if (InteractionBoxRelative.Height != default) {
                InteractionBox.Height += GameUtils.WindowHeight * InteractionBoxRelative.Height;
            }
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