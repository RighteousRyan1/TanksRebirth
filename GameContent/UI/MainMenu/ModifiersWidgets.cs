using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using FontStashSharp;

namespace TanksRebirth.GameContent.UI.MainMenu;

// i will need to make cornflower blue not hardcoded
static class ModifierColors {
    public static readonly Color On = Color.LimeGreen;
    public static readonly Color OnText = (Color.LimeGreen * 0.75f) with { A = 255 };
    public static readonly Color RowOn = Color.LightGreen; // new(220, 240, 210);
    public static readonly Color Value = (Color.LimeGreen * 0.5f) with { A = 255 };
    public static readonly Color Gold = Color.Gold;

    public static Vector2 Scale(float scale) => new Vector2(scale).ToResolution();
}

/// <summary>
/// One modifier in the modifiers window, drawn from its <see cref="ModifierDefinition"/>.
/// </summary>
public sealed class ModifierRow : UIElement {
    public ModifierDefinition Definition { get; }

    public string Label => Definition.Name();
    public string Description => Definition.Description();
    /// <summary>Whether this row adds to the "N active" counts (off for the second half of a pair).</summary>
    public bool CountsAsModifier => Definition.CountsTowardTotal;
    public bool IsActive => Modifiers.IsOn(Definition.Key);
    public bool IsPicker => Definition.Kind != ModifierKind.Toggle;

    string ValueText => Definition.FormatValue(Modifiers.Get(Definition.Key));

    float _switch;

    public ModifierRow(ModifierDefinition definition) {
        Definition = definition;
        _switch = IsActive ? 1f : 0f;
        OnLeftClick = _ => Click(1);
        OnRightClick = _ => Click(-1);
        OnMiddleClick = _ => {
            if (!CanClick())
                return;
            Modifiers.Reset(Definition.Key);
            SettingsUI.PlayTick();
        };
        OnMouseOver = _ => SettingsUI.PlayTick();
    }

    static bool CanClick() {
        if (!MainMenuUI.ModifiersInputReady)
            return false;
        if (!MainMenuUI.CanEditModifiers) {
            SoundPlayer.SoundError();
            return false;
        }
        return true;
    }

    void Click(int dir) {
        if (CanClick())
            Modifiers.Step(Definition.Key, dir);
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var editable = MainMenuUI.CanEditModifiers;
        var active = IsActive;
        var hovered = editable && Hitbox.Contains(MouseUtils.MousePosition);
        var background = !editable ? (active ? ModifierColors.RowOn : Color.WhiteSmoke) * 0.55f
            : hovered ? Color.CornflowerBlue
            : active ? ModifierColors.RowOn
            : Color.WhiteSmoke;
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, background, Vector2.Zero);

        var pixel = TextureGlobals.Pixels[Color.White];
        var stripeInset = (int)10f.ToResolutionY();
        var stripe = new Rectangle(Hitbox.X + (int)10f.ToResolutionX(), Hitbox.Y + stripeInset,
            Math.Max(3, (int)6f.ToResolutionX()), Hitbox.Height - stripeInset * 2);
        spriteBatch.Draw(pixel, stripe, active ? ModifierColors.On : Color.Black * 0.15f);

        var font = FontGlobals.RebirthFont;
        var scale = ModifierColors.Scale(0.7f);
        var textColor = editable ? Color.Black : Color.Black * 0.55f;
        var pad = 22f.ToResolutionX();
        var centerY = Hitbox.Center.Y;
        var labelX = stripe.Right + 14f.ToResolutionX();

        var label = Label;
        var labelSize = font.MeasureString(label);
        spriteBatch.DrawString(font, label, new Vector2(labelX, centerY), textColor, scale, 0f, new Vector2(0f, labelSize.Y / 2f));
        var labelRight = labelX + labelSize.X * scale.X;

