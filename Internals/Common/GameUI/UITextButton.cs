using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.Internals.Common.GameUI;

public class UITextButton : UIImage
{
    public string Text { get; set; }

    public SpriteFontBase Font { get; set; }

    public new Color Color { get; set; }

    public Color HoverColor { get; set; } = Color.CornflowerBlue;

    public Func<Vector2> TextScale { get; set; }

    public float TextRotation;

    public static bool AutoResolutionHandle = true;
    public bool DrawText = true;
    public UITextButton(string text, SpriteFontBase font, Color color, Func<Vector2> textScale) : base(null, new(1), null)
    {
        Text = text;
        Font = font;
        Color = color;
        TextScale = textScale;
    }
    public UITextButton(string text, SpriteFontBase font, Color color, float textScale = 1f) : base(null, new(1), null)
    {
        Text = text;
        Font = font;
        Color = color;
        TextScale = () => new(textScale);
    }

    public override void DrawSelf(SpriteBatch spriteBatch)
    {
        DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, MouseHovering ? HoverColor : Color, GameUtils.GetAnchor(Anchor, UIPanelBackground.Size()));
        SpriteFontBase font = FontGlobals.RebirthFont;
        Vector2 drawOrigin = font.MeasureString(Text) / 2f;
        if (TextScale != null && DrawText)
            spriteBatch.DrawString(font, Text, Hitbox.Center.ToVector2(), Color.Black, AutoResolutionHandle ? TextScale.Invoke().ToResolution() : TextScale.Invoke(), TextRotation, drawOrigin);
    }
}