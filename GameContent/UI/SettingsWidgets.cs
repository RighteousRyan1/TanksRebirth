using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using FontStashSharp;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI;

/// <summary>One option: label on the left, "&lt; value &gt;" on the right. Left click = next, right click = previous.</summary>
public sealed class SettingRow : UIElement {
    public string Label;
    public string Description;
    /// <summary>Overrides <see cref="Description"/> when set (for descriptions that change, e.g. with a count in them).</summary>
    public Func<string>? DynamicDescription;
    /// <summary>Draw the "&lt;" and "&gt;" around the value (off for rows that aren't cycled, like key bindings).</summary>
    public bool ShowArrows = true;
    /// <summary>Draws the row gold while true (e.g. while waiting for a key).</summary>
    public Func<bool>? Highlighted;

    readonly Func<string> _value;
    readonly Action<int> _step;
    readonly Func<bool>? _enabled;

    public bool Enabled => _enabled?.Invoke() ?? true;
    public string CurrentDescription => DynamicDescription?.Invoke() ?? Description;

    public SettingRow(string label, string description, Func<string> value, Action<int> step, Func<bool>? enabled) {
        Label = label;
        Description = description;
        _value = value;
        _step = step;
        _enabled = enabled;
        OnLeftClick = _ => Click(1);
        OnRightClick = _ => Click(-1);
        OnMouseOver = _ => SettingsUI.PlayTick();
    }

    void Click(int dir) {
        if (!SettingsUI.InputReady)
            return;
        if (!Enabled) {
            SoundPlayer.SoundError();
            return;
        }
        Step(dir);
    }

    /// <summary>Changes the value as if clicked (1 = next, -1 = previous).</summary>
    public void Step(int dir) => _step(dir);

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var enabled = Enabled;
        var highlighted = Highlighted?.Invoke() ?? false;
        var hovered = enabled && Hitbox.Contains(MouseUtils.MousePosition);
        var background = highlighted ? Color.Gold : !enabled ? Color.Gray * 0.55f : hovered ? Color.CornflowerBlue : Color.WhiteSmoke;
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, background, Vector2.Zero);

        var font = FontGlobals.RebirthFont;
        var scale = SettingsText.Scale(0.72f);
        var textColor = enabled ? Color.Black : Color.Black * 0.5f;
        var pad = 22f.ToResolutionX();
        var centerY = Hitbox.Center.Y;

        var labelSize = font.MeasureString(Label);
        spriteBatch.DrawString(font, Label, new Vector2(Hitbox.X + pad, centerY), textColor, scale, 0f, new Vector2(0f, labelSize.Y / 2f));

        var value = _value();
        var valueSize = font.MeasureString(value);
        var valueColor = enabled ? new Color(20, 40, 110) : Color.Black * 0.45f;
        var right = Hitbox.Right - pad;

        if (!ShowArrows) {
            spriteBatch.DrawString(font, value, new Vector2(right, centerY), valueColor, scale, 0f, new Vector2(valueSize.X, valueSize.Y / 2f));
            return;
        }

        var arrowSize = font.MeasureString(">");
        var arrowColor = enabled ? (hovered ? Color.White : Color.DimGray) : Color.Black * 0.3f;
        spriteBatch.DrawString(font, ">", new Vector2(right, centerY), arrowColor, scale, 0f, new Vector2(arrowSize.X, arrowSize.Y / 2f));
        var valueRight = right - (arrowSize.X + 14f) * scale.X;
        spriteBatch.DrawString(font, value, new Vector2(valueRight, centerY), valueColor, scale, 0f, new Vector2(valueSize.X, valueSize.Y / 2f));
        var arrowLeft = valueRight - (valueSize.X + 14f) * scale.X;
        spriteBatch.DrawString(font, "<", new Vector2(arrowLeft, centerY), arrowColor, scale, 0f, new Vector2(arrowSize.X, arrowSize.Y / 2f));
    }
}

/// <summary>
/// A 0 - 1 value as a bar: label on the left, percentage and bar on the right. Drag (or click) the bar, scroll over the
/// row for 5% steps, right click for the default.
/// </summary>
public sealed class SliderRow : UIElement {
    public string Label;
    public string Description;

