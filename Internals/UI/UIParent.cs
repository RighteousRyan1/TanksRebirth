using System.Collections.Generic;

namespace WiiPlayTanksRemake.Internals.UI
{
    public class UIParent
    {
        public static List<UIParent> TotalParents { get; private set; } = new();
        public List<UIElement> Elements { get; private set; } = new();

        public bool Visible { get; set; } = true;

        public void AppendElement(UIElement element) {
            TotalParents.Add(this);
            Elements.Add(element);
            element.Parent = this;
        }

        public void RemoveElement(UIElement element) {
            Elements.Remove(element);
            if (Elements.Count <= 0)
                TotalParents.Remove(this);
            element.Parent = null;
        }

        internal void DrawElements() {
            if (Visible) {
                foreach (var elem in UIElement.AllUIElements)
                    elem.Draw();
            }
        }
    }
}