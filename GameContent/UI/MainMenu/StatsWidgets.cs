using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using FontStashSharp;

namespace TanksRebirth.GameContent.UI.MainMenu;

readonly record struct StatRow(string Label, double Number, string Format, Color LabelColor, Color LabelBorder) {
    public StatRow(string label, double number, string format = "{0:0}") : this(label, number, format, Color.White, Color.Black) { }

    /// <summary>The number as shown when it is <paramref name="amount"/> (0 to 1) of the way to its real value.</summary>
    public string ValueAt(float amount) => string.Format(Format, Number * amount);
}

/// <summary>"Service Record" across the top of the stats window.</summary>
public sealed class StatsTitleBar : UIElement {
    public StatsTitleBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var font = FontGlobals.RebirthFont;

        // localize ts
        var title = "Service Record";
        var titleScale = ModifierColors.Scale(0.95f);
        var titleSize = font.MeasureString(title);
        var pill = new Rectangle(Hitbox.X, Hitbox.Y, (int)(titleSize.X * titleScale.X + 56f.ToResolutionX()), Hitbox.Height);
        ModifierDraw.PillButton(spriteBatch, pill, false, 1f);
        spriteBatch.DrawString(font, title, pill.Center.ToVector2(), ModifierColors.Ink, titleScale, 0f, titleSize / 2f);
    }
}

/// <summary>A grey card with a white title strip, listing <see cref="StatRow"/>s as a label on the left and a value on the right.</summary>
/// <remarks>Rows that don't fit one column are split over two, and shrunk if they still don't fit.</remarks>
sealed class StatsSection : UIElement {
    const float HeaderHeight = 46f;
    const float RowStep = 40f;
    const float Pad = 10f;
    const float ColumnGap = 14f;

    // the numbers count up when the window opens: each row starts a little after the one before it, and every row takes OpenSeconds to finish
    const float OpenSeconds = 1f;
    const float RowDelay = 0.05f;

    readonly string _title;
    readonly IReadOnlyList<StatRow> _rows;
    readonly string _empty;
    readonly float _startDelay;
    readonly float _openedAt = RuntimeData.RunTime;

    /// <param name="startDelay">Seconds before the first row starts counting.</param>
    public StatsSection(string title, IReadOnlyList<StatRow> rows, string empty, float startDelay = 0f) {
        _title = title;
        _rows = rows;
        _empty = empty;
        _startDelay = startDelay;
        IgnoreMouseInteractions = true;
    }

    /// <summary>How far row <paramref name="index"/> has counted up, from 0 to 1.</summary>
    float Progress(int index) {
        var seconds = (RuntimeData.RunTime - _openedAt) / 60f - _startDelay - index * RowDelay;
        return Easings.OutCubic(MathHelper.Clamp(seconds / OpenSeconds, 0f, 1f));
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        var font = FontGlobals.RebirthFont;
        var section = Hitbox;
        var header = section with { Height = (int)HeaderHeight.ToResolutionY() };
        var body = new Rectangle(section.X, header.Bottom, section.Width, section.Bottom - header.Bottom);

        ModifierDraw.Section(spriteBatch, header, section);

        var titleSize = font.MeasureString(_title);
        spriteBatch.DrawString(font, _title, new Vector2(header.X + 18f.ToResolutionX(), header.Center.Y), ModifierColors.Ink, ModifierColors.Scale(0.8f), 0f,
            new Vector2(0f, titleSize.Y / 2f));

        var pad = (int)Pad.ToResolutionY();
        var inner = new Rectangle(body.X + pad, body.Y + pad, body.Width - pad * 2, body.Height - pad * 2);

        if (_rows.Count == 0) {
            DrawUtils.DrawStringWithBorder(spriteBatch, font, _empty, inner.Center.ToVector2(), Color.LightGray, Color.Black, ModifierColors.Scale(0.65f), 0f,
                Anchor.Center, 1f);
            return;
        }

        var fullStep = (int)RowStep.ToResolutionY();
        var columns = _rows.Count * fullStep > inner.Height ? 2 : 1;
        var perColumn = (int)MathF.Ceiling(_rows.Count / (float)columns);
        var step = Math.Min(fullStep, inner.Height / perColumn);
        var gap = (int)ColumnGap.ToResolutionX();
        var columnWidth = (inner.Width - gap * (columns - 1)) / columns;

        // rows squeezed under their normal height get smaller text too
        var shrink = step / (float)fullStep;

        for (var i = 0; i < _rows.Count; i++) {
            var column = i / perColumn;
            var line = i % perColumn;
            var row = new Rectangle(inner.X + column * (columnWidth + gap), inner.Y + line * step, columnWidth, Math.Max(1, step - ModifierDraw.Px(3f)));

            // rows appear one after the other
            var progress = Progress(i);
            if (progress <= 0f)
                continue;

            ModifierDraw.Fill(spriteBatch, row, ModifierDraw.Fade(Color.Black, (line % 2 == 0 ? 0.16f : 0.07f) * progress));
            DrawRow(spriteBatch, row, _rows[i], shrink, progress);
        }
    }

