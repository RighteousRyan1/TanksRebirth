using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Speedrunning;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems;
public static class IntermissionHandler {
    private static bool _wasOverhead;
    private static bool _wasInMission;
    /// <summary>The time (in ticks) the game waits before initiating the mission after the intermission screen is finished.</summary>
    public static float TankFunctionWait = 190;
    private static float _oldWait;

    public static MissionEndContext LastResult = (MissionEndContext)(-1);
    public static void DoEndMissionWorkload(int delay, MissionEndContext context, bool result1up) // bool major = (if true, play M100 fanfare, else M20)
    {
        TankMusicSystem.StopAll();

        LastResult = context;

        //if (result1up && context != MissionEndContext.Lose)
        //delay += 200;

        if (context == MissionEndContext.CampaignCompleteMajor) {
            TankGame.GameData.CampaignsCompleted++;
            string victory = "Assets/fanfares/mission_complete_M100.ogg";
            SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        else if (context == MissionEndContext.CampaignCompleteMinor) {
            TankGame.GameData.CampaignsCompleted++;
            var victory = "Assets/fanfares/mission_complete_M20.ogg";
            SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        if (result1up && context == MissionEndContext.Win) {
            TankGame.GameData.MissionsCompleted++;
            PlayerTank.AddLives(1);
            var lifeget = "Assets/fanfares/life_get.ogg";
            SoundPlayer.PlaySoundInstance(lifeget, SoundContext.Effect, 0.5f, rememberMe: true);
        }
        if (!Client.IsConnected()) {
            if (context == MissionEndContext.Lose) {
                // hardcode hell
                if (!Difficulties.Types["InfiniteLives"])
                    PlayerTank.AddLives(-1);

                // what is this comment?
                /*int len = $"{VanillaCampaign.CachedMissions.Count(x => !string.IsNullOrEmpty(x.Name))}".Length;
                int diff = len - $"{VanillaCampaign.CurrentMissionId}".Length;

                string realName = "";

                for (int i = 0; i < diff; i++)
                    realName += "0";
                realName += $"{VanillaCampaign.CurrentMissionId + 1}";

                VanillaCampaign.CachedMissions[VanillaCampaign.CurrentMissionId] = Mission.Load(realName, VanillaCampaign.Name);*/
                var deathSound = "Assets/fanfares/tank_player_death.ogg";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }
            else if (context == MissionEndContext.GameOver) {
                //PlayerTank.AddLives(-1);

                var deathSound = "Assets/fanfares/gameover_playerdeath.ogg";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }
        }
        else {
            if (context == MissionEndContext.Lose) {
                // PlayerTank.AddLives(-1);

                var deathSound = "Assets/fanfares/tank_player_death.ogg";
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
            GameProperties.LoadedCampaign.LoadNextMission();
            // hijack the next mission if random tanks is enabled.
            // IntermissionSystem.cs line 89 contains when the next mission is actually set-up.
            GameProperties.LoadedCampaign.CachedMissions[GameProperties.LoadedCampaign.CurrentMissionId].Tanks
                        = Difficulties.HijackTanks(GameProperties.LoadedCampaign.CachedMissions[GameProperties.LoadedCampaign.CurrentMissionId].Tanks);
            SoundPlayer.PlaySoundInstance("Assets/fanfares/mission_complete.ogg", SoundContext.Effect, 0.5f);
            if (Speedrun.CurrentSpeedrun is not null) {
                if (GameProperties.LoadedCampaign.CurrentMissionId > 1) {
                    var prevTime = Speedrun.CurrentSpeedrun.MissionTimes.ElementAt(GameProperties.LoadedCampaign.CurrentMissionId - 2).Value; // previous mission time.
                    var realTime = Speedrun.CurrentSpeedrun.Timer.Elapsed - prevTime.Item1; // current total time - previous total time
                    Speedrun.CurrentSpeedrun.MissionTimes[GameProperties.LoadedCampaign.CurrentMission.Name] = (Speedrun.CurrentSpeedrun.Timer.Elapsed, realTime);
                }
                else
                    Speedrun.CurrentSpeedrun.MissionTimes[GameProperties.LoadedCampaign.CurrentMission.Name] = (Speedrun.CurrentSpeedrun.Timer.Elapsed, Speedrun.CurrentSpeedrun.Timer.Elapsed);
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
        if (GameProperties.LoadedCampaign.CachedMissions[0].Name is null)
            return;

        var nothingAnymore = NothingCanHappenAnymore(GameProperties.LoadedCampaign.CurrentMission, out bool victory);

        if (nothingAnymore) {
            GameProperties.InMission = false;
            if (!GameProperties.InMission && _wasInMission) {
                IntermissionSystem.InitializeCountdowns();
                bool isExtraLifeMission = GameProperties.LoadedCampaign.MetaData.ExtraLivesMissions.Contains(GameProperties.LoadedCampaign.CurrentMissionId + 1);
                if (victory) {
                    int restartTime = 600;
                    //if (isExtraLifeMission)
                    //restartTime += 200;

                    var cxt = MissionEndContext.Win;

                    if (GameProperties.LoadedCampaign.CurrentMissionId >= GameProperties.LoadedCampaign.CachedMissions.Length - 1)
                        cxt = GameProperties.LoadedCampaign.MetaData.HasMajorVictory ? MissionEndContext.CampaignCompleteMajor : MissionEndContext.CampaignCompleteMinor;

                    GameProperties.MissionEndEvent_Invoke(restartTime, cxt, isExtraLifeMission);
                }
                else {
                    int restartTime = 600;

                    // if a 1-up mission, extend by X amount of time (TBD?)
                    // we check <= 1 since the lives haven't actually been deducted yet.

                    if (Client.IsConnected()) {
                        for (int i = 0; i < PlayerTank.Lives.Length; i++) {
                            if (i >= Server.CurrentClientCount) {
                                PlayerTank.Lives[i] = 0;
                            }
                        }
                    }
                    // doesnt work on 1 client
                    bool check = Client.IsConnected() ? PlayerTank.Lives.All(x => x == 0) : PlayerTank.GetMyLives() <= 1;

                    var cxt = !GameHandler.AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead) ? (check ? MissionEndContext.GameOver : MissionEndContext.Lose) : MissionEndContext.Win;

                    // hardcode hell 2: electric boogaloo
                    if (Difficulties.Types["InfiniteLives"])
                        cxt = MissionEndContext.Lose;

                    GameProperties.MissionEndEvent_Invoke(restartTime, cxt, isExtraLifeMission);
                }
            }
        }
        if (IntermissionSystem.CurrentWaitTime > 0)
            IntermissionSystem.Tick(TankGame.DeltaTime);

        //if (IntermissionSystem.CurrentWaitTime == 220)
            //BeginIntroSequence();
        //if (IntermissionSystem.CurrentWaitTime == IntermissionSystem.WaitTime / 2 && IntermissionSystem.CurrentWaitTime != 0)
            // GameProperties.LoadedCampaign.SetupLoadedMission(GameHandler.AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead));
        // waittime - 150
        // between
        if (IntermissionSystem.CurrentWaitTime > 240 && IntermissionSystem.CurrentWaitTime < 450) {
            // this hardcode makes me want to commit neck rope
            // boolean is changed within the scope of the check so we check again. weird.
            IntermissionSystem.TickAlpha(1f / 60f * TankGame.DeltaTime);
            // ^ when the mission info popup starts to appear
        }
        else // fading into black...
            IntermissionSystem.TickAlpha(-1f / 45f * TankGame.DeltaTime);
        /*if (IntermissionSystem.CurrentWaitTime == 420) {
            SceneManager.CleanupScene();
            var missionStarting = "Assets/fanfares/mission_starting.ogg";
            SoundPlayer.PlaySoundInstance(missionStarting, SoundContext.Effect, 0.8f);
        }*/
    }
    /// <summary>This marks the beginning of the player seeing all of the tanks on the map, before the round begins.</summary>
    public static void BeginIntroSequence() {
        TankFunctionWait = 190;

        TankMusicSystem.StopAll();

        var tune = "Assets/fanfares/mission_snare.ogg";

        SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);

        foreach (var tank in GameHandler.AllTanks)
            if (tank is not null)
                tank.Velocity = Vector2.Zero;

        SceneManager.CleanupScene();

        GameProperties.InMission = false;
    }

    public static void Update() {
        if (TankFunctionWait > 0)
            TankFunctionWait -= TankGame.DeltaTime;
        if (TankFunctionWait <= 0 && _oldWait > 0 && !MainMenu.Active) {
            // FIXME: maybe causes issues since the mission is 1 tick from starting?
            if (!GameProperties.InMission) {
                GameProperties.InMission = true;
                GameProperties.DoMissionStartInvoke();
                TankMusicSystem.PlayAll();
            }
        }
        if (LevelEditor.Active)
            if (DebugManager.DebuggingEnabled)
                if (InputUtils.KeyJustPressed(Keys.T))
                    PlacementSquare.DrawStacks = !PlacementSquare.DrawStacks;
        DebugManager.UpdateDebug();

        if (MainMenu.Active) {
            PlayerTank.KillCount = 0;
            // don't know if this fucks with the stack or not. to be determined.
            PlayerTank.PlayerStatistics = default;
        }

        if (!TankGame.OverheadView && _wasOverhead && !LevelEditor.Active)
            BeginIntroSequence();

        _wasOverhead = TankGame.OverheadView;
        _wasInMission = GameProperties.InMission;
        _oldWait = TankFunctionWait;
    }

    public static string PrepareDisplay = string.Empty;

    public static Animator CountdownAnimator = null;

    private static EasingFunction _ez = EasingFunction.InOutQuad;
    public static void Initialize() {
        CountdownAnimator = Animator.Create()
            // id = 0
            .WithFrame(new(Vector2.One * 2, [], TimeSpan.FromSeconds(1.5), _ez))  // ready 
            .WithFrame(new(Vector2.One * 1, [], TimeSpan.FromSeconds(0), _ez))  // ready 
            .WithFrame(new(Vector2.One * 2, [], TimeSpan.FromSeconds(1.5), _ez))  // set
            .WithFrame(new(Vector2.One * 1, [], TimeSpan.FromSeconds(0), _ez))  // set
            .WithFrame(new(Vector2.One * 2, [], TimeSpan.FromSeconds(1), _ez))  // start 
            .WithFrame(new(new Vector2(2, 0), [], TimeSpan.FromSeconds(1), _ez));
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
        var sec = MathF.Round(TankFunctionWait / 60) + 1;

        if (!MainMenu.Active && !TankGame.OverheadView && !LevelEditor.Active/* && TankFunctionWait > 0*/) {
            var txt = $"{MathF.Round(TankFunctionWait / 60) + 1}";
            SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFontLarge, PrepareDisplay, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 3), 
                IntermissionSystem.BackgroundColor, IntermissionSystem.StripColor, CountdownAnimator.CurrentScale.ToResolution(), 0f, Anchor.Center, 3);
        }
    }
}
