using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Framework.Input;
/// <summary>
/// A named input that can be rebound. It's bound to a key, or (when <see cref="AllowMouse"/> is set) to a mouse button
/// instead, <see cref="AssignedMouse"/> takes over whenever it isn't <see cref="MouseInput.None"/>.
/// </summary>
public class Keybind : IInputBind<Keys> {
    public static List<Keybind> AllKeybinds { get; internal set; } = [];
    /// <summary>Mouse buttons a rebind never picks up (left click shoots and clicks the UI).</summary>
    public static readonly HashSet<MouseInput> ReservedMouseButtons = [MouseInput.Left];

    public string Name { get; set; } = "Not Named";
    public bool JustPressed => !PendReassign && (InputUtils.KeyJustPressed(Assigned) || InputUtils.CheckMouseFreshInput(AssignedMouse));
    public bool IsPressed => !PendReassign && (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Assigned) || InputUtils.CheckMouseInputPressed(AssignedMouse));
    public bool PendReassign { get; set; } = false;
    public Keys Assigned { get; set; } = Keys.None;
    /// <summary>The mouse button this is bound to, or <see cref="MouseInput.None"/> when it uses <see cref="Assigned"/>.</summary>
    public MouseInput AssignedMouse { get; set; } = MouseInput.None;
    /// <summary>Whether a rebind also listens for mouse buttons (except <see cref="ReservedMouseButtons"/>).</summary>
    public bool AllowMouse { get; set; }
    public Action? OnPress { get; set; }
    public Action<Keys>? OnReassign { get; set; }
    /// <summary>Called instead of <see cref="OnReassign"/> when a rebind picks up a mouse button.</summary>
    public Action<MouseInput>? OnReassignMouse { get; set; }
    public Keybind(string name, Keys defaultKey = Keys.None) {
        Name = name;
        Assigned = defaultKey;
        AllKeybinds.Add(this);
    }
    /// <summary>Binds a key (and drops any mouse button).</summary>
    public void ForceReassign(Keys newKey) {
        Assigned = newKey;
        AssignedMouse = MouseInput.None;
    }
    /// <summary>Binds a mouse button (and drops the key). <see cref="MouseInput.None"/> leaves the key as it is.</summary>
    public void ForceReassign(MouseInput newMouse) {
        AssignedMouse = newMouse;
        if (newMouse != MouseInput.None)
            Assigned = Keys.None;
    }

    /// <summary>The key or mouse button as text, e.g. "W" or "Right Mouse".</summary>
    public string InputAsString() => AssignedMouse != MouseInput.None ? AssignedMouse.MouseAsString() : Assigned.KeyAsString();

    void PollReassign() {
        if (AllowMouse) {
            var pressedMouse = InputUtils.GetPressedMouseButtons();
            for (int i = 0; i < pressedMouse.Length; i++) {
                var button = pressedMouse[i];
                if (ReservedMouseButtons.Contains(button) || !InputUtils.CheckMouseFreshInput(button))
                    continue;
                ForceReassign(button);
                PendReassign = false;
                OnReassignMouse?.Invoke(button);
                return;
            }
        }

        var pressedKeys = InputUtils.KeyboardMouse.CurrentKey.GetPressedKeys();
        if (pressedKeys.Length > 0) {
            var firstKey = pressedKeys[0];
            if (InputUtils.KeyJustPressed(firstKey) && firstKey == Assigned) {
                OnReassign?.Invoke(Assigned);
                PendReassign = false;
                return;
            }
            else if (InputUtils.KeyJustPressed(firstKey) && firstKey == Keys.Escape) {
                ForceReassign(Keys.None);
                OnReassign?.Invoke(Assigned);
                PendReassign = false;
                return;
            }
            ForceReassign(firstKey);
            OnReassign?.Invoke(Assigned);
            PendReassign = false;
            return;
        }
    }
    public void Fire() => OnPress?.Invoke();
    internal void Update() {
        if (PendReassign)
            PollReassign();

        if (JustPressed) {
            OnPress?.Invoke();
        }
    }

    public override string ToString() => Name + " = {" + $"Key: {InputAsString()} | Pressed: {IsPressed} | ReassignPending: {PendReassign} " + "}";
}
