using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Framework.Input;
public class MouseBind : IInputBind<MouseInput> {
    public static List<MouseBind> AllKeybinds { get; internal set; } = [];
    public string Name { get; set; } = "Not Named";
    public bool JustPressed => InputUtils.CheckMouseFreshInput(Assigned) && !PendReassign;
    public bool IsPressed => InputUtils.CheckMouseInputPressed(Assigned) && !PendReassign;
    public bool PendReassign { get; set; } = false;
    public MouseInput Assigned { get; set; } = MouseInput.None;
    public Action? OnPress { get; set; }
    public Action<MouseInput>? OnReassign { get; set; }
    public MouseBind(string name, MouseInput defaultMouse = MouseInput.None) {
        Name = name;
        Assigned = defaultMouse;
        AllKeybinds.Add(this);
    }
    public void ForceReassign(MouseInput newKey) {
        Assigned = newKey;
    }

    void PollReassign() {
        var pressedMouse = InputUtils.GetPressedMouseButtons();

        if (pressedMouse.Length > 0) {
            var firstMouse = pressedMouse[0];

            Assigned = firstMouse;
            OnReassign?.Invoke(Assigned);
            PendReassign = false;
            return;
        }
        else if (InputUtils.KeyJustPressed(Keys.Escape)) {
            Assigned = MouseInput.None;
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

    public override string ToString() => Name + " = {" + $"Key: {Assigned} | Pressed: {IsPressed} | ReassignPending: {PendReassign} " + "}";
}
