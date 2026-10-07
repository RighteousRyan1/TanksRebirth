using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI;

/// <summary>The Controls page of the settings window (<see cref="SettingsUI.Controls"/>): bindings and the keyboard player.</summary>
/// <remarks>
/// Click a binding, then press a key or a mouse button (right, middle or a side button; left click shoots, so it can't
/// be bound). Escape or a left click cancels; an input that's already used by another binding swaps with it.
/// Right click puts a binding back to its default.
/// </remarks>
public static class ControlsUI {
    public static SettingRow UpKeybindButton = null!;
    public static SettingRow LeftKeybindButton = null!;
    public static SettingRow RightKeybindButton = null!;
    public static SettingRow DownKeybindButton = null!;
    public static SettingRow MineKeybindButton = null!;
    public static SettingRow KeyboardPlayerButton = null!;

    /// <summary>Whether the Controls page is on screen. Clearing it closes the settings window.</summary>
    public static bool BatchVisible { get; set; }

    /// <summary>A binding is waiting for a key or mouse button.</summary>
    public static bool IsRebinding => _rebinding is not null;

    /// <summary>
    /// True while waiting for an input and for a moment after. The input that finishes a rebind (Escape, a right
    /// click...) also reaches the pause key and the UI on the same frame; they ignore it while this is set.
    /// </summary>
    public static bool InputLocked => IsRebinding || RuntimeData.UpdateCount - _rebindEndedAt <= 2;

    /// <summary>Same as <see cref="InputLocked"/> (Escape cancels a rebind instead of closing the settings).</summary>
    public static bool SwallowsEscape => InputLocked;

    static SettingsPage Page => SettingsUI.Controls;

    readonly record struct BindInput(Keys Key, MouseInput Mouse) {
        public bool Matches(Keybind bind) => Mouse != MouseInput.None ? bind.AssignedMouse == Mouse : bind.AssignedMouse == MouseInput.None && bind.Assigned == Key;
        public static BindInput Of(Keybind bind) => new(bind.Assigned, bind.AssignedMouse);
    }

    sealed record Binding(Keybind Bind, Func<GameConfig, BindInput> Get, Action<GameConfig, BindInput> Set);
    static readonly List<Binding> _bindings = [
        new(PlayerTank.MoveUp, c => new(c.UpKeybind, c.UpMouseBind), (c, i) => (c.UpKeybind, c.UpMouseBind) = (i.Key, i.Mouse)),
        new(PlayerTank.MoveDown, c => new(c.DownKeybind, c.DownMouseBind), (c, i) => (c.DownKeybind, c.DownMouseBind) = (i.Key, i.Mouse)),
        new(PlayerTank.MoveLeft, c => new(c.LeftKeybind, c.LeftMouseBind), (c, i) => (c.LeftKeybind, c.LeftMouseBind) = (i.Key, i.Mouse)),
        new(PlayerTank.MoveRight, c => new(c.RightKeybind, c.RightMouseBind), (c, i) => (c.RightKeybind, c.RightMouseBind) = (i.Key, i.Mouse)),
        new(PlayerTank.PlaceMine, c => new(c.MineKeybind, c.MineMouseBind), (c, i) => (c.MineKeybind, c.MineMouseBind) = (i.Key, i.Mouse)),
    ];

    static Binding? _rebinding;
    static BindInput _rebindPrevious;
    static uint _rebindStartedAt, _rebindEndedAt;

    /// <summary>Applies the saved bindings (keys and mouse buttons) to the player's keybinds. <see cref="Initialize"/> calls it.</summary>
    public static void LoadBindings(GameConfig settings) {
        foreach (var binding in _bindings) {
            binding.Bind.AllowMouse = true;
            Apply(binding.Bind, binding.Get(settings));
        }
    }

    public static void Initialize() {
        CancelRebind();
        LoadBindings(TankGame.Settings);
        Page.Clear();
        Page.Closed = CancelRebind;
        Page.Updated = UpdateRebind;

        const float left = SettingsUI.LeftX, right = SettingsUI.RightX;
        Page.Header("Keyboard & Mouse", left);
        Page.Header("Players", right);

        UpKeybindButton = BindingRow(0, "Move Up", _bindings[0]);
        DownKeybindButton = BindingRow(1, "Move Down", _bindings[1]);
        LeftKeybindButton = BindingRow(2, "Move Left", _bindings[2]);
        RightKeybindButton = BindingRow(3, "Move Right", _bindings[3]);
        MineKeybindButton = BindingRow(4, "Place Mine", _bindings[4]);

        Page.Button(left, SettingsUI.RowY(6), SettingsUI.ColumnW, "Reset Bindings",
            "Puts every binding back to its default (W A S D and Space).", ResetBindings);

        var lang = TankGame.GameLanguage;
        KeyboardPlayerButton = Page.Row(right, SettingsUI.RowY(0), lang.Settings.KeyboardPlayer ?? "Keyboard Player",
            "",
            () => PlayerID.GetLocalizedPlayerColorName(PlayerTank.KbPlayer),
            StepKeyboardPlayer,
            () => !Client.IsConnected());
        KeyboardPlayerButton.DynamicDescription = () => Client.IsConnected()
            ? "In multiplayer the keyboard and mouse always control your own tank."
            : $"Which player the keyboard and mouse control. The others use gamepads ({InputUtils.NumGamepadsConnected} connected).";

        var shoot = Page.Row(right, SettingsUI.RowY(1), "Shoot", "Left click always shoots (and clicks menus), so it can't be rebound.",
            () => MouseInput.Left.MouseAsString(), _ => { }, () => false);
        shoot.ShowArrows = false;

        var pause = Page.Row(right, SettingsUI.RowY(2), "Pause / Back", "Escape always pauses the game and goes back in menus.",
            () => Keys.Escape.KeyAsString(), _ => { }, () => false);
        pause.ShowArrows = false;
    }

