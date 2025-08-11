using Microsoft.Xna.Framework.Input;

namespace TanksRebirth.Internals.Common.Framework.Input;
public enum BindingDevice {
    None = 0,
    Keyboard,
    Gamepad,
    Mouse
}

/// <summary>
/// Describes the current assignment (device + control code).
/// For mouse, we use an arbitrary button index (0..N).
/// </summary>
public readonly struct AssignedBinding {
    public readonly BindingDevice Device;
    public readonly Keys Key;
    public readonly Buttons Button;
    public readonly int MouseIndex;

    public AssignedBinding(Keys key) {
        Device = BindingDevice.Keyboard;
        Key = key;
        Button = 0;
        MouseIndex = -1;
    }
    public AssignedBinding(Buttons button) {
        Device = BindingDevice.Gamepad;
        Key = 0;
        Button = button;
        MouseIndex = -1;
    }
    public AssignedBinding(int mouseIndex) {
        Device = BindingDevice.Mouse;
        Key = 0;
        Button = 0;
        MouseIndex = mouseIndex;
    }

    AssignedBinding(BindingDevice device, Keys key, Buttons button, int mouseIndex) {
        Device = device; Key = key; Button = button; MouseIndex = mouseIndex;
    }

    public static AssignedBinding None => new(BindingDevice.None, 0, 0, -1);

    public override string ToString() {
        return Device switch {
            BindingDevice.Keyboard => $"Key:{Key.KeyAsString()}",
            BindingDevice.Gamepad => $"Pad:{Button}",
            BindingDevice.Mouse => $"Mouse:{UniversalBind.MouseNameFor(MouseIndex)}",
            _ => "Unassigned"
        };
    }
}