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

/// <summary>The Controls page of the settings window (<see cref="SettingsUI.Controls"/>): key bindings and the keyboard player.</summary>
/// <remarks>
/// Click a binding, then press the new key. Escape cancels; a key that's already used by another binding swaps with it.
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

    /// <summary>A binding is waiting for a key.</summary>
    public static bool IsRebinding => _rebinding is not null;

    /// <summary>
    /// True while waiting for a key and for a moment after: Escape then cancels the rebind instead of closing the
    /// settings (the pause key sees the same key press).
    /// </summary>
    public static bool SwallowsEscape => IsRebinding || RuntimeData.UpdateCount - _rebindEndedAt <= 2;

    static SettingsPage Page => SettingsUI.Controls;

    sealed record Binding(Keybind Bind, Func<GameConfig, Keys> Get, Action<Keys> Save);
    static readonly List<Binding> _bindings = [];

    static Keybind? _rebinding;
    static Keys _rebindPrevious;
    static uint _rebindEndedAt;

    public static void Initialize() {
        CancelRebind();
        Page.Clear();
        Page.Closed = CancelRebind;
        _bindings.Clear();

        const float left = SettingsUI.LeftX, right = SettingsUI.RightX;
        Page.Header("Keyboard", left);
        Page.Header("Players", right);

        UpKeybindButton = BindingRow(0, "Move Up", PlayerTank.MoveUp, c => c.UpKeybind, k => TankGame.Settings.UpKeybind = k);
        DownKeybindButton = BindingRow(1, "Move Down", PlayerTank.MoveDown, c => c.DownKeybind, k => TankGame.Settings.DownKeybind = k);
        LeftKeybindButton = BindingRow(2, "Move Left", PlayerTank.MoveLeft, c => c.LeftKeybind, k => TankGame.Settings.LeftKeybind = k);
        RightKeybindButton = BindingRow(3, "Move Right", PlayerTank.MoveRight, c => c.RightKeybind, k => TankGame.Settings.RightKeybind = k);
        MineKeybindButton = BindingRow(4, "Place Mine", PlayerTank.PlaceMine, c => c.MineKeybind, k => TankGame.Settings.MineKeybind = k);

        Page.Button(left, SettingsUI.RowY(6), SettingsUI.ColumnW, "Reset Keybinds",
            "Puts every key binding back to its default (W A S D and Space).", ResetBindings);

        var lang = TankGame.GameLanguage;
        KeyboardPlayerButton = Page.Row(right, SettingsUI.RowY(0), lang.Settings.KeyboardPlayer ?? "Keyboard Player",
            "",
            () => PlayerID.GetLocalizedPlayerColorName(PlayerTank.KbPlayer),
            StepKeyboardPlayer,
            () => !Client.IsConnected());
        KeyboardPlayerButton.DynamicDescription = () => Client.IsConnected()
            ? "In multiplayer the keyboard and mouse always control your own tank."
            : $"Which player the keyboard and mouse control. The others use gamepads ({InputUtils.NumGamepadsConnected} connected).";

        var pause = Page.Row(right, SettingsUI.RowY(1), "Pause / Back", "Escape always pauses the game and goes back in menus.",
            () => Keys.Escape.KeyAsString(), _ => { }, () => false);
        pause.ShowArrows = false;
    }

    static SettingRow BindingRow(int index, string label, Keybind bind, Func<GameConfig, Keys> getDefault, Action<Keys> save) {
        _bindings.Add(new Binding(bind, getDefault, save));
        var row = Page.Row(SettingsUI.LeftX, SettingsUI.RowY(index), label,
            "Click, then press the new key. Escape cancels. A key that's already in use swaps with that binding. Right click for the default.",
            () => _rebinding == bind ? TankGame.GameLanguage.Menu.PressAKey ?? "Press a key..." : KeyName(bind.Assigned),
            dir => {
                if (dir > 0)
                    StartRebind(bind, save);
                else
                    Assign(bind, getDefault(new GameConfig()), save);
            });
        row.ShowArrows = false;
        row.Highlighted = () => _rebinding == bind;
        return row;
    }

    static string KeyName(Keys key) => key == Keys.None ? "Unbound" : key.KeyAsString();

    static void StartRebind(Keybind bind, Action<Keys> save) {
        CancelRebind();
        _rebinding = bind;
        _rebindPrevious = bind.Assigned;
        bind.OnReassign = key => {
            bind.OnReassign = null;
            _rebinding = null;
            _rebindEndedAt = RuntimeData.UpdateCount;
            // Escape makes the keybind unbind itself (Keys.None): treat it as cancel
            if (key == Keys.None) {
                bind.ForceReassign(_rebindPrevious);
                return;
            }
            bind.ForceReassign(_rebindPrevious);
            Assign(bind, key, save);
        };
        bind.PendReassign = true;
    }

    static void CancelRebind() {
        if (_rebinding is null)
            return;
        _rebinding.PendReassign = false;
        _rebinding.OnReassign = null;
        _rebinding.ForceReassign(_rebindPrevious);
        _rebinding = null;
        _rebindEndedAt = RuntimeData.UpdateCount;
    }

    /// <summary>Binds <paramref name="key"/>; a binding that already used it gets this one's old key.</summary>
    static void Assign(Keybind bind, Keys key, Action<Keys> save) {
        var old = bind.Assigned;
        if (key == old)
            return;
        foreach (var other in _bindings) {
            if (other.Bind != bind && other.Bind.Assigned == key) {
                other.Bind.ForceReassign(old);
                other.Save(old);
            }
        }
        bind.ForceReassign(key);
        save(key);
        SettingsUI.PlayTick();
    }

    static void ResetBindings() {
        CancelRebind();
        var defaults = new GameConfig();
        foreach (var binding in _bindings) {
            var key = binding.Get(defaults);
            binding.Bind.ForceReassign(key);
            binding.Save(key);
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
