using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;

namespace TanksRebirth.GameContent.Systems;
public static class IntermissionHandler {
    public static Animator ThirdPersonTransitionAnimation;

    public static Animator[] PopupAnimators;

    private static bool _wasOverhead;
    private static bool _wasInMission;

    public const int TANK_FUNC_WAIT_MAX = 190;
    /// <summary>The time (in ticks) the game waits before initiating the mission after the intermission screen is finished.</summary>
    public static float TankFunctionWait = TANK_FUNC_WAIT_MAX;
    private static float _oldWait;

    public static MissionEndContext LastResult = (MissionEndContext)(-1);
    public static void DoEndMissionWorkload(int delay, MissionEndContext context, bool result1up) // bool major = (if true, play M100 fanfare, else M20)
    {
        TankMusicSystem.StopAll();

        // Server.SyncSeeds();

        LastResult = context;

        // initialize animators
        //PopupAnimators = new Animator[CampaignGlobals.DeltaMissionStats.NumStatsWithDelta];

        //if (result1up && context != MissionEndContext.Lose)
        //delay += 200;

        if (context == MissionEndContext.CampaignCompleteMajor) {
            TankGame.GameData.CampaignsCompleted++;
            string victory = "Assets/music/fanfares/mission_complete_M100.ogg";
            SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        else if (context == MissionEndContext.CampaignCompleteMinor) {
            TankGame.GameData.CampaignsCompleted++;
            var victory = "Assets/music/fanfares/mission_complete_M20.ogg";
            SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        if (result1up && context == MissionEndContext.Win) {
            TankGame.GameData.MissionsCompleted++;
            PlayerTank.AddLives(1);
            var lifeget = "Assets/music/fanfares/life_get.ogg";
            SoundPlayer.PlaySoundInstance(lifeget, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        if (!Client.IsConnected()) {
            if (context == MissionEndContext.Lose) {
                // hardcode hell
                /*if (!Difficulties.Types["InfiniteLives"])
                    PlayerTank.AddLives(-1);*/

                // what is this comment?
                /*int len = $"{VanillaCampaign.CachedMissions.Count(x => !string.IsNullOrEmpty(x.Name))}".Length;
                int diff = len - $"{VanillaCampaign.CurrentMissionId}".Length;

                string realName = "";

                for (int i = 0; i < diff; i++)
                    realName += "0";
                realName += $"{VanillaCampaign.CurrentMissionId + 1}";

                VanillaCampaign.CachedMissions[VanillaCampaign.CurrentMissionId] = Mission.Load(realName, VanillaCampaign.Name);*/
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
            TankGame.GameData.MissionsCompleted++;
            CampaignGlobals.LoadedCampaign.LoadNextMission();
            // hijack the next mission if random tanks is enabled.
            // IntermissionSystem.cs line 89 contains when the next mission is actually set-up.
            if (Difficulties.Types["RandomizedTanks"])
                CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks
                            = Difficulties.HijackTanks(CampaignGlobals.LoadedCampaign.CachedMissions[CampaignGlobals.LoadedCampaign.CurrentMissionId].Tanks);
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

        if (CampaignCompleteUI.FanfaresAndDurations.ContainsKey(context)) {
            CampaignCompleteUI.FanfaresAndDurations[context].Item1.Instance?.Play();
            CampaignCompleteUI.FanfaresAndDurations[context].Item1.Instance.Volume = TankGame.Settings.MusicVolume;
            SceneManager.DoEndScene(CampaignCompleteUI.FanfaresAndDurations[context].Item2, context);
        }
        else
            IntermissionSystem.BeginOperation(delay);
    }
    /// <summary>
    /// A method that returns whether or not there was a victory- be it for the enemy or the player.
    /// </summary>
    /// <param name="mission">The mission to check.</param>
    /// <param name="victory">Whether or not it resulted in victory for the player.</param>
    /// <returns>Whether or not one team or one player dominates the map.</returns>
    public static bool NothingCanHappenAnymore(Mission mission, out bool victory) {
        if (mission.Tanks is null) {
            victory = false;
            return false;
        }
        if (mission.Tanks.Any(tnk => tnk.IsPlayer)) {
            var activeTeams = Tank.GetActiveTeams();

            if (activeTeams.Contains(TeamID.NoTeam) && GameHandler.AllTanks.Count(tnk => tnk != null && !tnk.Dead) <= 1) {
                victory = GameHandler.AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead);
                return true;
            }
            // check if it's not only FFA, and if teams left doesnt contain ffa. 
            else if (!activeTeams.Contains(TeamID.NoTeam) && activeTeams.Length <= 1) {
                victory = activeTeams.Contains(PlayerTank.MyTeam);
                return true;
            }
        }
        else {
            var activeTeams = Tank.GetActiveTeams();
            // if a player was not initially spawned in the mission, check if a team is still alive and end the mission
            if (activeTeams.Contains(TeamID.NoTeam) && GameHandler.AllTanks.Count(tnk => tnk != null && !tnk.Dead) <= 1) {
                victory = true;
                return true;
            }
            else if (!activeTeams.Contains(TeamID.NoTeam) && activeTeams.Length <= 1) {
                victory = true;
                return true;
            }
        }
        victory = false;
        return false;
    }
    public static void HandleMissionChanging() {
        if (CampaignGlobals.LoadedCampaign.CachedMissions[0].Name is null)
            return;

        var nothingAnymore = NothingCanHappenAnymore(CampaignGlobals.LoadedCampaign.CurrentMission, out bool victory);

        if (nothingAnymore) {
            CampaignGlobals.InMission = false;
            if (!CampaignGlobals.InMission && _wasInMission) {
                IntermissionSystem.InitializeCountdowns();
                bool isExtraLifeMission = CampaignGlobals.LoadedCampaign.MetaData.ExtraLivesMissions.Contains(CampaignGlobals.LoadedCampaign.CurrentMissionId + 1);
                int restartTime;
                MissionEndContext endContext;
                if (victory) {
                    restartTime = 600;
                    //if (isExtraLifeMission)
                    //restartTime += 200;

                    endContext = MissionEndContext.Win;

                    if (CampaignGlobals.LoadedCampaign.CurrentMissionId >= CampaignGlobals.LoadedCampaign.CachedMissions.Length - 1)
                        endContext = CampaignGlobals.LoadedCampaign.MetaData.HasMajorVictory ? MissionEndContext.CampaignCompleteMajor : MissionEndContext.CampaignCompleteMinor;
                }
                else {
                    restartTime = 600;

                    // TODO: if a 1-up mission, extend by X amount of time (TBD?)
                    // we check <= 1 since the lives haven't actually been deducted yet.

                    if (Client.IsConnected()) {
                        for (int i = 0; i < PlayerTank.Lives.Length; i++) {
                            if (i >= Server.CurrentClientCount) {
                                PlayerTank.Lives[i] = 0;
                            }
                        }
                    }
                    // doesnt work on 1 client
                    bool check = Client.IsConnected() ? PlayerTank.Lives.All(x => x == 0) : PlayerTank.GetMyLives() <= 0;

                    // why is "win" a ternary result? it will never be "win" given the we are in the "!victory" branch. wtf ryan code once again
                    endContext = !GameHandler.AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead) ? (check ? MissionEndContext.GameOver : MissionEndContext.Lose) : MissionEndContext.Win;

                    // hardcode hell 2: electric boogaloo
                    if (Difficulties.Types["InfiniteLives"])
                        endContext = MissionEndContext.Lose;
                }
                CampaignGlobals.MissionEndEvent_Invoke(restartTime, endContext, isExtraLifeMission);
            }
        }
        if (IntermissionSystem.CurrentWaitTime > 0)
            IntermissionSystem.Tick(TankGame.DeltaTime);

        if (IntermissionSystem.CurrentWaitTime > 240 && IntermissionSystem.CurrentWaitTime < 450) {
            // this hardcode makes me want to commit neck rope
            // boolean is changed within the scope of the check so we check again. weird.
            IntermissionSystem.TickAlpha(1f / 60f * TankGame.DeltaTime);
            // ^ when the mission info popup starts to appear
        }
        else
            IntermissionSystem.TickAlpha(-1f / 45f * TankGame.DeltaTime);
    }
    /// <summary>This marks the beginning of the player seeing all of the tanks on the map, before the round begins.</summary>
    public static void BeginIntroSequence() {
        TankFunctionWait = TANK_FUNC_WAIT_MAX;

        TankMusicSystem.StopAll();

        var tune = "Assets/music/fanfares/mission_snare.ogg";

        SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);

        foreach (var tank in GameHandler.AllTanks)
            if (tank is not null)
                tank.Velocity = Vector2.Zero;

        SceneManager.CleanupScene();

        CampaignGlobals.InMission = false;
    }

    public static void Update() {
        if (TankFunctionWait > 0)
            TankFunctionWait -= TankGame.DeltaTime;
        if (TankFunctionWait <= 0 && _oldWait > 0 && !MainMenuUI.Active) {
            // FIXME: maybe causes issues since the mission is 1 tick from starting?
            // TODO: move this to the animator?
            if (!CampaignGlobals.InMission) {
                CampaignGlobals.InMission = true;
                CampaignGlobals.DoMissionStartInvoke();
                TankMusicSystem.PlayAll();
            }
        }
        DebugManager.UpdateDebug();

        if (MainMenuUI.Active) {
            PlayerTank.KillCount = 0;
            // don't know if this fucks with the stack or not. to be determined.
            PlayerTank.PlayerStatistics = default;
        }

        if (!TankGame.OverheadView && _wasOverhead && !LevelEditorUI.Active)
            BeginIntroSequence();

        _wasOverhead = TankGame.OverheadView;
        _wasInMission = CampaignGlobals.InMission;
        _oldWait = TankFunctionWait;
    }

    public static string PrepareDisplay = string.Empty;

    public static Animator CountdownAnimator = null;

    private static EasingFunction _ez = EasingFunction.InOutQuad;
    public static void Initialize() {
        CountdownAnimator = Animator.Create()
            // id = 0
            .WithFrame(new(scale: Vector2.One * 2, duration: TimeSpan.FromSeconds(1.5), easing: _ez))  // ready 
            .WithFrame(new(scale: Vector2.One * 1, duration: TimeSpan.FromSeconds(0), easing: _ez))  // ready 
            .WithFrame(new(scale: Vector2.One * 2, duration: TimeSpan.FromSeconds(1.5), easing: _ez))  // set
            .WithFrame(new(scale: Vector2.One * 1, duration: TimeSpan.FromSeconds(0), easing: _ez))  // set
            .WithFrame(new(scale: Vector2.One * 2, duration: TimeSpan.FromSeconds(1), easing: _ez))  // start 
            .WithFrame(new(scale: new Vector2(2, 0), duration: TimeSpan.FromSeconds(1), easing: _ez));
        CountdownAnimator.OnKeyFrameFinish += CountdownAnimator_OnKeyFrameFinish;
        CountdownAnimator.OnAnimationRun += () => PrepareDisplay = "Ready?";
    }

    private static void CountdownAnimator_OnKeyFrameFinish(KeyFrame frame) {
        var frameId = CountdownAnimator.KeyFrames.FindIndex(f => f.Equals(frame));

        if (frameId == 1) {
            PrepareDisplay = "Set...";
        }
        else if (frameId == 3) {
            PrepareDisplay = "Start!";
        }
    }

    public static void RenderCountdownGraphics() {
        if (!MainMenuUI.Active && !TankGame.OverheadView && !LevelEditorUI.Active/* && TankFunctionWait > 0*/) {
            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, TankGame.TextFontLarge, PrepareDisplay, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 3), 
                IntermissionSystem.BackgroundColor, IntermissionSystem.StripColor, CountdownAnimator.CurrentScale.ToResolution(), 0f, Anchor.Center, 3);
        }
    }
}
