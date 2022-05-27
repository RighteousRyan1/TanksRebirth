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

namespace TanksRebirth.GameContent
{
    public class GameHandler
    {
        public static Random GameRand = new();

        private static int _tankFuncDelay = 190;

        public const int MAX_AI_TANKS = 1000;
        public const int MAX_PLAYERS = 1000;
        public static AITank[] AllAITanks { get; } = new AITank[MAX_AI_TANKS];
        public static PlayerTank[] AllPlayerTanks { get; } = new PlayerTank[MAX_PLAYERS];
        public static Tank[] AllTanks { get; } = new Tank[MAX_PLAYERS + MAX_AI_TANKS];

        public static Campaign LoadedCampaign { get; set; } = new();

        public static Logger ClientLog { get; set;  }

        public static bool InMission { get; set; } = false;

        public static Matrix UIMatrix => Matrix.CreateOrthographicOffCenter(0, TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, 0, -1, 1);

        public delegate void MissionStartEvent();

        public static event MissionStartEvent OnMissionStart;

        public delegate void MissionEndEvent(int delay, MissionEndContext context, bool result1up);

        public static event MissionEndEvent OnMissionEnd;


        private static bool _wasOverhead;

        public static bool ShouldMissionsProgress = true;

        private static bool _wasInMission;

        internal static void MapEvents()
        {
            OnMissionEnd += DoEndMissionWorkload;
        }

        public static void DoEndMissionWorkload(int delay, MissionEndContext context, bool result1up) // bool major = (if true, play M100 fanfare, else M20)
        {
            IntermissionSystem.SetTime(delay);
            
            TankMusicSystem.StopAll();

            if (result1up && context != MissionEndContext.Lose)
                delay += 200;

            if (context == MissionEndContext.CampaignCompleteMajor)
            {
                TankGame.GameData.CampaignsCompleted++;
                var victory = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/mission_complete_M100");
                SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f);

            }
            else if (context == MissionEndContext.CampaignCompleteMinor)
            {
                TankGame.GameData.CampaignsCompleted++;
                var victory = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/mission_complete_M20");
                SoundPlayer.PlaySoundInstance(victory, SoundContext.Effect, 0.5f);
            }
            if (result1up && context == MissionEndContext.Win)
            {
                TankGame.GameData.MissionsCompleted++;
                PlayerTank.Lives++;
                var lifeget = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/life_get");
                SoundPlayer.PlaySoundInstance(lifeget, SoundContext.Effect, 0.5f);
            }
            if (context == MissionEndContext.Lose)
            {
                PlayerTank.Lives--;

                /*int len = $"{VanillaCampaign.CachedMissions.Count(x => !string.IsNullOrEmpty(x.Name))}".Length;
                int diff = len - $"{VanillaCampaign.CurrentMissionId}".Length;

                string realName = "";

                for (int i = 0; i < diff; i++)
                    realName += "0";
                realName += $"{VanillaCampaign.CurrentMissionId + 1}";

                VanillaCampaign.CachedMissions[VanillaCampaign.CurrentMissionId] = Mission.Load(realName, VanillaCampaign.Name);*/
                if (PlayerTank.Lives > 0)
                {
                    var deathSound = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/tank_player_death");
                    SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
                }
                else
                {
                    var deathSound = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/gameover_playerdeath");
                    SoundPlayer.PlaySoundInstance(deathSound, SoundContext.Effect, 0.3f);
                }
            }
            else if (context == MissionEndContext.Win)
            {
                TankGame.GameData.MissionsCompleted++;
                LoadedCampaign.LoadNextMission();
                var victorySound = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/mission_complete");
                SoundPlayer.PlaySoundInstance(victorySound, SoundContext.Effect, 0.5f);
            }
        }
        private static void DoEndScene()
        {
            PlayerTank.TanksKillDict.Clear();
            MainMenu.Open();
            // this will be finished later...
        }
        internal static void UpdateAll()
        {
            // technically, level 0 in code is level 1, so we want to use that number (1) if the user is level 0.

            VanillaAchievements.Repository.UpdateCompletions();
            var floor1 = MathF.Floor(TankGame.GameData.ExpLevel + 1f);
            var floor0 = MathF.Floor(TankGame.GameData.ExpLevel);
            GameData.UniversalExpMultiplier = floor1 - (GameData.DecayPerLevel * floor0);

            if (Difficulties.Types["InfiniteLives"])
                PlayerTank.Lives = PlayerTank.StartingLives;
            foreach (var tank in AllTanks)
                tank?.Update();

            if (InMission)
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
            }
            else
                if (!InMission)
                    if (TankMusicSystem.Songs is not null)
                        foreach (var song in TankMusicSystem.Songs)
                            song.Volume = 0;
            foreach (var expl in Explosion.Explosions)
                expl?.Update();

