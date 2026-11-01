using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using FontStashSharp;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI;

// this stuff just needs to be integrated DIRECTLY into the ui system. this is bloat.
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

    public void Step(int dir) => _step(dir);

    // each element is drawn via this
    public override void DrawSelf(SpriteBatch spriteBatch) {
        var enabled = Enabled;
        var highlighted = Highlighted?.Invoke() ?? false;
        var hovered = enabled && Hitbox.Contains(MouseUtils.MousePosition);
        var dim = enabled ? 1f : 0.55f;

        if (hovered)
            UIStyle.Fill(spriteBatch, SettingsText.Plate(Hitbox), UIStyle.Fade(UIStyle.Hover, 0.5f));

        var font = FontGlobals.RebirthFont;
        var scale = SettingsText.Scale(0.72f);
        var pad = 22f.ToResolutionX();
        var edge = 16f.ToResolutionX();
        var gap = 12f.ToResolutionX();
        var labelGap = 24f.ToResolutionX();
        var centerY = Hitbox.Center.Y;

        var labelSize = font.MeasureString(Label);
        var value = _value();
        var valueSize = font.MeasureString(value);
        var arrowSize = font.MeasureString(">");

        // shrink long label and value pairs so they fit narrow rows
        var padding = ShowArrows ? (edge + gap) * 2f : edge * 2f;
        var arrowWidth = ShowArrows ? arrowSize.X * 2f : 0f;
        var textWidth = (labelSize.X + valueSize.X + arrowWidth) * scale.X;
        var room = MathF.Max(Hitbox.Width - pad * 2f - labelGap - padding, 40f);
        if (textWidth > room)
            scale *= room / textWidth;

        DrawUtils.DrawStringWithBorder(spriteBatch, font, Label, new Vector2(Hitbox.X + pad, centerY), UIStyle.Fade(Color.White, dim),
            UIStyle.Fade(Color.Black, dim), scale, 0f, Anchor.LeftCenter, 1f);

        var valueWidth = (valueSize.X + arrowWidth) * scale.X + padding;
        var available = Hitbox.Width - pad * 2f - labelGap - labelSize.X * scale.X;
        var pillWidth = MathF.Max(valueWidth, MathF.Min(130f.ToResolutionX(), available));
        var height = Hitbox.Height - UIStyle.Px(10f);
        var right = Hitbox.Right - pad;
        var pill = new Rectangle((int)(right - pillWidth), Hitbox.Center.Y - height / 2, (int)pillWidth, height);
        UIStyle.PillButton(spriteBatch, pill, hovered, enabled ? 1f : 0.6f, highlighted ? UIStyle.Gold : Color.White);

        var center = pill.Center.ToVector2();
        var valueColor = !enabled ? UIStyle.Fade(UIStyle.Ink, 0.5f) : value == GameUI.OnOff(true) ? UIStyle.OnDark : UIStyle.Ink;
        spriteBatch.DrawString(font, value, center, valueColor, scale, 0f, valueSize / 2f);

        if (!ShowArrows)
            return;

        var arrowColor = !enabled ? UIStyle.Fade(Color.Gray, 0.6f) : hovered ? UIStyle.HoverDark : new Color(96, 96, 96);
        var arrowOffset = pill.Width / 2f - edge - arrowSize.X * scale.X / 2f;
        spriteBatch.DrawString(font, "<", new Vector2(center.X - arrowOffset, center.Y), arrowColor, scale, 0f, arrowSize / 2f);
        spriteBatch.DrawString(font, ">", new Vector2(center.X + arrowOffset, center.Y), arrowColor, scale, 0f, arrowSize / 2f);
    }
}

public sealed class SliderRow : UIElement {
    public string Label;
    public string Description;

    readonly Func<float> _get;
    readonly Action<float> _set;
    readonly float _default;
    bool _dragging;

    /// <summary>The value, 0 - 1.</summary>
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
        if (active)
            UIStyle.Fill(spriteBatch, SettingsText.Plate(Hitbox), UIStyle.Fade(UIStyle.Hover, 0.5f));

        var font = FontGlobals.RebirthFont;
        var scale = SettingsText.Scale(0.72f);
        var pad = 22f.ToResolutionX();
        var centerY = Hitbox.Center.Y;

        DrawUtils.DrawStringWithBorder(spriteBatch, font, Label, new Vector2(Hitbox.X + pad, centerY), Color.White, Color.Black, scale, 0f,
            Anchor.LeftCenter, 1f);

        var value = Value;
        var track = Track;
        var border = UIStyle.Px(2f);

        var groove = track;
        groove.Inflate(border, border);
        UIStyle.Fill(spriteBatch, groove, Color.Black);
        UIStyle.Fill(spriteBatch, track, new Color(34, 34, 34));

        var fill = new Rectangle(track.X, track.Y, (int)(track.Width * value), track.Height);
        UIStyle.Fill(spriteBatch, fill, UIStyle.On);
        UIStyle.Fill(spriteBatch, new Rectangle(fill.X, fill.Y, fill.Width, fill.Height / 2), UIStyle.Fade(Color.White, 0.22f));

