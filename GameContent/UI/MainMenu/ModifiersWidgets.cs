using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using FontStashSharp;

namespace TanksRebirth.GameContent.UI.MainMenu;

// i will need to make cornflower blue not hardcoded
static class ModifierColors {
    public static readonly Color On = new(63, 191, 63);
    public static readonly Color OnDark = new(26, 118, 38);
    public static readonly Color OnText = (Color.LimeGreen * 0.75f) with { A = 255 };
    public static readonly Color Hover = Color.CornflowerBlue;
    public static readonly Color HoverDark = new(52, 92, 200);
    public static readonly Color Gold = Color.Gold;
    public static readonly Color Ink = new(22, 18, 14);
    public static readonly Color Cream = new(244, 231, 193);
    public static readonly Color PanelTop = new(142, 142, 142);
    public static readonly Color PanelBottom = new(114, 114, 114);

    public static Vector2 Scale(float scale) => new Vector2(scale).ToResolution();
}

/// <summary>Drawing helpers shared by the modifiers window widgets.</summary>
static class ModifierDraw {
    const string WoodTexture = "Assets/models/scene/skybox/textures/wood_dark";

    static Texture2D? _wood;

    static Texture2D Pixel => TextureGlobals.Pixels[Color.White];
    static Texture2D Wood => _wood ??= GameResources.GetGameResource<Texture2D>(WoodTexture);

    /// <summary>A length in pixels that follows the window size and never drops below one.</summary>
    public static int Px(float size) => Math.Max(1, (int)MathF.Round(size.ToResolutionY()));

    /// <summary>Sets the opacity of a color. Multiplying it would darken it instead, since the UI blends without premultiplied alpha.</summary>
    public static Color Fade(Color color, float alpha) => color with { A = (byte)MathHelper.Clamp(alpha * 255f, 0f, 255f) };

    public static void Fill(SpriteBatch spriteBatch, Rectangle area, Color color) => spriteBatch.Draw(Pixel, area, color);