    readonly Func<float> _get;
    readonly Action<float> _set;
    readonly float _default;
    bool _dragging;

    /// <summary>The value, 0 - 1. Setting it stores it right away (values under 1% become 0).</summary>
    public float Value {
        get => _get();
        set {
            var v = MathHelper.Clamp(value, 0f, 1f);
            _set(v < 0.01f ? 0f : v);
        }
    }

    public SliderRow(string label, string description, Func<float> get, Action<float> set, float defaultValue) {
        Label = label;
        Description = description;
        _get = get;
        _set = set;
        _default = defaultValue;
        OnRightClick = _ => {
            if (!SettingsUI.InputReady)
                return;
            Value = _default;
            SettingsUI.PlayTick();
        };
        OnMouseOver = _ => SettingsUI.PlayTick();
    }

    Rectangle Track {
        get {
            var width = (int)(Hitbox.Width * 0.42f);
            var height = Math.Max(4, (int)12f.ToResolutionY());
            var x = Hitbox.Right - (int)24f.ToResolutionX() - width;
            return new Rectangle(x, Hitbox.Center.Y - height / 2, width, height);
        }
    }

    internal void UpdateInput() {
        if (!IsVisible) {
            _dragging = false;
            return;
        }
        var mouse = MouseUtils.MousePosition;
        var track = Track;
        var grabArea = new Rectangle(track.X - (int)12f.ToResolutionX(), Hitbox.Y, track.Width + (int)24f.ToResolutionX(), Hitbox.Height);

        if (!_dragging && InputUtils.Click() && SettingsUI.InputReady && grabArea.Contains(mouse))
            _dragging = true;

        if (_dragging) {
            if (!InputUtils.MouseLeft)
                _dragging = false;
            else
                Value = (mouse.X - track.X) / track.Width;
        }
        else if (Hitbox.Contains(mouse)) {
            var wheel = InputUtils.GetScrollWheelChange();
            if (wheel != 0) {
                Value = MathF.Round((Value + wheel * 0.05f) * 20f) / 20f;
                SettingsUI.PlayTick();
            }
        }
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var active = _dragging || Hitbox.Contains(MouseUtils.MousePosition);
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, active ? Color.CornflowerBlue : Color.WhiteSmoke, Vector2.Zero);

        var font = FontGlobals.RebirthFont;
        var scale = SettingsText.Scale(0.72f);
        var pad = 22f.ToResolutionX();
        var centerY = Hitbox.Center.Y;

        var labelSize = font.MeasureString(Label);
        spriteBatch.DrawString(font, Label, new Vector2(Hitbox.X + pad, centerY), Color.Black, scale, 0f, new Vector2(0f, labelSize.Y / 2f));

        var value = Value;
        var track = Track;
        var pixel = TextureGlobals.Pixels[Color.White];
        var dark = new Color(20, 40, 110);

        spriteBatch.Draw(pixel, track, Color.Black * 0.25f);
        var fill = new Rectangle(track.X, track.Y, (int)(track.Width * value), track.Height);
        spriteBatch.Draw(pixel, fill, active ? Color.White : dark);

        var knobW = Math.Max(4, (int)10f.ToResolutionX());
        var knobH = (int)(Hitbox.Height * 0.62f);
        var knob = new Rectangle(track.X + fill.Width - knobW / 2, centerY - knobH / 2, knobW, knobH);
        spriteBatch.Draw(pixel, new Rectangle(knob.X - 2, knob.Y - 2, knob.Width + 4, knob.Height + 4), Color.Black * 0.6f);
        spriteBatch.Draw(pixel, knob, active ? Color.White : dark);

        var percent = $"{MathF.Round(value * 100f)}%";
        var percentSize = font.MeasureString(percent);
        spriteBatch.DrawString(font, percent, new Vector2(track.X - 22f.ToResolutionX(), centerY), dark, scale, 0f,
            new Vector2(percentSize.X, percentSize.Y / 2f));
    }
}

