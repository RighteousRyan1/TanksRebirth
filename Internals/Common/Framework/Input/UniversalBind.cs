using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TanksRebirth.Internals.Common.Framework.Input; 

// NOTE: mostly ai generated as a mockup. will be making it proper later, since universal inputs are strange

/// <summary>Mouse buttons we care about.</summary>
public enum MouseButton {
    Left,
    Right,
    Middle,
    X1,
    X2,
    // Add WheelUp / WheelDown later if you want these to count as "buttons".
}

/// <summary>
/// Universal input bind that can be a keyboard key, a gamepad button, or a mouse button.
/// </summary>
/// <summary>
/// Universal bind that can target keyboard, gamepad, or any mouse button index.
/// Mouse edge detection is based on InputUtils.OldMouseSnapshot / CurrentMouseSnapshot.
/// </summary>
public class UniversalBind {
    public static readonly List<UniversalBind> All = [];

    /// <summary>
    /// Call once per frame (before reading JustPressed/IsPressed).
    /// </summary>
    public static void UpdateAll() {
        for (int i = 0; i < All.Count; i++)
            All[i].Update();
    }

    // ---------- Static: mouse abstraction ----------
    // You can override how we detect if a specific mouse button index is down,
    // and how many buttons to scan during "press anything to bind" mode.
    public static void ConfigureMouseResolver(
        Func<MouseState, int, bool> isDownResolver,
        int maxButtons = 8,
        Func<int, string> nameResolver = null) {
        _mouseIsDownResolver = isDownResolver ?? _mouseIsDownResolver;
        _mouseMaxButtons = Math.Max(1, maxButtons);
        _mouseNameResolver = nameResolver ?? _mouseNameResolver;
    }

    private static int _mouseMaxButtons = 5; // L,R,M,X1,X2 by default
    private static Func<MouseState, int, bool> _mouseIsDownResolver = DefaultMouseIsDown;
    private static Func<int, string> _mouseNameResolver = DefaultMouseName;

    private static bool DefaultMouseIsDown(MouseState s, int index) {
        // 0..4 mapped to the standard MouseState buttons
        return index switch {
            0 => s.LeftButton.IsPressed(),
            1 => s.RightButton.IsPressed(),
            2 => s.MiddleButton.IsPressed(),
            3 => s.XButton1.IsPressed(),
            4 => s.XButton2.IsPressed(),
            _ => false // Beyond X2 requires a custom resolver (e.g., RawInput/SDL)
        };
    }

