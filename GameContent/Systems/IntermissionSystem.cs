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
using TanksRebirth.Internals.Common.Framework.Animation;
using System;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Internals.Common.Framework.Audio;
using System.Runtime.Intrinsics.X86;

namespace TanksRebirth.GameContent.Systems;
#pragma warning disable
public static class IntermissionSystem {
    public static Animator TextAnimatorLarge;
    public static Animator TextAnimatorSmall;

    public static Animator IntermissionAnimator;

    public static float TimeBlack; // for black screen when entering this game
    public static float BlackAlpha = 0;

    public static bool IsAwaitingNewMission => CurrentWaitTime > 240; // 3 seconds seems to be the opening fanfare duration

    public static float Alpha;
    public static float WaitTime { get; private set; }
    public static float CurrentWaitTime { get; private set; }

    public static readonly Color DefaultBackgroundColor = new(228, 231, 173); // color picked lol
    public static readonly Color DefaultStripColor = Color.DarkRed;

    public static Color BackgroundColor = new(228, 231, 173); // color picked lol
    public static Color StripColor = Color.DarkRed;

    private static Vector2 _offset;

    private static float _oldBlack;

    public static void InitializeAnmiations() {
        IntermissionHandler.Initialize();
        // should this be where the animator is re-instantiated?
        TextAnimatorSmall = Animator.Create()
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.Zero, duration: TimeSpan.FromSeconds(0.25), easing: EasingFunction.OutBack))
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.One * 0.4f, duration: TimeSpan.FromSeconds(0.25), easing: EasingFunction.OutBack));
        TextAnimatorLarge = Animator.Create()
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.Zero, duration: TimeSpan.FromSeconds(0.35), easing: EasingFunction.OutBack))
            .WithFrame(new(position2d: Vector2.Zero, scale: Vector2.One, duration: TimeSpan.FromSeconds(0.35), easing: EasingFunction.OutBack));

        IntermissionAnimator = Animator.Create()
            .WithFrame(new(duration: TimeSpan.FromSeconds(3), easing: EasingFunction.Linear))
            .WithFrame(new(duration: TimeSpan.FromSeconds(0.5), easing: EasingFunction.Linear))
            .WithFrame(new(duration: TimeSpan.FromSeconds(3.66), easing: EasingFunction.Linear))
            .WithFrame(new(duration: TimeSpan.FromSeconds(0), easing: EasingFunction.Linear));
        IntermissionAnimator.OnKeyFrameFinish += DoMidAnimationActions;
    }

    public static void InitializeCountdowns() {
        // 10 seconds to complete the entire thing
        // at 3 seconds in, the opening fanfare plays (but for some reason i gotta use 4 in this.)
        // at 220 frames left (3.66 seconds), the snare drums begin, and the scene is visible
        // at halfway through, the next mission is set-up
        float secs1 = 3;
        float secs2 = 3f;
        if (TimeBlack > 0) {
            secs1 = 4;
            secs2 = 3;
        }
        IntermissionAnimator.KeyFrames[0] = new(duration: TimeSpan.FromSeconds(secs1), easing: EasingFunction.Linear);
        IntermissionAnimator.KeyFrames[2] = new(duration: TimeSpan.FromSeconds(secs2), easing: EasingFunction.Linear);
        // the last frame is filler because i dunno how to fix the last frame finish event firing bug
        // ignore the last frame
        // TODO: fix this fuckery above
    }

    private static void DoMidAnimationActions(KeyFrame frame) {
        var frameId = IntermissionAnimator.KeyFrames.FindIndex(f => f.Equals(frame));

        if (MainMenu.Active) {
            if (frameId > 0) {
                IntermissionAnimator?.Stop(); // the player dipped during the intermission lol
                return;
            }
        }

        // play the opening fanfare n shit. xd.
        if (frameId == 0) {
            SceneManager.CleanupScene();
            var missionStarting = "Assets/fanfares/mission_starting.ogg";
            SoundPlayer.PlaySoundInstance(missionStarting, SoundContext.Effect, 0.8f);
        }
        else if (frameId == 1) {
            if (Difficulties.Types["RandomizedTanks"]) {
                if (CampaignGlobals.LoadedCampaign.CurrentMissionId == MainMenu.MissionCheckpoint && IntermissionHandler.LastResult != Enums.MissionEndContext.Lose) {
                    CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks
                        = Difficulties.HijackTanks(CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks);
                }
            }
            CampaignGlobals.LoadedCampaign.SetupLoadedMission(GameHandler.AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead));
        }
        else if (frameId == 2) {
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
        }
    }

    // TODO: turn this intermission stuff into animators, lol.
    /// <summary>Renders the intermission.</summary>
    public static void Draw(SpriteBatch spriteBatch) {
        // TankGame.Interp = Alpha <= 0 && BlackAlpha <= 0 && GameHandler.InterpCheck;

        if (TankGame.Instance.IsActive) {
            if (TimeBlack > -1) {
                TimeBlack -= TankGame.DeltaTime;
                BlackAlpha += 1f / 45f * TankGame.DeltaTime;
            }
            else
                BlackAlpha -= 1f / 45f * TankGame.DeltaTime;
        }

        if (BlackAlpha >= 1 && _oldBlack < 1) {
            MainMenu.Leave();

            if (CampaignGlobals.ShouldMissionsProgress) {
                // todo: should this happen?
                CampaignGlobals.LoadedCampaign.SetupLoadedMission(true);
            }

        }
        if (BlackAlpha <= 0.9f && _oldBlack > 0.9f) {
            TextAnimatorLarge?.Restart();
            TextAnimatorSmall?.Restart();
            TextAnimatorLarge?.Run();
            TextAnimatorSmall?.Run();
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

            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank_background_billboard");
            // draw small tank graphics using GameResources.GetGameResource
            for (int i = -padding; i < WindowUtils.WindowWidth / texWidth + padding; i++) {
                for (int j = -padding; j < WindowUtils.WindowHeight / texWidth + padding; j++) {
                    spriteBatch.Draw(tex, new Vector2(i, j) * texWidth + _offset, null, BackgroundColor * Alpha, 0f, Vector2.Zero, scale, default, default);
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
            int mafs1 = CampaignGlobals.LoadedCampaign.TrackedSpawnPoints.Count(p => p.Item2);
            int mafs2 = CampaignGlobals.LoadedCampaign.LoadedMission.Tanks.Count(x => x.IsPlayer);
            int mafs = mafs1 - mafs2; // waddafak. why is my old code so horrid.

            DrawUtils.DrawShadowedString(TankGame.TextFontLarge,
                new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 220.ToResolutionY()),
                Vector2.One,
                CampaignGlobals.LoadedCampaign.LoadedMission.Name,
                BackgroundColor,
                TextAnimatorLarge.CurrentScale.ToResolution(),
                Alpha);
            DrawUtils.DrawShadowedString(TankGame.TextFontLarge,
                new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 50.ToResolutionY()),
                Vector2.One,
                $"{TankGame.GameLanguage.EnemyTanks}: {mafs}",
                BackgroundColor,
                TextAnimatorLarge.CurrentScale.ToResolution(),
                Alpha);

            var tnk2d = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/playertank2d");

            var count = Server.CurrentClientCount > 0 ? Server.CurrentClientCount : Server.CurrentClientCount + 1;

            for (int i = 0; i < count; i++) {
                var name = Client.IsConnected() ? Server.ConnectedClients[i].Name : string.Empty;

                var pos = new Vector2(WindowUtils.WindowWidth / (count + 1) * (i + 1), WindowUtils.WindowHeight / 2 + 375.ToResolutionY());

                var lifeText = $"x  {PlayerTank.Lives[i]}";
                DrawUtils.DrawShadowedString(TankGame.TextFontLarge,
                    pos + new Vector2(75, -25).ToResolution(),
                    Vector2.One,
                    lifeText,
                    BackgroundColor,
                    Vector2.One.ToResolution(),
                    Alpha,
                    TankGame.TextFontLarge.MeasureString(lifeText) / 2);

                DrawUtils.DrawShadowedString(TankGame.TextFontLarge,
                    pos - new Vector2(0, 75).ToResolution(),
                    Vector2.One,
                    name,
                    PlayerID.PlayerTankColors[i].ToColor(),
                    new Vector2(0.3f).ToResolution(),
                    Alpha,
                    TankGame.TextFontLarge.MeasureString(name) / 2);
                DrawUtils.DrawShadowedTexture(tnk2d, pos - new Vector2(130, 0).ToResolution(), Vector2.One, PlayerID.PlayerTankColors[i].ToColor(), new Vector2(1.25f), Alpha, tnk2d.Size() / 2);
            }
            // draw mission data on the billboard (?) thing
            if (CampaignGlobals.LoadedCampaign.CurrentMissionId == 0)
                DrawUtils.DrawShadowedString(TankGame.TextFontLarge,
                    new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 295.ToResolutionY()),
                    Vector2.One,
                    $"{TankGame.GameLanguage.Campaign}: \"{CampaignGlobals.LoadedCampaign.MetaData.Name}\" ({TankGame.GameLanguage.Mission} #{CampaignGlobals.LoadedCampaign.CurrentMissionId + 1})",
                    BackgroundColor,
                    TextAnimatorSmall.CurrentScale.ToResolution(),
                    Alpha);
            else
                DrawUtils.DrawShadowedString(TankGame.TextFontLarge,
                    new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 295.ToResolutionY()),
                    Vector2.One,
                    $"{TankGame.GameLanguage.Mission} #{CampaignGlobals.LoadedCampaign.CurrentMissionId + 1}",
                    BackgroundColor,
                    TextAnimatorSmall.CurrentScale.ToResolution(),
                    Alpha);
        }
        _oldBlack = BlackAlpha;
    }

    private static void DrawBonusLifeHUD() {
        // TODO: implement.
    }

    private static void DrawStripe(SpriteBatch spriteBatch, Color color, float offsetY, float alpha) {
        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/banner");

        var scaling = new Vector2(3, 3f);

        spriteBatch.Draw(tex, new Vector2(-15, offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
        spriteBatch.Draw(tex, new Vector2(WindowUtils.WindowWidth / 2, offsetY), null, color * alpha, 0f, Vector2.Zero, scaling.ToResolution(), default, default);
    }

    public static void BeginOperation(float time) {
        WaitTime = time;
        CurrentWaitTime = time;
        IntermissionAnimator?.Restart();
        IntermissionAnimator?.Run();
    }

    public static void Tick(float delta) {
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