            if (Difficulties.Types["ThunderMode"])
                DoThunderStuff();

            if (ShouldMissionsProgress && !MainMenu.Active)
                HandleMissionChanging();

            foreach (var cube in Block.AllBlocks)
                cube?.Update();

            if (TankGame.OverheadView)
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

            if (!TankGame.OverheadView && _wasOverhead)
                BeginIntroSequence();

            if (TankGame.OverheadView || MainMenu.Active)
            {
                InMission = false;
                _tankFuncDelay = 600;
            }

            if (_tankFuncDelay > 0)
                _tankFuncDelay--;
            if (_tankFuncDelay == 1)
            {
                if (!InMission)
                {
                    InMission = true;
                    OnMissionStart?.Invoke();
                    TankMusicSystem.PlayAll();
                }
            }

            if (Input.KeyJustPressed(Keys.PageUp))
                SpawnTankPlethorae(true);
            if (Input.KeyJustPressed(Keys.PageDown))
                SpawnMe();

            if (Input.KeyJustPressed(Keys.NumPad7))
                tankToSpawnType--;
            if (Input.KeyJustPressed(Keys.NumPad9))
                tankToSpawnType++;

            if (Input.KeyJustPressed(Keys.NumPad1))
                tankToSpawnTeam--;
            if (Input.KeyJustPressed(Keys.NumPad3))
                tankToSpawnTeam++;

            if (Input.KeyJustPressed(Keys.OemPeriod))
                CubeHeight++;
            if (Input.KeyJustPressed(Keys.OemComma))
                CubeHeight--;

            if (Input.KeyJustPressed(Keys.Z))
                BlockType--;
            if (Input.KeyJustPressed(Keys.X))
                BlockType++;

            if (Input.KeyJustPressed(Keys.Home))
                SpawnTankAt(!TankGame.OverheadView ? GameUtils.GetWorldPosition(GameUtils.MousePosition) : PlacementSquare.CurrentlyHovered.Position, (TankTier)tankToSpawnType, (TankTeam)tankToSpawnTeam);

            if (Input.KeyJustPressed(Keys.OemSemicolon))
                new Mine(null, GameUtils.GetWorldPosition(GameUtils.MousePosition).FlattenZ(), 400);
            if (Input.KeyJustPressed(Keys.OemQuotes))
                new Shell(GameUtils.GetWorldPosition(GameUtils.MousePosition) + new Vector3(0, 11, 0), Vector3.Zero, ShellType.Standard, null, 0, playSpawnSound: false);
            if (Input.KeyJustPressed(Keys.End))
                SpawnCrateAtMouse();

            if (Input.KeyJustPressed(Keys.I) && DebugUtils.DebugLevel == 4)
                new Powerup(powerups[mode]) { Position = GameUtils.GetWorldPosition(GameUtils.MousePosition) + new Vector3(0, 10, 0) };

            if (MainMenu.Active)
                PlayerTank.KillCount = 0;