    public static void Outline(SpriteBatch spriteBatch, Rectangle area, int thickness, Color color) {
        Fill(spriteBatch, new Rectangle(area.X, area.Y, area.Width, thickness), color);
        Fill(spriteBatch, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
        Fill(spriteBatch, new Rectangle(area.X, area.Y + thickness, thickness, area.Height - thickness * 2), color);
        Fill(spriteBatch, new Rectangle(area.Right - thickness, area.Y + thickness, thickness, area.Height - thickness * 2), color);
    }

    /// <summary>Fills <paramref name="area"/> with a vertical fade from <paramref name="top"/> to <paramref name="bottom"/>.</summary>
    public static void Gradient(SpriteBatch spriteBatch, Rectangle area, Color top, Color bottom) {
        var steps = Math.Clamp(area.Height / 4, 1, 32);
        for (var i = 0; i < steps; i++) {
            var y0 = area.Y + area.Height * i / steps;
            var y1 = area.Y + area.Height * (i + 1) / steps;
            Fill(spriteBatch, new Rectangle(area.X, y0, area.Width, y1 - y0), Color.Lerp(top, bottom, (i + 0.5f) / steps));
        }
    }

    public static void Shadow(SpriteBatch spriteBatch, Rectangle area, float offset, float spread, float strength) {
        const int layers = 5;
        var shift = Px(offset);
        for (var i = layers; i >= 1; i--) {
            var grow = Px(spread * i / layers);
            Fill(spriteBatch, new Rectangle(area.X - grow + shift, area.Y - grow + shift, area.Width + grow * 2, area.Height + grow * 2),
                Color.Black * (strength / layers));
        }
    }

    /// <summary>Tiles the wood texture across <paramref name="area"/>, mirroring every other tile so the seams line up.</summary>
    public static void DrawWood(SpriteBatch spriteBatch, Rectangle area, Color tint) {
        var wood = Wood;
        var tile = Math.Max(1, area.Height);
        for (int x = area.X, index = 0; x < area.Right; x += tile, index++) {
            var width = Math.Min(tile, area.Right - x);
            var flip = index % 2 == 1;
            var sourceWidth = Math.Max(1, wood.Width * width / tile);
            var source = new Rectangle(flip ? wood.Width - sourceWidth : 0, 0, sourceWidth, wood.Height);
            spriteBatch.Draw(wood, new Rectangle(x, area.Y, width, area.Height), source, tint, 0f, Vector2.Zero,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }
    }

    /// <summary>The wooden backdrop with a dark edge and a faint highlight along the top and left.</summary>
    public static void Frame(SpriteBatch spriteBatch, Rectangle area) {
        DrawWood(spriteBatch, area, new Color(235, 235, 235));

        var edge = Px(3f);
        Outline(spriteBatch, area, edge, Color.Black * 0.85f);
        Fill(spriteBatch, new Rectangle(area.X + edge, area.Y + edge, area.Width - edge * 2, 1), Fade(Color.White, 0.16f));
        Fill(spriteBatch, new Rectangle(area.X + edge, area.Y + edge, 1, area.Height - edge * 2), Fade(Color.White, 0.1f));
    }

    /// <summary>A grey card with a white title strip (<paramref name="header"/>) along the top of <paramref name="section"/>.</summary>
    public static void Section(SpriteBatch spriteBatch, Rectangle header, Rectangle section) {
        var edge = Px(2f);
        var body = new Rectangle(section.X, header.Bottom, section.Width, section.Bottom - header.Bottom);

        Shadow(spriteBatch, section, 5f, 9f, 0.45f);
        Gradient(spriteBatch, body, ModifierColors.PanelTop, ModifierColors.PanelBottom);
        Gradient(spriteBatch, new Rectangle(body.X, body.Y, body.Width, Px(12f)), Color.Black * 0.3f, Color.Transparent);
        Gradient(spriteBatch, header, Color.White, new Color(218, 218, 218));
        Fill(spriteBatch, new Rectangle(header.X, header.Bottom - edge, header.Width, edge), Color.Black * 0.55f);
        Outline(spriteBatch, section, edge, Color.Black * 0.7f);
    }

    /// <summary>A dark bar for hints and descriptions.</summary>
    public static void DarkBar(SpriteBatch spriteBatch, Rectangle area) {
        var edge = Px(2f);
        Shadow(spriteBatch, area, 4f, 8f, 0.4f);
        Gradient(spriteBatch, area, new Color(76, 76, 76), new Color(46, 46, 46));
        Fill(spriteBatch, new Rectangle(area.X + edge, area.Y + edge, area.Width - edge * 2, 1), Fade(Color.White, 0.14f));
        Outline(spriteBatch, area, edge, Color.Black * 0.7f);
    }

    /// <summary>A small square toggle. Dark when off and green when on, with <paramref name="amount"/> blending between the two.</summary>
    public static void Swatch(SpriteBatch spriteBatch, Rectangle swatch, float amount, float dim) {
        var border = Px(2f);
        Fill(spriteBatch, swatch.GetOffset(border, border), Color.Black * (0.35f * dim));
        Fill(spriteBatch, swatch, Color.Black * dim);

        var inner = new Rectangle(swatch.X + border, swatch.Y + border, swatch.Width - border * 2, swatch.Height - border * 2);
        Fill(spriteBatch, inner, Fade(Color.Lerp(new Color(34, 34, 34), ModifierColors.On, amount), dim));
    }

    public static void Pill(SpriteBatch spriteBatch, Rectangle area, Color color)
        => DrawUtils.DrawNineSliced(spriteBatch, UIElement.UIPanelBackground, 12, area, color, Vector2.Zero);

    /// <summary>A white rounded button with a drop shadow, outlined in the hover color when <paramref name="hovered"/>.</summary>
    public static void PillButton(SpriteBatch spriteBatch, Rectangle area, bool hovered, float alpha) {
        Pill(spriteBatch, area.GetOffset(Px(2f), Px(4f)), Color.Black * (0.4f * alpha));
        if (hovered) {
            var outline = area;
            outline.Inflate(Px(2f), Px(2f));
            Pill(spriteBatch, outline, ModifierColors.Hover);
        }
        Pill(spriteBatch, area, Fade(Color.White, alpha));
    }
}

/// <summary>The wooden backdrop behind everything in the modifiers window.</summary>
public sealed class ModifiersFrame : UIElement {
    public ModifiersFrame() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) => ModifierDraw.Frame(spriteBatch, Hitbox);
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
        var dim = editable ? 1f : 0.55f;

        _switch = MathHelper.Lerp(_switch, active ? 1f : 0f, 0.3f);
        if (MathF.Abs(_switch - (active ? 1f : 0f)) < 0.01f)
            _switch = active ? 1f : 0f;

        if (_switch > 0f)
            ModifierDraw.Fill(spriteBatch, Hitbox, ModifierDraw.Fade(ModifierColors.On, 0.22f * _switch * dim));
        if (hovered)
            ModifierDraw.Fill(spriteBatch, Hitbox, ModifierDraw.Fade(ModifierColors.Hover, 0.5f));

        var side = ModifierDraw.Px(26f);
        var swatch = new Rectangle(Hitbox.X + (int)14f.ToResolutionX(), Hitbox.Center.Y - side / 2, side, side);
        DrawSwatch(spriteBatch, swatch, dim);

        var font = FontGlobals.RebirthFont;
        var scale = ModifierColors.Scale(0.7f);
        var label = Label;
        var labelSize = font.MeasureString(label);
        var labelX = swatch.Right + 16f.ToResolutionX();
        DrawUtils.DrawStringWithBorder(spriteBatch, font, label, new Vector2(labelX, Hitbox.Center.Y), ModifierDraw.Fade(Color.White, (active ? 1f : 0.86f) * dim),
            Color.Black * dim, scale, 0f, Anchor.LeftCenter, 1f);

        if (IsPicker)
            DrawPicker(spriteBatch, labelX + labelSize.X * scale.X, scale, active, hovered, editable);
    }

