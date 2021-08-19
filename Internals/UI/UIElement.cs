using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.Internals.UI
{
    public abstract class UIElement
    {
        public static List<UIElement> AllUIElements { get; internal set; } = new();

        public UIParent Parent
        {
            get; set;
        }

        public Rectangle InteractionBox { get; set; } = new(0, 0, 0, 0);

        public float Rotation { get; set; } = 0;

        internal UIElement() {
            AllUIElements.Add(this);
        }

        public virtual void Draw() {

        }

        public virtual void MouseClick() {
        
        }

        public virtual void MouseRightClick() {
        
        }

        public virtual void MouseMiddleClick() {
        
        }
    }
}