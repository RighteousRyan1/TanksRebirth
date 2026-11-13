using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI.MainMenu;

sealed class ModMenuEntry(string name, string displayName, string description, string version, Texture2D? icon, Color accent, bool loadable) {
    /// <summary>The folder name, which is what <see cref="ModLoader.IsModEnabled"/> goes by.</summary>
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public string Description { get; } = description;
    public string Version { get; } = version;
    public Texture2D? Icon { get; } = icon;
    // /// <summary>The average color of the icon, used for the stripe along the card.</summary>
    // public Color Accent { get; } = accent;
    /// <summary>False when the folder has no <c>.csproj</c>, which means the mod loader skips it.</summary>
    public bool Loadable { get; } = loadable;

    public bool Enabled => Loadable && ModLoader.IsModEnabled(Name);

    string _wrapped = string.Empty;
    Vector2 _wrappedScale;
    int _wrappedWidth = -1;
    int _wrappedHeight = -1;

    /// <summary>The description wrapped to fit the box. The result is kept until the box changes size.</summary>
    public string Wrapped(SpriteFontBase font, int width, int height, float baseScale, out Vector2 scale) {
        if (width != _wrappedWidth || height != _wrappedHeight) {
            _wrappedScale = ModifierColors.Scale(baseScale);
            _wrapped = StringUtils.WrapToFit(font, Description, width, height, ref _wrappedScale);
            _wrappedWidth = width;
            _wrappedHeight = height;
        }
        scale = _wrappedScale;
        return _wrapped;
    }
}

/// <summary>"Mods" and how many of them are on, across the top of the mods window.</summary>
public sealed class ModsTitleBar : UIElement {
    public ModsTitleBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var font = FontGlobals.RebirthFont;
        var centerY = Hitbox.Center.Y;

        var title = TankGame.GameLanguage.Misc.Mods;
        var titleScale = ModifierColors.Scale(0.95f);
        var titleSize = font.MeasureString(title);
        var pill = new Rectangle(Hitbox.X, Hitbox.Y, (int)(titleSize.X * titleScale.X + 56f.ToResolutionX()), Hitbox.Height);
        ModifierDraw.PillButton(spriteBatch, pill, false, 1f);
        spriteBatch.DrawString(font, title, pill.Center.ToVector2(), ModifierColors.Ink, titleScale, 0f, titleSize / 2f);

        int on = 0, total = 0;
        foreach (var entry in MainMenuUI.ModEntries) {
            if (!entry.Loadable)
                continue;
            total++;
            if (entry.Enabled)
                on++;
        }

        DrawUtils.DrawStringWithBorder(spriteBatch, font, $"{on} / {total}", new Vector2(pill.Right + 20f.ToResolutionX(), centerY),
            on > 0 ? ModifierColors.OnText : Color.LightGray, Color.Black, ModifierColors.Scale(0.75f), 0f, Anchor.LeftCenter, 1f);
    }
}

/// <summary>A white pill button with a label, drawn like the buttons in the modifiers window.</summary>
public sealed class ModsPillButton : UIElement {
    readonly Func<string> _label;

    public ModsPillButton(Func<string> label, Action onClick) {
        _label = label;
        OnLeftClick = _ => {
            SettingsUI.PlayTick();
            onClick();
        };
        OnMouseOver = _ => SettingsUI.PlayTick();
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        ModifierDraw.PillButton(spriteBatch, Hitbox, MouseHovering, 1f);

        var font = FontGlobals.RebirthFont;
        var text = _label();
        var size = font.MeasureString(text);
        var scale = ModifierColors.Scale(0.75f);

        // shrink long translations instead of running them over the edge
        var room = Hitbox.Width - 28f.ToResolutionX();
        scale *= MathF.Min(1f, room / MathF.Max(1f, size.X * scale.X));

        spriteBatch.DrawString(font, text, Hitbox.Center.ToVector2(), ModifierColors.Ink, scale, 0f, size / 2f);
    }
}

