using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UIPanel : UIElement
    {
        public Color BackgroundColor = Color.White;

        public Action<UIPanel, SpriteBatch> UniqueDraw;

        public UIPanel(Action<UIPanel, SpriteBatch> uniqueDraw = null) {
            UniqueDraw = uniqueDraw;
        }

        public override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);
            DrawUtils.DrawNineSliced(spriteBatch, UIPanelBackground, 12, Hitbox, BackgroundColor, Vector2.Zero);
            UniqueDraw?.Invoke(this, spriteBatch);
        }
    }
}
