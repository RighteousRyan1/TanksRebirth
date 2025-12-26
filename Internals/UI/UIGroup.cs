using System.Collections.Generic;

namespace TanksRebirth.Internals.UI; 

// ensures keeping visibility, etc.
public sealed class UIGroup : IUIProperties {
    public UIGroup? Parent { get; set; }

    bool _visible;

    /// <summary>Determines visibility of every child appended to this <see cref="UIGroup"/>.</summary>
    public bool IsVisible {
        get => _visible;
        set {
            // don't do anything if the value is the same
            if (_visible == value) return;
            _visible = value;

            foreach (var ui in _uis) {
                ui.IsVisible = value;
            }
        }
    }

    readonly List<UIElement> _uis;

    public UIGroup() {
        _uis = [];
    }

    public UIElement this[int index] {
        get => _uis[index];
    }

    public UIGroup AppendChild(UIElement element) {
        _uis.Add(element);
        return this;
    }

    public void RemoveChild(UIElement element) => _uis.Remove(element);
    public void RemoveChild(int index) => _uis.RemoveAt(index);
}
