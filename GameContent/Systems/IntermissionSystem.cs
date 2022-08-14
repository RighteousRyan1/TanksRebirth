using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.UI;
using FontStashSharp;
using System.Linq;
using TanksRebirth.GameContent.Properties;

namespace TanksRebirth.GameContent.Systems
{
    public static class IntermissionSystem
    {
        public static int TimeBlack; // for black screen when entering this game
        public static float BlackAlpha;

        public static bool IsAwaitingNewMission => CurrentWaitTime > 240; // 3 seconds seems to be the opening fanfare duration

        public static float Alpha;
        public static int WaitTime { get; private set; }
        public static int CurrentWaitTime { get; private set; }

        public static readonly Color DefaultBackgroundColor = new(228, 231, 173); // color picked lol
        public static readonly Color DefaultStripColor = Color.DarkRed;

        public static Color BackgroundColor = new(228, 231, 173); // color picked lol
        public static Color StripColor = Color.DarkRed;

        private static Vector2 _offset;

        private static float _oldBlack;

        /// <summary>Renders the intermission.</summary>
        public static void Draw(SpriteBatch spriteBatch)
        {
            if (TankGame.Instance.IsActive)
            {
                if (TimeBlack > -1)
                {
                    TimeBlack--;
                    BlackAlpha += 1f / 45f;
                }
                else
                    BlackAlpha -= 1f / 45f;
            }

            if (BlackAlpha >= 1 && _oldBlack < 1)
            {
                MainMenu.Leave();

                if (GameProperties.ShouldMissionsProgress)
                {
                    GameProperties.LoadedCampaign.SetupLoadedMission(true);
                    // GameHandler.LoadedCampaign.LoadMission(27);
                }
                
            }

            BlackAlpha = MathHelper.Clamp(BlackAlpha, 0f, 1f);

            spriteBatch.Draw(
                TankGame.WhitePixel,
                new Rectangle(0, 0, GameUtils.WindowWidth, GameUtils.WindowHeight),
                Color.Black * BlackAlpha);

            if (!GameUI.Paused)
            {
                _offset.Y -= 1f;
                _offset.X += 1f;
            }
            if (MainMenu.Active && BlackAlpha <= 0)
            {
                Alpha = 0f;
                CurrentWaitTime = 0;
            }
            if (Alpha <= 0f)
                _offset = Vector2.Zero;

            if (Alpha > 0f)
            {

                spriteBatch.Draw(
                    TankGame.WhitePixel,
                    new Rectangle(0, 0, GameUtils.WindowWidth, GameUtils.WindowHeight),
                    BackgroundColor * Alpha);

                int padding = 10;
                int scale = 2;

                int texWidth = 64 * scale;

                // draw small tank graphics using GameResources.GetGameResource
                for (int i = -padding; i < GameUtils.WindowWidth / texWidth + padding; i++)
                {
                    for (int j = -padding; j < GameUtils.WindowHeight / texWidth + padding; j++)
                    {
                        spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank_background_billboard"), new Vector2(i, j) * texWidth + _offset, null, BackgroundColor * Alpha, 0f, Vector2.Zero, scale, default, default);
                    }
                }
                float off = 0.045f;
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * 0.2f, Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off), Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off * 2), Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off * 3), Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off * 4), Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off * 5), Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off * 6), Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off * 7), Alpha);
                DrawStripe(spriteBatch, StripColor, GameUtils.WindowHeight * (0.2f + off * 8), Alpha);
                var wp = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
                spriteBatch.Draw(wp, new Vector2(0, GameUtils.WindowHeight * 0.2f), null, Color.Yellow * Alpha, 0f, new Vector2(0, wp.Size().Y / 2), new Vector2(GameUtils.WindowWidth, 5), default, default);
                spriteBatch.Draw(wp, new Vector2(0, GameUtils.WindowHeight * (0.2f + off * 8)), null, Color.Yellow * Alpha, 0f, new Vector2(0, wp.Size().Y / 2), new Vector2(GameUtils.WindowWidth, 5), default, default);
                int mafs1 = GameProperties.LoadedCampaign.TrackedSpawnPoints.Count(p => p.Item2);
                int mafs2 = GameProperties.LoadedCampaign.LoadedMission.Tanks.Count(x => x.IsPlayer);
                int mafs = mafs1 - mafs2;


                DrawShadowedString(TankGame.TextFontLarge, new Vector2(GameUtils.WindowWidth / 2, GameUtils.WindowHeight / 2 - 220), Vector2.One, GameProperties.LoadedCampaign.LoadedMission.Name, BackgroundColor, new(1f), Alpha);
                DrawShadowedString(TankGame.TextFontLarge, new Vector2(GameUtils.WindowWidth / 2, GameUtils.WindowHeight / 2 - 50), Vector2.One, $"Enemy tanks: {mafs}", BackgroundColor, new(0.8f), Alpha);
                DrawShadowedString(TankGame.TextFontLarge, new Vector2(GameUtils.WindowWidth / 2 - 100, GameUtils.WindowHeight / 2 + 350), Vector2.One, $"x   {PlayerTank.Lives}", BackgroundColor, new(1f), Alpha, new Vector2(0, TankGame.TextFontLarge.MeasureString($"x   {PlayerTank.Lives}").Y / 2));

                if (GameProperties.LoadedCampaign.CurrentMissionId == 0)
                    DrawShadowedString(TankGame.TextFontLarge, new Vector2(GameUtils.WindowWidth / 2, GameUtils.WindowHeight / 2 - 295), Vector2.One, $"Campaign: \"{GameProperties.LoadedCampaign.Properties.Name}\"", BackgroundColor, new(0.4f), Alpha);

                DrawShadowedTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/ui/playertank2d"), new Vector2(GameUtils.WindowWidth / 2 - 200, GameUtils.WindowHeight / 2 + 375), Vector2.One, Color.Blue, new(1.25f), Alpha);
                
                
            }


            _oldBlack = BlackAlpha;
        }

        public static void DrawShadowedString(SpriteFontBase font, Vector2 position, Vector2 shadowDir, string text, Color color, Vector2 scale, float alpha, Vector2 origin = default, float shadowDistScale = 1f)
        {
            TankGame.SpriteRenderer.DrawString(font, text, position + (Vector2.Normalize(shadowDir) * 10f * shadowDistScale * scale).ToResolution(), Color.Black * alpha * 0.75f, scale, 0f, origin == default ? TankGame.TextFontLarge.MeasureString(text) / 2 : origin, 0f);

            TankGame.SpriteRenderer.DrawString(font, text, position, color * alpha, scale, 0f, origin == default ? TankGame.TextFontLarge.MeasureString(text) / 2 : origin, 0f);
        }
        public static void DrawShadowedTexture(Texture2D texture, Vector2 position, Vector2 shadowDir, Color color, Vector2 scale, float alpha, Vector2 origin = default, bool flip = false, float shadowDistScale = 1f)
        {
            TankGame.SpriteRenderer.Draw(texture, position + (Vector2.Normalize(shadowDir) * 10f * shadowDistScale * scale).ToResolution(), null, Color.Black * alpha * 0.75f, 0f, origin == default ? texture.Size() / 2 : origin, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, default);
            TankGame.SpriteRenderer.Draw(texture, position, null, color * alpha, 0f, origin == default ? texture.Size() / 2 : origin, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, default);
        }

        private static void DrawStripe(SpriteBatch spriteBatch, Color color, float offsetY, float alpha)
        {
            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/banner");

            var scaling = new Vector2(GameUtils.WindowWidth / tex.Width / 2 + 0.2f, GameUtils.WindowHeight / tex.Height / 18.2f); // 18.2 ratio

            //var test = GameUtils.MousePosition.X / GameUtils.WindowWidth * 100;
            //ChatSystem.SendMessage(test, Color.White);

            spriteBatch.Draw(tex, new Vector2(-30 + 15, offsetY), null, color * alpha, 0f, new Vector2(0, tex.Size().Y / 2), scaling, default, default);
            spriteBatch.Draw(tex, new Vector2(-30 + (float)(tex.Width * scaling.X) + (3 * scaling.X / 2.2f), offsetY), null, color * alpha, 0f, new Vector2(0, tex.Size().Y / 2), scaling, default, default);
            spriteBatch.Draw(tex, new Vector2(-30 + (float)(tex.Width * 2 * scaling.X) - (8 * scaling.X / 2.2f), offsetY), null, color * alpha, 0f, new Vector2(0, tex.Size().Y / 2), scaling, default, default);
        }

        public static void SetTime(int time)
        {
            WaitTime = time;
            CurrentWaitTime = time;
        }

        public static void Tick(int delta)
        {
            if (CurrentWaitTime - delta < 0)
                CurrentWaitTime = 0;
            else
                CurrentWaitTime -= delta;
        }
        public static void TickAlpha(float delta)
        {
            if (Alpha + delta < 0)
                Alpha = 0;
            else if (Alpha + delta > 1)
                Alpha = 1;
            else
                Alpha += delta;
        }
    }
}