/// <summary>One mod in the list: a button of its own, placed and clipped by its <see cref="ModsList"/>.</summary>
sealed class ModEntryCard : UIElement {
    const float HeaderHeight = 46f;

    static readonly RasterizerState _clipped = new() { CullMode = CullMode.None, ScissorTestEnable = true };

    public int Index { get; }
    public ModMenuEntry Entry { get; }

    // where the whole card is, and the part of the screen it may be seen in (the hitbox is only the part inside it)
    Rectangle _bounds;
    Rectangle _view;

    public ModEntryCard(int index, ModMenuEntry entry) {
        Index = index;
        Entry = entry;

        OnLeftClick = _ => {
            if (!Entry.Loadable) {
                SoundPlayer.SoundError();
                return;
            }

            ModLoader.ToggleMod(Entry.Name);
            SettingsUI.PlayTick();
        };
        OnMouseOver = _ => SettingsUI.PlayTick();

        IsVisible = false;
    }

    /// <summary>Moves the card to <paramref name="bounds"/>. Only the part inside <paramref name="view"/> is drawn or can be clicked.</summary>
    public void Place(Rectangle bounds, Rectangle view, bool show) {
        _bounds = bounds;
        _view = view;
        Position = bounds.Location.ToVector2();
        Size = bounds.Size.ToVector2();

        // a card scrolled under the title row or the info bar must not take clicks meant for them
        var visible = Rectangle.Intersect(bounds, view);
        Hitbox = visible;
        IsVisible = show && !visible.IsEmpty;
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        // cards are clipped to the list, so one scrolling out of view doesn't draw over the title or the info bar
        var device = TankGame.Instance.GraphicsDevice;
        spriteBatch.End();
        device.ScissorRectangle = Rectangle.Intersect(_view, device.Viewport.Bounds);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: _clipped);

