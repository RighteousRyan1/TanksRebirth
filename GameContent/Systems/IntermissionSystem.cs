using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.UI;
using FontStashSharp;
using System.Linq;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.Net;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.GameContent.Systems
{
    public static class IntermissionSystem
    {
        public static int TimeBlack; // for black screen when entering this game
        public static float BlackAlpha = 0;

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
            TankGame.Interp = Alpha <= 0 && BlackAlpha <= 0 && GameHandler.InterpCheck;

            if (TankGame.Instance.IsActive)
            {
                if (TimeBlack > -1)
                {
                    TimeBlack--;
                    BlackAlpha += 1f / 45f * TankGame.DeltaTime;
                }
                else
                    BlackAlpha -= 1f / 45f * TankGame.DeltaTime;
            }

            if (BlackAlpha >= 1 && _oldBlack < 1)
            {
                MainMenu.Leave();

                if (GameProperties.ShouldMissionsProgress)
                {
                    // todo: should this happen?
                    GameProperties.LoadedCampaign.SetupLoadedMission(true);
                    // GameHandler.LoadedCampaign.LoadMission(27);
                }
                
            }

            BlackAlpha = MathHelper.Clamp(BlackAlpha, 0f, 1f);

            spriteBatch.Draw(
                TankGame.WhitePixel,
                new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight),
                Color.Black * BlackAlpha);

            if (!GameUI.Paused)
            {
                _offset.Y -= 1f * TankGame.DeltaTime;
                _offset.X += 1f * TankGame.DeltaTime;
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
                    new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight),
                    BackgroundColor * Alpha);

                int padding = 10;
                int scale = 2;

                int texWidth = 64 * scale;

                // draw small tank graphics using GameResources.GetGameResource
                for (int i = -padding; i < WindowUtils.WindowWidth / texWidth + padding; i++)
                {
                    for (int j = -padding; j < WindowUtils.WindowHeight / texWidth + padding; j++)
                    {
                        spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank_background_billboard"), new Vector2(i, j) * texWidth + _offset, null, BackgroundColor * Alpha, 0f, Vector2.Zero, scale, default, default);
                    }
                }
                // why didn't i use this approach before? i'm kind of braindead sometimes.
                for (int i = 0; i < 6; i++) {
                    var off = 75f;
                    DrawStripe(spriteBatch, StripColor, WindowUtils.WindowHeight * 0.16f + (off * i).ToResolutionY(), Alpha);
                }
                var wp = TankGame.WhitePixel;
                spriteBatch.Draw(wp, new Vector2(0, WindowUtils.WindowHeight * 0.19f), null, Color.Yellow * Alpha, 0f, new Vector2(0, wp.Size().Y / 2), new Vector2(WindowUtils.WindowWidth, 5), default, default);
                spriteBatch.Draw(wp, new Vector2(0, WindowUtils.WindowHeight * 0.19f + 400.ToResolutionY()), null, Color.Yellow * Alpha, 0f, new Vector2(0, wp.Size().Y / 2), new Vector2(WindowUtils.WindowWidth, 5), default, default);
                int mafs1 = GameProperties.LoadedCampaign.TrackedSpawnPoints.Count(p => p.Item2);
                int mafs2 = GameProperties.LoadedCampaign.LoadedMission.Tanks.Count(x => x.IsPlayer);
                int mafs = mafs1 - mafs2;

                DrawShadowedString(TankGame.TextFontLarge, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 220.ToResolutionY()), Vector2.One, GameProperties.LoadedCampaign.LoadedMission.Name, BackgroundColor, Vector2.One.ToResolution(), Alpha);
                DrawShadowedString(TankGame.TextFontLarge, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 50.ToResolutionY()), Vector2.One, $"{TankGame.GameLanguage.EnemyTanks}: {mafs}", BackgroundColor, new Vector2(0.8f).ToResolution(), Alpha);

                var tnk2d = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/playertank2d");

                var count = Server.CurrentClientCount > 0 ? Server.CurrentClientCount : Server.CurrentClientCount + 1;

                //var count = 2;

                for (int i = 0; i < count; i++)
                {
                    var name = Client.IsConnected() ? Server.ConnectedClients[i].Name : string.Empty;

                    if (GameProperties.LoadedCampaign.CurrentMissionId == 0)
                        DrawShadowedString(TankGame.TextFontLarge, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 295.ToResolutionY()), Vector2.One, $"{TankGame.GameLanguage.Campaign}: \"{GameProperties.LoadedCampaign.MetaData.Name}\" ({TankGame.GameLanguage.Mission} #{GameProperties.LoadedCampaign.CurrentMissionId + 1})", BackgroundColor, new Vector2(0.4f).ToResolution(), Alpha);
                    else
                        DrawShadowedString(TankGame.TextFontLarge, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 295.ToResolutionY()), Vector2.One, $"{TankGame.GameLanguage.Mission} #{GameProperties.LoadedCampaign.CurrentMissionId + 1}", BackgroundColor, new Vector2(0.4f).ToResolution(), Alpha);

                    var pos = new Vector2(WindowUtils.WindowWidth / (count + 1) * (i + 1), WindowUtils.WindowHeight / 2 + 375.ToResolutionY());

                    var lifeText = $"x  {PlayerTank.Lives[i]}";
                    DrawShadowedString(TankGame.TextFontLarge, pos + new Vector2(75, -25).ToResolution(), Vector2.One, lifeText, BackgroundColor, Vector2.One.ToResolution(), Alpha, TankGame.TextFontLarge.MeasureString(lifeText) / 2);

                    DrawShadowedString(TankGame.TextFontLarge, pos - new Vector2(0, 75).ToResolution(), Vector2.One, name, PlayerID.PlayerTankColors[i].ToColor(), 
                        new Vector2(0.3f).ToResolution(), Alpha, TankGame.TextFontLarge.MeasureString(name) / 2);
                    DrawShadowedTexture(tnk2d, pos - new Vector2(130, 0).ToResolution(), Vector2.One, PlayerID.PlayerTankColors[i].ToColor(), new Vector2(1.25f), Alpha, tnk2d.Size() / 2);
                }
            }
            _oldBlack = BlackAlpha;
        }

        private static void DrawBonusLifeHUD()
        {
            // TODO: implement.
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

            var scaling = new Vector2(3, 3f);//new Vector2(WindowUtils.WindowWidth / tex.Width / 2 + 0.2f, WindowUtils.WindowHeight / tex.Height / 18.2f); // 18.2 ratio

            //var test = MouseUtils.MousePosition.X / WindowUtils.WindowWidth * 100;
            //ChatSystem.SendMessage(test, Color.White);

            spriteBatch.Draw(tex, new Vector2(-15, offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
            spriteBatch.Draw(tex, new Vector2(WindowUtils.WindowWidth / 2, offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
            //spriteBatch.Draw(tex, new Vector2(-30 + (float)(tex.Width * scaling.X) + (3 * scaling.X / 2.2f), offsetY), null, color * alpha, 0f, new Vector2(0, tex.Size().Y / 2), scaling, default, default);
            //spriteBatch.Draw(tex, new Vector2(-30 + (float)(tex.Width * 2 * scaling.X) - (8 * scaling.X / 2.2f), offsetY), null, color * alpha, 0f, new Vector2(0, tex.Size().Y / 2), scaling, default, default);
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
// use this when i figure out the issue
/*using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.UI;
using FontStashSharp;
using System.Linq;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.Net;
using TanksRebirth.GameContent.ID;
using static TanksRebirth.Internals.Common.Utilities.TweenUtils;

namespace TanksRebirth.GameContent.Systems
{
    public static class IntermissionSystem
    {
        public static int TimeBlack; // for black screen when entering this game
        public static float BlackAlpha = 0;

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
        private static bool _initializedHeader;
        private static RenderTarget2D _header;

        public static void InitializeHeader(SpriteBatch spriteBatch) {
            _initializedHeader = true;

            _header?.Dispose();

            _header = new RenderTarget2D(
                spriteBatch.GraphicsDevice,
                WindowUtils.WindowWidth,
                WindowUtils.WindowHeight);

            var wp = TankGame.WhitePixel;

            var headerSb = new SpriteBatch(spriteBatch.GraphicsDevice);

            headerSb.GraphicsDevice.SetRenderTarget(_header);
            headerSb.GraphicsDevice.Clear(Color.Transparent);

            headerSb.Begin();

            for (int i = 0; i < 6; i++) {
                var off = 75f;
                DrawStripe(headerSb, StripColor, WindowUtils.WindowHeight * 0.16f + (off * i).ToResolutionY(), 1);
            }

            headerSb.Draw(
                wp,
                new Vector2(0, WindowUtils.WindowHeight * 0.19f),
                null,
                Color.Yellow,
                0f,
                new Vector2(0, wp.Size().Y / 2),
                new Vector2(WindowUtils.WindowWidth, 5),
                default,
                default);
            headerSb.Draw(
                wp,
                new Vector2(0, WindowUtils.WindowHeight * 0.19f + 400.ToResolutionY()),
                null,
                Color.Yellow,
                0f,
                new Vector2(0, wp.Size().Y / 2),
                new Vector2(WindowUtils.WindowWidth, 5),
                default,
                default);

            headerSb.End();

            headerSb.GraphicsDevice.SetRenderTarget(null);
        }

        /// <summary>Renders the intermission.</summary>
        public static void Draw(SpriteBatch spriteBatch) {

            InitializeHeader(spriteBatch);

            TankGame.Interp = Alpha <= 0 && BlackAlpha <= 0 && GameHandler.InterpCheck;

            if (TankGame.Instance.IsActive) {
                if (TimeBlack > -1) {
                    TimeBlack--;
                    BlackAlpha += 1f / 45f * TankGame.DeltaTime;
                }
                else
                    BlackAlpha -= 1f / 45f * TankGame.DeltaTime;
            }

            if (BlackAlpha >= 1 && _oldBlack < 1) {
                MainMenu.Leave();

                if (GameProperties.ShouldMissionsProgress) {
                    // todo: should this happen?
                    GameProperties.LoadedCampaign.SetupLoadedMission(true);
                    // GameHandler.LoadedCampaign.LoadMission(27);
                }

            }

            BlackAlpha = MathHelper.Clamp(BlackAlpha, 0f, 1f);

            spriteBatch.Draw(
                TankGame.WhitePixel,
                new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight),
                Color.Black * BlackAlpha);

            if (!GameUI.Paused) {
                _offset.Y -= 1f * TankGame.DeltaTime;
                _offset.X += 1f * TankGame.DeltaTime;
            }
            if (MainMenu.Active && BlackAlpha <= 0) {
                Alpha = 0f;
                CurrentWaitTime = 0;
            }
            if (Alpha <= 0f)
                _offset = Vector2.Zero;

            if (Alpha > 0f) {

                spriteBatch.Draw(
                    TankGame.WhitePixel,
                    new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight),
                    BackgroundColor * Alpha);

                int padding = 10;
                int scale = 2;

                int texWidth = 64 * scale;

                // draw small tank graphics using GameResources.GetGameResource
                for (int i = -padding; i < WindowUtils.WindowWidth / texWidth + padding; i++) {
                    for (int j = -padding; j < WindowUtils.WindowHeight / texWidth + padding; j++) {
                        spriteBatch.Draw(
                            GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank_background_billboard"),
                            new Vector2(i, j) * texWidth + _offset,
                            null,
                            BackgroundColor * Alpha,
                            0f,
                            Vector2.Zero,
                            scale,
                            default,
                            default);
                    }
                }
                // why didn't i use this approach before? i'm kind of braindead sometimes.
                BoundedTween headerTween = new(460, 420);
                BoundedTween textTween = new(420, 410, Easings.OutQuart);
                BoundedTween textTween2 = new(410, 400, Easings.InOutQuart);

                float textTweenVal = textTween.GetValue(CurrentWaitTime) * 1.1f - textTween2.GetValue(CurrentWaitTime) * 0.1f;
                spriteBatch.Draw(_header, WindowUtils.ScreenRect, null, Color.White * Alpha * headerTween.GetValue(CurrentWaitTime));
                int mafs1 = GameProperties.LoadedCampaign.TrackedSpawnPoints.Count(p => p.Item2);
                int mafs2 = GameProperties.LoadedCampaign.LoadedMission.Tanks.Count(x => x.IsPlayer);
                int mafs = mafs1 - mafs2;

                DrawShadowedString(
    TankGame.TextFontLarge,
    new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 220.ToResolutionY()),
    Vector2.One,
    GameProperties.LoadedCampaign.LoadedMission.Name,
    BackgroundColor,
    Vector2.One.ToResolution() * textTweenVal,
    Alpha);
                DrawShadowedString(
                    TankGame.TextFontLarge,
                    new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 50.ToResolutionY()),
                    Vector2.One,
                    $"{TankGame.GameLanguage.EnemyTanks}: {mafs}",
                    BackgroundColor,
                    new Vector2(0.8f).ToResolution() * textTweenVal,
                    Alpha);
                var tnk2d = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/playertank2d");

                var count = Server.CurrentClientCount > 0 ? Server.CurrentClientCount : Server.CurrentClientCount + 1;

                //var count = 2;

                for (int i = 0; i < count; i++) {
                    var name = Client.IsConnected() ? Server.ConnectedClients[i].Name : string.Empty;

                    //var name = "name" + i;

                    if (GameProperties.LoadedCampaign.CurrentMissionId == 0)
                        DrawShadowedString(TankGame.TextFontLarge, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 295.ToResolutionY()), Vector2.One, $"{TankGame.GameLanguage.Campaign}: \"{GameProperties.LoadedCampaign.MetaData.Name}\"", BackgroundColor, new Vector2(0.4f).ToResolution(), Alpha);

                    var pos = new Vector2(WindowUtils.WindowWidth / (count + 1) * (i + 1), WindowUtils.WindowHeight / 2 + 375.ToResolutionY());

                    var lifeText = $"x  {PlayerTank.Lives[i]}";
                    DrawShadowedString(
                        TankGame.TextFontLarge,
                        pos + new Vector2(75, -25).ToResolution(),
                        Vector2.One,
                        lifeText,
                        BackgroundColor,
                        Vector2.One.ToResolution(),
                        Alpha,
                        TankGame.TextFontLarge.MeasureString(lifeText) / 2);

                    DrawShadowedString(
                        TankGame.TextFontLarge,
                        pos - new Vector2(0, 75).ToResolution(),
                        Vector2.One,
                        name,
                        PlayerID.PlayerTankColors[i].ToColor(),
                        new Vector2(0.5f).ToResolution(),
                        Alpha,
                        TankGame.TextFontLarge.MeasureString(name) / 2);

                    DrawShadowedTexture(
                        tnk2d,
                        pos - new Vector2(130, 0).ToResolution(),
                        Vector2.One,
                        PlayerID.PlayerTankColors[i].ToColor(),
                        new Vector2(1.25f),
                        Alpha,
                        tnk2d.Size() / 2);
                }
            }
            _oldBlack = BlackAlpha;
        }

        private static void DrawBonusLifeHUD() {
            // TODO: implement.
        }

        public static void DrawShadowedString(SpriteFontBase font, Vector2 position, Vector2 shadowDir, string text, Color color, Vector2 scale, float alpha, Vector2 origin = default, float shadowDistScale = 1f) {
            TankGame.SpriteRenderer.DrawString(font, text, position + (Vector2.Normalize(shadowDir) * 10f * shadowDistScale * scale).ToResolution(), Color.Black * alpha * 0.75f, scale, 0f, origin == default ? TankGame.TextFontLarge.MeasureString(text) / 2 : origin, 0f);

            TankGame.SpriteRenderer.DrawString(font, text, position, color * alpha, scale, 0f, origin == default ? TankGame.TextFontLarge.MeasureString(text) / 2 : origin, 0f);
        }
        public static void DrawShadowedTexture(Texture2D texture, Vector2 position, Vector2 shadowDir, Color color, Vector2 scale, float alpha, Vector2 origin = default, bool flip = false, float shadowDistScale = 1f) {
            TankGame.SpriteRenderer.Draw(texture, position + (Vector2.Normalize(shadowDir) * 10f * shadowDistScale * scale).ToResolution(), null, Color.Black * alpha * 0.75f, 0f, origin == default ? texture.Size() / 2 : origin, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, default);
            TankGame.SpriteRenderer.Draw(texture, position, null, color * alpha, 0f, origin == default ? texture.Size() / 2 : origin, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, default);
        }
        private static void DrawStripe(SpriteBatch spriteBatch, Color color, float offsetY, float alpha) {
            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/banner");

            var scaling = new Vector2(3, 3f);//new Vector2(WindowUtils.WindowWidth / tex.Width / 2 + 0.2f, WindowUtils.WindowHeight / tex.Height / 18.2f); // 18.2 ratio

            //var test = MouseUtils.MousePosition.X / WindowUtils.WindowWidth * 100;
            //ChatSystem.SendMessage(test, Color.White);

            spriteBatch.Draw(tex, new Vector2(-15.ToResolutionX(), offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
            spriteBatch.Draw(tex, new Vector2(WindowUtils.WindowWidth / 2, offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
            //spriteBatch.Draw(tex, new Vector2(-30 + (float)(tex.Width * scaling.X) + (3 * scaling.X / 2.2f), offsetY), null, color * alpha, 0f, new Vector2(0, tex.Size().Y / 2), scaling, default, default);
            //spriteBatch.Draw(tex, new Vector2(-30 + (float)(tex.Width * 2 * scaling.X) - (8 * scaling.X / 2.2f), offsetY), null, color * alpha, 0f, new Vector2(0, tex.Size().Y / 2), scaling, default, default);
        }
        public static void SetTime(int time) {
            WaitTime = time;
            CurrentWaitTime = time;
        }
        public static void Tick(int delta) {
            if (CurrentWaitTime - delta < 0)
                CurrentWaitTime = 0;
            else
                CurrentWaitTime -= delta;
        }
        public static void TickAlpha(float delta) {
            if (Alpha + delta < 0)
                Alpha = 0;
            else if (Alpha + delta > 1)
                Alpha = 1;
            else
                Alpha += delta;
        }
    }
}*/