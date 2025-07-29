using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.UI;
using FontStashSharp;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Net;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Framework.Animation;
using System;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Internals.Common.Framework.Audio;
using System.Runtime.Intrinsics.X86;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Enums;
using TanksRebirth.Graphics.Shaders;

namespace TanksRebirth.GameContent.Systems;
#pragma warning disable
public static class IntermissionSystem {
    public static RenderTarget2D BackgroundBuffer;
    public static RenderTarget2D BannerBuffer;
    public static RenderTarget2D BonusBannerBuffer;
    public static RenderTarget2D BonusBannerTextBuffer;

    public static Animator TextAnimatorLarge;
    public static Animator TextAnimatorSmall;

    public static Animator IntermissionAnimator;

    public static Animator BonusLifeAnimator;

    public static float TimeBlack; // for black screen when entering this game
    public static float BlackAlpha = 0;

    public static bool IsAwaitingNewMission { get; internal set; } // 3 seconds seems to be the opening fanfare duration
    public static bool ShouldDrawBanner = true;
    public static bool ShouldFade;
    public static bool ShouldFadeIn;

    public const float ALPHA_FADE_CONSTANT = 1f / 30f;
    public static float BannerAlpha = 1f;
    public static float BonusBannerAlpha;
    public static bool ShouldDrawBonusBanner;

    public static float Alpha;
    public static float ColorMultiplierForBorders = 0.35f;

    public static Color ColorForBorders => (BackgroundColor * ColorMultiplierForBorders) with { A = 255 };

    // values are color-picked
    public static readonly Color DefaultBackgroundColor = new(250, 230, 150);
    public static readonly Color DefaultStripColor = new(186, 62, 47);
    public static readonly Color BonusBannerColor = new(93, 145, 92);

    public static Color BackgroundColor = new(228, 231, 173);
    public static Color BannerColor = new(186, 62, 47);

    static Vector2 _offset;
    static Rectangle _cutForLength;
    internal static float oldBlack;
    static bool _forceBonusDrawToHeight;

    public static Texture2D BonusBannerBase;
    public static Texture2D BonusBannerStar;

    public static GradientEffect BonusTextGradient;
    public static readonly Color GradientTopColor = new(223, 223, 28);
    public static readonly Color GradientBottomColor = new(163, 157, 19);
    public static readonly Color BonusBannerTextBorderColor = new(59, 66, 12);