        DrawCard(spriteBatch, _bounds, MouseHovering);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: RenderGlobals.DefaultRasterizer);
    }

    void DrawCard(SpriteBatch spriteBatch, Rectangle section, bool hovered) {
        var entry = Entry;
        var font = FontGlobals.RebirthFont;
        var enabled = entry.Enabled;
        var dim = entry.Loadable ? 1f : 0.55f;
        var edge = ModifierDraw.Px(2f);

        var header = section with { Height = (int)HeaderHeight.ToResolutionY() };
        var body = new Rectangle(section.X, header.Bottom, section.Width, section.Bottom - header.Bottom);

        ModifierDraw.Section(spriteBatch, header, section);
        if (enabled)
            ModifierDraw.Fill(spriteBatch, body, ModifierDraw.Fade(ModifierColors.On, 0.22f));

        // the accent (but im not using it)
        //ModifierDraw.Fill(spriteBatch, new Rectangle(section.X + edge, section.Y + edge, ModifierDraw.Px(4f), section.Height - edge * 2),
        //    ModifierDraw.Fade(entry.Accent, dim));

        if (hovered && entry.Loadable) {
            ModifierDraw.Fill(spriteBatch, section, ModifierDraw.Fade(ModifierColors.Hover, 0.16f));
            ModifierDraw.Outline(spriteBatch, section, ModifierDraw.Px(3f), ModifierDraw.Fade(ModifierColors.Hover, 0.95f));
        }

        // header: on/off swatch, name, version and status
        var side = ModifierDraw.Px(22f);
        var swatch = new Rectangle(header.X + (int)18f.ToResolutionX(), header.Center.Y - side / 2, side, side);
        ModifierDraw.Swatch(spriteBatch, swatch, enabled ? 1f : 0f, dim);

        var textX = swatch.Right + 14f.ToResolutionX();
        var right = header.Right - 16f.ToResolutionX();

        var status = !entry.Loadable ? "No project" : TankGame.GameLanguage.GetEnablement(enabled);
        var statusColor = !entry.Loadable ? new Color(110, 110, 110) : enabled ? ModifierColors.OnDark : Color.Firebrick;
        var statusScale = ModifierColors.Scale(0.55f);
        var statusSize = font.MeasureString(status);
        spriteBatch.DrawString(font, status, new Vector2(right, header.Center.Y), statusColor, statusScale, 0f, new Vector2(statusSize.X, statusSize.Y / 2f));

        var nameScale = ModifierColors.Scale(0.8f);
        var nameSize = font.MeasureString(entry.DisplayName);
        var version = entry.Version.Length == 0 ? string.Empty : char.IsDigit(entry.Version[0]) ? "v" + entry.Version : entry.Version;
        var versionScale = ModifierColors.Scale(0.5f);
        var versionSize = font.MeasureString(version);
        var gap = version.Length == 0 ? 0f : 12f.ToResolutionX();

        // shrink a long name and version together instead of running them into the status
        var room = right - statusSize.X * statusScale.X - textX - 24f.ToResolutionX();
        var wanted = nameSize.X * nameScale.X + gap + versionSize.X * versionScale.X;
        var fit = MathF.Min(1f, room / MathF.Max(1f, wanted));
        nameScale *= fit;
        versionScale *= fit;

        spriteBatch.DrawString(font, entry.DisplayName, new Vector2(textX, header.Center.Y), ModifierDraw.Fade(ModifierColors.Ink, dim), nameScale, 0f,
            new Vector2(0f, nameSize.Y / 2f));
        if (version.Length > 0) {
            spriteBatch.DrawString(font, version, new Vector2(textX + nameSize.X * nameScale.X + gap * fit, header.Center.Y), new Color(96, 96, 96), versionScale, 0f,
                new Vector2(0f, versionSize.Y / 2f));
        }

        // body: icon and description
        var pad = (int)10f.ToResolutionY();
        var iconSide = body.Height - pad * 2;
        var icon = new Rectangle(body.X + (int)18f.ToResolutionX(), body.Y + pad, iconSide, iconSide);
        DrawIcon(spriteBatch, entry, icon, dim);

        var textLeft = icon.Right + (int)16f.ToResolutionX();
        var textArea = new Rectangle(textLeft, body.Y + pad, section.Right - textLeft - (int)16f.ToResolutionX(), iconSide);
        var description = entry.Wrapped(font, textArea.Width, textArea.Height, 0.55f, out var descriptionScale);
        DrawUtils.DrawStringWithBorder(spriteBatch, font, description, textArea.Location.ToVector2(), ModifierDraw.Fade(Color.White, dim), Color.Black * dim,
            descriptionScale, 0f, Anchor.TopLeft, 1f);
    }

    static void DrawIcon(SpriteBatch spriteBatch, ModMenuEntry entry, Rectangle icon, float dim) {
        var border = ModifierDraw.Px(2f);
        var outer = icon;
        outer.Inflate(border, border);

        ModifierDraw.Fill(spriteBatch, outer.GetOffset(border, border), Color.Black * (0.35f * dim));
        ModifierDraw.Fill(spriteBatch, outer, Color.Black * dim);

        if (entry.Icon is { } texture) {
            spriteBatch.Draw(texture, icon, ModifierDraw.Fade(Color.White, dim));
            return;
        }

        // no mod_icon.png, so a tile with the first letter
        ModifierDraw.Gradient(spriteBatch, icon, ModifierDraw.Fade(new Color(52, 52, 62), dim), ModifierDraw.Fade(new Color(28, 28, 34), dim));
        var font = FontGlobals.RebirthFont;
        var letter = entry.DisplayName[..1].ToUpperInvariant();
        var size = font.MeasureString(letter);
        spriteBatch.DrawString(font, letter, icon.Center.ToVector2(), ModifierDraw.Fade(ModifierColors.Gold, dim), ModifierColors.Scale(1.3f), 0f, size / 2f);
    }
}

/// <summary>Scrolls and lays out the <see cref="ModEntryCard"/>s, and draws the scroll bar and edge fades over them.</summary>
/// <remarks>Create it after its cards so it is drawn on top of them.</remarks>
public sealed class ModsList : UIElement {
    internal const float CardHeight = 130f;
    internal const float CardStep = 146f;
    internal const float EdgeMargin = 14f;
    internal const float ScrollbarRoom = 42f;