        if (IsPicker)
            DrawPicker(spriteBatch, labelRight, pad, scale, active, hovered, editable);
        else
            DrawSwitch(spriteBatch, pad, active, editable);
    }

    void DrawSwitch(SpriteBatch spriteBatch, float pad, bool active, bool editable) {
        _switch = MathHelper.Lerp(_switch, active ? 1f : 0f, 0.3f);
        if (MathF.Abs(_switch - (active ? 1f : 0f)) < 0.01f)
            _switch = active ? 1f : 0f;

        // i rly need to use rendertargets instead of doing resolution hacks
        var width = (int)72f.ToResolutionX();
        var height = (int)30f.ToResolutionY();
        var track = new Rectangle((int)(Hitbox.Right - pad) - width, Hitbox.Center.Y - height / 2, width, height);
        var off = Color.Black * 0.3f;
        var trackColor = Color.Lerp(off, ModifierColors.On, _switch) * (editable ? 1f : 0.6f);
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, track, trackColor, Vector2.Zero);

        var inset = Math.Max(2, (int)3f.ToResolutionY());
        var knobSize = height - inset * 2;
        var knobX = track.X + inset + (int)((track.Width - inset * 2 - knobSize) * _switch);
        var knob = new Rectangle(knobX, track.Y + inset, knobSize, knobSize);
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, knob, Color.White, Vector2.Zero);

        var font = FontGlobals.RebirthFont;
        var text = active ? "ON" : "OFF";
        var textScale = ModifierColors.Scale(0.42f);
        var size = font.MeasureString(text);
        var freeLeft = active ? track.X : knob.Right;
        var freeRight = active ? knob.X : track.Right;
        var textPos = new Vector2((freeLeft + freeRight) / 2f, track.Center.Y);
        spriteBatch.DrawString(font, text, textPos, active ? Color.White : new Color(70, 70, 70), textScale, 0f, size / 2f);
    }

    void DrawPicker(SpriteBatch spriteBatch, float labelRight, float pad, Vector2 scale, bool active, bool hovered, bool editable) {
        var font = FontGlobals.RebirthFont;
        var value = ValueText;
        var valueSize = font.MeasureString(value);
        var arrowSize = font.MeasureString(">");
        var gap = 14f;

        var right = Hitbox.Right - pad;
        var available = right - labelRight - 24f.ToResolutionX();
        var needed = (valueSize.X + (arrowSize.X + gap) * 2f) * scale.X;
        var valueScale = needed > available && needed > 0 ? scale * (available / needed) : scale;

        var valueColor = !editable ? Color.Black * 0.45f : active ? ModifierColors.Value : Color.DimGray;
        var arrowColor = !editable ? Color.Black * 0.3f : hovered ? Color.White : Color.DimGray;
        var centerY = Hitbox.Center.Y;

        spriteBatch.DrawString(font, ">", new Vector2(right, centerY), arrowColor, valueScale, 0f, new Vector2(arrowSize.X, arrowSize.Y / 2f));
        var valueRight = right - (arrowSize.X + gap) * valueScale.X;
        spriteBatch.DrawString(font, value, new Vector2(valueRight, centerY), valueColor, valueScale, 0f, new Vector2(valueSize.X, valueSize.Y / 2f));
        var arrowLeft = valueRight - (valueSize.X + gap) * valueScale.X;
        spriteBatch.DrawString(font, "<", new Vector2(arrowLeft, centerY), arrowColor, valueScale, 0f, new Vector2(arrowSize.X, arrowSize.Y / 2f));
    }
}

/// <summary>A section title in the modifiers window, with how many of its modifiers are on.</summary>
public sealed class ModifiersHeader : UIElement {
    readonly string _text;
    readonly IReadOnlyList<ModifierRow> _rows;

    public ModifiersHeader(string text, IReadOnlyList<ModifierRow> rows) {
        _text = text;
        _rows = rows;
        IgnoreMouseInteractions = true;
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var font = FontGlobals.RebirthFont;
        var centerY = Hitbox.Center.Y;
        DrawUtils.DrawStringWithBorder(spriteBatch, font, _text, new Vector2(Hitbox.X + 6f.ToResolutionX(), centerY),
            Color.White, Color.Black, ModifierColors.Scale(0.85f), 0f, Anchor.LeftCenter, 1f);

        int on = 0, total = 0;
        foreach (var row in _rows) {
            if (!row.CountsAsModifier)
                continue;
            total++;
            if (row.IsActive)
                on++;
        }
        var count = $"{on} / {total}";
        DrawUtils.DrawStringWithBorder(spriteBatch, font, count, new Vector2(Hitbox.Right - 6f.ToResolutionX(), centerY),
            on > 0 ? ModifierColors.OnText : Color.LightGray * 0.8f, Color.Black, ModifierColors.Scale(0.6f), 0f, Anchor.RightCenter, 1f);

        var line = new Rectangle(Hitbox.X, Hitbox.Bottom - (int)3f.ToResolutionY(), Hitbox.Width, Math.Max(1, (int)3f.ToResolutionY()));
        spriteBatch.Draw(TextureGlobals.Pixels[Color.White], line, Color.White * 0.8f);
    }
}

