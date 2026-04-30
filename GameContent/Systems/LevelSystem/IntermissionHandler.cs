using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.LevelSystem;
public static class IntermissionHandler {
    public const int DEF_INTERMISSION_TIME = 600;
    public const int DEF_PLUSLIFE_TIME = 240;
    public static Animator ThirdPersonTransition;

    public static Animator[] PopupAnimators;

    private static bool _wasOverhead;
    private static bool _wasInMission;

    public const int TANK_FUNC_WAIT_MAX = 190;
    /// <summary>The time (in ticks) the game waits before initiating the mission after the intermission screen is finished.</summary>
    public static float TankFunctionWait = TANK_FUNC_WAIT_MAX;
    private static float _oldWait;

    public static MissionEndContext LastResult = (MissionEndContext)(-1);

    // bool major = (if true, play M100 fanfare, else M20)
    public static void DoEndMissionWorkload(int delay, MissionEndContext context) {
        TankMusicSystem.StopAll();

        // Server.SyncSeeds();

        LastResult = context;

        // initialize animators
        //PopupAnimators = new Animator[CampaignGlobals.DeltaMissionStats.NumStatsWithDelta];

        //if (result1up && context != MissionEndContext.Lose)
        //delay += 200;

        if (context == MissionEndContext.CampaignCompleteMajor) {
            TankGame.SaveFile.CampaignsCompleted++;
            string victory = "Assets/music/fanfares/mission_complete_M100.ogg";
            SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        else if (context == MissionEndContext.CampaignCompleteMinor) {
            TankGame.SaveFile.CampaignsCompleted++;
            var victory = "Assets/music/fanfares/mission_complete_M20.ogg";
            SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        if (context == MissionEndContext.Win) {
            TankGame.SaveFile.MissionsCompleted++;
        }
        if (!Client.IsConnected()) {
            if (context == MissionEndContext.Lose) {
                var deathSound = "Assets/music/fanfares/tank_player_death.ogg";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }
            else if (context == MissionEndContext.GameOver) {
                //PlayerTank.AddLives(-1);

                var deathSound = "Assets/music/fanfares/gameover_playerdeath.ogg";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }
        }
        else {
            if (context == MissionEndContext.Lose) {
                // PlayerTank.AddLives(-1);

                var deathSound = "Assets/music/fanfares/tank_player_death.ogg";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }
            /*if (PlayerTank.Lives.All(x => x == 0))
            {
                var deathSound = "Assets/fanfares/gameover_playerdeath";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }*/
        }
        if (context == MissionEndContext.Win) {
            TankGame.SaveFile.MissionsCompleted++;
            CampaignGlobals.LoadedCampaign.LoadNextMission();
            // hijack the next mission if random tanks is enabled.
            // IntermissionSystem.cs line 89 contains when the next mission is actually set-up.
            if (Modifiers.Map[Modifiers.RANDOM_ENEMY])
                CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks
                            = Modifiers.HijackTanks(CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks);
            SoundPlayer.PlaySoundInstance("Assets/music/fanfares/mission_complete.ogg", SoundContext.Effect, 0.5f);
            if (Speedrun.CurrentSpeedrun is not null) {
                if (CampaignGlobals.LoadedCampaign.CurrentMissionId > 1) {
                    var prevTime = Speedrun.CurrentSpeedrun.MissionTimes.ElementAt(CampaignGlobals.LoadedCampaign.CurrentMissionId - 2).Value; // previous mission time.
                    var realTime = Speedrun.CurrentSpeedrun.Timer.Elapsed - prevTime.Item1; // current total time - previous total time
                    Speedrun.CurrentSpeedrun.MissionTimes[CampaignGlobals.LoadedCampaign.CurrentMission.Name] = (Speedrun.CurrentSpeedrun.Timer.Elapsed, realTime);
                }
                else
                    Speedrun.CurrentSpeedrun.MissionTimes[CampaignGlobals.LoadedCampaign.CurrentMission.Name] = (Speedrun.CurrentSpeedrun.Timer.Elapsed, Speedrun.CurrentSpeedrun.Timer.Elapsed);
            }
        }

        if (CampaignCompleteUI.FanfaresAndDurations.TryGetValue(context, out (OggAudio, TimeSpan) value)) {
            value.Item1.Instance?.Play();
            value.Item1.Instance!.Volume = TankGame.Settings.MusicVolume;
            SceneManager.DoEndScene(value.Item2, context);
        }
        else
            IntermissionSystem.BeginOperation(delay);
    }
    public static void PrepareIntermission(bool victory) {
        IntermissionSystem.IsAwaitingNewMission = true;
        CampaignGlobals.InMission = false;

        foreach (var tank in GameHandler.AllTanks) {
            if (tank is null) continue;
            tank.Velocity = Vector2.Zero;
            tank.Physics.LinearVelocity = Vector2.Zero;
        }

        if (!CampaignGlobals.InMission && _wasInMission) {
            bool isExtraLifeMission = CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].GrantsExtraLife;
            int restartTime;
            MissionEndContext endContext;

            IntermissionSystem.InitializeCountdowns(isExtraLifeMission && victory);
            if (victory) {
                restartTime = DEF_INTERMISSION_TIME;

                if (isExtraLifeMission) {
                    restartTime += DEF_PLUSLIFE_TIME;
                    IntermissionSystem.ShouldDrawBanner = false;
                }

                endContext = MissionEndContext.Win;

                if (CampaignGlobals.LoadedCampaign.CurrentMissionId >= CampaignGlobals.LoadedCampaign.CachedMissions.Length - 1)
                    endContext = CampaignGlobals.LoadedCampaign.MetaData.HasMajorVictory ?
                        MissionEndContext.CampaignCompleteMajor : MissionEndContext.CampaignCompleteMinor;
            }
            else {
                restartTime = DEF_INTERMISSION_TIME;

                // we check <= 1 since the lives haven't actually been deducted yet.

                /*if (Client.IsConnected()) {
                    for (int i = 0; i < PlayerTank.Lives.Length; i++) {
                        if (i >= Server.CurrentClientCount) {
                            PlayerTank.Lives[i] = 0;
                        }
                    }
                }*/

                // we want to move on anyway even if one player 'lost' and another 'won' so other players aren't held back while others advance (desync alert!!)
                var allPlayersDead = !GameHandler.AllPlayerTanks.Any(tnk => tnk != null && !tnk.IsDestroyed);

                // assume true, but set to false later if any player has lives remaining
                bool everyoneLostAllLives = true;

                if (allPlayersDead) {
                    // networking is *consistently* behind the host here
                    var clientConnected = Client.IsConnected();
                    for (int i = 0; i < GameHandler.AllPlayerTanks.Length; i++) {
                        var tank = GameHandler.AllPlayerTanks[i];
                        if (tank is null) continue;
                        var lives = PlayerTank.Lives[i];
                        int livesCountLocal = lives;
                        if (tank.IsDestroyed) {
                            if (clientConnected) {
                                livesCountLocal = i != NetPlay.GetMyClientId() ? lives - 1 : lives;
                            }
                        }
                        // if any player has any lives remaining, the campaign isn't over
                        if (livesCountLocal > 0)
                            everyoneLostAllLives = false;
                    }
                }

                if (allPlayersDead) {
                    if (everyoneLostAllLives)
                        endContext = MissionEndContext.GameOver;
                    else
                        endContext = MissionEndContext.Lose;
                }
                else
                    endContext = MissionEndContext.Win;

                // hardcode hell 2: electric boogaloo
                if (Modifiers.Map[Modifiers.INF_LIFE])
                    endContext = MissionEndContext.Lose;
            }
            CampaignGlobals.MissionEndEvent_Invoke(restartTime, endContext);
        }
    }
    /// <summary>This marks the beginning of the player seeing all of the tanks on the map, before the round begins.</summary>
    public static void BeginIntroSequence() {
        TankFunctionWait = TANK_FUNC_WAIT_MAX;

        TankMusicSystem.StopAll();
        IntermissionSystem.SetMusic();

        foreach (var tank in GameHandler.AllTanks)
            if (tank is not null)
                tank.Velocity = Vector2.Zero;

        SceneManager.CleanupScene();

        CampaignGlobals.InMission = false;
    }

    public static void Update() {
        if (TankFunctionWait > 0)
            TankFunctionWait -= RuntimeData.DeltaTime;
        if (TankFunctionWait <= 0 && _oldWait > 0 && !MainMenuUI.IsActive) {
            // FIXME: maybe causes issues since the mission is 1 tick from starting?
            // TODO: move this to the animator?
            if (!CampaignGlobals.InMission) {
                CampaignGlobals.InMission = true;
                CampaignGlobals.DoMissionStartInvoke();

                // if for some reason we return back to a mission with no enemies left
                CampaignProgression.CheckMissionCompletion();
                TankMusicSystem.PlayAll();
            }
        }
        DebugManager.UpdateDebug();

        if (MainMenuUI.IsActive) {
            PlayerTank.KillCounts[NetPlay.GetMyClientId()] = 0;
            // don't know if this fucks with the stack or not. to be determined.
            PlayerTank.PlayerStatistics = default;
        }

        if (!CameraGlobals.OverheadView && _wasOverhead && !LevelEditorUI.IsActive)
            BeginIntroSequence();

        _wasOverhead = CameraGlobals.OverheadView;
        _wasInMission = CampaignGlobals.InMission;
        _oldWait = TankFunctionWait;
    }

    public static string PrepareDisplay = string.Empty;

    public static Animator CountdownAnimator = null;

    readonly static EasingFunction _ez = EasingFunction.InOutQuad;
    public static void Initialize() {
        CountdownAnimator = Animator.Create()
                    .WithFrame(new(scale: Vector3.One * 2)) // ready 
                    .WithFrame(new(scale: Vector3.One * 1, duration: TimeSpan.FromSeconds(1.5), easing: _ez))  // ready 
                    .WithFrame(new(scale: Vector3.One * 2, duration: TimeSpan.FromSeconds(0), easing: _ez))  // set
                    .WithFrame(new(scale: Vector3.One * 1, duration: TimeSpan.FromSeconds(1.5), easing: _ez))  // set
                    .WithFrame(new(scale: Vector3.One * 2, duration: TimeSpan.FromSeconds(0), easing: _ez))  // start 
                    .WithFrame(new(scale: new Vector3(2, 0, 0), duration: TimeSpan.FromSeconds(1), easing: _ez));
        CountdownAnimator.OnKeyFrameFinish += CountdownAnimator_OnKeyFrameFinish;
    }
    // ported
    static void CountdownAnimator_OnKeyFrameFinish(int frameIndex) {
        // just started, also localize
        if (frameIndex == -1) {
            PrepareDisplay = TankGame.GameLanguage.Gameplay.MissionReady;
        }
        if (frameIndex == 1) {
            PrepareDisplay = TankGame.GameLanguage.Gameplay.MissionSet;
        }
        else if (frameIndex == 3) {
            PrepareDisplay = TankGame.GameLanguage.Gameplay.MissionStart;
        }
    }

    public static void RenderCountdownGraphics() {
        if (!MainMenuUI.IsActive && !CameraGlobals.OverheadView && !LevelEditorUI.IsActive) {
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, PrepareDisplay,
                new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 3),
                IntermissionSystem.BackgroundColor, IntermissionSystem.BannerColor, CountdownAnimator.CurrentScale.Flatten().ToResolution(), 0f, Anchor.Center, CountdownAnimator.CurrentScale.Length());
        }
    }
}