using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Framework.Input;

public class Keybind : IInputBind<Keys> {
    public static List<Keybind> AllKeybinds { get; internal set; } = [];

    public string Name { get; set; } = "Not Named";
    public bool JustPressed => InputUtils.KeyJustPressed(Assigned) && !PendReassign;
    public bool IsPressed => InputUtils.CurrentKeySnapshot.IsKeyDown(Assigned) && !PendReassign;
    public bool PendReassign { get; set; } = false;
    public Keys Assigned { get; set; } = Keys.None;
    public Action OnPress { get; set; }
    public Action<Keys> OnReassign { get; set; }
    public Keybind(string name, Keys defaultKey = Keys.None) {
        Name = name;
        Assigned = defaultKey;
        AllKeybinds.Add(this);
    }
    public void ForceReassign(Keys newKey) {
        Assigned = newKey;
    }

    private void PollReassign() {
        if (InputUtils.CurrentKeySnapshot.GetPressedKeys().Length > 0) {
            var firstKey = InputUtils.CurrentKeySnapshot.GetPressedKeys()[0];
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
    }

    public void Fire() => OnPress?.Invoke();

    internal void Update() {
        if (PendReassign)
            PollReassign();

        if (JustPressed) {
            OnPress?.Invoke();
        }
    }

    public override string ToString() {
        return Name + " = {" + $"Key: {Assigned.KeyAsString()} | Pressed: {IsPressed} | ReassignPending: {PendReassign} " + "}";
    }
}