using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Framework.Input;
public class Keybind : IInputBind<Keys> {
    public static List<Keybind> AllKeybinds { get; internal set; } = [];
    public string Name { get; set; } = "Not Named";
    public bool JustPressed => InputUtils.KeyJustPressed(Assigned) && !PendReassign;
    public bool IsPressed => InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Assigned) && !PendReassign;
    public bool PendReassign { get; set; } = false;
    public Keys Assigned { get; set; } = Keys.None;
    public Action? OnPress { get; set; }
    public Action<Keys>? OnReassign { get; set; }
    public Keybind(string name, Keys defaultKey = Keys.None) {
        Name = name;
        Assigned = defaultKey;
        AllKeybinds.Add(this);
    }
    public void ForceReassign(Keys newKey) {
        Assigned = newKey;
    }

    void PollReassign() {
        var pressedKeys = InputUtils.KeyboardMouse.CurrentKey.GetPressedKeys();
        if (pressedKeys.Length > 0) {
            var firstKey = pressedKeys[0];
            if (InputUtils.KeyJustPressed(firstKey) && firstKey == Assigned) {
                OnReassign?.Invoke(Assigned);
                PendReassign = false;
                return;
            }
            else if (InputUtils.KeyJustPressed(firstKey) && firstKey == Keys.Escape) {
                Assigned = Keys.None;
                OnReassign?.Invoke(Assigned);
                PendReassign = false;
                return;
            }
            Assigned = firstKey;
            OnReassign?.Invoke(Assigned);
            PendReassign = false;
            return;
        }
        else {

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

    public override string ToString() => Name + " = {" + $"Key: {Assigned.KeyAsString()} | Pressed: {IsPressed} | ReassignPending: {PendReassign} " + "}";
}