    void DrawSwatch(SpriteBatch spriteBatch, Rectangle swatch, float dim) => ModifierDraw.Swatch(spriteBatch, swatch, _switch, dim);

    void DrawPicker(SpriteBatch spriteBatch, float labelRight, Vector2 scale, bool active, bool hovered, bool editable) {
        var font = FontGlobals.RebirthFont;
        var value = ValueText;
        var valueSize = font.MeasureString(value);
        var arrowSize = font.MeasureString(">");
        var edge = 16f.ToResolutionX();
        var gap = 12f.ToResolutionX();

        var right = Hitbox.Right - 14f.ToResolutionX();
        var available = right - labelRight - 18f.ToResolutionX();
        var textWidth = (valueSize.X + arrowSize.X * 2f) * scale.X;
        var width = MathF.Min(MathF.Max(textWidth + (edge + gap) * 2f, 150f.ToResolutionX()), MathF.Max(available, 80f.ToResolutionX()));
        var room = MathF.Max(width - (edge + gap) * 2f, 10f);
        var textScale = textWidth > room ? scale * (room / textWidth) : scale;

        var height = Hitbox.Height - ModifierDraw.Px(12f);
        var pill = new Rectangle((int)(right - width), Hitbox.Center.Y - height / 2, (int)width, height);
        ModifierDraw.PillButton(spriteBatch, pill, hovered, editable ? 1f : 0.6f);

        var center = pill.Center.ToVector2();
        var valueColor = !editable ? ModifierDraw.Fade(ModifierColors.Ink, 0.5f) : active ? ModifierColors.OnDark : ModifierColors.Ink;
        spriteBatch.DrawString(font, value, center, valueColor, textScale, 0f, valueSize / 2f);

        var arrowColor = !editable ? ModifierDraw.Fade(Color.Gray, 0.6f) : hovered ? ModifierColors.HoverDark : new Color(96, 96, 96);
        var arrowOffset = pill.Width / 2f - edge - arrowSize.X * textScale.X / 2f;
        spriteBatch.DrawString(font, "<", new Vector2(center.X - arrowOffset, center.Y), arrowColor, textScale, 0f, arrowSize / 2f);
        spriteBatch.DrawString(font, ">", new Vector2(center.X + arrowOffset, center.Y), arrowColor, textScale, 0f, arrowSize / 2f);
    }
}

/// <summary>A section title in the modifiers window, with how many of its modifiers are on.</summary>
public sealed class ModifiersHeader : UIElement {
    readonly string _text;
    readonly IReadOnlyList<ModifierDefinition> _definitions;
    readonly int _rowsPerColumn;

    public ModifiersHeader(string text, IReadOnlyList<ModifierDefinition> definitions, int rowsPerColumn) {
        _text = text;
        _definitions = definitions;
        _rowsPerColumn = rowsPerColumn;
        IgnoreMouseInteractions = true;
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var sectionBottom = Hitbox.Y + (int)(MainMenuUI.ModSlotStep.ToResolutionY() * (_rowsPerColumn + 1)) - ModifierDraw.Px(3f);
        var section = new Rectangle(Hitbox.X, Hitbox.Y, Hitbox.Width, sectionBottom - Hitbox.Y);
        ModifierDraw.Section(spriteBatch, Hitbox, section);

        int on = 0, total = 0;
        foreach (var definition in _definitions) {
            if (!definition.CountsTowardTotal)
                continue;
            total++;
            if (Modifiers.IsOn(definition.Key))
                on++;
        }

        var font = FontGlobals.RebirthFont;
        var centerY = Hitbox.Center.Y;
        var titleSize = font.MeasureString(_text);
        spriteBatch.DrawString(font, _text, new Vector2(Hitbox.X + 16f.ToResolutionX(), centerY), ModifierColors.Ink,
            ModifierColors.Scale(0.85f), 0f, new Vector2(0f, titleSize.Y / 2f));

        var count = $"{on} / {total}";
        var countSize = font.MeasureString(count);
        spriteBatch.DrawString(font, count, new Vector2(Hitbox.Right - 16f.ToResolutionX(), centerY), on > 0 ? ModifierColors.OnDark : new Color(100, 100, 100),
            ModifierColors.Scale(0.6f), 0f, new Vector2(countSize.X, countSize.Y / 2f));
    }
}

