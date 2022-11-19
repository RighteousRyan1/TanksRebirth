using TanksRebirth.Internals;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Common.GameInput;
using TanksRebirth.Internals.Common.GameUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using TanksRebirth.Enums;
using System;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.GameContent.Systems;
using System.Collections.Generic;
using TanksRebirth.Internals.Core.Interfaces;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Core;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using System.IO;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Net;
using System.Threading;
using System.Diagnostics;
using Microsoft.Xna.Framework.Media;
using TanksRebirth.Internals.Common.Framework;
using NativeFileDialogSharp;
using TanksRebirth.IO;
using TanksRebirth.Achievements;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Speedrunning;
using FontStashSharp;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.GameContent
{
    public class GameHandler
    {
        public static Random GameRand = new();

        private static float _tankFuncDelay = 190;
        private static float _oldDelay;

        public const int MAX_AI_TANKS = 1000;
        public const int MAX_PLAYERS = 1000;
        public static AITank[] AllAITanks = new AITank[MAX_AI_TANKS];
        public static PlayerTank[] AllPlayerTanks = new PlayerTank[MAX_PLAYERS];
        public static Tank[] AllTanks = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        public static Logger ClientLog { get; set; }

        public static XpBar Xp;

        private static bool _wasOverhead;

        private static bool _wasInMission;

        public static bool InterpCheck;

        public static ParticleSystem ParticleSystem { get; } = new(15000);

        internal static void MapEvents()
        {
            GameProperties.OnMissionEnd += DoEndMissionWorkload;
            GameProperties.OnMissionStart += HandleStart;
        }

        private static void HandleStart()
        {
            InterpCheck = true;
        }

        public static void DoEndMissionWorkload(int delay, MissionEndContext context, bool result1up) // bool major = (if true, play M100 fanfare, else M20)
        {
            TankMusicSystem.StopAll();

            InterpCheck = false;

            if (result1up && context != MissionEndContext.Lose)
                delay += 200;

            if (context == MissionEndContext.CampaignCompleteMajor)
            {
                TankGame.GameData.CampaignsCompleted++;
                string victory = "Assets/fanfares/mission_complete_M100";
                SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f);
            }
            else if (context == MissionEndContext.CampaignCompleteMinor)
            {
                TankGame.GameData.CampaignsCompleted++;
                var victory = "Assets/fanfares/mission_complete_M20";
                SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f);
            }
            if (result1up && context == MissionEndContext.Win)
            {
                TankGame.GameData.MissionsCompleted++;
                PlayerTank.Lives++;
                var lifeget = "Assets/fanfares/life_get";
                SoundPlayer.PlaySoundInstance(lifeget, SoundContext.Effect, 0.5f);
            }

            if (context == MissionEndContext.Lose)
            {
                PlayerTank.Lives--;

                // what is this comment?
                /*int len = $"{VanillaCampaign.CachedMissions.Count(x => !string.IsNullOrEmpty(x.Name))}".Length;
                int diff = len - $"{VanillaCampaign.CurrentMissionId}".Length;

                string realName = "";

                for (int i = 0; i < diff; i++)
                    realName += "0";
                realName += $"{VanillaCampaign.CurrentMissionId + 1}";

                VanillaCampaign.CachedMissions[VanillaCampaign.CurrentMissionId] = Mission.Load(realName, VanillaCampaign.Name);*/
                var deathSound = "Assets/fanfares/tank_player_death";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }
            else if (context == MissionEndContext.GameOver)
            {
                PlayerTank.Lives--;

                var deathSound = "Assets/fanfares/gameover_playerdeath";
                SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
            }
            else if (context == MissionEndContext.Win)
            {
                TankGame.GameData.MissionsCompleted++;
                GameProperties.LoadedCampaign.LoadNextMission();
                var victorySound = "Assets/fanfares/mission_complete";
                SoundPlayer.PlaySoundInstance(victorySound, SoundContext.Effect, 0.5f);
                if (CurrentSpeedrun is not null)
                {
                    if (GameProperties.LoadedCampaign.CurrentMissionId > 1)
                    {
                        var prevTime = CurrentSpeedrun.MissionTimes.ElementAt(GameProperties.LoadedCampaign.CurrentMissionId - 2).Value; // previous mission time.
                        var realTime = CurrentSpeedrun.Timer.Elapsed - prevTime.Item1; // current total time - previous total time
                        CurrentSpeedrun.MissionTimes[GameProperties.LoadedCampaign.CurrentMission.Name] = (CurrentSpeedrun.Timer.Elapsed, realTime);
                    }
                    else
                        CurrentSpeedrun.MissionTimes[GameProperties.LoadedCampaign.CurrentMission.Name] = (CurrentSpeedrun.Timer.Elapsed, CurrentSpeedrun.Timer.Elapsed);
                }
            }

            if (CampaignCompleteUI.FanfaresAndDurations.ContainsKey(context))
            {
                CampaignCompleteUI.FanfaresAndDurations[context].Item1.Instance?.Play();
                CampaignCompleteUI.FanfaresAndDurations[context].Item1.Instance.Volume = TankGame.Settings.MusicVolume;
                DoEndScene(CampaignCompleteUI.FanfaresAndDurations[context].Item2, context);
            }
            else
                IntermissionSystem.SetTime(delay);
        }
        /// <summary>
        /// Uses a multithreaded approach to start the campaign results screen.
        /// </summary>
        /// <param name="delay">The delay of time before starting the results screen.</param>
        /// <param name="context">The context of which the campaign is ending.</param>
        private static void DoEndScene(TimeSpan delay, MissionEndContext context)
        {
            // i think this works.
            // only adjusts speed for when this is called.
            Task.Run(async () =>
            {
                await Task.Delay(delay * TankGame.DeltaTime).ConfigureAwait(false);
                CampaignCompleteUI.PerformSequence(context);
            });
        }
        internal static void UpdateAll()
        {
            if (MainMenu.Active)
                InterpCheck = true;
            // technically, level 0 in code is level 1, so we want to use that number (1) if the user is level 0.
            if (Xp is not null)
                Xp.Value = TankGame.GameData.ExpLevel - MathF.Floor(TankGame.GameData.ExpLevel);
            else
                Xp = new();

            VanillaAchievements.Repository.UpdateCompletions();
            /* uh, yeah. this is the decay-per-level calculation. people don't want it!
            var floor1 = MathF.Floor(TankGame.GameData.ExpLevel + 1f);
            var floor0 = MathF.Floor(TankGame.GameData.ExpLevel);
            GameData.UniversalExpMultiplier = floor1 - (GameData.DecayPerLevel * floor0);*/

            if (Difficulties.Types["InfiniteLives"])
                PlayerTank.Lives = PlayerTank.StartingLives;
            foreach (var tank in AllTanks)
                tank?.Update();

            if (GameProperties.InMission)
            {
                TankMusicSystem.Update();

                foreach (var mine in Mine.AllMines)
                    mine?.Update();

                foreach (var bullet in Shell.AllShells)
                    bullet?.Update();

                foreach (var crate in Crate.crates)
                    crate?.Update();

                foreach (var pu in Powerup.Powerups)
                    pu?.Update();

                foreach (var fp in TankFootprint.footprints)
                    fp?.Update();
            }
            else
                if (!GameProperties.InMission)
                if (TankMusicSystem.Songs is not null)
                    foreach (var song in TankMusicSystem.Songs)
                        song.Volume = 0;
            LevelEditor.Update();

            foreach (var expl in Explosion.Explosions)
                expl?.Update();

            if (Difficulties.Types["ThunderMode"])
                DoThunderStuff();

            if (GameProperties.ShouldMissionsProgress && !MainMenu.Active)
                HandleMissionChanging();

            foreach (var cube in Block.AllBlocks)
                cube?.Update();

            if ((DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == DebugUtils.Id.LevelEditDebug && TankGame.OverheadView) || LevelEditor.Active)
                foreach (var sq in PlacementSquare.Placements)
                    sq?.Update();

            ParticleSystem.UpdateParticles();

            if (MainMenu.Active)
                MainMenu.Update();

            if (Input.KeyJustPressed(Keys.Insert))
                DebugUtils.DebuggingEnabled = !DebugUtils.DebuggingEnabled;

            if (Input.KeyJustPressed(Keys.Multiply))
                DebugUtils.DebugLevel++;
            if (Input.KeyJustPressed(Keys.Divide))
                DebugUtils.DebugLevel--;

            if (!TankGame.OverheadView && _wasOverhead && !LevelEditor.Active)
                BeginIntroSequence();

            if ((TankGame.OverheadView || MainMenu.Active) && !LevelEditor.Active)
            {
                GameProperties.InMission = false;
                _tankFuncDelay = 600;
            }

            if (LevelEditor.Active)
                _tankFuncDelay = 190;

            if (_tankFuncDelay > 0)
                _tankFuncDelay -= TankGame.DeltaTime;
            if (_tankFuncDelay <= 0 && _oldDelay > 0 && !MainMenu.Active)
            {
                // FIXME: maybe causes issues since the mission is 1 tick from starting?
                if (!GameProperties.InMission)
                {
                    GameProperties.InMission = true;
                    GameProperties.DoMissionStartInvoke();
                    TankMusicSystem.PlayAll();
                }
            }
            if (!MainMenu.Active)
            {
                if (Input.KeyJustPressed(Keys.Z))
                    blockType--;
                if (Input.KeyJustPressed(Keys.X))
                    blockType++;
                if (DebugUtils.DebuggingEnabled)
                {
                    if (Input.KeyJustPressed(Keys.NumPad7))
                        tankToSpawnType--;
                    if (Input.KeyJustPressed(Keys.NumPad9))
                        tankToSpawnType++;

                    if (Input.KeyJustPressed(Keys.NumPad1))
                        tankToSpawnTeam--;
                    if (Input.KeyJustPressed(Keys.NumPad3))
                        tankToSpawnTeam++;

                    if (Input.KeyJustPressed(Keys.OemPeriod))
                        blockHeight++;
                    if (Input.KeyJustPressed(Keys.OemComma))
                        blockHeight--;


                    if (Input.KeyJustPressed(Keys.PageUp))
                        SpawnTankPlethorae(true);
                    if (Input.KeyJustPressed(Keys.PageDown))
                        SpawnMe(PlayerID.Blue, tankToSpawnTeam);
                    if (Input.KeyJustPressed(Keys.Home))
                        SpawnTankAt(!TankGame.OverheadView ? GameUtils.GetWorldPosition(GameUtils.MousePosition) : PlacementSquare.CurrentlyHovered.Position, tankToSpawnType, tankToSpawnTeam);

                    if (Input.KeyJustPressed(Keys.OemSemicolon))
                        new Mine(null, GameUtils.GetWorldPosition(GameUtils.MousePosition).FlattenZ(), 400);
                    if (Input.KeyJustPressed(Keys.OemQuotes))
                        new Shell(GameUtils.GetWorldPosition(GameUtils.MousePosition) + new Vector3(0, 11, 0), Vector3.Zero, ShellID.Standard, null, 0, playSpawnSound: false);
                    if (Input.KeyJustPressed(Keys.End))
                        SpawnCrateAtMouse();

                    if (Input.KeyJustPressed(Keys.I) && DebugUtils.DebugLevel == 4)
                        new Powerup(powerups[mode]) { Position = GameUtils.GetWorldPosition(GameUtils.MousePosition) + new Vector3(0, 10, 0) };
                }
            }

            if (MainMenu.Active)
            {
                PlayerTank.KillCount = 0;
                // don't know if this fucks with the stack or not. to be determined.
                PlayerTank.PlayerStatistics = default;
            }

            blockHeight = MathHelper.Clamp(blockHeight, 1, 7);
            blockType = MathHelper.Clamp(blockType, 0, 3);

            _wasOverhead = TankGame.OverheadView;
            _wasInMission = GameProperties.InMission;
            _oldDelay = _tankFuncDelay;

            if (TankGame.OverheadView)
                HandleLevelEditorModifications();
            /*GameLight.Brightness = GameUtils.MousePosition.Y / GameUtils.WindowHeight;
            ChatSystem.SendMessage(GameLight.Brightness, Color.White);
            GameLight.Apply(false);*/
            OnPostUpdate?.Invoke();
        }

        private static void DoThunderStuff()
        {
            if (IntermissionSystem.BlackAlpha > 0 || IntermissionSystem.Alpha >= 1f || MainMenu.Active || GameUI.Paused)
            {
                if (Thunder.SoftRain.IsPlaying())
                {
                    Thunder.SoftRain.Instance.Stop();
                    TankGame.ClearColor = Color.Black;

                    GameLight.Color = new(150, 150, 170);
                    GameLight.Brightness = 0.71f;

                    GameLight.Apply(false);
                }
                return;
            }
            if (!Thunder.SoftRain.IsPlaying())
                Thunder.SoftRain.Instance.Play();
            Thunder.SoftRain.Instance.Volume = TankGame.Settings.AmbientVolume;

            if (GameRand.NextFloat(0, 1f) <= 0.003f)
            {
                var rand = new Range<Thunder.ThunderType>(Thunder.ThunderType.Fast, Thunder.ThunderType.Instant2);
                var type = (Thunder.ThunderType)GameRand.Next((int)rand.Min, (int)rand.Max);

                if (!Thunder.Thunders.Any(x => x is not null && x.Type == type))
                    new Thunder(type);
            }

            Thunder brightest = null;

            float minThresh = 0.005f;

            foreach (var thun in Thunder.Thunders)
            {
                if (thun is not null)
                {
                    thun.Update();

                    if (brightest is null)
                        brightest = thun;
                    else
                        if (thun.CurBright > brightest.CurBright && thun.CurBright > minThresh)
                        brightest = thun;
                }
            }

            GameLight.Color = Color.Multiply(Color.DeepSkyBlue, 0.5f); // DeepSkyBlue


            if (brightest is not null)
            {
                TankGame.ClearColor = Color.DeepSkyBlue * brightest.CurBright;
                GameLight.Brightness = brightest.CurBright / 6;
            }
            else
                GameLight.Brightness = minThresh;

            GameLight.Apply(false);
        }
        public static Speedrun CurrentSpeedrun;
        public static void StartSpeedrun()
        {
            if (GameProperties.ShouldMissionsProgress)
            {
                if (GameProperties.LoadedCampaign.CurrentMissionId <= 0)
                {
                    CurrentSpeedrun = new(GameProperties.LoadedCampaign.MetaData.Name);
                    foreach (var mission in GameProperties.LoadedCampaign.CachedMissions)
                        CurrentSpeedrun.MissionTimes.Add(mission.Name, (TimeSpan.Zero, TimeSpan.Zero));
                    CurrentSpeedrun.Timer.Start();
                }
            }
        }
        /// <summary>
        /// A method that returns whether or not there was a victory- be it for the enemy or the player.
        /// </summary>
        /// <param name="mission">The mission to check.</param>
        /// <param name="victory">Whether or not it resulted in victory for the player.</param>
        /// <returns>Whether or not one team or one player dominates the map.</returns>
        public static bool NothingCanHappenAnymore(Mission mission, out bool victory)
        {
            if (mission.Tanks.Any(tnk => tnk.IsPlayer))
            {
                var activeTeams = Tank.GetActiveTeams();

                if (activeTeams.Contains(TeamID.NoTeam) && AllTanks.Count(tnk => tnk != null && !tnk.Dead) <= 1)
                {
                    victory = AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead);
                    return true;
                }
                // check if it's not only FFA, and if teams left doesnt contain ffa. 
                else if (!activeTeams.Contains(TeamID.NoTeam) && activeTeams.Length <= 1)
                {
                    victory = activeTeams.Contains(PlayerTank.MyTeam);
                    return true;
                }
            }
            else
            {
                var activeTeams = Tank.GetActiveTeams();
                // if a player was not initially spawned in the mission, check if a team is still alive and end the mission
                if (activeTeams.Contains(TeamID.NoTeam) && AllTanks.Count(tnk => tnk != null && !tnk.Dead) <= 1)
                {
                    victory = true;
                    return true;
                }
                else if (!activeTeams.Contains(TeamID.NoTeam) && activeTeams.Length <= 1)
                {
                    victory = true;
                    return true;
                }
            }
            victory = false;
            return false;
        }
        private static void HandleMissionChanging()
        {
            if (GameProperties.LoadedCampaign.CachedMissions[0].Name is null)
                return;

            var nothingAnymore = NothingCanHappenAnymore(GameProperties.LoadedCampaign.CurrentMission, out bool victory);

            if (nothingAnymore)
            {
                GameProperties.InMission = false;
                if (!GameProperties.InMission && _wasInMission)
                {
                    bool isExtraLifeMission = GameProperties.LoadedCampaign.MetaData.ExtraLivesMissions.Contains(GameProperties.LoadedCampaign.CurrentMissionId + 1);
                    if (victory)
                    {
                        int restartTime = 600;
                        if (isExtraLifeMission)
                            restartTime += 200;

                        var cxt = MissionEndContext.Win;

                        if (GameProperties.LoadedCampaign.CurrentMissionId >= GameProperties.LoadedCampaign.CachedMissions.Length - 1)
                            cxt = GameProperties.LoadedCampaign.MetaData.HasMajorVictory ? MissionEndContext.CampaignCompleteMajor : MissionEndContext.CampaignCompleteMinor;

                        GameProperties.MissionEndEvent_Invoke(restartTime, cxt, isExtraLifeMission);
                    }
                    else
                    {
                        int restartTime = 600;

                        // if a 1-up mission, extend by X amount of time (TBD?)
                        // we check <= 1 since the lives haven't actually been deducted yet.
                        MissionEndContext cxt = !AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead) ? (PlayerTank.Lives <= 1 ? MissionEndContext.GameOver : MissionEndContext.Lose) : MissionEndContext.Win;


                        GameProperties.MissionEndEvent_Invoke(restartTime, cxt, isExtraLifeMission);
                    }
                }
            }
            if (IntermissionSystem.CurrentWaitTime > 0)
                IntermissionSystem.Tick(1);

            if (IntermissionSystem.CurrentWaitTime == 220)
                BeginIntroSequence();
            if (IntermissionSystem.CurrentWaitTime == IntermissionSystem.WaitTime / 2 && IntermissionSystem.CurrentWaitTime != 0)
                GameProperties.LoadedCampaign.SetupLoadedMission(AllPlayerTanks.Any(tnk => tnk != null && !tnk.Dead));
            if (IntermissionSystem.CurrentWaitTime > 240 && IntermissionSystem.CurrentWaitTime < IntermissionSystem.WaitTime - 150)
            {
                // this hardcode makes me want to commit neck rope
                // boolean is changed within the scope of the check so we check again. weird.
                IntermissionSystem.TickAlpha(1f / 45f);
            }
            else
                IntermissionSystem.TickAlpha(-1f / 45f);
            if (IntermissionSystem.CurrentWaitTime == IntermissionSystem.WaitTime - 180)
            {
                CleanupScene();
                var missionStarting = "Assets/fanfares/mission_starting";
                SoundPlayer.PlaySoundInstance(missionStarting, SoundContext.Effect, 0.8f);
            }
        }

        public static int blockType = 0;
        public static int blockHeight = 1;
        public static int tankToSpawnType;
        public static int tankToSpawnTeam;
        internal static void RenderAll()
        {
            TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

            if (!MainMenu.Active && !LevelEditor.Editing)
                Xp?.Render(TankGame.SpriteRenderer, new(GameUtils.WindowWidth / 2, 50.ToResolutionY()), new Vector2(100, 20).ToResolution(), Anchor.Center, Color.Red, Color.Lime);

            if (_tankFuncDelay > 0 && !MainMenu.Active && !TankGame.OverheadView && !LevelEditor.Active)
                // $"{MathF.Round(_tankFuncDelay / 60)}"
                TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, $"{MathF.Round(_tankFuncDelay / 60) + 1}", GameUtils.WindowCenter, Color.White, new Vector2(3).ToResolution(), 0f, TankGame.TextFontLarge.MeasureString($"{MathF.Round(_tankFuncDelay / 60) + 1}") / 2, 0f);

            if (!MainMenu.Active)
                MapRenderer.RenderWorldModels();

            foreach (var tank in AllTanks)
                tank?.Render();

            foreach (var cube in Block.AllBlocks)
                cube?.Render();

            foreach (var mine in Mine.AllMines)
                mine?.Render();

            foreach (var bullet in Shell.AllShells)
                bullet?.Render();

            foreach (var mark in TankDeathMark.deathMarks)
                mark?.Render();

            //foreach (var print in TankFootprint.footprints)
                //print?.Render();

            foreach (var crate in Crate.crates)
                crate?.Render();

            foreach (var powerup in Powerup.Powerups)
                powerup?.Render();

            if ((DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == DebugUtils.Id.LevelEditDebug && TankGame.OverheadView) || LevelEditor.Active)
                foreach (var sq in PlacementSquare.Placements)
                    sq?.Render();

            TankGame.Instance.GraphicsDevice.BlendState = BlendState.Additive;
            foreach (var expl in Explosion.Explosions)
                expl?.Render();
            TankGame.Instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            ParticleSystem.RenderParticles();

            if (MainMenu.Active)
            MainMenu.Render();

            if (LevelEditor.Active)
                LevelEditor.Render();

            foreach (var body in Tank.CollisionsWorld.BodyList.ToList())
            {
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"BODY", 
                    GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(body.Position.X * Tank.UNITS_PER_METER, 0, body.Position.Y * Tank.UNITS_PER_METER), TankGame.GameView, TankGame.GameProjection), centered: true);
            }

            for (int i = 0; i < VanillaAchievements.Repository.GetAchievements().Count; i++)
            {
                var achievement = VanillaAchievements.Repository.GetAchievements()[i];
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{achievement.Name}: {(achievement.IsComplete ? "Complete" : "Incomplete")}",
                    new Vector2(8, 24 + (i * 20)), level: DebugUtils.Id.AchievementData, centered: false);
            }

            tankToSpawnType = MathHelper.Clamp(tankToSpawnType, 2, TankID.Collection.Count - 1);
            tankToSpawnTeam = MathHelper.Clamp(tankToSpawnTeam, 0, TeamID.Collection.Count - 1);

            #region TankInfo
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "Spawn Tank With Info:", GameUtils.WindowTop + new Vector2(0, 8), 3, centered: true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Tier: {TankID.Collection.GetKey(tankToSpawnType)}", GameUtils.WindowTop + new Vector2(0, 24), 3, centered: true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Team: {TeamID.Collection.GetKey(tankToSpawnTeam)}", GameUtils.WindowTop + new Vector2(0, 40), 3, centered: true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"CubeStack: {blockHeight} | CubeType: {BlockID.Collection.GetKey(blockType)}", GameUtils.WindowBottom - new Vector2(0, 20), 3, centered: true);

            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"HighestTier: {AITank.GetHighestTierActive()}", new(10, GameUtils.WindowHeight * 0.26f), 1);
            // DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f).Name : "N/A")}", new(10, GameUtils.WindowHeight - 100), 1);
            for (int i = 0; i < TankID.Collection.Count; i++) {
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{TankID.Collection.GetKey(i)}: {AITank.GetTankCountOfType(i)}", new(10, GameUtils.WindowHeight * 0.3f + (i * 20)), 1);
            }
            #endregion

            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Logic Time: {TankGame.LogicTime.TotalMilliseconds:0.00}ms" +
                $"\nLogic FPS: {TankGame.LogicFPS}" +
                $"\n\nRender Time: {TankGame.RenderTime.TotalMilliseconds:0.00}ms" +
                $"\nRender FPS: {TankGame.RenderFPS}", new(10, 500));

            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Current Mission: {GameProperties.LoadedCampaign.CurrentMission.Name}\nCurrent Campaign: {GameProperties.LoadedCampaign.MetaData.Name}", GameUtils.WindowBottomLeft - new Vector2(-4, 40), 3, centered: false);
            if (!MainMenu.Active && !LevelEditor.Active)
            {
                if (IntermissionSystem.IsAwaitingNewMission)
                {
                    // uhhh... what was i doing here?
                }
                for (int i = -4; i < 10; i++)
                {
                    IntermissionSystem.DrawShadowedTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard"), new Vector2((i * 14).ToResolutionX(), GameUtils.WindowHeight * 0.9f), Vector2.UnitY, Color.White, new Vector2(2f).ToResolution(), 1f, new(0, GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard").Size().Y / 2), true);
                }
                IntermissionSystem.DrawShadowedString(TankGame.TextFontLarge, new Vector2(80.ToResolutionX(), GameUtils.WindowHeight * 0.9f - 14f.ToResolutionY()), Vector2.One, $"{PlayerTank.KillCount}", new(119, 190, 238), new Vector2(0.675f).ToResolution(), 1f);
            }

            ChatSystem.DrawMessages();

            if (!MainMenu.Active)
            {
                ClearTracks.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 0;
                ClearChecks.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 0;
                SetupMissionAgain.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 0;
                MovePULeft.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 4;
                MovePURight.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 4;
                Display.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 4;
                MissionName.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 3;
                LoadMission.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 3;
                SaveMission.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 3;
                LoadCampaign.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 3;
                CampaignName.IsVisible = DebugUtils.DebuggingEnabled && DebugUtils.DebugLevel == 3;
            }
            GameUI.MissionInfoBar.IsVisible = !MainMenu.Active && !LevelEditor.Active && !CampaignCompleteUI.IsViewingResults;
            OnPostRender?.Invoke();
        }
        public static void RenderUI()
        {
            foreach (var element in UIElement.AllUIElements.ToList())
            {
                // element.Position = Vector2.Transform(element.Position, UIMatrix * Matrix.CreateTranslation(element.Position.X, element.Position.Y, 0));
                if (element.Parent != null)
                    continue;

                if (element.HasScissor)
                    TankGame.SpriteRenderer.End();

                element?.Draw(TankGame.SpriteRenderer);

                if (element.HasScissor)
                    TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, rasterizerState: TankGame.DefaultRasterizer);
            }
            foreach (var element in UIElement.AllUIElements.ToList())
                element?.DrawTooltips(TankGame.SpriteRenderer);
        }
        private static int _oldelta;
        public static void HandleLevelEditorModifications()
        {
            var cur = PlacementSquare.CurrentlyHovered;

            if (cur is not null && cur.HasItem && cur.HasBlock && cur.BlockId > -1 && cur.BlockId < Block.AllBlocks.Length)
            {
                if (Block.AllBlocks[cur.BlockId] != null)
                {
                    if (Block.AllBlocks[cur.BlockId].Type == BlockID.Teleporter)
                    {
                        // ChatSystem.SendMessage($"{Input.DeltaScrollWheel}", Color.White);

                        if (Input.DeltaScrollWheel != _oldelta)
                            Block.AllBlocks[cur.BlockId].TpLink += (sbyte)(Input.DeltaScrollWheel - _oldelta);
                    }
                }
            }

            _oldelta = Input.DeltaScrollWheel;
        }

        // fix shitty mission init (innit?)

        private static readonly PowerupTemplate[] powerups =
        {
             Powerup.Speed,
             Powerup.ShellHome,
             Powerup.Invisibility
        };

        public static Lighting.LightProfile GameLight = new()
        {
            Color = new(150, 150, 170),
            Brightness = 0.75f,
            //isNight = true
        };

        public static void StartTnkScene()
        {
            DebugUtils.DebuggingEnabled = false;

            LoadTnkScene();

            GameLight.Apply(false);
        }

        public static void SetupGraphics()
        {
            GameShaders.Initialize();
            MapRenderer.InitializeRenderers();

            InitDebugUi();
            PlacementSquare.InitializeLevelEditorSquares();
        }

        private static bool _musicLoaded;

        public delegate void LoadTankScene();
        public static event LoadTankScene OnLoadTankScene; 
        public delegate void PostUpdate();
        public static event PostUpdate OnPostUpdate;
        public delegate void PostRender();
        public static event PostRender OnPostRender;
        public delegate void MissionCleanupEvent();
        public static event MissionCleanupEvent OnMissionCleanup;
        public static void LoadTnkScene()
        {
            if (!_musicLoaded)
            {
                TankMusicSystem.LoadMusic();
                TankMusicSystem.LoadAmbienceTracks();
                OnLoadTankScene?.Invoke();
                _musicLoaded = true;
            }
            else
            {
                foreach (var song in TankMusicSystem.Songs)
                {
                    song.Stop();
                }
                TankMusicSystem.SnowLoop.Stop();
                TankMusicSystem.SnowLoop.Play();
            }
        }

        public static PlayerTank SpawnMe(int playerType, int team)
        {
            var pos = TankGame.OverheadView ? PlacementSquare.CurrentlyHovered.Position : GameUtils.GetWorldPosition(GameUtils.MousePosition);
            var myTank = new PlayerTank(playerType);

            myTank.Team = team;
            myTank.Dead = false;
            myTank.Body.Position = pos.FlattenZ() / Tank.UNITS_PER_METER;
            myTank.Position = pos.FlattenZ();

            if (Client.IsConnected())
                Client.RequestPlayerTankSpawn(myTank);

            return myTank;
        }

        public static void SpawnTankInCrate(int tierOverride = default, int teamOverride = default, bool createEvenDrop = false)
        {
            var random = new CubeMapPosition(GameRand.Next(0, 26), GameRand.Next(0, 20));

            var drop = Crate.SpawnCrate(new(CubeMapPosition.Convert3D(random).X, 500 + (createEvenDrop ? 0 : GameRand.Next(-300, 301)), CubeMapPosition.Convert3D(random).Z), 2f);
            drop.scale = 1.25f;
            drop.TankToSpawn = new TankTemplate()
            {
                AiTier = tierOverride == default ? AITank.PickRandomTier() : tierOverride,
                Team = teamOverride == default ? GameRand.Next(TeamID.NoTeam, TeamID.Collection.Count) : teamOverride
            };
        }

        public static void SpawnCrateAtMouse()
        {
            var pos = GameUtils.GetWorldPosition(GameUtils.MousePosition);

            var drop = Crate.SpawnCrate(new(pos.X, 200, pos.Z), 2f);
            drop.scale = 1.25f;
            drop.TankToSpawn = new TankTemplate()
            {
                AiTier = AITank.PickRandomTier(),
                Team = TeamID.NoTeam
            };
        }
        public static void CleanupScene()
        {
            foreach (var mine in Mine.AllMines)
                mine?.Remove();

            foreach (var bullet in Shell.AllShells)
                bullet?.Remove();

            foreach (var expl in Explosion.Explosions)
                expl?.Remove();

            foreach (var crate in Crate.crates)
                crate?.Remove();

            foreach (var pu in Powerup.Powerups)
                pu?.Remove();

            ClearTankDeathmarks(null);
            ClearTankTracks(null);

            OnMissionCleanup?.Invoke();
        }
        public static void BeginIntroSequence()
        {
            _tankFuncDelay = 190;

            TankMusicSystem.StopAll();

            var tune = "Assets/fanfares/mission_snare";

            SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);

            foreach (var tank in AllTanks)
                if (tank is not null)
                    tank.Velocity = Vector2.Zero;

            CleanupScene();

            GameProperties.InMission = false;
        }
        public static AITank SpawnTank(int tier, int team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var t = new AITank(tier);
            t.TankRotation = rot;
            t.TurretRotation = rot;
            t.Team = team;
            t.Dead = false;
            var pos = new CubeMapPosition(GameRand.Next(0, 27), GameRand.Next(0, 20));
            t.Body.Position = pos;
            t.Position = pos;

            return t;
        }
        public static AITank SpawnTankAt(Vector3 position, int tier, int team)
        {
            var rot = 0f;

            var x = new AITank(tier);
            x.TargetTankRotation = rot + MathHelper.Pi;
            x.TankRotation = rot;
            x.TurretRotation = -rot;

            x.Team = team;
            x.Dead = false;
            x.Body.Position = position.FlattenZ() / Tank.UNITS_PER_METER;
            x.Position = position.FlattenZ();
            return x;
        }
        public static void SpawnTankPlethorae(bool useCurTank = false)
        {
            for (int i = 0; i < 5; i++)
            {
                var random = new CubeMapPosition(GameRand.Next(0, 23),GameRand.Next(0, 18));
                var rot = GeometryUtils.GetPiRandom();
                var t = new AITank(useCurTank ? tankToSpawnType : AITank.PickRandomTier());
                t.TankRotation = rot;
                t.TurretRotation = rot;
                t.Dead = false;
                t.Team = useCurTank ? tankToSpawnTeam : TeamID.NoTeam;
                t.Body.Position = random;
                t.Position = random;
            }
        }

        public static UITextButton ClearTracks;
        public static UITextButton ClearChecks;

        public static UITextButton SetupMissionAgain;

        public static UITextButton MovePURight;
        public static UITextButton MovePULeft;

        public static UITextButton Display;

        public static UITextInput MissionName;
        public static UITextInput CampaignName;
        public static UITextButton LoadMission;
        public static UITextButton SaveMission;

        public static UITextButton LoadCampaign;

        private static int mode;

        public static void InitDebugUi()
        {
            MissionName = new(TankGame.TextFont, Color.White, 0.75f, 20)
            {
                DefaultString = "Mission Name",
                IsVisible = false
            };
            MissionName.SetDimensions(20, 60, 230, 50);
            CampaignName = new(TankGame.TextFont, Color.White, 0.75f, 20)
            {
                DefaultString = "Campaign Name",
                IsVisible = false
            };
            CampaignName.SetDimensions(20, 120, 230, 50);

            SaveMission = new("Save", TankGame.TextFont, Color.White, 0.5f);
            SaveMission.OnLeftClick = (l) =>
            {
                if (MissionName.IsEmpty())
                {
                    ChatSystem.SendMessage("Invalid name for mission.", Color.Red);
                    return;
                }
                Mission.Save(MissionName.GetRealText(), CampaignName.IsEmpty() ? null : CampaignName.GetRealText());

                ChatSystem.SendMessage(CampaignName.IsEmpty() ? $"Saved mission '{MissionName.GetRealText()}'." : $"Saved mission '{MissionName.GetRealText()}' to Campaign folder '{CampaignName.GetRealText()}'.", Color.White);
            };
            SaveMission.IsVisible = false;
            SaveMission.SetDimensions(20, 180, 105, 50);

            LoadMission = new("Load", TankGame.TextFont, Color.White, 0.5f);
            LoadMission.OnLeftClick = (l) =>
            {
                if (TankGame.IsWindows && MissionName.IsEmpty())
                {
                    var res = Dialog.FileOpen("mission", TankGame.SaveDirectory);
                    if (res.Path != null && res.IsOk)
                    {
                        try
                        {
                            GameProperties.LoadedCampaign.LoadMission(Mission.Load(res.Path, null));
                            GameProperties.LoadedCampaign.SetupLoadedMission(true);
                            
                            ChatSystem.SendMessage($"Loaded mission '{Path.GetFileNameWithoutExtension(res.Path)}'.", Color.White);
                        }
                        catch
                        {
                            ChatSystem.SendMessage("Failed to load mission.", Color.Red);
                        }
                    }
                    return;
                }

                GameProperties.LoadedCampaign.LoadMission(Mission.Load(MissionName.GetRealText(), CampaignName.IsEmpty() ? null : CampaignName.GetRealText()));
                GameProperties.LoadedCampaign.SetupLoadedMission(true);
            };
            LoadMission.IsVisible = false;
            LoadMission.SetDimensions(145, 180, 105, 50);

            LoadCampaign = new("Load Campaign", TankGame.TextFont, Color.White, 0.75f);
            LoadCampaign.OnLeftClick = (l) =>
            {
                if (MissionName.IsEmpty())
                {
                    ChatSystem.SendMessage("Invalid name for campaign.", Color.Red);
                    return;
                }
                GameProperties.LoadedCampaign = Campaign.LoadFromFolder(CampaignName.GetRealText(), true);
                GameProperties.LoadedCampaign.SetupLoadedMission(true);
            };
            LoadCampaign.IsVisible = false;
            LoadCampaign.SetDimensions(20, 240, 230, 50);

            ClearTracks = new("Clear Tracks", TankGame.TextFont, Color.LightBlue, 0.5f);
            ClearTracks.SetDimensions(250, 25, 100, 50);
            ClearTracks.IsVisible = false;

            ClearTracks.OnLeftClick += ClearTankTracks;

            ClearChecks = new("Clear Checks", TankGame.TextFont, Color.LightBlue, 0.5f);
            ClearChecks.SetDimensions(250, 95, 100, 50);
            ClearChecks.IsVisible = false;

            ClearChecks.OnLeftClick += ClearTankDeathmarks;

            SetupMissionAgain = new("Restart\nMission", TankGame.TextFont, Color.LightBlue, 0.5f);
            SetupMissionAgain.SetDimensions(250, 165, 100, 50);
            SetupMissionAgain.IsVisible = false;

            SetupMissionAgain.OnLeftClick = (obj) => BeginIntroSequence();

            MovePULeft = new("<", TankGame.TextFont, Color.LightBlue, 0.5f);
            MovePULeft.SetDimensions(GameUtils.WindowWidth / 2 - 100, 25, 50, 50);
            MovePULeft.IsVisible = false;

            MovePURight = new(">", TankGame.TextFont, Color.LightBlue, 0.5f);
            MovePURight.SetDimensions(GameUtils.WindowWidth / 2 + 100, 25, 50, 50);
            MovePURight.IsVisible = false;

            Display = new(powerups[mode].Name, TankGame.TextFont, Color.LightBlue, 0.5f);
            Display.SetDimensions(GameUtils.WindowWidth / 2 - 35, 25, 125, 50);
            Display.IsVisible = false;

            MovePULeft.OnLeftClick = (obj) =>
            {
                if (mode < powerups.Length - 1)
                    mode++;
                Display.Text = powerups[mode].Name;
            };
            MovePURight.OnLeftClick = (obj) =>
            {
                if (mode > 0)
                    mode--;
                Display.Text = powerups[mode].Name;
            };
        }

        private static void ClearTankDeathmarks(UIElement affectedElement)
        {
            for (int i = 0; i < TankDeathMark.deathMarks.Length; i++)
            {
                if (TankDeathMark.deathMarks[i] != null)
                    TankDeathMark.deathMarks[i].check?.Destroy();
                TankDeathMark.deathMarks[i] = null;
            }

            TankDeathMark.total_death_marks = 0;
        }

        private static void ClearTankTracks(UIElement affectedElement)
        {
            for (int i = 0; i < TankFootprint.footprints.Length; i++)
            {
                TankFootprint.footprints[i]?.Remove();
                TankFootprint.footprints[i] = null;
            }
        }
    }

    public static class MouseRenderer
    {
        public static Texture2D MouseTexture { get; private set; }

        public static int numDots = 10;

        private static float _sinScale;

        public static bool ShouldRender = true;

        public static float DistUntilPathTrace = 1575f;

        private static Vector2 _oldMouse;

        public static bool DoTrail = false;

        public static void DrawMouse()
        {
            numDots = 10;
            if (!ShouldRender)
                return;
            if (!MainMenu.Active && !GameUI.Paused && !LevelEditor.Active)
            {
                if (GameHandler.AllPlayerTanks[0] is not null)
                {
                    var me = GameHandler.AllPlayerTanks[0];
                    var tankPos = GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), me.World, TankGame.GameView, TankGame.GameProjection);

                    if (GameUtils.Distance_WiiTanksUnits(tankPos, GameUtils.MousePosition) >= DistUntilPathTrace.ToResolutionX()) // any scale doesnt matter?
                    {
                        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/mouse_dot");

                        // GameHandler.ClientLog.Write("One Loop:", LogType.Info);
                        for (int i = 0; i < numDots; i++)
                        {
                            var curDrawPos = Vector2.Lerp(tankPos, GameUtils.MousePosition, (float)i / numDots);// tankPos.DirectionOf(GameUtils.MousePosition) * i;

                            for (int j = 0; j < 4; j++)
                                TankGame.SpriteRenderer.Draw(tex, curDrawPos, null, Color.White, MathHelper.PiOver2 * j, tex.Size(), new Vector2(0.35f).ToResolution(), default, default);
                        }
                    }
                }
            }

            if (DoTrail)
            {
                var p = GameHandler.ParticleSystem.MakeParticle(new Vector3(GameUtils.MousePosition.X, GameUtils.MousePosition.Y, 0), TankGame.WhitePixel);
                p.Is2d = true;
                var dir = _oldMouse.DirectionOf(GameUtils.MousePosition).ToResolution();
                p.Rotation2D = dir.ToRotation();
                p.TextureScale = new Vector2(dir.Length() * 1.1f, 20.ToResolutionY());
                p.Origin2D = new(0, TankGame.WhitePixel.Size().Y / 2);
                p.isAddative = false;
                p.ToScreenSpace = false;
                p.UniqueBehavior = (pa) =>
                {
                    p.Alpha -= 0.06f;
                    p.TextureScale -= new Vector2(0.06f);

                    if (p.Alpha <= 0)
                        p.Destroy();

                    p.Color = Color.SkyBlue;//GameUtils.HsvToRgb(TankGame.GameUpdateTime % 255 / 255f * 360, 1, 1);
                };
            }
            _sinScale = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);

            MouseTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/cursor_1");
            TankGame.SpriteRenderer.Draw(MouseTexture, GameUtils.MousePosition, null, Color.White, 0f, MouseTexture.Size() / 2, (1f + _sinScale / 16).ToResolution(), default, default);
            _oldMouse = GameUtils.MousePosition;
        }
    }
    public class GameShaders
    {
        public static Effect MouseShader { get; private set; }
        public static Effect GaussianBlurShader { get; private set; }

        public static Effect TestShader { get; private set; }

        private static bool _lantern;
        public static bool LanternMode
        {
            get => _lantern;
            set
            {
                _lantern = value;
                TestShader.Parameters["oLantern"]?.SetValue(value);
            }
        }

        public static void Initialize()
        {
            GaussianBlurShader = GameResources.GetGameResource<Effect>("Assets/Shaders/GaussianBlur");
            MouseShader = GameResources.GetGameResource<Effect>("Assets/Shaders/MouseShader");
            TestShader = GameResources.GetGameResource<Effect>("Assets/Shaders/testshader");
        }
        //static float val = 1f;
        public static void UpdateShaders()
        {
            MouseShader.Parameters["oGlobalTime"].SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
            MouseShader.Parameters["oColor"].SetValue(new Vector3(0f, 0f, 1f)
                /*MouseRenderer.HsvToRgb(TankGame.GameUpdateTime % 255 / 255f * 360, 1, 1).ToVector3()*/);
            MouseShader.Parameters["oSpeed"].SetValue(-20f);
            MouseShader.Parameters["oSpacing"].SetValue(10f);

            GaussianBlurShader.Parameters["oResolution"].SetValue(new Vector2(GameUtils.WindowWidth, GameUtils.WindowHeight));
            GaussianBlurShader.Parameters["oBlurFactor"].SetValue(6f);

            /*if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Up))
                val += 0.01f;
            else if (Input.CurrentKeySnapshot.IsKeyDown(Keys.Down))
                val -= 0.01f;*/


            TestShader.Parameters["oTime"]?.SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
            //TestShader.Parameters["oBend"]?.SetValue(val);
            //TestShader.Parameters["oDistortionFactor"].SetValue(GameUtils.MousePosition.X / GameUtils.WindowWidth);

            var index = Array.FindIndex(GameHandler.AllPlayerTanks, x => x is not null && !x.Dead);
            var pos = index > -1 ? GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GameHandler.AllPlayerTanks[index].Position.X, 11, GameHandler.AllPlayerTanks[index].Position.Y), TankGame.GameView, TankGame.GameProjection).ToCartesianCoordinates() : new Vector2(-1);
            // var val = (float)TankGame.LastGameTime.TotalGameTime.TotalSeconds;
            TestShader.Parameters["oPower"]?.SetValue(MainMenu.Active ? 100f : GameHandler.GameRand.NextFloat(0.195f, 0.20f));
            TestShader.Parameters["oPosition"]?.SetValue(pos/*GameUtils.MousePosition.ToCartesianCoordinates()*/);
        }
    }
}