/// <summary>"Modifiers" and how many are on, across the top of the window.</summary>
public sealed class ModifiersTitleBar : UIElement {
    public ModifiersTitleBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var font = FontGlobals.RebirthFont;
        var centerY = Hitbox.Center.Y;
        var x = Hitbox.X + 6f.ToResolutionX();

        var title = "Modifiers";
        var titleScale = ModifierColors.Scale(1.1f);
        DrawUtils.DrawStringWithBorder(spriteBatch, font, title, new Vector2(x, centerY), Color.White, Color.Black,
            titleScale, 0f, Anchor.LeftCenter, 1.5f);
        x += font.MeasureString(title).X * titleScale.X + 30f.ToResolutionX();

        var active = Modifiers.ActiveCount();
        var status = active == 0 ? "None active" : active == 1 ? "1 active" : $"{active} active";
        var statusScale = ModifierColors.Scale(0.7f);
        DrawUtils.DrawStringWithBorder(spriteBatch, font, status, new Vector2(x, centerY), active > 0 ? ModifierColors.OnText : Color.LightGray,
            Color.Black, statusScale, 0f, Anchor.LeftCenter, 1f);

        if (!MainMenuUI.CanEditModifiers) {
            x += font.MeasureString(status).X * statusScale.X + 30f.ToResolutionX();
            DrawUtils.DrawStringWithBorder(spriteBatch, font, "Only the host can change modifiers", new Vector2(x, centerY), ModifierColors.Gold,
                Color.Black, statusScale, 0f, Anchor.LeftCenter, 1f);
        }
    }
}

/// <summary>Turns every modifier off.</summary>
public sealed class ModifiersResetButton : UIElement {
    public const string Description = "Turns every modifier off.";

    public ModifiersResetButton() {
        OnLeftClick = _ => {
            if (!MainMenuUI.ModifiersInputReady)
                return;
            if (!Usable) {
                SoundPlayer.SoundError();
                return;
            }
            MainMenuUI.ResetAllModifiers();
            SettingsUI.PlayTick();
        };
        OnMouseOver = _ => {
            if (Usable)
                SettingsUI.PlayTick();
        };
    }

    static bool Usable => MainMenuUI.CanEditModifiers && Modifiers.ActiveCount() > 0;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var usable = Usable;
        var hovered = usable && Hitbox.Contains(MouseUtils.MousePosition);
        var background = hovered ? Color.CornflowerBlue : Color.Black * (usable ? 0.45f : 0.25f);
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, background, Vector2.Zero);
        DrawUtils.DrawStringWithBorder(spriteBatch, FontGlobals.RebirthFont, "Reset All", Hitbox.Center.ToVector2(),
            usable ? Color.White : Color.Gray, Color.Black, ModifierColors.Scale(0.75f), 0f, Anchor.Center, 1f);
    }
}

/// <summary>What the modifier under the mouse does (or how to use the window, when the mouse isn't over one).</summary>
public sealed class ModifiersDescriptionBar : UIElement {
    const string PickerHint = "Left click: next  |  Right click: previous  |  Middle click: reset";
    const string Hint = "Click a modifier to turn it on or off. Hover one to see what it does." +
        "\nIdeas are welcome! Let us know in our DISCORD server!";
    const string PagedHint = "Click a modifier to turn it on or off. Hover one to see what it does. Q / E switch pages." +
        "\nIdeas are welcome! Let us know in our DISCORD server!";

