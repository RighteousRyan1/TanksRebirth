using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using WiiPlayTanksRemake.Internals.Core;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.GameContent.Systems;
using WiiPlayTanksRemake.Internals.Common;

namespace WiiPlayTanksRemake.GameContent.UI
{
    public static class IngameUI
    {
        public static bool InOptions { get; set; }

        public static Keybind Pause = new("Up", Keys.Escape);

        public static UIPanel MissionInfoBar;

        public static UIImageButton ResumeButton;

        public static UIImageButton RestartButton;

        public static UIImageButton QuitButton;

        public static UIImageButton OptionsButton;

        public static UIImageButton BackButton;

        internal static UISlider MusicVolume;

        internal static UISlider AmbientVolume;

        public static bool Paused { get; set; } = false;

        public const int SETTINGS_BUTTONS_CT = 10;

        public static UIImageButton[] SettingsButtons = new UIImageButton[SETTINGS_BUTTONS_CT];

        private static int _delay;

        // TODO: make rect scissor work -> get powerups to be pickupable

        internal static void Initialize()
        {
            SpriteFont font = TankGame.Fonts.Default;
            Vector2 drawOrigin = font.MeasureString("Mission 1") / 2f;
            MissionInfoBar = new((uiPanel, spriteBatch) => spriteBatch.DrawString(font, "Mission 1", uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f));
            MissionInfoBar.BackgroundColor = Color.Red;
            MissionInfoBar.SetDimensions(650, 1000, 500, 50);

            ResumeButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Resume", Color.WhiteSmoke));
            ResumeButton.SetDimensions(700, 100, 500, 150);
            ResumeButton.OnMouseClick += ResumeButton_OnMouseClick;

            RestartButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Start Over", Color.WhiteSmoke));
            RestartButton.SetDimensions(700, 350, 500, 150);

            OptionsButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Options", Color.WhiteSmoke));
            OptionsButton.SetDimensions(700, 600, 500, 150);
            OptionsButton.OnMouseClick += OptionsButton_OnMouseClick;

            QuitButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Quit", Color.WhiteSmoke));
            QuitButton.SetDimensions(700, 850, 500, 150);
            QuitButton.OnMouseClick += QuitButton_OnMouseClick;

            MusicVolume = new();
            MusicVolume.SetDimensions(700, 100, 500, 150);
            MusicVolume.Initialize();
            MusicVolume.Value = TankGame.Settings.MusicVolume;
            MusicVolume.BarWidth = 15;
            MusicVolume.SliderColor = Color.WhiteSmoke;

            AmbientVolume = new();
            AmbientVolume.SetDimensions(700, 600, 500, 150);
            AmbientVolume.Initialize();
            AmbientVolume.Value = TankGame.Settings.AmbientVolume;
            AmbientVolume.BarWidth = 15;
            AmbientVolume.SliderColor = Color.WhiteSmoke;

            BackButton = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, "Back", Color.WhiteSmoke));
            BackButton.SetDimensions(700, 850, 500, 150);
            BackButton.OnMouseClick += BackButton_OnMouseClick;

            ResumeButton.Visible = false;
            RestartButton.Visible = false;
            QuitButton.Visible = false;
            OptionsButton.Visible = false;
            MusicVolume.Visible = false;
            AmbientVolume.Visible = false;
            BackButton.Visible = false;
        }

        private static void BackButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            InOptions = false;
            MusicVolume.Visible = false;
            AmbientVolume.Visible = false;
            BackButton.Visible = false;
        }

        private static void OptionsButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            _delay = 1;
            InOptions = true;
            ResumeButton.Visible = false;
            RestartButton.Visible = false;
            QuitButton.Visible = false;
            OptionsButton.Visible = false;
            MusicVolume.Visible = true;
            AmbientVolume.Visible = true;
            AmbientVolume.IgnoreMouseInteractions = true;
            BackButton.Visible = true;
        }

        private static void QuitButton_OnMouseClick(Internals.UI.UIElement affectedElement)
        {
            TankGame.Instance.Exit();
        }

        private static void ResumeButton_OnMouseClick(Internals.UI.UIElement affectedElement) {
            Paused = false;
        }

        public static void QuickButton(UIImage imageButton, SpriteBatch spriteBatch, string text, Color color, float scale = 1f, bool onLeft = false)
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
            spriteBatch.DrawString(font, text, imageButton.Hitbox.Center.ToVector2(), Color.Black, 0, drawOrigin, scale, SpriteEffects.None, 1f);
        }

        public static void UpdateButtons()
        {
            var text = $"Mission 1        x{AITank.CountAll()}";
            Vector2 drawOrigin = TankGame.Fonts.Default.MeasureString(text) / 2f;
            MissionInfoBar.UniqueDraw =
                (uiPanel, spriteBatch) => spriteBatch.DrawString(TankGame.Fonts.Default, text, uiPanel.Hitbox.Center.ToVector2(), Color.White, 0, drawOrigin, 1.5f, SpriteEffects.None, 1f);
            if (Pause.JustPressed) {
                if (InOptions)
                {
                    InOptions = false;
                    MusicVolume.Visible = false;
                    AmbientVolume.Visible = false;
                    BackButton.Visible = false;
                    return;
                }
                if (Paused)
                    TankMusicSystem.ResumeAll();
                else
                    TankMusicSystem.PauseAll();
                Paused = !Paused;
            }

            if (!InOptions)
            {
                ResumeButton.Visible = Paused;
                RestartButton.Visible = Paused;
                QuitButton.Visible = Paused;
                OptionsButton.Visible = Paused;
            }

            if (MusicVolume.Value != 0f)
                TankGame.Settings.MusicVolume = MusicVolume.Value;

            TankGame.Settings.AmbientVolume = AmbientVolume.Value;
            TankMusicSystem.UpdateVolume();

            if (_delay > 0 && !Input.MouseLeft)
                _delay--;
            if (_delay <= 0)
                AmbientVolume.IgnoreMouseInteractions = false;
        }

        public class Options
        {
            public SettingsButton<float> MasterVolButton;
        }

        public class SettingsButton<TSource>
        {
            public UIImageButton button;

            public string name;

            private static int num_sets_btns;

            public SettingsButton(bool setDefaultDimensions = true)
            {
                if (setDefaultDimensions)
                    SetDefDims();
                num_sets_btns++;
            }

            public void ApplyChanges(ref TSource value)
            {

            }

            public void SetDefDims()
            {
                button = new(null, 1f, (uiImageButton, spriteBatch) => QuickButton(uiImageButton, spriteBatch, name, Color.WhiteSmoke));
                button.SetDimensions(new Rectangle(100, 100 * num_sets_btns, GameUtils.WindowWidth - 200, 50));

                button.HasScissor = true;
                button.Scissor = GeometryUtils.CreateRectangleFromCenter(GameUtils.WindowWidth / 2, GameUtils.WindowHeight / 2, GameUtils.WindowWidth / 4, (int)(GameUtils.WindowHeight * 0.4f));
            }
        }
    }
}