    public static void InitializeAllStartupLogic() {
        InitializeAnmiations();
        InitializeTextureLogic();

        BonusTextGradient = new(GradientTopColor, GradientBottomColor);
    }
    private static void InitializeTextureLogic() {
        BonusBannerBase = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tnk_bonus_base");
        BonusBannerStar = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tnk_bonus_star");

        _cutForLength = new Rectangle(31, 0, 1, BonusBannerBase.Height);
    }
    public static void InitializeAnmiations() {

        TankGame.OnFocusLost += PauseIntermissionSounds;
        TankGame.OnFocusRegained += ResumeIntermissionSounds;
        GameUI.Pause.OnPress += () => {
        };

        IntermissionHandler.Initialize();
        // should this be where the animator is re-instantiated?
        TextAnimatorSmall = Animator.Create()
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.Zero, duration: TimeSpan.FromSeconds(0.25), easing: EasingFunction.OutBack))
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.One * 0.4f, duration: TimeSpan.FromSeconds(0.25), easing: EasingFunction.OutBack));
        TextAnimatorLarge = Animator.Create()
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.Zero, duration: TimeSpan.FromSeconds(0.35), easing: EasingFunction.OutBack))
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.One * 1.2f, duration: TimeSpan.FromSeconds(0.35), easing: EasingFunction.OutBack));

        IntermissionAnimator = Animator.Create()
            .WithFrame(new(duration: TimeSpan.FromSeconds(3), easing: EasingFunction.Linear))
            // this frame does not affect the timeline but instead what happens for loading
            .WithFrame(new(duration: TimeSpan.FromSeconds(1), easing: EasingFunction.Linear))
            .WithFrame(new(duration: TimeSpan.FromSeconds(4), easing: EasingFunction.Linear))
            // 3.66?... also i just realized this number doesn't even fucking matter
            .WithFrame(new(duration: TimeSpan.FromSeconds(3.16), easing: EasingFunction.Linear))
            .WithFrame(new(duration: TimeSpan.FromSeconds(0), easing: EasingFunction.Linear));

        // only use position2d.Y when referencing!!!
        BonusLifeAnimator = Animator.Create()
            // time before the banner drops in
            .WithFrame(new(duration: TimeSpan.FromSeconds(0.5), scale: Vector2.One, position2d: Vector2.UnitY * -200, easing: EasingFunction.OutElastic))
            // elastic animation where the banner drops from the top
            // after this frame, force drawing to 40% of window height
            .WithFrame(new(duration: TimeSpan.FromSeconds(1), scale: Vector2.One, position2d: Vector2.UnitY * WindowUtils.WindowHeight * 0.4f, easing: EasingFunction.Linear))
            // text gets brighter and slightly scales up
            // also makes the player life text(s) glow yellow and grow for a second
            .WithFrame(new(duration: TimeSpan.FromSeconds(0.2), scale: Vector2.One, position2d: Vector2.Zero, easing: EasingFunction.OutBack))
            // text shrinks back to original size
            // duration = 0 so it shrinks as soon as the OutBack animation is done
            .WithFrame(new(duration: TimeSpan.FromSeconds(0.2), scale: Vector2.One * 1.25f, position2d: Vector2.Zero, easing: EasingFunction.Linear))
            // time before banner fades out of existence
            .WithFrame(new(duration: TimeSpan.FromSeconds(2.5), scale: Vector2.One, position2d: Vector2.Zero, easing: EasingFunction.Linear))
            // banner actually fades from existence
            // opacity will be handled in the mid-frame actions
            .WithFrame(new(duration: TimeSpan.FromSeconds(0.5), scale: Vector2.One, position2d: Vector2.Zero, easing: EasingFunction.Linear))
            // dummy frame :(
            .WithFrame(new(duration: TimeSpan.FromSeconds(0), scale: Vector2.One, position2d: Vector2.Zero, easing: EasingFunction.Linear));

        BonusLifeAnimator?.Restart();
        BonusLifeAnimator?.Stop(); // to ensure brightness calculations are proper

        BonusLifeAnimator.OnKeyFrameFinish += DoActionsForBonusLife;
        IntermissionAnimator.OnKeyFrameFinish += DoMidAnimationActions;
    }

    public static void TryPauseAll() {
        if (_snareDrums != null)
            if (_snareDrums.IsPlaying())
                _snareDrums.Pause();
        if (_startingFanfare != null)
            if (_startingFanfare.IsPlaying())
                _startingFanfare.Pause();
        if (_bonusLifeGet != null)
            if (_bonusLifeGet.IsPlaying())
                _bonusLifeGet.Pause();
    }
    public static void TryResumeAll() {
        if (_snareDrums != null)
            if (_snareDrums.IsPaused())
                _snareDrums.Play();
        if (_startingFanfare != null)
            if (_startingFanfare.IsPaused())
                _startingFanfare.Play();
        if (_bonusLifeGet != null)
            if (_bonusLifeGet.IsPaused())
                _bonusLifeGet.Play();
    }

    private static void ResumeIntermissionSounds(object? sender, nint e) {
        TryResumeAll();
    }

    private static void PauseIntermissionSounds(object? sender, nint e) {
        TryPauseAll();
    }

    private static void DoActionsForBonusLife(KeyFrame frame) {
        var frameId = BonusLifeAnimator.KeyFrames.FindIndex(f => f.Equals(frame));

        if (frameId == 0) {
            var lifeget = "Assets/music/fanfares/life_get.ogg";
            _bonusLifeGet = SoundPlayer.PlaySoundInstance(lifeget, SoundContext.Effect, 0.5f, rememberMe: true);
            _forceBonusDrawToHeight = true;
        } 
        else if (frameId == 1) {
            PlayerTank.AddLives(1);
        }
        else if (frameId == 2) {

        }
        else if (frameId == 3) {

        }
        else if (frameId == 4) {
            ShouldDrawBonusBanner = false;
            MissionSetup();
        }
        else if (frameId == 5) {
            
        }
        // FIXME: frameId 6 doesn't get called because of some stupid ahh code
    }

    public static void InitializeCountdowns(bool oneUp = false) {
        // 10 seconds to complete the entire thing (14 if one up)
        // at 3 seconds in, the opening fanfare plays (but for some reason i gotta use 4 in this.)
        // at 220 frames left (3.66 seconds), the snare drums begin, and the scene is visible
        // at halfway through, the next mission is set-up
        float secs1 = 3;

        // + 4 since DEF_PLUSLIFE_TIME is 240 ticks (4 seconds)
        float secs3 = 2.5f + (oneUp ? 1f : 0f);

        // no bonus time if no one up
        float secs2 = oneUp ? IntermissionHandler.DEF_PLUSLIFE_TIME / 60 : 0;

        // for loading into a campaign
        if (TimeBlack > 0) {
            secs1 = 4;
            secs3 = 3;
        }
        IntermissionAnimator.KeyFrames[0] = new(duration: TimeSpan.FromSeconds(secs1), easing: EasingFunction.Linear);
        // to allow for the bonus life animation
        IntermissionAnimator.KeyFrames[2] = new(duration: TimeSpan.FromSeconds(secs2), easing: EasingFunction.Linear);
        IntermissionAnimator.KeyFrames[3] = new(duration: TimeSpan.FromSeconds(secs3), easing: EasingFunction.Linear);

        BonusLifeAnimator.KeyFrames[1] = new(duration: TimeSpan.FromSeconds(1), scale: Vector2.One, position2d: Vector2.UnitY * WindowUtils.WindowHeight * 0.4f, easing: EasingFunction.OutElastic);


        // the last frame is filler because i dunno how to fix the last frame finish event firing bug
        // ignore the last frame
        // TODO: fix this fuckery above
    }
    private static void DoMidAnimationActions(KeyFrame frame) {
        var frameId = IntermissionAnimator.KeyFrames.FindIndex(f => f.Equals(frame));

        if (MainMenuUI.IsActive) {
            IntermissionAnimator?.Restart();
            IntermissionAnimator?.Stop(); // the player dipped during the intermission lol
            BonusLifeAnimator?.Restart();
            BonusLifeAnimator?.Stop();
            ShouldDrawBanner = true;
            _forceBonusDrawToHeight = false;
            return;
        }

        // happens as soon as the intermission is fully opaque
        if (frameId == 0) {
            // tell the game to fade the intermission screen into view
            ShouldFade = true;
            ShouldFadeIn = true;
            SceneManager.CleanupScene();

            // ShouldDrawBanner indicates there wasn't a bonus life to be had
            if (ShouldDrawBanner) {
                PlayOpeningFanfare();

                ReplayTextAnimations();
            }
            // ensures it isnt the first load. hacky asf but whatever
            else if (PlayerTank.KillCounts[0] > 0) {
                _forceBonusDrawToHeight = false;
                ShouldDrawBonusBanner = true;
                BonusLifeAnimator?.Restart();
                BonusLifeAnimator?.Run();
            }
        }
        // happens in the middle of full opaque-ness
        else if (frameId == 1) {
            // tell the game to not fade at all
            ShouldFade = false;
            ShouldFadeIn = false;

            if (!ShouldDrawBonusBanner)
                MissionSetup();
        }
        else if (frameId == 2 && !ShouldDrawBanner) {
            ShouldDrawBanner = true;

            ReplayTextAnimations();

            // only plays if the banner is fading in
            PlayOpeningFanfare();
        }
        // as the intermission is beginning to fade to fully transparent
        else if (frameId == 3) {
            // tell the game to fade out to enter game scene view
            IsAwaitingNewMission = false;
            ShouldFade = true;

            // TODO: fix float interp
            if (PlayerTank.ClientTank is not null) {
                // hacky using vectors for now.
                IntermissionHandler.ThirdPersonTransitionAnimation = Animator.Create()
                    //.WithFrame(new(position2d: Vector2.Zero, position3d: PlayerTank.ClientTank.Position3D + new Vector3(0, 100, 0), duration: TimeSpan.FromSeconds(2)))
                    .WithFrame(new(position2d: Vector2.Zero, position3d: PlayerTank.ClientTank.Position3D + new Vector3(0, 100, 0), duration: TimeSpan.FromSeconds(3), easing: EasingFunction.InOutQuad))
                    .WithFrame(new(position2d: new Vector2(-PlayerTank.ClientTank.TurretRotation), position3d: PlayerTank.ClientTank.Position3D));
                IntermissionHandler.ThirdPersonTransitionAnimation?.Restart();
                IntermissionHandler.ThirdPersonTransitionAnimation?.Run();
            }
            IntermissionHandler.BeginIntroSequence();
            
            IntermissionHandler.CountdownAnimator?.Restart();
            IntermissionHandler.CountdownAnimator?.Run();

            // just in case.
            _forceBonusDrawToHeight = false;
        }
    }
    public static void MissionSetup() {
        if (Difficulties.Types["RandomizedTanks"]) {
            if (CampaignGlobals.LoadedCampaign.CurrentMissionId == MainMenuUI.MissionCheckpoint
                && IntermissionHandler.LastResult != MissionEndContext.Lose) {
                CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks
                    = Difficulties.HijackTanks(CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks);
            }
        }
        CampaignGlobals.LoadedCampaign.SetupLoadedMission(GameHandler.AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead));
    }
    public static void SetMusic() {
        var tune = "Assets/music/fanfares/mission_snare.ogg";
        _snareDrums = SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);
    }
    public static void PrepareBuffers(GraphicsDevice device, SpriteBatch spriteBatch) {
        RenderGlobals.EnsureRenderTargetOK(ref BackgroundBuffer, device, WindowUtils.WindowWidth, WindowUtils.WindowHeight);
        RenderGlobals.EnsureRenderTargetOK(ref BannerBuffer, device, WindowUtils.WindowWidth, WindowUtils.WindowHeight / 2);
        RenderGlobals.EnsureRenderTargetOK(ref BonusBannerBuffer, device, /*(int)(WindowUtils.WindowWidth * 0.75f)*/ WindowUtils.WindowWidth, BonusBannerBase.Height * 2);
        RenderGlobals.EnsureRenderTargetOK(ref BonusBannerTextBuffer, device, WindowUtils.WindowWidth / 3, (int)125.ToResolutionY());

        if (TankGame.Instance.IsActive) {
            // System.Diagnostics.Debug.WriteLine(TimeBlack);
            if (TimeBlack > 0) {
                TimeBlack -= RuntimeData.DeltaTime;
                BlackAlpha += ALPHA_FADE_CONSTANT * RuntimeData.DeltaTime;
            }
            else
                BlackAlpha -= ALPHA_FADE_CONSTANT * RuntimeData.DeltaTime;
        }
        if (BlackAlpha >= 1f && oldBlack < 1f) {
            MainMenuUI.Leave();
        }
        // wait a little bit to do animations so they are clearly visible to the user
        Alpha = MathHelper.Clamp(Alpha, 0f, 1f);

        if (MainMenuUI.IsActive) return;

        if (!GameUI.Paused && TankGame.Instance.IsActive) {
            // moves the background tank graphic
            _offset.Y -= 1f * RuntimeData.DeltaTime;
            _offset.X += 1f * RuntimeData.DeltaTime;
        }
        if (MainMenuUI.IsActive && BlackAlpha <= 0) {
            Alpha = 0f;
        }

        // System.Diagnostics.Debug.WriteLine($"{IntermissionAnimator.ElapsedTime} - {IntermissionAnimator.CurrentInterpolation}");
        // TankGame.Interp = Alpha <= 0 && BlackAlpha <= 0 && GameHandler.InterpCheck;
        // switch to RT, begin SB, do drawing, end SB, SetRenderTarget(null), begin SB again, draw RT, end SB

        // used in the bonus life animation, to determine color flashing upon growth
        var brightness = (BonusLifeAnimator.CurrentScale - Vector2.One).Length();

        #region RenderToBackground

        device.SetRenderTarget(BackgroundBuffer);

        device.Clear(RenderGlobals.BackBufferColor);

        spriteBatch.Begin(rasterizerState: RenderGlobals.DefaultRasterizer);

        // draw the background color
        spriteBatch.Draw(
            TextureGlobals.Pixels[Color.White],
            new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight),
            BackgroundColor);

        int padding = 10;
        var scale = new Vector2(2).ToResolution();

        // draw small tank graphics in the background
        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank_background_billboard");

        var dims = tex.Size() * scale;
        for (int i = -padding; i < WindowUtils.WindowWidth / dims.X + padding; i++) {
            for (int j = -padding; j < WindowUtils.WindowHeight / dims.Y + padding; j++) {
                spriteBatch.Draw(tex, new Vector2(i, j) * dims + _offset.ToResolution(), 
                    null, BackgroundColor, 0f, Vector2.Zero, scale, default, default);
            }
        }

        // draw player graphics & life remaining
        var tnk2d = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/playertank2d");
        var count = Server.CurrentClientCount > 0 ? Server.CurrentClientCount : Server.CurrentClientCount + 1;

        for (int i = 0; i < count; i++) {
            var name = Client.IsConnected() ? Server.ConnectedClients[i].Name : string.Empty;

            var brightPlayerColor = ColorUtils.ChangeColorBrightness(PlayerID.PlayerTankColors[i], 0.85f);
            var brighterPlayerColor = ColorUtils.ChangeColorBrightness(PlayerID.PlayerTankColors[i], 0.25f);

            var pos = new Vector2(WindowUtils.WindowWidth / (count + 1) * (i + 1), WindowUtils.WindowHeight / 2 + 375.ToResolutionY());

            var lerpedColor = Color.Lerp(brightPlayerColor, GradientTopColor, brightness);

            var lifeText = $"×  {PlayerTank.Lives[i]}";
            DrawUtils.DrawBorderedStringWithShadow(spriteBatch, FontGlobals.RebirthFontLarge,
                pos + new Vector2(75, -25).ToResolution(),
                Vector2.One,
                lifeText,
                lerpedColor,
                // hacky or not?
                PlayerID.PlayerTankColors[i],
                Vector2.One.ToResolution() * (ShouldDrawBanner ? Vector2.One : BonusLifeAnimator.CurrentScale),
                1f,
                Anchor.Center, shadowDistScale: 1.5f, shadowAlpha: 0.5f, borderThickness: 1f);

            DrawUtils.DrawStringWithShadow(spriteBatch, FontGlobals.RebirthFontLarge,
                pos - new Vector2(0, 75).ToResolution(),
                Vector2.One,
                name,
                PlayerID.PlayerTankColors[i],
                new Vector2(0.3f).ToResolution(),
                1f,
                Anchor.Center, shadowDistScale: 1.5f, shadowAlpha: 0.5f);
            DrawUtils.DrawTextureWithShadow(spriteBatch, tnk2d, pos - new Vector2(130, 0).ToResolution(), Vector2.One,
                brighterPlayerColor, Vector2.One * 1.5f, 1f, Anchor.Center,
                shadowDistScale: 1f, shadowAlpha: 0.5f);
        }

        oldBlack = BlackAlpha;

        spriteBatch.End();

        #endregion

        #region RenderToBanner

        // draw information to the BannerBuffer now

        device.SetRenderTarget(BannerBuffer);

        device.Clear(RenderGlobals.BackBufferColor);

        spriteBatch.Begin(rasterizerState: RenderGlobals.DefaultRasterizer);

        // why didn't i use this approach before? i'm kind of braindead sometimes.
        // draws the plaid(?) banner across the screen
        for (int i = 0; i < 6; i++) {
            var off = 75f;
            DrawStripe(spriteBatch, BannerColor, (off * i).ToResolutionY(), 1f);
        }

        // i got this by mathing 1080 * 0.18 - 1080 * 0.16 (what the old offset was)
        // similar magic for the other values
        var lineOffset = 21.6f.ToResolutionY();
        var textOffsetMissionName = 167.2f.ToResolutionY();
        var textOffsetTanksLeft = 317.2f.ToResolutionY();
        var textOffsetDetails = 72.2f.ToResolutionY();

        // draw the little golden lines across the plaid stripe
        var wp = TextureGlobals.Pixels[Color.White];
        spriteBatch.Draw(wp, new Vector2(0, lineOffset), null,
            Color.Goldenrod * 1f, 0f, new Vector2(0, wp.Size().Y / 2), new Vector2(WindowUtils.WindowWidth, 15), default, default);
        spriteBatch.Draw(wp, new Vector2(0, lineOffset + 420.ToResolutionY()), null,
            Color.Goldenrod * 1f, 0f, new Vector2(0, wp.Size().Y / 2), new Vector2(WindowUtils.WindowWidth, 15), default, default);

        // calculate total enemy tanks (remaining)
        int mafs1 = CampaignGlobals.LoadedCampaign.TrackedSpawnPoints.Count(p => p.Item2);
        int mafs2 = CampaignGlobals.LoadedCampaign.LoadedMission.Tanks.Count(x => x.IsPlayer);
        int mafs = mafs1 - mafs2; // waddafak. why is my old code so horrid.

        // draw large(r) text
        float spacing = 10;
        string enemyTankDisplay = $"{TankGame.GameLanguage.EnemyTanks}: {mafs}";
        string missionName = CampaignGlobals.LoadedCampaign.LoadedMission.Name;

        float spacingEnemyTankDisplay = DrawUtils.GetTextXOffsetForSpacing(enemyTankDisplay, spacing) / TextAnimatorLarge.CurrentScale.X;
        float spacingMissionName = DrawUtils.GetTextXOffsetForSpacing(missionName, spacing) / TextAnimatorLarge.CurrentScale.X;

        // slight misalignment

        // draw mission name, tanks remaining
        DrawUtils.DrawBorderedStringWithShadow(spriteBatch, FontGlobals.RebirthFontLarge,
            new Vector2(WindowUtils.WindowWidth / 2 - spacingMissionName.ToResolutionX(), textOffsetMissionName),
            Vector2.One,
            missionName,
            BackgroundColor,
            ColorForBorders,
            TextAnimatorLarge.CurrentScale.ToResolution(),
            1f, shadowDistScale: 1.5f, shadowAlpha: 0.5f, borderThickness: 3f, charSpacing: spacing);
        DrawUtils.DrawBorderedStringWithShadow(spriteBatch, FontGlobals.RebirthFontLarge,
            new Vector2(WindowUtils.WindowWidth / 2 - spacingEnemyTankDisplay.ToResolutionX(), textOffsetTanksLeft),
            Vector2.One,
            enemyTankDisplay,
            BackgroundColor,
            ColorForBorders,
            TextAnimatorLarge.CurrentScale.ToResolution() * 0.75f,
            1f, shadowDistScale: 1.5f, shadowAlpha: 0.5f, borderThickness: 2.5f, charSpacing: spacing);

        // draw campaign/mission data
        if (CampaignGlobals.LoadedCampaign.CurrentMissionId == 0)
            DrawUtils.DrawBorderedStringWithShadow(spriteBatch, FontGlobals.RebirthFontLarge,
                new Vector2(WindowUtils.WindowWidth / 2, textOffsetDetails),
                Vector2.One,
                $"{TankGame.GameLanguage.Campaign}: \"{CampaignGlobals.LoadedCampaign.MetaData.Name}\" ({TankGame.GameLanguage.Mission} #{CampaignGlobals.LoadedCampaign.CurrentMissionId + 1})",
                BackgroundColor,
                ColorForBorders,
                TextAnimatorSmall.CurrentScale.ToResolution(),
                1f, shadowDistScale: 1.5f, shadowAlpha: 0.5f, borderThickness: 1.5f);
        else
            DrawUtils.DrawBorderedStringWithShadow(spriteBatch, FontGlobals.RebirthFontLarge,
                new Vector2(WindowUtils.WindowWidth / 2, textOffsetDetails),
                Vector2.One,
                $"{TankGame.GameLanguage.Mission} #{CampaignGlobals.LoadedCampaign.CurrentMissionId + 1}",
                BackgroundColor,
                ColorForBorders,
                TextAnimatorSmall.CurrentScale.ToResolution(),
                1f, shadowDistScale: 1.5f, shadowAlpha: 0.5f, borderThickness: 1.5f);

        spriteBatch.End();

        #endregion

        #region RenderToBonusBanner

        device.SetRenderTarget(BonusBannerBuffer);

        device.Clear(RenderGlobals.BackBufferColor);

        // magical ass calculations
        // idk how to center it...
        _renderBeginX = BonusBannerBuffer.Width * _renderMargin; // / _bannerScale;
        _renderEndX = BonusBannerBuffer.Width * (1f - _renderMargin); // / _bannerScale);

        spriteBatch.Begin(rasterizerState: RenderGlobals.DefaultRasterizer, blendState: BlendState.NonPremultiplied);

        DrawBonusLifeBanner(spriteBatch);

        spriteBatch.End();

        #endregion

        #region RenderToBonusBannerText

        device.SetRenderTarget(BonusBannerTextBuffer);

        device.Clear(RenderGlobals.BackBufferColor);

        spriteBatch.Begin(rasterizerState: RenderGlobals.DefaultRasterizer, blendState: BlendState.NonPremultiplied);

        // slight offset to make the top color pop more...?
        // TODO: make the animation that makes the player tank graphic grow and glow, as well as the banner
        BonusTextGradient.Center = 0.6f;

        var gradientModifiedTopColor = ColorUtils.ChangeColorBrightness(GradientTopColor, brightness);
        var gradientModifiedBottomColor = ColorUtils.ChangeColorBrightness(GradientBottomColor, brightness);

        BonusTextGradient.Top = gradientModifiedTopColor;
        BonusTextGradient.Bottom = gradientModifiedBottomColor;

        spriteBatch.DrawString(FontGlobals.RebirthFontLarge, TankGame.GameLanguage.BonusTank, 
            new Vector2(BonusBannerTextBuffer.Width / 2, BonusBannerTextBuffer.Height / 2), 
            Color.White, (Vector2.One * _bannerScale * 0.5f * BonusLifeAnimator.CurrentScale).ToResolution(), 
            origin: Anchor.Center.GetAnchor(FontGlobals.RebirthFontLarge.MeasureString(TankGame.GameLanguage.BonusTank)));

        spriteBatch.End();

        device.SetRenderTarget(null);

        #endregion

        // maximum value should be 40% of window height
        _renderY = _forceBonusDrawToHeight ? WindowUtils.WindowHeight * 0.4f : BonusLifeAnimator.CurrentPosition2D.Y;
    }

    static float _renderMargin = 0.33f;
    static float _renderBeginX, _renderEndX;

    static float _renderY;
    static float _bannerScale = 2f;

    static OggAudio _startingFanfare;
    static OggAudio _bonusLifeGet;
    static OggAudio _snareDrums;

    /// <summary>Renders the intermission.</summary>
    public static void Draw(SpriteBatch spriteBatch) {
        if (Alpha > 0f) {
            // ChatSystem.SendMessage(IsAwaitingNewMission.ToString());
            spriteBatch.Begin(rasterizerState: RenderGlobals.DefaultRasterizer);
            spriteBatch.Draw(BackgroundBuffer, Vector2.Zero, Color.White * Alpha);

            // manage banner alpha
            if (ShouldDrawBanner) {
                BannerAlpha += ALPHA_FADE_CONSTANT * RuntimeData.DeltaTime;
                if (BannerAlpha > 1) BannerAlpha = 1;
            }
            else {
                BannerAlpha = 0f;
            }

            if (ShouldDrawBonusBanner) {
                BonusBannerAlpha = 1f;
            }
            else {
                BonusBannerAlpha -= ALPHA_FADE_CONSTANT * 2 * RuntimeData.DeltaTime;
                if (BonusBannerAlpha < 0) BonusBannerAlpha = 0;
            }

            // draw the banner only if we want it to
            // fading out will not happen, only fading in
            if (ShouldDrawBanner) {
                var bannerPos = WindowUtils.WindowBottomLeft * 0.16f;
                DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, BannerBuffer, bannerPos, Vector2.UnitY, Color.White,
                    Vector2.One, BannerAlpha * Alpha, Anchor.TopLeft, shadowDistScale: 1.5f, shadowAlpha: 0.5f);
                spriteBatch.Draw(BannerBuffer, WindowUtils.WindowBottomLeft * 0.16f, Color.White * Alpha * BonusBannerAlpha);
            }

            // render the bonus life banner now
            // window width / 2
            var drawPos = new Vector2(WindowUtils.WindowWidth / 2, _renderY);

            DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, BonusBannerBuffer, drawPos, 
                Vector2.UnitY, BonusBannerColor, Vector2.One * _bannerScale, BonusBannerAlpha * Alpha, Anchor.Center, shadowDistScale: 1f.ToResolutionY(), shadowAlpha: 0.5f);

            // DrawUtils.DrawBox(drawPos, drawPos + BonusBannerBuffer.Size(), Color.Blue, Anchor.Center.GetAnchor(BonusBannerBuffer.Size()));

            spriteBatch.End();

            if (ShouldDrawBonusBanner) {
                var bannerDims = BonusBannerBuffer.Size();
                var textOffset = 20f;
                // draw strictly the bonus text shadow and border here
                spriteBatch.Begin();
                var textPos = new Vector2(WindowUtils.WindowWidth / 2, _renderY - 20.ToResolutionY());
                DrawUtils.DrawStringShadowOnly(spriteBatch, FontGlobals.RebirthFontLarge, textPos, Vector2.One * _bannerScale,
                    TankGame.GameLanguage.BonusTank, (Vector2.One * _bannerScale * 0.5f * BonusLifeAnimator.CurrentScale).ToResolution(), BonusBannerAlpha * Alpha, shadowAlpha: 0.5f);
                DrawUtils.DrawStringBorderOnly(spriteBatch, FontGlobals.RebirthFontLarge, TankGame.GameLanguage.BonusTank, 
                    textPos,
                    BonusBannerTextBorderColor * Alpha * BonusBannerAlpha, (Vector2.One * _bannerScale * 0.5f * BonusLifeAnimator.CurrentScale).ToResolution(), 0f, borderThickness: 1.5f);
                spriteBatch.End();

                // draw the inner text with the gradient
                spriteBatch.Begin(blendState: BlendState.AlphaBlend, effect: BonusTextGradient);
                BonusTextGradient.Opacity = Alpha * BonusBannerAlpha;
                // fuck these mental calculations yo... 
                // in the RT prepration i have to add half of the buffer height and then subtract half of it here
                var textRTOrigin = Anchor.Center.GetAnchor(BonusBannerTextBuffer.Size());

                spriteBatch.Draw(BonusBannerTextBuffer, textPos, null, Color.White * BonusBannerAlpha,
                    0f, textRTOrigin, Vector2.One, default, 0f);

                spriteBatch.End();

                spriteBatch.Begin(blendState: BlendState.NonPremultiplied);
                // now draw the stars. draw them not within the rendertarget beacuse they need their own shadows

                var starScale = 2f;

                // left star
                DrawUtils.DrawTextureWithShadow(spriteBatch, BonusBannerStar, new Vector2(WindowUtils.WindowWidth * 0.2f, _renderY), Vector2.UnitY, GradientTopColor, new Vector2(starScale).ToResolution(),
                    Alpha * BonusBannerAlpha, shadowDistScale: 0.5f, shadowAlpha: 0.5f);

                // right star
                DrawUtils.DrawTextureWithShadow(spriteBatch, BonusBannerStar, new Vector2(WindowUtils.WindowWidth * 0.8f, _renderY), Vector2.UnitY, GradientTopColor, new Vector2(starScale).ToResolution(),
                    Alpha * BonusBannerAlpha, shadowDistScale: 0.5f, shadowAlpha: 0.5f);

                spriteBatch.End();
            }
        } else {
            _offset = Vector2.Zero;
        }

        // manage fading in & out

        if (GameUI.Paused) return;

        if (ShouldFade && ShouldFadeIn)
            IntermissionSystem.TickAlpha(ALPHA_FADE_CONSTANT * RuntimeData.DeltaTime);
        if (ShouldFade && !ShouldFadeIn)
            IntermissionSystem.TickAlpha(-ALPHA_FADE_CONSTANT * RuntimeData.DeltaTime);
    }
    public static void PlayOpeningFanfare() {
        var missionStarting = "Assets/music/fanfares/mission_starting.ogg";
        _startingFanfare = SoundPlayer.PlaySoundInstance(missionStarting, SoundContext.Effect, 0.8f);
    }
    public static void DrawBlack(SpriteBatch spriteBatch) {
        BlackAlpha = MathHelper.Clamp(BlackAlpha, 0f, 1f);
        spriteBatch.Draw(
            TextureGlobals.Pixels[Color.White],
            new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight),
            Color.Black * BlackAlpha);
    }
    private static void DrawBonusLifeBanner(SpriteBatch spriteBatch) {
        var baseDims = BonusBannerBase.Size();
        var resY = 1f.ToResolutionY();
        var origin = Anchor.BottomCenter.GetAnchor(baseDims);
        // draw the left segment
        spriteBatch.Draw(BonusBannerBase, new Vector2(_renderBeginX, baseDims.Y), null, Color.White, 0f,
            Anchor.BottomRight.GetAnchor(baseDims), Vector2.One.ToResolution(), default, 0f);
        spriteBatch.Draw(BonusBannerBase, new Vector2(_renderBeginX, baseDims.Y), null, Color.White, 0f,
            Anchor.TopRight.GetAnchor(baseDims), Vector2.One.ToResolution(), SpriteEffects.FlipVertically, 0f);
        // draw the middle segment
        spriteBatch.Draw(BonusBannerBase, new Vector2(_renderBeginX, baseDims.Y), _cutForLength, Color.White, 0f,
            Vector2.Zero, new Vector2(_renderEndX - _renderBeginX, resY), SpriteEffects.FlipVertically, 0f);
        spriteBatch.Draw(BonusBannerBase, new Vector2(_renderBeginX, baseDims.Y), _cutForLength, Color.White,0f,
            Anchor.BottomLeft.GetAnchor(new Vector2(1, baseDims.Y)), new Vector2(_renderEndX - _renderBeginX, resY), default, 0f);

        // Anchor.BottomRight.GetAnchor(new Vector2(1, baseDims.Y))
        // draw the right segment
        spriteBatch.Draw(BonusBannerBase, new Vector2(_renderEndX, baseDims.Y), null, Color.White, 0f,
            Anchor.BottomLeft.GetAnchor(baseDims), Vector2.One.ToResolution(), SpriteEffects.FlipHorizontally, 0f);
        spriteBatch.Draw(BonusBannerBase, new Vector2(_renderEndX, baseDims.Y), null, Color.White, 0f,
            Vector2.Zero, Vector2.One.ToResolution(), SpriteEffects.FlipVertically | SpriteEffects.FlipHorizontally, 0f);
    }

    private static void DrawStripe(SpriteBatch spriteBatch, Color color, float offsetY, float alpha) {
        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/banner");

        var scaling = new Vector2(3.25f, 3f);

        spriteBatch.Draw(tex, new Vector2(-12, offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
        spriteBatch.Draw(tex, new Vector2(WindowUtils.WindowWidth / 2, offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
    }

    public static void BeginOperation(float time) {
        IntermissionAnimator?.Restart();
        IntermissionAnimator?.Run();
    }

    public static void TickAlpha(float delta) {
        if (Alpha + delta < 0)
            Alpha = 0;
        else if (Alpha + delta > 1)
            Alpha = 1;
        else
            Alpha += delta;
    }

    public static void ReplayTextAnimations() {
        TextAnimatorLarge?.Restart();
        TextAnimatorSmall?.Restart();
        TextAnimatorLarge?.Run();
        TextAnimatorSmall?.Run();
    }
}