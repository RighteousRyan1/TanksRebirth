using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace TanksRebirth.Internals.Common.Framework.Input;

// Update seems really... bad. but we can fix that some other time.
public class GamepadBind : IInputBind<Buttons>
{
    public static List<GamepadBind> AllGamepadBinds { get; internal set; } = [];

    public string Name { get; set; } = "Not Named";
    public bool JustPressed => InputUtils.ButtonJustPressed(Assigned) && !PendReassign;
    public bool IsPressed => InputUtils.CurrentGamePadSnapshot.IsButtonDown(Assigned) && !PendReassign;
    public bool PendReassign { get; set; } = false;
    public Buttons Assigned { get; set; } = BindParser.None; // for some reason Buttons.None does not exist. fml
    public Action OnPress { get; }
    public Action<Buttons> OnReassign { get; set; }

    public GamepadBind(string name, Buttons defaultButton = 0) {
        Name = name;
        Assigned = defaultButton;
        AllGamepadBinds.Add(this);
    }
    public void ForceReassign(Buttons newKey) {
        Assigned = newKey;
    }

    private void PollReassign() {
        var buttons = InputUtils.GetPressedButtons(InputUtils.CurrentGamePadSnapshot.Buttons);
        if (buttons.Length > 0) {
            var firstKey = buttons[0];
            if (InputUtils.ButtonJustPressed(firstKey) && firstKey == Assigned) {
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
        if (PendReassign)
            PollReassign();

        if (JustPressed) {
            OnPress?.Invoke();
        }
    }

    public override string ToString() {
        return Name + " = {" + $"Key: {Assigned} | Pressed: {IsPressed} | ReassignPending: {PendReassign} " + "}";
    }
}