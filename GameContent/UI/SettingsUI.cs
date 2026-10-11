using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI;

/// <summary>
/// The settings window: one panel with Audio / Video / Controls tabs along the top, a description bar for whatever
/// the mouse is over, and the pages themselves (<see cref="VolumeUI"/>, <see cref="GraphicsUI"/>, <see cref="ControlsUI"/>).
/// </summary>
/// <remarks>
/// <para>Click a tab (or the arrows beside them, or press Q / E) to switch pages. The window remembers the last page.</para>
/// <para>Each page is a <see cref="SettingsPage"/>: it owns its rows and builds them with <see cref="SettingsPage.Row"/>,
/// <see cref="SettingsPage.Slider"/>, <see cref="SettingsPage.Button"/> and <see cref="SettingsPage.Header"/>.
/// Layout is in 1920x1080 units and scaled with <c>ToResolution</c>, like the rest of the UI.</para>
/// </remarks>
public static class SettingsUI {
    public static readonly SettingsPage Audio = new("Audio",
        "Music, sound effect and ambient volume.",
        "Drag a slider (or scroll over it) to change the volume. Right click puts it back to the default.");
    public static readonly SettingsPage Video = new("Video",
        "Window mode, resolution, frame rate, anti-aliasing and dynamic lighting.",
        "Left click an option for the next value, right click for the previous one.");
    public static readonly SettingsPage Controls = new("Controls",
        "Keyboard bindings and which player the keyboard and mouse control.",
        "Click a binding, then press the new key. Escape cancels, right click puts back the default.");

    static readonly SettingsPage[] _pages = [Audio, Video, Controls];

    /// <summary>Whether the settings window is showing.</summary>
    public static bool IsOpen { get; private set; }
    /// <summary>The page on screen (or the one that opens next time).</summary>
    public static SettingsPage Current { get; private set; } = Audio;

    // layout

    public const float PanelX = 150, PanelY = 30, PanelW = 1620, PanelH = 800;
    public const float LeftX = 190, RightX = 990, CenterX = 590, ColumnW = 740;
    public const float HeaderY = 115, FirstRowY = 172, RowH = 50, RowStep = 57;
    public const float DescY = 752, DescH = 58;

    const float TabY = 44, TabW = 300, TabH = 54, TabGap = 12, ArrowW = 110;

    public static float RowY(int index) => FirstRowY + index * RowStep;

    static readonly List<UIElement> _frame = [];
    static UIPanel _panel = null!;
    static uint _openedAt;

    /// <summary>Ignores the click that opened the window or switched the page (it lands on the same frame the rows appear).</summary>
    public static bool InputReady => RuntimeData.UpdateCount - _openedAt > 5;

    // setup

    /// <summary>Builds the window frame (panel, tabs, description bar). Call before the pages' Initialize.</summary>
    public static void Initialize() {
        foreach (var element in _frame)
            element.Remove();
        _frame.Clear();
        IsOpen = false;

        _panel = new UIPanel((_, sb) => DrawFrame(sb)) {
            BackgroundColor = Color.Black * 0.55f,
            IgnoreMouseInteractions = true,
        };
        AddFrame(_panel, PanelX, PanelY, PanelW, PanelH);

        var tabsWidth = _pages.Length * TabW + (_pages.Length - 1) * TabGap;
        var tabsX = PanelX + (PanelW - tabsWidth) / 2f;
        for (int i = 0; i < _pages.Length; i++)
            AddFrame(new SettingsTab(_pages[i]), tabsX + i * (TabW + TabGap), TabY, TabW, TabH);
        AddFrame(new SettingsTab(-1), tabsX - TabGap - ArrowW, TabY, ArrowW, TabH);
        AddFrame(new SettingsTab(1), tabsX + tabsWidth + TabGap, TabY, ArrowW, TabH);

        AddFrame(new SettingsDescriptionBar(), LeftX, DescY, RightX + ColumnW - LeftX, DescH);

        SetFrameVisible(false);
        foreach (var page in _pages)
            page.SetVisible(false);
        SyncLegacyFlags();
    }

