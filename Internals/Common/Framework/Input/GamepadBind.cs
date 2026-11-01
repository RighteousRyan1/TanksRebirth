using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Framework.Input;

// Update seems really... bad. but we can fix that some other time.
public class GamepadBind : IInputBind<Buttons> {
    public static List<GamepadBind> AllGamepadBinds { get; internal set; } = [];
    public string Name { get; set; } = "Not Named";
    public bool JustPressed(int player) {
        if (PendReassign)
            return false;

        return InputUtils.ButtonJustPressed(player, Assigned);
    }
    public bool IsPressed(int player) {
        if (PendReassign)
            return false;

        return InputUtils.GamePads[player].Current.IsButtonDown(Assigned);
    }
    public bool PendReassign { get; set; } = false;
    public Buttons Assigned { get; set; } = BindParser.None; // for some reason Buttons.None does not exist. fml
    public Action? OnPress { get; }
    public Action<Buttons>? OnReassign { get; set; }

    public GamepadBind(string name, Buttons defaultButton = 0) {
        Name = name;
        Assigned = defaultButton;
        AllGamepadBinds.Add(this);
    }
    public void ForceReassign(Buttons newBtn) {
        Assigned = newBtn;
    }

    private void PollReassign(int reassigningPlayer) {
        var buttons = InputUtils.GetPressedButtons(InputUtils.GamePads[reassigningPlayer].Current.Buttons);
        if (buttons.Length > 0) {
            var firstKey = buttons[0];
            if (InputUtils.ButtonJustPressed(reassigningPlayer, firstKey) && firstKey == Assigned) {
                OnReassign?.Invoke(Assigned);
                PendReassign = false;
                return;
            }
            // use keyboard escape to cancel new gamepad bind process
            else if (InputUtils.KeyJustPressed(Keys.Escape)) {
                Assigned = BindParser.None;
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
        if (PendReassign) {
            // first connected controller will always have to reassign
            PollReassign(0);
            return;
        }

        for (int i = 0; i < InputUtils.MAX_GAMEPADS; i++) {
            if (JustPressed(i)) {
                OnPress?.Invoke();
            }
        }
    }

    public override string ToString() {
        return Name + " = {" + $"Key: {Assigned} | Pressed: {IsPressed} | ReassignPending: {PendReassign} " + "}";
    }
}