    static void DrawRow(SpriteBatch spriteBatch, Rectangle row, StatRow stat, float shrink, float progress) {
        var font = FontGlobals.RebirthFont;
        var scale = ModifierColors.Scale(0.6f * shrink);
        var inset = 14f.ToResolutionX();

        // shrink long labels (usually translations) instead of running them into the value. measured with the final value so the text doesn't change size while counting
        var labelWidth = font.MeasureString(stat.Label).X * scale.X;
        var valueWidth = font.MeasureString(stat.ValueAt(1f)).X * scale.X;
        var room = row.Width - inset * 2f - 16f.ToResolutionX();
        var fit = MathF.Min(1f, room / MathF.Max(1f, labelWidth + valueWidth));
        scale *= fit;

        var centerY = row.Center.Y;
        DrawUtils.DrawStringWithBorder(spriteBatch, font, stat.Label, new Vector2(row.X + inset, centerY), ModifierDraw.Fade(stat.LabelColor, progress),
            ModifierDraw.Fade(stat.LabelBorder, progress), scale, 0f, Anchor.LeftCenter, 1f);
        DrawUtils.DrawStringWithBorder(spriteBatch, font, stat.ValueAt(progress), new Vector2(row.Right - inset, centerY), ModifierDraw.Fade(ModifierColors.Gold, progress),
            ModifierDraw.Fade(Color.Black, progress), scale, 0f, Anchor.RightCenter, 1f);
    }
}

/// <summary>The dark bar along the bottom of the stats window: how to leave it, and a warning if the save file is old.</summary>
public sealed class StatsInfoBar : UIElement {
    public StatsInfoBar() => IgnoreMouseInteractions = true;

    public override void DrawSelf(SpriteBatch spriteBatch) {
        ModifierDraw.DarkBar(spriteBatch, Hitbox);

        var font = FontGlobals.RebirthFont;
        var half = Hitbox.Height / 2;
        var top = new Vector2(Hitbox.Center.X, Hitbox.Y + half / 2f + 4f.ToResolutionY());
        var bottom = new Vector2(Hitbox.Center.X, Hitbox.Y + half + half / 2f - 4f.ToResolutionY());

        // localize ts
        DrawUtils.DrawStringWithBorder(spriteBatch, font, "Press ESC or click Back to return.", top, Color.White, Color.Black, ModifierColors.Scale(0.55f), 0f,
            Anchor.Center, 1f);

        if (TankGame.SaveFile.ReadingOutdatedFile) {
            DrawUtils.DrawStringWithBorder(spriteBatch, font, $"Warning: Outdated save file ({TankGame.SaveFile.Name})!", bottom, Color.Red, Color.Black,
                ModifierColors.Scale(0.5f), 0f, Anchor.Center, 1f);
        }
        else {
            DrawUtils.DrawStringWithBorder(spriteBatch, font, TankGame.GameLanguage.Menu.GameStatsFlavor, bottom, Color.LightGray, Color.Black,
                ModifierColors.Scale(0.5f), 0f, Anchor.Center, 1f);
        }
    }
}
