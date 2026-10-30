using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI;

/// <summary>The colors and drawing helpers behind the wooden window and grey panel look.</summary>
static class UIStyle {
    const string WoodTexture = "Assets/models/scene/skybox/textures/wood_dark";

    // i will need to make cornflower blue not hardcoded
    public static readonly Color Hover = Color.CornflowerBlue;
    public static readonly Color HoverDark = new(52, 92, 200);
    public static readonly Color On = new(63, 191, 63);
    public static readonly Color OnDark = new(26, 118, 38);
    public static readonly Color Gold = Color.Gold;
    public static readonly Color Ink = new(22, 18, 14);
    public static readonly Color Cream = new(244, 231, 193);
    public static readonly Color Charcoal = new(44, 44, 44);
    public static readonly Color PanelTop = new(142, 142, 142);
    public static readonly Color PanelBottom = new(114, 114, 114);

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

    /// <summary>A wooden window with a shadow, a darkened edge and a thin bevel.</summary>
    public static void Window(SpriteBatch spriteBatch, Rectangle area) {
        Shadow(spriteBatch, area, 10f, 18f, 0.5f);
        DrawWood(spriteBatch, area, new Color(235, 235, 235));

        const int rings = 6;
        var ringSize = Px(5f);
        for (var i = 0; i < rings; i++) {
            var inset = i * ringSize;
            var ring = new Rectangle(area.X + inset, area.Y + inset, area.Width - inset * 2, area.Height - inset * 2);
            Outline(spriteBatch, ring, ringSize, Color.Black * (0.32f * (1f - i / (float)rings)));
        }

        var edge = Px(3f);
        Outline(spriteBatch, area, edge, Color.Black * 0.85f);
        Fill(spriteBatch, new Rectangle(area.X + edge, area.Y + edge, area.Width - edge * 2, 1), Fade(Color.White, 0.16f));
        Fill(spriteBatch, new Rectangle(area.X + edge, area.Y + edge, 1, area.Height - edge * 2), Fade(Color.White, 0.1f));
    }

    /// <summary>A grey panel under a white header strip. <paramref name="bottom"/> is the lowest edge of the panel.</summary>
    public static void Section(SpriteBatch spriteBatch, Rectangle header, int bottom) {
        var edge = Px(2f);
        var section = new Rectangle(header.X, header.Y, header.Width, Math.Max(header.Height, bottom - header.Y));
        var body = new Rectangle(section.X, header.Bottom, section.Width, section.Bottom - header.Bottom);

        Shadow(spriteBatch, section, 5f, 9f, 0.45f);
        Gradient(spriteBatch, body, PanelTop, PanelBottom);
        Gradient(spriteBatch, new Rectangle(body.X, body.Y, body.Width, Px(12f)), Color.Black * 0.3f, Color.Transparent);
        Gradient(spriteBatch, header, Color.White, new Color(218, 218, 218));
        Fill(spriteBatch, new Rectangle(header.X, header.Bottom - edge, header.Width, edge), Color.Black * 0.55f);
        Outline(spriteBatch, section, edge, Color.Black * 0.7f);
    }

    /// <summary>A dark panel for descriptions and hints.</summary>
    public static void Bar(SpriteBatch spriteBatch, Rectangle area) {
        var edge = Px(2f);
        Shadow(spriteBatch, area, 4f, 8f, 0.4f);
        Gradient(spriteBatch, area, new Color(76, 76, 76), new Color(46, 46, 46));
        Fill(spriteBatch, new Rectangle(area.X + edge, area.Y + edge, area.Width - edge * 2, 1), Fade(Color.White, 0.14f));
        Outline(spriteBatch, area, edge, Color.Black * 0.7f);
    }

    public static void Pill(SpriteBatch spriteBatch, Rectangle area, Color color)
        => DrawUtils.DrawNineSliced(spriteBatch, UIElement.UIPanelBackground, 12, area, color, Vector2.Zero);

    /// <summary>A rounded button with a drop shadow, outlined in the hover color when <paramref name="hovered"/>. White unless given a <paramref name="fill"/>.</summary>
    public static void PillButton(SpriteBatch spriteBatch, Rectangle area, bool hovered, float alpha = 1f, Color? fill = null) {
        Pill(spriteBatch, area.GetOffset(Px(2f), Px(4f)), Color.Black * (0.4f * alpha));
        if (hovered) {
            var outline = area;
            outline.Inflate(Px(2f), Px(2f));
            Pill(spriteBatch, outline, Hover);
        }
        Pill(spriteBatch, area, Fade(fill ?? Color.White, alpha));
    }
}