    private static string DefaultMouseName(int index) => index switch {
        0 => "Left",
        1 => "Right",
        2 => "Middle",
        3 => "X1",
        4 => "X2",
        _ => $"Btn{index}"
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MouseIsDown(int index)
        => _mouseIsDownResolver(InputUtils.CurrentMouseSnapshot, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MouseWasDown(int index)
        => _mouseIsDownResolver(InputUtils.OldMouseSnapshot, index);

    private static bool MouseJustPressed(out int idx) {
        // Scan 0.._mouseMaxButtons-1 for a rising edge
        for (int i = 0; i < _mouseMaxButtons; i++) {
            if (!MouseWasDown(i) && MouseIsDown(i)) {
                idx = i;
                return true;
            }
        }
        idx = -1;
        return false;
    }
    private static bool MouseJustPressed(int index)
        => !MouseWasDown(index) && MouseIsDown(index);

    internal static string MouseNameFor(int index) => _mouseNameResolver(index);

    // ---------- Instance ----------
    public string Name { get; set; } = "Not Named";

    public AssignedBinding Assigned { get; private set; } = AssignedBinding.None;

    public Action<AssignedBinding> OnReassign { get; set; }
    public Action OnPress { get; set; }

    public bool PendReassign { get; set; }

    public bool JustPressed => !PendReassign && IsJustPressedInternal();
    public bool IsPressed => !PendReassign && IsPressedInternal();

    public UniversalBind(string name) {
        Name = name;
        All.Add(this);
    }
    public UniversalBind(string name, Keys defaultKey) : this(name) => Assigned = new AssignedBinding(defaultKey);
    public UniversalBind(string name, Buttons defaultPad) : this(name) => Assigned = new AssignedBinding(defaultPad);
    /// <param name="mouseIndex">Arbitrary mouse button index (0..N). Defaults: 0=L,1=R,2=M,3=X1,4=X2</param>
    public UniversalBind(string name, int mouseIndex) : this(name) => Assigned = new AssignedBinding(mouseIndex);

    public void ForceReassign(Keys key) => SetAssigned(new AssignedBinding(key));
    public void ForceReassign(Buttons button) => SetAssigned(new AssignedBinding(button));
    public void ForceReassign(int mouseIndex) => SetAssigned(new AssignedBinding(mouseIndex));
    public void Clear() => SetAssigned(AssignedBinding.None);

    public void Fire() => OnPress?.Invoke();

    private void Update() {
        if (PendReassign) {
            if (PollReassign())
                return;
        }

        if (JustPressed)
            OnPress?.Invoke();
    }

    private void SetAssigned(AssignedBinding ab) {
        Assigned = ab;
        OnReassign?.Invoke(Assigned);
    }

    /// <summary>“Press anything” rebinding using InputUtils snapshots for all devices.</summary>
    private bool PollReassign() {
        // Cancel w/ Escape
        if (InputUtils.KeyJustPressed(Keys.Escape)) {
            Clear();
            PendReassign = false;
            return true;
        }

        // Keyboard
        var pressedKeys = InputUtils.CurrentKeySnapshot.GetPressedKeys();
        if (pressedKeys.Length > 0) {
            var k = pressedKeys[0];
            if (InputUtils.KeyJustPressed(k)) {
                SetAssigned(new AssignedBinding(k));
                PendReassign = false;
                return true;
            }
        }

        // Gamepad
        var buttons = InputUtils.GetPressedButtons(InputUtils.CurrentGamePadSnapshot.Buttons);
        if (buttons.Length > 0) {
            var b = buttons[0];
            if (InputUtils.ButtonJustPressed(b)) {
                SetAssigned(new AssignedBinding(b));
                PendReassign = false;
                return true;
            }
        }

        // Mouse (any index 0..N)
        if (MouseJustPressed(out var mbIndex)) {
            SetAssigned(new AssignedBinding(mbIndex));
            PendReassign = false;
            return true;
        }

        return false; // still waiting
    }

    private bool IsJustPressedInternal() {
        return Assigned.Device switch {
            BindingDevice.Keyboard => InputUtils.KeyJustPressed(Assigned.Key),
            BindingDevice.Gamepad => InputUtils.ButtonJustPressed(Assigned.Button),
            BindingDevice.Mouse => Assigned.MouseIndex >= 0 && MouseJustPressed(Assigned.MouseIndex),
            _ => false
        };
    }

    private bool IsPressedInternal() {
        return Assigned.Device switch {
            BindingDevice.Keyboard => InputUtils.CurrentKeySnapshot.IsKeyDown(Assigned.Key),
            BindingDevice.Gamepad => InputUtils.CurrentGamePadSnapshot.IsButtonDown(Assigned.Button),
            BindingDevice.Mouse => Assigned.MouseIndex >= 0 && MouseIsDown(Assigned.MouseIndex),
            _ => false
        };
    }

    public override string ToString()
        => $"{Name} = {{ {Assigned} | Pressed: {IsPressed} | ReassignPending: {PendReassign} }}";

    // Convenience names for the common 0..4 mapping if you want them.
    public static class MouseButtons {
        public const int Left = 0, Right = 1, Middle = 2, X1 = 3, X2 = 4;
    }
}

internal static class MouseButtonStateExt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPressed(this ButtonState s) => s == ButtonState.Pressed;
}