            CubeHeight = MathHelper.Clamp(CubeHeight, 1, 7);
            BlockType = MathHelper.Clamp(BlockType, 0, 3);

            _wasOverhead = TankGame.OverheadView;
            _wasInMission = InMission;

            if (TankGame.OverheadView)
                HandleLevelEditorModifications();
        }

        private static void DoThunderStuff()
        {
            if (IntermissionSystem.BlackAlpha > 0 || IntermissionSystem.Alpha >= 1f || MainMenu.Active || GameUI.Paused)
            {
                if (Thunder.SoftRain.IsPlaying())
                    Thunder.SoftRain.Stop();

                TankGame.ClearColor = Color.Black;

                GameLight.Color = new(150, 150, 170);
                GameLight.Brightness = 0.71f;

                GameLight.Apply(false);

                return;
            }
            if (!Thunder.SoftRain.IsPlaying())
            {
                Thunder.SoftRain.Play();
            }
            Thunder.SoftRain.Volume = TankGame.Settings.AmbientVolume;
            
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

        private static void HandleMissionChanging()
        {
            if (LoadedCampaign.CachedMissions[0].Name is null)
                return;

            if (LoadedCampaign.CurrentMission.Tanks.Any(tnk => tnk.IsPlayer))
            {
                /*if (AllAITanks.Count(tnk => tnk != null && !tnk.Dead) <= 0)
                {
                    InMission = false;
                    // if a 1-up mission, extend by X amount of time (TBD?)
                    if (!InMission && _wasInMission)
                        OnMissionEnd?.Invoke(600, false);
                }
                else if (AllPlayerTanks.Count(tnk => tnk != null && !tnk.Dead) <= 0)
                {
                    InMission = false;

                    if (!InMission && _wasInMission)
                        OnMissionEnd?.Invoke(600, true);
                }*/
                var activeTeams = Tank.GetActiveTeams();

                bool isExtraLifeMission = LoadedCampaign.Properties.ExtraLivesMissions.Contains(LoadedCampaign.CurrentMissionId + 1);

                int restartTime = 600;

                if (activeTeams.Contains(TankTeam.NoTeam) && AllTanks.Count(tnk => tnk != null && !tnk.Properties.Dead) <= 1)
                {
                    InMission = false;
                    // if a 1-up mission, extend by X amount of time (TBD?)
                    if (!InMission && _wasInMission)
                    {
                        MissionEndContext cxt = AllPlayerTanks.Count(tnk => tnk != null && !tnk.Properties.Dead) <= 0 ? MissionEndContext.Lose : MissionEndContext.Win;
                        OnMissionEnd?.Invoke(restartTime, cxt, isExtraLifeMission);
                    }
                }
                else if (!activeTeams.Contains(TankTeam.NoTeam) && activeTeams.Count <= 1) // check if it's not only FFA, and if teams left doesnt contain ffa.
                {
                    InMission = false;
                    // if a 1-up mission, extend by X amount of time (TBD?)
                    if (!InMission && _wasInMission)
                    {
                        MissionEndContext cxt = !activeTeams.Contains(PlayerTank.MyTeam) ? MissionEndContext.Lose : MissionEndContext.Win;
                        OnMissionEnd?.Invoke(restartTime, cxt, isExtraLifeMission);
                    }
                }
            }
            else
            {
                var activeTeams = Tank.GetActiveTeams();

                bool isExtraLifeMission = LoadedCampaign.Properties.ExtraLivesMissions.Contains(LoadedCampaign.CurrentMissionId);

                int restartTime = 600;
                if (isExtraLifeMission)
                    restartTime += 200;
                // if a player was not initially spawned in the mission, check if a team is still alive and end the mission
                if (activeTeams.Contains(TankTeam.NoTeam) && AllTanks.Count(tnk => tnk != null && !tnk.Properties.Dead) <= 1)
                {
                    InMission = false;
                    // if a 1-up mission, extend by X amount of time (TBD?)
                    if (!InMission && _wasInMission)
                        OnMissionEnd?.Invoke(restartTime, MissionEndContext.Win, isExtraLifeMission);
                }
                else if (!activeTeams.Contains(TankTeam.NoTeam) && activeTeams.Count <= 1)
                {
                    InMission = false;
                    // if a 1-up mission, extend by X amount of time (TBD?)
                    if (!InMission && _wasInMission)
                        OnMissionEnd?.Invoke(restartTime, MissionEndContext.Win, isExtraLifeMission);
                }
            }
            if (IntermissionSystem.CurrentWaitTime > 0)
                IntermissionSystem.Tick(1);

            if (IntermissionSystem.CurrentWaitTime == 220)
                BeginIntroSequence();
            if (IntermissionSystem.CurrentWaitTime == IntermissionSystem.WaitTime / 2 && IntermissionSystem.CurrentWaitTime != 0)
                LoadedCampaign.SetupLoadedMission(AllPlayerTanks.Count(tnk => tnk != null && !tnk.Properties.Dead) > 0);
            if (IntermissionSystem.CurrentWaitTime > 240 && IntermissionSystem.CurrentWaitTime < IntermissionSystem.WaitTime - 150)
            {
                if (PlayerTank.Lives <= 0)
                    DoEndScene();
                IntermissionSystem.TickAlpha(1f / 45f);
            }
            else
                IntermissionSystem.TickAlpha(-1f / 45f);
            if (IntermissionSystem.CurrentWaitTime == IntermissionSystem.WaitTime - 180)
            {
                CleanupScene();
                SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>("Assets/fanfares/mission_starting"), SoundContext.Effect, 0.8f);
            }
        }

        public static int BlockType = 0;
        public static int CubeHeight = 1;
        public static int tankToSpawnType;
        public static int tankToSpawnTeam;

        internal static void RenderAll()
        {
            TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

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

            TankGame.Instance.GraphicsDevice.BlendState = BlendState.Additive;
            foreach (var expl in Explosion.Explosions)
                expl?.Render();
            TankGame.Instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            foreach (var mark in TankDeathMark.deathMarks)
                mark?.Render();

            foreach (var print in TankFootprint.footprints)
                print?.Render();

            foreach (var crate in Crate.crates)
                crate?.Render();

            foreach (var powerup in Powerup.Powerups)
                powerup?.Render();

            if (TankGame.OverheadView)
                foreach (var sq in PlacementSquare.Placements)
                    sq?.Render();

            ParticleSystem.RenderParticles();
            MainMenu.Render();

            foreach (var body in Tank.CollisionsWorld.BodyList)
            {
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"BODY", 
                    GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(body.Position.X, 0, body.Position.Y), TankGame.GameView, TankGame.GameProjection), centered: true);
            }

            for (int i = 0; i < VanillaAchievements.Repository.GetAchievements().Count; i++)
            {
                var achievement = VanillaAchievements.Repository.GetAchievements()[i];
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{achievement.Name}: {(achievement.IsComplete ? "Complete" : "Incomplete")}",
                    new Vector2(8, 24 + (i * 20)), level: DebugUtils.Id.AchievementData, centered: false);
            }
            // TODO: Fix translation
            // TODO: Scaling with screen size.

            foreach (var element in UIElement.AllUIElements.ToList()) {
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
            {
                element?.DrawTooltips(TankGame.SpriteRenderer);
            }
            tankToSpawnType = MathHelper.Clamp(tankToSpawnType, 2, Enum.GetValues<TankTier>().Length - 1);
            tankToSpawnTeam = MathHelper.Clamp(tankToSpawnTeam, 0, Enum.GetValues<TankTeam>().Length - 1);

            #region TankInfo
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "Spawn Tank With Info:", GameUtils.WindowTop + new Vector2(0, 8), 3, centered: true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Tier: {Enum.GetNames<TankTier>()[tankToSpawnType]}", GameUtils.WindowTop + new Vector2(0, 24), 3, centered: true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Team: {Enum.GetNames<TankTeam>()[tankToSpawnTeam]}", GameUtils.WindowTop + new Vector2(0, 40), 3, centered: true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"CubeStack: {CubeHeight} | CubeType: {Enum.GetNames<Block.BlockType>()[BlockType]}", GameUtils.WindowBottom - new Vector2(0, 20), 3, centered: true);

            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"HighestTier: {AITank.GetHighestTierActive()}", new(10, GameUtils.WindowHeight * 0.26f), 1);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f).Name : "N/A")}", new(10, GameUtils.WindowHeight - 100), 1);
            for (int i = 0; i < Enum.GetNames<TankTier>().Length; i++)
            {
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{Enum.GetNames<TankTier>()[i]}: {AITank.GetTankCountOfType((TankTier)i)}", new(10, GameUtils.WindowHeight * 0.3f + (i * 20)), 1);
            }
            #endregion

            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Logic Time: {TankGame.LogicTime.TotalMilliseconds:0.00}ms" +
                $"\nLogic FPS: {TankGame.LogicFPS}" +
                $"\n\nRender Time: {TankGame.RenderTime.TotalMilliseconds:0.00}ms" +
                $"\nRender FPS: {TankGame.RenderFPS}", new(10, 500));

            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"Current Mission: {LoadedCampaign.CurrentMission.Name}\nCurrent Campaign: {LoadedCampaign.Properties.Name}", GameUtils.WindowBottomLeft - new Vector2(-4, 40), 3, centered: false);
            if (!MainMenu.Active)
            {
                if (IntermissionSystem.IsAwaitingNewMission)
                {
                    
                }
                for (int i = -4; i < 10; i++)
                {
                    IntermissionSystem.DrawShadowedTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard"), new Vector2(i * 14, GameUtils.WindowHeight * 0.9f), Vector2.UnitY, Color.White, new(2f, 2f), 1f, new(0, GameResources.GetGameResource<Texture2D>("Assets/textures/ui/scoreboard").Size().Y / 2), true);
                }
                IntermissionSystem.DrawShadowedString(new Vector2(80, GameUtils.WindowHeight * 0.89f), Vector2.One, $"{PlayerTank.KillCount}", new(119, 190, 238), new(0.675f), 1f);
            }
            IntermissionSystem.Draw(TankGame.SpriteRenderer);

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
            GameUI.MissionInfoBar.IsVisible = !MainMenu.Active;
        }


        private static int _oldelta;
        public static void HandleLevelEditorModifications()
        {
            var cur = PlacementSquare.CurrentlyHovered;

            if (cur is not null && cur.HasBlock)
            {
                if (Block.AllBlocks[cur.CurrentBlockId].Type == Block.BlockType.Teleporter)
                {
                    // ChatSystem.SendMessage($"{Input.DeltaScrollWheel}", Color.White);

                    if (Input.DeltaScrollWheel != _oldelta)
                        Block.AllBlocks[cur.CurrentBlockId].TpLink += (sbyte)(Input.DeltaScrollWheel - _oldelta);
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
            Brightness = 0.71f,

            //color = new(150, 150, 170),
            //brightness = 0.1f,
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
        public static void LoadTnkScene()
        {
            if (!_musicLoaded)
            {
                TankMusicSystem.LoadMusic();
                TankMusicSystem.LoadAmbienceTracks();
                _musicLoaded = true;
            }
            else
            {
                foreach (var song in TankMusicSystem.Songs)
                {
                    song.Stop();
                }
                TankMusicSystem.forestAmbience.Stop();
                TankMusicSystem.forestAmbience.Play();
            }
        }

        public static PlayerTank SpawnMe()
        {
            var pos = TankGame.OverheadView ? PlacementSquare.CurrentlyHovered.Position : GameUtils.GetWorldPosition(GameUtils.MousePosition);
            var myTank = new PlayerTank(PlayerType.Blue);

            myTank.Properties.Team = (TankTeam)tankToSpawnTeam;
            myTank.Properties.Dead = false;
            myTank.Body.Position = pos.FlattenZ();
            myTank.Properties.Position = pos.FlattenZ();

            if (Client.IsConnected())
                Client.RequestPlayerTankSpawn(myTank);
            return myTank;
        }

        public static void SpawnTankInCrate(TankTier tierOverride = default, TankTeam teamOverride = default, bool createEvenDrop = false)
        {
            var random = new CubeMapPosition(GameRand.Next(0, 26), GameRand.Next(0, 20));

            var drop = Crate.SpawnCrate(new(CubeMapPosition.Convert3D(random).X, 500 + (createEvenDrop ? 0 : GameRand.Next(-300, 301)), CubeMapPosition.Convert3D(random).Z), 2f);
            drop.scale = 1.25f;
            drop.TankToSpawn = new TankTemplate()
            {
                AiTier = tierOverride == default ? AITank.PickRandomTier() : tierOverride,
                Team = teamOverride == default ? GameUtils.PickRandom<TankTeam>() : teamOverride
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
                Team = TankTeam.NoTeam
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
        }
        public static void BeginIntroSequence()
        {
            _tankFuncDelay = 190;

            TankMusicSystem.StopAll();

            var tune = GameResources.GetGameResource<SoundEffect>("Assets/fanfares/mission_snare");

            SoundPlayer.PlaySoundInstance(tune, SoundContext.Music, 1f);

            foreach (var tank in AllTanks)
                if (tank is not null)
                    tank.Properties.Velocity = Vector2.Zero;

            CleanupScene();

            InMission = false;
        }
        public static AITank SpawnTank(TankTier tier, TankTeam team)
        {
            var rot = GeometryUtils.GetPiRandom();

            var t = new AITank(tier);
            t.Properties.TankRotation = rot;
            t.Properties.TurretRotation = rot;
            t.Properties.Team = team;
            t.Properties.Dead = false;
            var pos = new CubeMapPosition(GameRand.Next(0, 27), GameRand.Next(0, 20));
            t.Body.Position = pos;
            t.Properties.Position = pos;

            return t;
        }
        public static AITank SpawnTankAt(Vector3 position, TankTier tier, TankTeam team)
        {
            var rot = 0f;//GeometryUtils.GetPiRandom();

            var x = new AITank(tier);
            x.Properties.TargetTankRotation = rot - MathHelper.Pi;
            x.Properties.TankRotation = rot;
            x.Properties.TurretRotation = -rot;

            x.Properties.Team = team;
            x.Properties.Dead = false;
            x.Body.Position = position.FlattenZ();
            x.Properties.Position = position.FlattenZ();
            return x;
        }
        public static void SpawnTankPlethorae(bool useCurTank = false)
        {
            for (int i = 0; i < 5; i++)
            {
                var random = new CubeMapPosition(GameRand.Next(0, 23),GameRand.Next(0, 18));
                var rot = GeometryUtils.GetPiRandom();
                var t = new AITank(useCurTank ? (TankTier)tankToSpawnType : AITank.PickRandomTier());
                t.Properties.TankRotation = rot;
                t.Properties.TurretRotation = rot;
                t.Properties.Dead = false;
                t.Properties.Team = useCurTank ? (TankTeam)tankToSpawnTeam : TankTeam.NoTeam;
                t.Body.Position = random;
                t.Properties.Position = random;
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
                    /*using OpenFileDialog fdlg = new();
                    fdlg.Title = "Load a supported mission file...";
                    fdlg.InitialDirectory = TankGame.SaveDirectory;
                    fdlg.Filter = "Tanks Rebirth Levels (*.mission)|*.mission";
                    fdlg.FilterIndex = 1;

                    if (fdlg.ShowDialog() == true)
                    {
                        try {
                            Mission.Load(fdlg.FileName, null);
                            ChatSystem.SendMessage($"Loaded mission '{Path.GetFileNameWithoutExtension(fdlg.FileName)}'.", Color.White);
                        }
                        catch {
                            ChatSystem.SendMessage("Failed to load mission.", Color.Red);
                        }
                    }*/
                    
                    var res = Dialog.FileOpen("mission", TankGame.SaveDirectory);
                    if (res.Path != null && res.IsOk)
                    {
                        try
                        {
                            LoadedCampaign.LoadMission(Mission.Load(res.Path, null));
                            LoadedCampaign.SetupLoadedMission(true);
                            
                            ChatSystem.SendMessage($"Loaded mission '{Path.GetFileNameWithoutExtension(res.Path)}'.", Color.White);
                        }
                        catch
                        {
                            ChatSystem.SendMessage("Failed to load mission.", Color.Red);
                        }
                    }
                    return;
                }

                LoadedCampaign.LoadMission(Mission.Load(MissionName.GetRealText(), CampaignName.IsEmpty() ? null : CampaignName.GetRealText()));
                LoadedCampaign.SetupLoadedMission(true);
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
                LoadedCampaign = Campaign.LoadFromFolder(CampaignName.GetRealText(), true);
                LoadedCampaign.SetupLoadedMission(true);
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
                if (TankFootprint.footprints[i] != null)
                    TankFootprint.footprints[i].track?.Destroy();
                TankFootprint.footprints[i] = null;
            }

            TankFootprint.total_treads_placed = 0;
        }
    }

    public static class MouseRenderer
    {
        public static Texture2D MouseTexture { get; private set; }

        public static int numDots = 10;

        private static float _sinScale;

        public static bool ShouldRender = true;

        public static void DrawMouse()
        {
            if (!ShouldRender)
                return;
            if (GameHandler.AllPlayerTanks[0] is not null)
            {
                // mwvar tankPos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, GameHandler.myTank.World, TankGame.GameView, TankGame.GameProjection);

                // var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes");

                // GameHandler.ClientLog.Write("One Loop:", LogType.Info);

                /*for (int i = 0; i < numDots; i++)
                {
                    var ii = 1f / i;

                    var pos = (GameUtils.MousePosition - tankPos) * (ii * i) + tankPos;

                    GameHandler.ClientLog.Write(pos, LogType.Info);

                    TankGame.spriteBatch.Draw(tex, pos, null, Color.White, 0f, tex.Size() / 2, 1f, default, default);
                }*/

                // lata
            }

            _sinScale = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);

            MouseTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/cursor_1");
            TankGame.SpriteRenderer.Draw(MouseTexture, GameUtils.MousePosition, null, Color.White, 0f, MouseTexture.Size() / 2, 1f + _sinScale / 16, default, default);
        }
    }
    public class GameShaders
    {
        public static Effect MouseShader { get; set; }
        public static Effect GaussianBlurShader { get; set; }

        public static void Initialize()
        {
            GaussianBlurShader = GameResources.GetGameResource<Effect>("Assets/Shaders/GaussianBlur");
            MouseShader = GameResources.GetGameResource<Effect>("Assets/Shaders/MouseShader");
        }

        public static void UpdateShaders()
        {
            MouseShader.Parameters["oGlobalTime"].SetValue((float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);
            MouseShader.Parameters["oColor"].SetValue(new Vector3(0f, 0f, 1f));
            MouseShader.Parameters["oSpeed"].SetValue(-20f);
            MouseShader.Parameters["oSpacing"].SetValue(10f);

            GaussianBlurShader.Parameters["oResolution"].SetValue(new Vector2(GameUtils.WindowWidth, GameUtils.WindowHeight));
            GaussianBlurShader.Parameters["oBlurFactor"].SetValue(6f);
        }
    }
}