    readonly IReadOnlyList<ModEntryCard> _cards;

    float _scroll;
    float _targetScroll;

    internal ModsList(IReadOnlyList<ModEntryCard> cards) {
        _cards = cards;
        IgnoreMouseInteractions = true;
    }

    static int PadPx => ModifierDraw.Px(10f);
    static int StepPx => (int)CardStep.ToResolutionY();
    static int CardPx => (int)CardHeight.ToResolutionY();

    float MaxScroll {
        get {
            var count = _cards.Count;
            if (count == 0)
                return 0f;
            var content = (count - 1) * StepPx + CardPx + PadPx * 2;
            return MathF.Max(0f, content - Hitbox.Height);
        }
    }

    public void ScrollBy(float pixels) => _targetScroll = MathHelper.Clamp(_targetScroll + pixels, 0f, MaxScroll);

    /// <summary>Eases the list towards where it is meant to be scrolled to, then puts the cards where that leaves them.</summary>
    /// <param name="hidden">True while the menu is covered (mods loading), so no card takes input.</param>
    public void Tick(bool hidden) {
        _targetScroll = MathHelper.Clamp(_targetScroll, 0f, MaxScroll);
        _scroll = MathUtils.SoftStep(_scroll, _targetScroll, 0.2f);

        var view = Hitbox;
        var hovered = -1;
        for (var i = 0; i < _cards.Count; i++) {
            var card = _cards[i];
            card.Place(CardAt(i), view, !hidden);
            if (!hidden && card.IsVisible && card.MouseHovering)
                hovered = i;
        }
        MainMenuUI.ModsHovered = hovered;
    }

    Rectangle CardAt(int index) {
        var view = Hitbox;
        var y = view.Y + PadPx + index * StepPx - (int)_scroll;
        return new Rectangle(view.X + (int)EdgeMargin.ToResolutionX(), y, view.Width - (int)ScrollbarRoom.ToResolutionX(), CardPx);
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var view = Hitbox;

        if (_cards.Count == 0)
            DrawEmpty(spriteBatch, view);

        DrawScrollHints(spriteBatch, view);
    }

    static void DrawEmpty(SpriteBatch spriteBatch, Rectangle view) {
        var font = FontGlobals.RebirthFont;
        var text = "No mods found";
        var scale = ModifierColors.Scale(0.8f);
        DrawUtils.DrawStringWithBorder(spriteBatch, font, text, view.Center.ToVector2(), Color.White, Color.Black, scale, 0f, Anchor.Center, 1f);
    }

    /// <summary>A fade at whichever edges have more cards past them, and the scroll bar.</summary>
    void DrawScrollHints(SpriteBatch spriteBatch, Rectangle view) {
        var max = MaxScroll;
        if (max <= 0f)
            return;

        var fade = ModifierDraw.Px(18f);
        if (_scroll > 1f)
            ModifierDraw.Gradient(spriteBatch, new Rectangle(view.X, view.Y, view.Width, fade), Color.Black * 0.45f, Color.Transparent);
        if (_scroll < max - 1f)
            ModifierDraw.Gradient(spriteBatch, new Rectangle(view.X, view.Bottom - fade, view.Width, fade), Color.Transparent, Color.Black * 0.45f);

        var barWidth = ModifierDraw.Px(8f);
        var track = new Rectangle(view.Right - barWidth - ModifierDraw.Px(8f), view.Y + PadPx, barWidth, view.Height - PadPx * 2);
        var thumbHeight = (int)MathF.Max(ModifierDraw.Px(36f), track.Height * (view.Height / (view.Height + max)));
        var thumbY = track.Y + (int)((track.Height - thumbHeight) * (_scroll / max));
        var thumb = new Rectangle(track.X, thumbY, track.Width, thumbHeight);

        ModifierDraw.Fill(spriteBatch, track, Color.Black * 0.35f);
        ModifierDraw.Gradient(spriteBatch, thumb, Color.White, new Color(200, 200, 200));
        ModifierDraw.Outline(spriteBatch, thumb, 1, Color.Black * 0.7f);
    }
}