    public ModifiersDescriptionBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, Color.Black * 0.45f, Vector2.Zero);

        var mouse = MouseUtils.MousePosition;
        ModifierRow? hovered = null;
        foreach (var row in MainMenuUI.AllModifierRows) {
            if (row.IsVisible && row.Hitbox.Contains(mouse)) {
                hovered = row;
                break;
            }
        }

        var padX = 24f.ToResolutionX();
        var padY = 12f.ToResolutionY();
        var inner = new Rectangle(Hitbox.X + (int)padX, Hitbox.Y + (int)padY, Hitbox.Width - (int)(padX * 2), Hitbox.Height - (int)(padY * 2));

        if (hovered is null) {
            var resetHovered = false;
            foreach (var element in MainMenuUI.AllDifficultyButtons)
                if (element is ModifiersResetButton && element.Hitbox.Contains(mouse))
                    resetHovered = true;
            var idle = MainMenuUI.ModifiersPageCount > 1 ? PagedHint : Hint;
            DrawFitted(spriteBatch, resetHovered ? ModifiersResetButton.Description : idle, inner, Color.White, 0.6f, center: true);
            return;
        }

        var font = FontGlobals.RebirthFont;
        var titleScale = ModifierColors.Scale(0.68f);
        var label = hovered.Label;
        var titleY = inner.Y + font.MeasureString(label).Y * titleScale.Y / 2f;
        DrawUtils.DrawStringWithBorder(spriteBatch, font, label, new Vector2(inner.X, titleY), ModifierColors.Gold, Color.Black,
            titleScale, 0f, Anchor.LeftCenter, 1f);

        var state = hovered.IsActive ? "ON" : "OFF";
        var stateX = inner.X + font.MeasureString(label).X * titleScale.X + 18f.ToResolutionX();
        DrawUtils.DrawStringWithBorder(spriteBatch, font, state, new Vector2(stateX, titleY), hovered.IsActive ? ModifierColors.OnText : Color.LightGray,
            Color.Black, ModifierColors.Scale(0.55f), 0f, Anchor.LeftCenter, 1f);

        if (hovered.IsPicker) {
            DrawUtils.DrawStringWithBorder(spriteBatch, font, PickerHint, new Vector2(inner.Right, titleY), Color.LightGray,
                Color.Black, ModifierColors.Scale(0.5f), 0f, Anchor.RightCenter, 1f);
        }

        var descTop = (int)(inner.Y + 34f.ToResolutionY());
        var descArea = new Rectangle(inner.X, descTop, inner.Width, inner.Bottom - descTop);
        DrawFitted(spriteBatch, hovered.Description, descArea, Color.White, 0.55f, center: false);
    }

    /// <summary>Draws <paramref name="text"/> in <paramref name="area"/>, shrinking it if it doesn't fit.</summary>
    static void DrawFitted(SpriteBatch spriteBatch, string text, Rectangle area, Color color, float baseScale, bool center) {
        var font = FontGlobals.RebirthFont;
        var scale = ModifierColors.Scale(baseScale);
        var size = font.MeasureString(text);
        var fit = MathF.Min(1f, MathF.Min(area.Width / MathF.Max(1f, size.X * scale.X), area.Height / MathF.Max(1f, size.Y * scale.Y)));
        scale *= fit;

        if (center)
            spriteBatch.DrawString(font, text, area.Center.ToVector2(), color, scale, 0f, size / 2f);
        else
            spriteBatch.DrawString(font, text, new Vector2(area.X, area.Y), color, scale, 0f, Vector2.Zero);
    }
}

/// <summary>One of the arrows that switch pages of the modifiers window (only shown when there's more than one page).</summary>
public sealed class ModifiersPageButton : UIElement {
    readonly int _direction;

    public ModifiersPageButton(int direction) {
        _direction = direction;
        OnLeftClick = _ => {
            if (!MainMenuUI.ModifiersInputReady)
                return;
            MainMenuUI.CycleModifiersPage(_direction);
            SettingsUI.PlayTick();
        };
        OnMouseOver = _ => SettingsUI.PlayTick();
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var hovered = Hitbox.Contains(MouseUtils.MousePosition);
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, hovered ? Color.CornflowerBlue : Color.Black * 0.45f, Vector2.Zero);
        DrawUtils.DrawStringWithBorder(spriteBatch, FontGlobals.RebirthFont, _direction < 0 ? "< Q" : "E >", Hitbox.Center.ToVector2(),
            Color.White, Color.Black, ModifierColors.Scale(0.7f), 0f, Anchor.Center, 1f);
    }
}

public sealed class ModifiersPageLabel : UIElement {
    public ModifiersPageLabel() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var text = $"{MainMenuUI.ModifiersPage + 1} / {MainMenuUI.ModifiersPageCount}";
        DrawUtils.DrawStringWithBorder(spriteBatch, FontGlobals.RebirthFont, text, Hitbox.Center.ToVector2(),
            Color.White, Color.Black, ModifierColors.Scale(0.75f), 0f, Anchor.Center, 1f);
    }
}
