using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class IngameUI
    {
        public static Keybind Pause = new("Up", Keys.Escape);

        public static UIPanel MissionInfoBar;

        public static UIImageButton ResumeButton;

        public static UIImageButton RestartButton;

        public static UIImageButton QuitButton;

        internal static void Initialize()
        {
            SpriteFont font = TankGame.Fonts.Default;
            Vector2 drawOrigin = font.MeasureString("Mission 1        x4") / 2f;
            MissionInfoBar = new((uiPanel, spriteBatch) => spriteBatch.DrawString(font, "Mission 1        x4", uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f));
            MissionInfoBar.BackgroundColor = Color.Red;
            MissionInfoBar.SetDimensions(650, 1000, 500, 50);
            ResumeButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Resume", Color.WhiteSmoke));
            ResumeButton.SetDimensions(700, 200, 500, 150);
            ResumeButton.OnMouseClick += ResumeButton_OnMouseClick;
            RestartButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Start Over", Color.WhiteSmoke));
            RestartButton.SetDimensions(700, 450, 500, 150);
            QuitButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Quit", Color.WhiteSmoke));
            QuitButton.SetDimensions(700, 700, 500, 150);
        }

        private static void ResumeButton_OnMouseClick(Internals.UI.UIElement affectedElement) {
            ResumeButton.Visible = false;
            RestartButton.Visible = false;
            QuitButton.Visible = false;
        }

        private static void QuickButton(UIImage imageButton, SpriteBatch spriteBatch, string text, Color color, bool onLeft = false)
        {
            Texture2D texture = TankGame.UITextures.UIPanelBackground;

            int border = 12;

            Rectangle Hitbox = imageButton.Hitbox;

            int middleX = Hitbox.X + border;
            int rightX = Hitbox.Right - border;

            int middleY = Hitbox.Y + border;
            int bottomY = Hitbox.Bottom - border;

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, border, border), new Rectangle(0, 0, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - border * 2, border), new Rectangle(border, 0, texture.Width - border * 2, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, border, border), new Rectangle(texture.Width - border, 0, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, border, Hitbox.Height - border * 2), new Rectangle(0, border, border, texture.Height - border * 2), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - border * 2, Hitbox.Height - border * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, border, Hitbox.Height - border * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), imageButton.MouseHovering ? Color.CornflowerBlue : color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, border, border), new Rectangle(0, texture.Height - border, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - border * 2, border), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, border, border), new Rectangle(texture.Width - border, texture.Height - border, border, border), imageButton.MouseHovering ? Color.CornflowerBlue : color);
            SpriteFont font = TankGame.Fonts.Default;
            Vector2 drawOrigin = font.MeasureString(text) / 2f;
            spriteBatch.DrawString(font, text, imageButton.Hitbox.Center.ToVector2(), Color.Black, 0, drawOrigin, 1f, SpriteEffects.None, 1f);
        }

        public static void UpdateButtons()
        {
            var text = $"Mission 1        x{AITank.CountAll()}";
            Vector2 drawOrigin = TankGame.Fonts.Default.MeasureString(text) / 2f;
            MissionInfoBar.UniqueDraw =
                (uiPanel, spriteBatch) => spriteBatch.DrawString(TankGame.Fonts.Default, text, uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f);
            if (Pause.JustPressed) {
                ResumeButton.Visible = true;
                RestartButton.Visible = true;
                QuitButton.Visible = true;
            }
        }
    }
}