/// <summary>The dark bar along the bottom of the mods window: what the hovered mod is, or how the window works.</summary>
public sealed class ModsInfoBar : UIElement {
    // localize ts!!!!
    const string Hint = "Click a mod to turn it on or off, then reload mods to apply the change. Disabled mods are remembered between sessions.";

    public ModsInfoBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        ModifierDraw.DarkBar(spriteBatch, Hitbox);

        var padX = (int)24f.ToResolutionX();
        var padY = (int)12f.ToResolutionY();
        var inner = new Rectangle(Hitbox.X + padX, Hitbox.Y + padY, Hitbox.Width - padX * 2, Hitbox.Height - padY * 2);

        var entries = MainMenuUI.ModEntries;
        var hovered = MainMenuUI.ModsHovered;

        if (hovered < 0 || hovered >= entries.Count) {
            var half = inner.Height / 2;
            DrawWrapped(spriteBatch, Hint, new Rectangle(inner.X, inner.Y, inner.Width, half), Color.White, 0.55f, true);
            DrawWrapped(spriteBatch, $"Mods are loaded from: {ModLoader.ModsPath}", new Rectangle(inner.X, inner.Y + half, inner.Width, inner.Height - half),
                Color.LightGray, 0.5f, true);
            return;
        }

        var entry = entries[hovered];
        var font = FontGlobals.RebirthFont;
        var titleScale = ModifierColors.Scale(0.68f);
        var titleSize = font.MeasureString(entry.DisplayName);
        var titleY = inner.Y + titleSize.Y * titleScale.Y / 2f;
        DrawUtils.DrawStringWithBorder(spriteBatch, font, entry.DisplayName, new Vector2(inner.X, titleY), ModifierColors.Gold, Color.Black, titleScale, 0f,
            Anchor.LeftCenter, 1f);

        var state = !entry.Loadable ? "NOT A MOD" : entry.Enabled ? "ON" : "OFF";
        var stateColor = !entry.Loadable ? Color.Gray : entry.Enabled ? ModifierColors.OnText : Color.LightGray;
        var stateX = inner.X + titleSize.X * titleScale.X + 18f.ToResolutionX();
        DrawUtils.DrawStringWithBorder(spriteBatch, font, state, new Vector2(stateX, titleY), stateColor, Color.Black, ModifierColors.Scale(0.55f), 0f,
            Anchor.LeftCenter, 1f);

        DrawUtils.DrawStringWithBorder(spriteBatch, font, $"Folder: {entry.Name}", new Vector2(inner.Right, titleY), Color.LightGray, Color.Black,
            ModifierColors.Scale(0.5f), 0f, Anchor.RightCenter, 1f);

        var top = (int)(inner.Y + 34f.ToResolutionY());
        var description = entry.Description.Length == 0 ? "No description provided." : entry.Description;
        DrawWrapped(spriteBatch, description, new Rectangle(inner.X, top, inner.Width, inner.Bottom - top), Color.White, 0.55f, false);
    }

    /// <summary>Draws <paramref name="text"/> wrapped inside <paramref name="area"/>, shrinking it if it doesn't fit.</summary>
    static void DrawWrapped(SpriteBatch spriteBatch, string text, Rectangle area, Color color, float baseScale, bool center) {
        var font = FontGlobals.RebirthFont;
        var scale = ModifierColors.Scale(baseScale);
        var wrapped = StringUtils.WrapToFit(font, text, area.Width, area.Height, ref scale);
        var size = font.MeasureString(wrapped);

        if (center)
            spriteBatch.DrawString(font, wrapped, area.Center.ToVector2(), color, scale, 0f, size / 2f);
        else
            spriteBatch.DrawString(font, wrapped, new Vector2(area.X, area.Y), color, scale, 0f, Vector2.Zero);
    }
}