    static void AddFrame(UIElement element, float x, float y, float width, float height) {
        element.SetDimensions(() => new Vector2(x, y).ToResolution(), () => new Vector2(width, height).ToResolution());
        element.IsVisible = false;
        _frame.Add(element);
    }

    // state management

    /// <summary>Shows the window on <paramref name="page"/> (the last used page if null).</summary>
    public static void Open(SettingsPage? page = null) {
        IsOpen = true;
        SetFrameVisible(true);
        Show(page ?? Current, force: true);
    }

    /// <summary>Hides the window (the pages discard anything they only apply on demand, like an unapplied resolution).</summary>
    public static void Close() {
        if (!IsOpen)
            return;
        IsOpen = false;
        Current.SetVisible(false);
        Current.Closed?.Invoke();
        SetFrameVisible(false);
        SyncLegacyFlags();
    }

    /// <summary>Switches to <paramref name="page"/>. Opens the window if it was closed.</summary>
    public static void Show(SettingsPage page, bool force = false) {
        if (!IsOpen) {
            Open(page);
            return;
        }
        if (page == Current && !force)
            return;

        if (page != Current && Current.IsVisible) {
            Current.SetVisible(false);
            Current.Closed?.Invoke();
        }
        Current = page;
        _openedAt = RuntimeData.UpdateCount;
        page.Opened?.Invoke();
        foreach (var p in _pages)
            p.SetVisible(p == page);
        SyncLegacyFlags();
    }

    /// <summary>The next (1) or previous (-1) page, wrapping around.</summary>
    public static void Cycle(int dir) {
        var index = Array.IndexOf(_pages, Current);
        Show(_pages[((index + dir) % _pages.Length + _pages.Length) % _pages.Length]);
    }

    /// <summary>Per-frame input: Q / E switch pages, sliders follow the mouse. Called from <see cref="GameUI.UpdateButtons"/>.</summary>
    public static void Update() {
        if (!IsOpen)
            return;
        if (!Validate())
            return;

        if (!ControlsUI.IsRebinding && UITextInput.currentActiveBox == -1 && !ChatSystem.ActiveHandle) {
            if (InputUtils.KeyJustPressed(Keys.Q)) {
                Cycle(-1);
                PlayTick();
            }
            else if (InputUtils.KeyJustPressed(Keys.E)) {
                Cycle(1);
                PlayTick();
            }
        }
        Current.UpdateInput();
    }

    /// <summary>
    /// Older code closes the settings by clearing <see cref="GraphicsUI.IsVisible"/>, <see cref="VolumeUI.BatchVisible"/> or
    /// <see cref="ControlsUI.BatchVisible"/> directly (the main menu does when it closes). Follow it.
    /// </summary>
    static bool Validate() {
        var stillOpen = Current == Video ? GraphicsUI.IsVisible : Current == Audio ? VolumeUI.BatchVisible : ControlsUI.BatchVisible;
        if (!stillOpen)
            Close();
        return stillOpen;
    }

    static void SyncLegacyFlags() {
        VolumeUI.BatchVisible = IsOpen && Current == Audio;
        GraphicsUI.IsVisible = IsOpen && Current == Video;
        ControlsUI.BatchVisible = IsOpen && Current == Controls;
    }

    static void SetFrameVisible(bool visible) {
        foreach (var element in _frame)
            element.IsVisible = visible;
    }

    // drawing

    static void DrawFrame(SpriteBatch sb) {
        if (!IsOpen || !Validate())
            return;
        Current.RefreshButtons();
    }

    /// <summary>The description for whatever the mouse is over: a tab, a row on the current page, or the page's hint.</summary>
    internal static string HoveredDescription() {
        var mouse = MouseUtils.MousePosition;
        foreach (var element in _frame) {
            if (element is SettingsTab tab && tab.IsVisible && tab.Hitbox.Contains(mouse))
                return tab.Description;
        }
        return Current.HoveredDescription() ?? Current.Hint;
    }

    internal static void PlayTick() => SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect);
}