/// <summary>A column title with an underline.</summary>
public sealed class SettingsHeader : UIElement {
    readonly string _text;
    public SettingsHeader(string text) {
        _text = text;
        IgnoreMouseInteractions = true;
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var font = FontGlobals.RebirthFont;
        DrawUtils.DrawStringWithBorder(spriteBatch, font, _text, new Vector2(Hitbox.X + 6f.ToResolutionX(), Hitbox.Center.Y),
            Color.White, Color.Black, SettingsText.Scale(0.9f), 0f, Anchor.LeftCenter, 1f);
        var line = new Rectangle(Hitbox.X, Hitbox.Bottom - (int)3f.ToResolutionY(), Hitbox.Width, Math.Max(1, (int)3f.ToResolutionY()));
        spriteBatch.Draw(TextureGlobals.Pixels[Color.White], line, Color.White * 0.8f);
    }
}

/// <summary>A tab at the top of the settings window, or one of the arrows beside the tabs.</summary>
public sealed class SettingsTab : UIElement {
    readonly SettingsPage? _page;
    readonly int _dir;

    public string Description => _page?.Description ?? (_dir < 0 ? "Previous page (Q)" : "Next page (E)");

    /// <summary>A tab that opens <paramref name="page"/>.</summary>
    public SettingsTab(SettingsPage page) {
        _page = page;
        OnLeftClick = _ => Activate();
        OnMouseOver = _ => SettingsUI.PlayTick();
    }

    /// <summary>An arrow that cycles the pages (-1 = previous, 1 = next).</summary>
    public SettingsTab(int dir) {
        _dir = dir;
        OnLeftClick = _ => Activate();
        OnMouseOver = _ => SettingsUI.PlayTick();
    }

    void Activate() {
        if (ControlsUI.InputLocked)
            return;
        if (_page is not null)
            SettingsUI.Show(_page);
        else
            SettingsUI.Cycle(_dir);
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var selected = _page is not null && _page == SettingsUI.Current;
        var hovered = Hitbox.Contains(MouseUtils.MousePosition);
        var background = selected ? Color.WhiteSmoke : hovered ? Color.CornflowerBlue : Color.Black * 0.45f;
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, background, Vector2.Zero);

        var font = FontGlobals.RebirthFont;
        var center = Hitbox.Center.ToVector2();
        if (_page is null) {
            var arrow = _dir < 0 ? "< Q" : "E >";
            DrawUtils.DrawStringWithBorder(spriteBatch, font, arrow, center, Color.White, Color.Black, SettingsText.Scale(0.7f), 0f, Anchor.Center, 1f);
            return;
        }

        if (selected) {
            var size = font.MeasureString(_page.Name);
            spriteBatch.DrawString(font, _page.Name, center, Color.Black, SettingsText.Scale(0.9f), 0f, size / 2f);
            var bar = new Rectangle(Hitbox.X + (int)16f.ToResolutionX(), Hitbox.Bottom - (int)8f.ToResolutionY(),
                Hitbox.Width - (int)32f.ToResolutionX(), Math.Max(2, (int)4f.ToResolutionY()));
            spriteBatch.Draw(TextureGlobals.Pixels[Color.White], bar, Color.CornflowerBlue);
        }
        else {
            DrawUtils.DrawStringWithBorder(spriteBatch, font, _page.Name, center, hovered ? Color.White : Color.LightGray, Color.Black,
                SettingsText.Scale(0.85f), 0f, Anchor.Center, 1f);
        }
    }
}

/// <summary>Shows the description of whatever the mouse is over in the settings window.</summary>
public sealed class SettingsDescriptionBar : UIElement {
    public SettingsDescriptionBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, Color.Black * 0.45f, Vector2.Zero);
        var text = SettingsUI.HoveredDescription();
        var font = FontGlobals.RebirthFont;
        var scale = SettingsText.Scale(0.6f);
        var maxWidth = Hitbox.Width - 40f.ToResolutionX();
        var size = font.MeasureString(text);
        // shrink long descriptions to fit on one line
        if (size.X * scale.X > maxWidth)
            scale *= maxWidth / (size.X * scale.X);
        spriteBatch.DrawString(font, text, Hitbox.Center.ToVector2(), Color.White, scale, 0f, size / 2f);
    }
}

static class SettingsText {
    public static Vector2 Scale(float scale) => new Vector2(scale).ToResolution();
}