    static SettingRow BindingRow(int index, string label, Binding binding) {
        binding.Bind.AllowMouse = true;
        var row = Page.Row(SettingsUI.LeftX, SettingsUI.RowY(index), label,
            "Click, then press a key or a mouse button (right, middle or side). Escape or left click cancels. " +
            "An input that's already in use swaps with that binding. Right click for the default.",
            () => _rebinding == binding ? TankGame.GameLanguage.Menu.PressAKey ?? "Press a key..." : InputName(BindInput.Of(binding.Bind)),
            dir => {
                if (InputLocked)
                    return;
                if (dir > 0)
                    StartRebind(binding);
                else
                    Assign(binding, binding.Get(new GameConfig()));
            });
        row.ShowArrows = false;
        row.Highlighted = () => _rebinding == binding;
        return row;
    }

    static string InputName(BindInput input) =>
        input.Mouse != MouseInput.None ? input.Mouse.MouseAsString() : input.Key == Keys.None ? "Unbound" : input.Key.KeyAsString();

    static void StartRebind(Binding binding) {
        CancelRebind();
        var bind = binding.Bind;
        _rebinding = binding;
        _rebindPrevious = BindInput.Of(bind);
        _rebindStartedAt = RuntimeData.UpdateCount;

        bind.OnReassign = key => FinishRebind(binding, new BindInput(key, MouseInput.None));
        bind.OnReassignMouse = mouse => FinishRebind(binding, new BindInput(Keys.None, mouse));
        bind.PendReassign = true;
    }

    static void FinishRebind(Binding binding, BindInput input) {
        var bind = binding.Bind;
        bind.OnReassign = null;
        bind.OnReassignMouse = null;
        _rebinding = null;
        _rebindEndedAt = RuntimeData.UpdateCount;

        Apply(bind, _rebindPrevious);
        // Escape makes the keybind unbind itself (no key, no mouse button): treat it as cancel
        if (input.Key == Keys.None && input.Mouse == MouseInput.None)
            return;
        Assign(binding, input);
    }

    static void UpdateRebind() {
        // a left click anywhere (after the one that started the rebind) cancels it
        if (IsRebinding && RuntimeData.UpdateCount > _rebindStartedAt && InputUtils.Click())
            CancelRebind();
    }

    static void CancelRebind() {
        if (_rebinding is null)
            return;
        var bind = _rebinding.Bind;
        bind.PendReassign = false;
        bind.OnReassign = null;
        bind.OnReassignMouse = null;
        Apply(bind, _rebindPrevious);
        _rebinding = null;
        _rebindEndedAt = RuntimeData.UpdateCount;
    }

    static void Apply(Keybind bind, BindInput input) {
        if (input.Mouse != MouseInput.None)
            bind.ForceReassign(input.Mouse);
        else
            bind.ForceReassign(input.Key);
    }

    /// <summary>Binds <paramref name="input"/>; a binding that already used it gets this one's old input.</summary>
    static void Assign(Binding binding, BindInput input) {
        var old = BindInput.Of(binding.Bind);
        if (input.Matches(binding.Bind))
            return;
        foreach (var other in _bindings) {
            if (other != binding && input.Matches(other.Bind)) {
                Apply(other.Bind, old);
                other.Set(TankGame.Settings, old);
            }
        }
        Apply(binding.Bind, input);
        binding.Set(TankGame.Settings, input);
        SettingsUI.PlayTick();
    }

    static void ResetBindings() {
        CancelRebind();
        var defaults = new GameConfig();
        foreach (var binding in _bindings) {
            var input = binding.Get(defaults);
            Apply(binding.Bind, input);
            binding.Set(TankGame.Settings, input);
        }
    }

    static void StepKeyboardPlayer(int dir) {
        var max = InputUtils.NumGamepadsConnected;
        var next = PlayerTank.KbPlayer + dir;
        if (next > max)
            next = -1;
        else if (next < -1)
            next = max;
        PlayerTank.KbPlayer = next;
        if (GameUI.KeyboardPlayerButton is not null)
            GameUI.KeyboardPlayerButton.Text = $"{TankGame.GameLanguage.Settings.KeyboardPlayer}: {PlayerID.GetLocalizedPlayerColorName(next)}";
    }

    /// <summary>Opens the settings window on this page.</summary>
    public static void ShowAll() => SettingsUI.Open(SettingsUI.Controls);

    /// <summary>Closes the settings window if it's on this page.</summary>
    public static void HideAll() {
        if (SettingsUI.IsOpen && SettingsUI.Current == SettingsUI.Controls)
            SettingsUI.Close();
    }
}