        var knobW = Math.Max(6, (int)12f.ToResolutionX());
        var knobH = (int)(Hitbox.Height * 0.62f);
        var knob = new Rectangle(track.X + fill.Width - knobW / 2, centerY - knobH / 2, knobW, knobH);
        UIStyle.Fill(spriteBatch, knob.GetOffset(border, border), UIStyle.Fade(Color.Black, 0.35f));
        UIStyle.Fill(spriteBatch, new Rectangle(knob.X - border, knob.Y - border, knob.Width + border * 2, knob.Height + border * 2), Color.Black);
        UIStyle.Fill(spriteBatch, knob, active ? Color.White : new Color(226, 226, 226));
        UIStyle.Fill(spriteBatch, new Rectangle(knob.X, knob.Y, knob.Width, knob.Height / 2), UIStyle.Fade(Color.White, 0.5f));

        var percent = $"{MathF.Round(value * 100f)}%";
        DrawUtils.DrawStringWithBorder(spriteBatch, font, percent, new Vector2(groove.X - 22f.ToResolutionX(), centerY), active ? UIStyle.Gold : Color.White,
            Color.Black, scale, 0f, Anchor.RightCenter, 1f);
    }
}

/// <summary>A column title over a grey panel that reaches down to the last row in the column.</summary>
public sealed class SettingsHeader : UIElement {
    readonly string _text;
    readonly SettingsPage _page;

    public SettingsHeader(string text, SettingsPage page) {
        _text = text;
        _page = page;
        IgnoreMouseInteractions = true;
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var bottom = Hitbox.Bottom;
        foreach (var element in _page.Elements) {
            var box = element.Hitbox;
            if (ReferenceEquals(element, this) || box.Y < Hitbox.Bottom || box.Center.X < Hitbox.X || box.Center.X > Hitbox.Right)
                continue;
            bottom = Math.Max(bottom, box.Bottom);
        }
        UIStyle.Section(spriteBatch, Hitbox, bottom + UIStyle.Px(bottom > Hitbox.Bottom ? 8f : 12f));

        var font = FontGlobals.RebirthFont;
        var size = font.MeasureString(_text);
        spriteBatch.DrawString(font, _text, new Vector2(Hitbox.X + 16f.ToResolutionX(), Hitbox.Center.Y), UIStyle.Ink, SettingsText.Scale(0.85f), 0f,
            new Vector2(0f, size.Y / 2f));
    }
}

/// <summary>A push button on a settings page. Draws gold when highlighted and faded when disabled.</summary>
public sealed class SettingsButton : UITextButton {
    public SettingsButton(string text, float textScale) : base(text, FontGlobals.RebirthFont, Color.WhiteSmoke, textScale) { }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var enabled = Color != Color.Gray;
        UIStyle.PillButton(spriteBatch, Hitbox, enabled && MouseHovering, enabled ? 1f : 0.45f, Color == Color.Gold ? UIStyle.Gold : Color.White);

        if (TextScale is null || !DrawText)
            return;

        var font = FontGlobals.RebirthFont;
        var scale = AutoResolutionHandle ? TextScale.Invoke().ToResolution() : TextScale.Invoke();
        spriteBatch.DrawString(font, Text, Hitbox.Center.ToVector2(), enabled ? UIStyle.Ink : Color.Gray, scale, TextRotation, font.MeasureString(Text) / 2f);
    }
}

/// <summary>The wooden backdrop of the settings window.</summary>
public sealed class SettingsFrame : UIElement {
    readonly Action<SpriteBatch> _afterDraw;

    public SettingsFrame(Action<SpriteBatch> afterDraw) {
        _afterDraw = afterDraw;
        IgnoreMouseInteractions = true;
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        UIStyle.Window(spriteBatch, Hitbox);
        _afterDraw(spriteBatch);
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

        var font = FontGlobals.RebirthFont;
        var center = Hitbox.Center.ToVector2();
        if (_page is null) {
            // this would be really awkward on keyboards like AZERTY
            UIStyle.PillButton(spriteBatch, Hitbox, hovered);
            var arrow = _dir < 0 ? "< Q" : "E >";
            spriteBatch.DrawString(font, arrow, center, UIStyle.Ink, SettingsText.Scale(0.7f), 0f, font.MeasureString(arrow) / 2f);
            return;
        }

        UIStyle.PillButton(spriteBatch, Hitbox, hovered && !selected, 1f, selected ? Color.White : UIStyle.Charcoal);
        if (selected) {
            var size = font.MeasureString(_page.Name);
            spriteBatch.DrawString(font, _page.Name, center, UIStyle.Ink, SettingsText.Scale(0.9f), 0f, size / 2f);
        }
        else {
            DrawUtils.DrawStringWithBorder(spriteBatch, font, _page.Name, center, hovered ? Color.White : UIStyle.Cream, Color.Black,
                SettingsText.Scale(0.85f), 0f, Anchor.Center, 1f);
        }
    }
}

/// <summary>Shows the description of whatever the mouse is over in the settings window.</summary>
public sealed class SettingsDescriptionBar : UIElement {
    public SettingsDescriptionBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        UIStyle.Bar(spriteBatch, Hitbox);
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

    /// <summary>A row's area pulled in from the sides so it doesn't cover the edge of its panel.</summary>
    public static Rectangle Plate(Rectangle area) {
        var inset = UIStyle.Px(3f);
        return new Rectangle(area.X + inset, area.Y, area.Width - inset * 2, area.Height);
    }
}