/// <summary>"Modifiers" and which ones are on, across the top of the window.</summary>
public sealed class ModifiersTitleBar : UIElement {
    const string HostOnly = "Only the host can change modifiers";

    string _names = string.Empty;
    int _namesRegistry = -1;
    int _namesValues = -1;
    int _namesWidth = -1;

    public ModifiersTitleBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var font = FontGlobals.RebirthFont;
        var centerY = Hitbox.Center.Y;

        var title = "Modifiers";
        var titleScale = ModifierColors.Scale(0.95f);
        var titleSize = font.MeasureString(title);
        var pill = new Rectangle(Hitbox.X, Hitbox.Y, (int)(titleSize.X * titleScale.X + 56f.ToResolutionX()), Hitbox.Height);
        ModifierDraw.PillButton(spriteBatch, pill, false, 1f);
        spriteBatch.DrawString(font, title, pill.Center.ToVector2(), ModifierColors.Ink, titleScale, 0f, titleSize / 2f);

        var statusScale = ModifierColors.Scale(0.7f);
        var right = (float)Hitbox.Right;

        if (!MainMenuUI.CanEditModifiers) {
            var hostWidth = font.MeasureString(HostOnly).X * statusScale.X;
            DrawUtils.DrawStringWithBorder(spriteBatch, font, HostOnly, new Vector2(right - hostWidth, centerY), ModifierColors.Gold,
                Color.Black, statusScale, 0f, Anchor.LeftCenter, 1f);
        }
    }

    string ActiveNames(SpriteFontBase font, float scaleX, float room) {
        var width = (int)room;
        if (_namesRegistry == Modifiers.RegistryVersion && _namesValues == Modifiers.ValuesVersion && _namesWidth == width)
            return _names;

        var names = new List<string>();
        foreach (var definition in Modifiers.Definitions) {
            var value = Modifiers.Get(definition.Key);
            if (!definition.CountsTowardTotal || !definition.IsActiveWith(value))
                continue;
            names.Add(definition.Kind == ModifierKind.Toggle ? definition.Name() : $"{definition.Name()} {definition.FormatValue(value)}");
        }

        var shown = names.Count;
        var text = string.Join(", ", names);
        while (shown > 1 && font.MeasureString(text).X * scaleX > room) {
            shown--;
            text = $"{string.Join(", ", names.GetRange(0, shown))} +{names.Count - shown}";
        }

        _names = text;
        _namesRegistry = Modifiers.RegistryVersion;
        _namesValues = Modifiers.ValuesVersion;
        _namesWidth = width;
        return text;
    }
}

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
        ModifierDraw.PillButton(spriteBatch, Hitbox, hovered, usable ? 1f : 0.45f);

        var font = FontGlobals.RebirthFont;
        var size = font.MeasureString("Reset All");
        spriteBatch.DrawString(font, "Reset All", Hitbox.Center.ToVector2(), usable ? ModifierColors.Ink : Color.Gray,
            ModifierColors.Scale(0.75f), 0f, size / 2f);
    }
}

public sealed class ModifiersDescriptionBar : UIElement {
    // localize ts!!!!
    const string PickerHint = "Left click: next  |  Right click: previous  |  Middle click: reset";
    const string Hint = "Click a modifier to turn it on or off. Hover one to see what it does." +
        "\nIdeas are welcome! Let us know in our DISCORD server!";
    const string PagedHint = "Click a modifier to turn it on or off. Hover one to see what it does. Q / E switch pages." +
        "\nIdeas are welcome! Let us know in our DISCORD server!";

    public ModifiersDescriptionBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        ModifierDraw.DarkBar(spriteBatch, Hitbox);

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
        ModifierDraw.PillButton(spriteBatch, Hitbox, Hitbox.Contains(MouseUtils.MousePosition), 1f);

        var font = FontGlobals.RebirthFont;
        var text = _direction < 0 ? "< Q" : "E >";
        var size = font.MeasureString(text);
        spriteBatch.DrawString(font, text, Hitbox.Center.ToVector2(), ModifierColors.Ink, ModifierColors.Scale(0.7f), 0f, size / 2f);
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