/// <summary>One page of the settings window: its rows, buttons and headers.</summary>
public sealed class SettingsPage(string name, string description, string hint) {
    /// <summary>Tab title.</summary>
    public readonly string Name = name;
    /// <summary>Shown when the mouse is over the tab.</summary>
    public readonly string Description = description;
    /// <summary>Shown in the description bar when the mouse isn't over anything on the page.</summary>
    public readonly string Hint = hint;

    /// <summary>Runs when the page comes on screen.</summary>
    public Action? Opened;
    /// <summary>Runs when the page leaves the screen (another tab, or the window closed).</summary>
    public Action? Closed;
    /// <summary>Runs every update while the page is on screen.</summary>
    public Action? Updated;

    public bool IsVisible { get; private set; }
    public IReadOnlyList<UIElement> Elements => _elements;

    readonly List<UIElement> _elements = [];
    readonly Dictionary<UITextButton, (string Description, Func<bool> Enabled, bool Highlight)> _buttons = [];

    /// <summary>Removes everything on the page (call at the start of the page's Initialize).</summary>
    public void Clear() {
        foreach (var element in _elements)
            element.Remove();
        _elements.Clear();
        _buttons.Clear();
    }

    public T Add<T>(T element, float x, float y, float width, float height) where T : UIElement {
        element.SetDimensions(() => new Vector2(x, y).ToResolution(), () => new Vector2(width, height).ToResolution());
        element.IsVisible = IsVisible;
        _elements.Add(element);
        return element;
    }

    /// <summary>A column title with an underline.</summary>
    public void Header(string text, float x, float y = SettingsUI.HeaderY, float width = SettingsUI.ColumnW)
        => Add(new SettingsHeader(text), x, y, width, 46);

    /// <summary>An option row: left click = next value, right click = previous.</summary>
    public SettingRow Row(float x, float y, string label, string description, Func<string> value, Action<int> step,
        Func<bool>? enabled = null, float width = SettingsUI.ColumnW)
        => Add(new SettingRow(label, description, value, step, enabled), x, y, width, SettingsUI.RowH);

    /// <summary>A 0 - 1 slider row (shown as a percentage).</summary>
    public SliderRow Slider(float x, float y, string label, string description, Func<float> get, Action<float> set, float defaultValue,
        float width = SettingsUI.ColumnW)
        => Add(new SliderRow(label, description, get, set, defaultValue), x, y, width, SettingsUI.RowH);

    /// <summary>A push button. Greys out (and ignores clicks) while <paramref name="enabled"/> is false.</summary>
    public UITextButton Button(float x, float y, float width, string text, string description, Action onClick,
        Func<bool>? enabled = null, bool highlight = false) {
        enabled ??= () => true;
        var button = new UITextButton(text, FontGlobals.RebirthFont, Color.WhiteSmoke, 0.75f) {
            OnLeftClick = _ => {
                if (!SettingsUI.InputReady || !enabled())
                    return;
                onClick();
            },
            OnMouseOver = _ => SettingsUI.PlayTick()
        };
        Add(button, x, y, width, SettingsUI.RowH);
        _buttons[button] = (description, enabled, highlight);
        return button;
    }

    internal void SetVisible(bool visible) {
        IsVisible = visible;
        foreach (var element in _elements)
            element.IsVisible = visible;
    }

    internal void UpdateInput() {
        Updated?.Invoke();
        foreach (var element in _elements)
            if (element is SliderRow slider)
                slider.UpdateInput();
    }

    internal void RefreshButtons() {
        foreach (var (button, info) in _buttons) {
            var on = info.Enabled();
            button.Color = on ? (info.Highlight ? Color.Gold : Color.WhiteSmoke) : Color.Gray;
            button.HoverColor = on ? Color.CornflowerBlue : Color.Gray;
        }
    }

    internal string? HoveredDescription() {
        var mouse = MouseUtils.MousePosition;
        foreach (var element in _elements) {
            if (!element.IsVisible || !element.Hitbox.Contains(mouse))
                continue;
            if (element is SettingRow row)
                return row.CurrentDescription;
            if (element is SliderRow slider)
                return slider.Description;
            if (element is UITextButton button && _buttons.TryGetValue(button, out var info))
                return info.Description;
        }
        return null;
    }
}
