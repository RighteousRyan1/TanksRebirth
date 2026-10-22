using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tainicom.Aether.Physics2D.Collision.Shapes;
using TanksRebirth.Achievements;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Seasonal;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.Tanks.AI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.DynamicLighting;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.IO;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.RebirthUtils;

#pragma warning disable 
public static class DebugManager {
    public readonly struct Id {
        public const int FREECAM = -3;
        public const int AIRPLANE = -2;
        public const int METRICS = -1;
        public const int GENERAL = 0;
        public const int ENT_DAT = 1;
        public const int PL_DAT = 2;
        public const int LVL_DBG = 3;
        public const int PWR = 4;
        public const int ACH_DAT = 5;
        public const int TankData = 6;
        public const int LIGHTS = 7;
    }
    private static readonly Dictionary<int, string> DebuggingNames = new() {
        [Id.FREECAM] = "freecam",
        [Id.AIRPLANE] = "planetest",
        [Id.METRICS] = "metrics",
        [Id.GENERAL] = "gen", // general
        [Id.ENT_DAT] = "entdat", // entity data
        [Id.PL_DAT] = "plrdat", // plrdat
        [Id.LVL_DBG] = "lvlmake", // level editor debug
        [Id.PWR] = "pwrup", // powerup
        [Id.ACH_DAT] = "achdat", // achievement data
        [Id.TankData] = "tnknav", // Owner tank navigation
        [Id.LIGHTS] = "lights" // dynamic lighting
    };

    static int mode;
    static bool _collPersist;
    public static bool RenderWireframe = false;
    public static bool IsFreecamEnabled => persistFreecam || DebugLevel == Id.FREECAM;
    public static bool persistFreecam;
    public static int blockType = 0;
    public static int blockHeight = 1;
    public static int tankToSpawnType;
    public static int tankToSpawnTeam;
    public static string CurDebugLabel => !DebuggingNames.TryGetValue(DebugLevel, out string? value) ? $"Unknown - {DebugLevel}" : value;
    public static bool DebuggingEnabled { get; set; }
    public static bool SecretCosmeticSetting;
    public static bool SuperSecretDevOption;
    public static int DebugLevel { get; set; }

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

    static readonly PowerupTemplate[] powerups = [Powerup.Speed, Powerup.ShellHome, Powerup.Invisibility];

    static Animator _povAnimatorTest; // used in debug cam mode to create custom transitions
    static EasingFunction _povAnimatorFunction;
    static List<KeyFrame> _povAnimatorKeyframeBuilder = [];
    static double _povAnimatorDuration = 1.00;
    static List<Vector3> _bezierPts = [];

    static bool _hideUi;

    // this method seems like stack hell. i probably should remove it
    public static void DrawDebugString(SpriteBatch sb, object info, Vector2 position, int level = Id.GENERAL, float scale = 1f, bool centered = false, Color color = default, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        var sizeAdjust = new Vector2(scale * 0.6f * (float)(WindowUtils.WindowWidth / 1920f), scale * 0.6f * (float)(WindowUtils.WindowHeight / 1080f));

        sb.DrawString(FontGlobals.RebirthFont, info.ToString(), position, color == default ? Color.White : color, sizeAdjust, 0f, centered ? FontGlobals.RebirthFont.MeasureString(info.ToString()) / 2 : default);

        if (beginSb)
            sb.End();
    }
    public static void DrawDebugString(SpriteFontBase font, SpriteBatch sb, object info, Vector2 position, int level = Id.GENERAL, float scale = 1f, bool centered = false, Color color = default, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        var sizeAdjust = new Vector2(scale * 0.6f * (float)(WindowUtils.WindowWidth / 1920f), scale * 0.6f * (float)(WindowUtils.WindowHeight / 1080f));

        sb.DrawString(font, info.ToString(), position, color == default ? Color.White : color, sizeAdjust, 0f, centered ? FontGlobals.RebirthFont.MeasureString(info.ToString()) / 2 : default);

        if (beginSb)
            sb.End();
    }
    public static void DrawDebugTexture(SpriteBatch sb, Texture2D texture, Vector2 position, int level = Id.GENERAL, float scale = 1f, Color color = default, bool centered = false, bool beginSb = false) {
        if (!DebuggingEnabled || DebugLevel != level)
            return;

        if (beginSb)
            sb.Begin();

        sb.Draw(texture, position, null, color == default ? Color.White : color, 0f, centered ? texture.Size() / 2 : default, scale, default, 0f);

        if (beginSb)
            sb.End();
    }
    public static void InitDebugUI() {
        if (!GameLauncher.IsConsoleAllocated) return;

        MissionName = new(FontGlobals.RebirthFont, Color.White, 0.75f, 20) {
            DefaultString = "Mission Name",
            IsVisible = false
        };
        MissionName.SetDimensions(20, 60, 230, 50);
        CampaignName = new(FontGlobals.RebirthFont, Color.White, 0.75f, 20) {
            DefaultString = "Campaign Name",
            IsVisible = false
        };
        CampaignName.SetDimensions(20, 120, 230, 50);

        SaveMission = new("Save", FontGlobals.RebirthFont, Color.White, 0.5f);
        SaveMission.OnLeftClick = (l) => {
            if (MissionName.IsEmpty()) {
                ChatSystem.SendMessage("Invalid name for mission.", Color.Red);
                return;
            }
            Mission.GetCurrent(CampaignGlobals.LoadedCampaign.CurrentMission.Name).Save(MissionName.GetRealText());

            ChatSystem.SendMessage($"Saved mission '{MissionName.GetRealText()}'.", Color.White);
        };
        SaveMission.IsVisible = false;
        SaveMission.SetDimensions(20, 180, 105, 50);

        LoadMission = new("Load", FontGlobals.RebirthFont, Color.White, 0.5f);
        LoadMission.OnLeftClick = (l) => {
            if (RuntimeData.IsWindows && MissionName.IsEmpty()) {
                var res = Dialog.FileOpen("mission", TankGame.SaveDirectory);
                if (res.Path != null && res.IsOk) {
                    try {
                        CampaignGlobals.LoadedCampaign.LoadMission(Mission.Load(res.Path, null));
                        CampaignGlobals.LoadedCampaign.SetupLoadedMission(true);

                        ChatSystem.SendMessage($"Loaded mission '{Path.GetFileNameWithoutExtension(res.Path)}'.", Color.White);
                    } catch {
                        ChatSystem.SendMessage("Failed to load mission.", Color.Red);
                    }
                }
                return;
            }

            CampaignGlobals.LoadedCampaign.LoadMission(Mission.Load(MissionName.GetRealText(), CampaignName.IsEmpty() ? null : CampaignName.GetRealText()));
            CampaignGlobals.LoadedCampaign.SetupLoadedMission(true);
        };
        LoadMission.IsVisible = false;
        LoadMission.SetDimensions(145, 180, 105, 50);

        LoadCampaign = new("Load Campaign", FontGlobals.RebirthFont, Color.White, 0.75f) {
            OnLeftClick = (l) => {
                if (MissionName.IsEmpty()) {
                    ChatSystem.SendMessage("Invalid name for campaign.", Color.Red);
                    return;
                }
                CampaignGlobals.LoadedCampaign = Campaign.Load(CampaignName.GetRealText());
                CampaignGlobals.LoadedCampaign.SetupLoadedMission(true);
            },
            IsVisible = false
        };
        LoadCampaign.SetDimensions(20, 240, 230, 50);

        ClearTracks = new("Clear Tracks", FontGlobals.RebirthFont, Color.LightBlue, 0.5f);
        ClearTracks.SetDimensions(250, 25, 100, 50);
        ClearTracks.IsVisible = false;

        ClearTracks.OnLeftClick = (a) => SceneManager.ClearTankTracks();

        ClearChecks = new("Clear Checks", FontGlobals.RebirthFont, Color.LightBlue, 0.5f);
        ClearChecks.SetDimensions(250, 95, 100, 50);
        ClearChecks.IsVisible = false;

        ClearChecks.OnLeftClick = (a) => SceneManager.ClearTankDeathmarks();

        SetupMissionAgain = new("Restart\nMission", FontGlobals.RebirthFont, Color.LightBlue, 0.5f);
        SetupMissionAgain.SetDimensions(250, 165, 100, 50);
        SetupMissionAgain.IsVisible = false;

        SetupMissionAgain.OnLeftClick = (obj) => IntermissionHandler.BeginIntroSequence();

        MovePULeft = new("<", FontGlobals.RebirthFont, Color.LightBlue, 0.5f);
        MovePULeft.SetDimensions(WindowUtils.WindowWidth / 2 - 100, 25, 50, 50);
        MovePULeft.IsVisible = false;

        MovePURight = new(">", FontGlobals.RebirthFont, Color.LightBlue, 0.5f);
        MovePURight.SetDimensions(WindowUtils.WindowWidth / 2 + 100, 25, 50, 50);
        MovePURight.IsVisible = false;

        Display = new(powerups[mode].Name, FontGlobals.RebirthFont, Color.LightBlue, 0.5f);
        Display.SetDimensions(WindowUtils.WindowWidth / 2 - 35, 25, 125, 50);
        Display.IsVisible = false;

        MovePULeft.OnLeftClick = (obj) => {
            if (mode < powerups.Length - 1)
                mode++;
            Display.Text = powerups[mode].Name;
        };
        MovePURight.OnLeftClick = (obj) => {
            if (mode > 0)
                mode--;
            Display.Text = powerups[mode].Name;
        };
    }
    static JackOLantern jol;
    public static void UpdateDebug() {
        if (!GameLauncher.IsConsoleAllocated) return;
        if (InputUtils.KeyJustPressed(Keys.F4))
            DebuggingEnabled = !DebuggingEnabled;

        if (!MainMenuUI.IsActive) {
            ClearTracks.IsVisible = DebuggingEnabled && DebugLevel == 0;
            ClearChecks.IsVisible = DebuggingEnabled && DebugLevel == 0;
            SetupMissionAgain.IsVisible = DebuggingEnabled && DebugLevel == 0;
            MovePULeft.IsVisible = DebuggingEnabled && DebugLevel == 4;
            MovePURight.IsVisible = DebuggingEnabled && DebugLevel == 4;
            Display.IsVisible = DebuggingEnabled && DebugLevel == 4;
            MissionName.IsVisible = DebuggingEnabled && DebugLevel == 3;
            LoadMission.IsVisible = DebuggingEnabled && DebugLevel == 3;
            SaveMission.IsVisible = DebuggingEnabled && DebugLevel == 3;
            LoadCampaign.IsVisible = DebuggingEnabled && DebugLevel == 3;
            CampaignName.IsVisible = DebuggingEnabled && DebugLevel == 3;
            // wtf was this
        }

        if (!DebuggingEnabled)
            return; // won't update debug if debugging is not currently enabled.

        if (InputUtils.KeyJustPressed(Keys.H)) {
            _hideUi = !_hideUi;
            TankGame.gameUiDraw = !TankGame.gameUiDraw;
        }

        // guh...
        if (InputUtils.KeyJustPressed(Keys.L)) {
            jol?.Remove();
            jol = JackOLantern.Create(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition, 0) + new Vector3(0, 35, 0), 0f, 15f);
        }

        if (SuperSecretDevOption) {
            var tnkGet = Array.FindIndex(GameHandler.AllAITanks, x => x is not null && !x.IsDestroyed && !x.Properties.Stationary);
            if (tnkGet > -1) {
                var tnk = GameHandler.AllAITanks[tnkGet];
                tnk.DesiredChassisRotation = MathUtils.RotationOf(MouseUtils.MousePosition - MatrixUtils.ConvertWorldToScreen(Vector3.Zero, tnk.DrawParams.World, tnk.DrawParams.View, tnk.DrawParams.Projection));
            }
        }

        if (RuntimeData.RunTime % 60 <= RuntimeData.DeltaFrames) {
            RuntimeData.MemoryUsageInBytes = RuntimeData.ProcessMemory;
        }
        if (DebugLevel == Id.METRICS) {
            RuntimeData.RenderFpsGraph.Update();
            RuntimeData.LogicFpsGraph.Update();

            RuntimeData.RenderTimeGraph.Update();
            RuntimeData.LogicTimeGraph.Update();
        }
        if (MainMenuUI.MenuState == MainMenuUI.UIState.Multiplayer) {
            if (InputUtils.AreKeysJustPressed(Keys.Q, Keys.W)) {
                MainMenuUI.IPInput.Text = "localhost";
                MainMenuUI.PortInput.Text = "7777";
                MainMenuUI.ServerNameInput.Text = "TestServer";
                MainMenuUI.UsernameInput.Text = Client.ClientRandom.Next(0, ushort.MaxValue).ToString();
            }
        }
        if (InputUtils.AreKeysJustPressed(Keys.Q, Keys.E))
            Server.SyncSeeds();
        if (InputUtils.KeyJustPressed(Keys.M))
            if (PlayerTank.ClientTank is not null)
                ParticleGameplay.CreateSmokeGrenade(GameHandler.Particles, PlayerTank.ClientTank.Position3D + new Vector3(0, 10, 0), Vector3.Up);
        if (InputUtils.KeyJustPressed(Keys.G)) {
            TankGame.VanillaAchievementPopupHandler.SummonOrQueue(Client.ClientRandom.Next(VanillaAchievements.Repository.GetAchievements().Count));
        }
        if (InputUtils.KeyJustPressed(Keys.OemPipe)) {
            Explosion.Create(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition).FlattenZ(), 5f);
        }

        if (InputUtils.AreKeysJustPressed(Keys.LeftShift, Keys.RightShift))
            RenderWireframe = !RenderWireframe;

        if (InputUtils.KeyJustPressed(Keys.F6))
            DebugLevel++;
        if (InputUtils.KeyJustPressed(Keys.F5))
            DebugLevel--;

        if (MainMenuUI.IsActive) return;
        if (InputUtils.KeyJustPressed(Keys.Z))
            blockType--;
        if (InputUtils.KeyJustPressed(Keys.X))
            blockType++;
        if (InputUtils.KeyJustPressed(Keys.J))
            CameraGlobals.OverheadView = !CameraGlobals.OverheadView;

        if (DebugLevel != Id.FREECAM) {
            if (InputUtils.MouseRight)
                CameraGlobals.OrthoRotationVector += MouseUtils.MouseVelocity / 500f;

            if (InputUtils.MouseRight && InputUtils.MouseLeft) {
                CameraGlobals.OrthoRotationVector = new Vector2(0, LevelEditorUI.IsActive ? CameraGlobals.LVL_EDIT_ANGLE : CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE);
                CameraGlobals.AddativeZoom = LevelEditorUI.IsActive ? CameraGlobals.LVL_EDIT_ZOOM : 1f;
                CameraGlobals.CameraFocusOffset = LevelEditorUI.IsActive ? new Vector3(0, CameraGlobals.LVL_EDIT_Y_OFF, 0) : Vector3.Zero;
            }

            if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.Subtract))
                CameraGlobals.AddativeZoom += 0.025f * RuntimeData.DeltaFrames;
            if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.Add))
                CameraGlobals.AddativeZoom -= 0.025f * RuntimeData.DeltaFrames;

            if (InputUtils.MouseMiddle) {
                var mtx = CameraGlobals.GameView;
                Vector3 cameraRight = mtx.Right;
                Vector3 cameraUp = mtx.Up;

                cameraRight.Normalize();
                cameraUp.Normalize();

                Vector3 movement = (cameraRight * MouseUtils.MouseVelocity.X) + (cameraUp * MouseUtils.MouseVelocity.Y);
                ChatSystem.SendMessage($"movement: {movement}");
                ChatSystem.SendMessage($"right:    {cameraRight}");

                CameraGlobals.CameraFocusOffset += movement;
            }
        }
        else {
            ref var fc = ref CameraGlobals.RebirthFreecam;
            if (InputUtils.KeyJustPressed(Keys.Enter)) {
                List<Vector3> pts = [];

                for (int i = 0; i < _bezierPts.Count; i++)
                    pts.Add(_bezierPts[i]);

                _povAnimatorKeyframeBuilder.Add(new(
                    position: fc.Position,
                    scale: fc.Rotation,
                    duration: TimeSpan.FromSeconds(_povAnimatorDuration), easing: _povAnimatorFunction,
                    floats: [fc.FieldOfView],
                    bezierPoints: pts));

                _bezierPts.Clear();
            }
            if (InputUtils.KeyJustPressed(Keys.LeftAlt)) {
                _bezierPts.Add(fc.Position);
            }
            if (InputUtils.AreKeysJustPressed(Keys.K, Keys.OemPlus)) {
                _povAnimatorFunction++;
                if (_povAnimatorFunction > EasingFunction.InOutBounce)
                    _povAnimatorFunction = EasingFunction.Linear;
            }
            else if (InputUtils.AreKeysJustPressed(Keys.K, Keys.OemMinus)) {
                _povAnimatorFunction--;
                if (_povAnimatorFunction < EasingFunction.Linear)
                    _povAnimatorFunction = EasingFunction.InOutBounce;
            }
            if (InputUtils.AreKeysJustPressed(Keys.T, Keys.OemMinus))
                _povAnimatorDuration -= 0.25;
            else if (InputUtils.AreKeysJustPressed(Keys.T, Keys.OemPlus))
                _povAnimatorDuration += 0.25;
            if (InputUtils.AreKeysJustPressed(Keys.OemMinus, Keys.OemPlus)) {
                if (_povAnimatorKeyframeBuilder.Count > 0) {
                    _povAnimatorTest?.Stop();

                    _povAnimatorTest = Animator.Create();

                    for (int i = 0; i < _povAnimatorKeyframeBuilder.Count; i++) {
                        _povAnimatorTest.WithFrame(_povAnimatorKeyframeBuilder[i]);
                    }
                    _povAnimatorTest.Restart();
                    _povAnimatorTest.Run();
                }
            }

            if (InputUtils.KeyJustPressed(Keys.Delete)) {
                if (_povAnimatorKeyframeBuilder.Count > 0)
                    _povAnimatorKeyframeBuilder.RemoveAt(_povAnimatorKeyframeBuilder.Count - 1);
            }

            if (_povAnimatorTest != null && _povAnimatorTest.IsPlaying) {
                fc.Position = _povAnimatorTest.CurrentPosition;
                // ehhh. whatever. it's okay
                fc.Rotation = _povAnimatorTest.CurrentScale;
                fc.FieldOfView = _povAnimatorTest.CurrentFloats[0];
            }

            if (InputUtils.AreKeysJustPressed(Keys.LeftShift, Keys.X)) {
                fc.Position = new(0, fc.Position.Y, fc.Position.Z);
            }
            else if (InputUtils.AreKeysJustPressed(Keys.LeftShift, Keys.Y)) {
                fc.Position = new(fc.Position.X, 0, fc.Position.Z);
            }
            else if (InputUtils.AreKeysJustPressed(Keys.LeftShift, Keys.Z)) {
                fc.Position = new(fc.Position.X, fc.Position.Y, 0);
            }
            
            if (InputUtils.AreKeysJustPressed(Keys.RightShift, Keys.Y)) {
                fc.Rotation = new(0, SnapToQuarterCircle(fc.Rotation.Y), fc.Rotation.Z);
            }
            else if (InputUtils.AreKeysJustPressed(Keys.RightShift, Keys.Z)) {
                fc.Rotation = new(0, fc.Rotation.Y, SnapToQuarterCircle(fc.Rotation.Z));
            }
        }

        if (InputUtils.KeyJustPressed(Keys.NumPad7)) tankToSpawnType--;
        if (InputUtils.KeyJustPressed(Keys.NumPad9)) tankToSpawnType++;

        if (InputUtils.KeyJustPressed(Keys.NumPad1)) tankToSpawnTeam--;
        if (InputUtils.KeyJustPressed(Keys.NumPad3)) tankToSpawnTeam++;

        if (InputUtils.KeyJustPressed(Keys.OemPeriod)) blockHeight++;
        if (InputUtils.KeyJustPressed(Keys.OemComma)) blockHeight--;


        if (InputUtils.KeyJustPressed(Keys.PageDown)) SpawnMe(PlayerID.Blue, tankToSpawnTeam);
        if (InputUtils.KeyJustPressed(Keys.Home))
            SpawnTankAt(!CameraGlobals.OverheadView ? MatrixUtils.GetWorldPosition(MouseUtils.MousePosition) : EditorTile.CurrentlyHovered.Position, tankToSpawnType, tankToSpawnTeam);

        if (InputUtils.KeyJustPressed(Keys.OemSemicolon)) Mine.Create(null, MatrixUtils.GetWorldPosition(MouseUtils.MousePosition).FlattenZ(), 400);
        if (InputUtils.KeyJustPressed(Keys.OemQuotes)) Shell.Create(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition).FlattenZ(), Vector2.Zero, ShellID.Rocket, null!, 0);
        if (InputUtils.KeyJustPressed(Keys.End)) SpawnCrateAtMouse();

        if (InputUtils.KeyJustPressed(Keys.OemQuestion)) new Block(blockType, blockHeight, MatrixUtils.GetWorldPosition(MouseUtils.MousePosition).FlattenZ());

        // someday these will be implemented lol
        if (InputUtils.KeyJustPressed(Keys.I) && DebugLevel == 4) new Powerup(powerups[mode]) { Position = MatrixUtils.GetWorldPosition(MouseUtils.MousePosition) + new Vector3(0, 10, 0) };

        blockHeight = MathHelper.Clamp(blockHeight, 1, 7);
        blockType = MathHelper.Clamp(blockType, 0, 3);
    }

    public static float SnapToQuarterCircle(float radians) => MathF.Round(radians / MathHelper.PiOver2) * MathHelper.PiOver2;
    public static void DrawDebug(SpriteBatch spriteBatch) {
        if (!GameLauncher.IsConsoleAllocated) return;
        if (Client.IsConnected()) {
            var myClient = Client.NetClient;
            var ping = myClient.Ping;
            var pLoss = myClient.Statistics.PacketLossPercent;

            var info = new string[] {
                $"Client ID: {myClient.Id}",
                $"Ping: {ping}",
                $"Packet Loss: {pLoss}%",
            };

            var textScale = new Vector2(0.5f).ToResolution();

            var pingColor = new StatisticalColor<int>(Color.Lime, Color.Red, 30, ping, 250);
            var packetLossColor = new StatisticalColor<float>(Color.Lime, Color.Red, 0f, pLoss, 0.25f);

            DrawUtils.DrawStringWithBorder(spriteBatch, FontGlobals.RebirthFont, info[0], WindowUtils.WindowTop, Color.White, Color.Black, textScale, 0f, Anchor.TopCenter, 0.5f);
            DrawUtils.DrawStringWithBorder(spriteBatch, FontGlobals.RebirthFont, info[1], WindowUtils.WindowTop * 0.75f, pingColor.FinalColor, Color.Black, textScale, 0f, Anchor.TopCenter, 0.5f);
            DrawUtils.DrawStringWithBorder(spriteBatch, FontGlobals.RebirthFont, info[2], WindowUtils.WindowTop * 1.25f, packetLossColor.FinalColor, Color.Black, textScale, 0f, Anchor.TopCenter, 0.5f);
        }

        if (!DebuggingEnabled) return;
        if (_hideUi) return;

        DrawUtils.DrawAxes();
        DrawUtils.DrawFrustum(TankGame.Instance.GraphicsDevice, CameraGlobals.ViewFrustum, Color.White, CameraGlobals.GameView, CameraGlobals.GameProjection);

        var posOffset = new Vector2(0, 80);

        DrawDebugString(spriteBatch, "Spawn Tank With Info:", WindowUtils.WindowTop + posOffset, 3, centered: true);
        DrawDebugString(spriteBatch, $"Tier: {TankID.Collection.GetKey(tankToSpawnType)}", WindowUtils.WindowTop + posOffset + new Vector2(0, 16), 3, centered: true);
        DrawDebugString(spriteBatch, $"Team: {TeamID.Collection.GetKey(tankToSpawnTeam)}", WindowUtils.WindowTop + posOffset + new Vector2(0, 32), 3, centered: true);
        DrawDebugString(spriteBatch, $"BlockStack: {blockHeight} | BlockType: {BlockID.Collection.GetKey(blockType)}", WindowUtils.WindowBottom - new Vector2(0, 20), 3, centered: true);

        tankToSpawnType = MathHelper.Clamp(tankToSpawnType, TankID.Brown, TankID.Collection.Count - 1);
        tankToSpawnTeam = MathHelper.Clamp(tankToSpawnTeam, TeamID.NoTeam, TeamID.Collection.Count - 1);

        DrawDebugString(spriteBatch, $"Logic Time: {RuntimeData.LogicTime.TotalMilliseconds:0.00}ms" +
                                                   $"\nLogic FPS: {RuntimeData.LogicFPS}" +
                                                   $"\n\nRender Time: {RuntimeData.RenderTime.TotalMilliseconds:0.00}ms" +
                                                   $"\nRender FPS: {RuntimeData.RenderFPS}" +
                                                   $"\nKeys Q + W: Localhost Connect for Multiplayer Debug" +
                                                   $"\nKeys U + I: Unload All Mods" +
                                                   $"\nKeys O + P: Reload All Mods" +
                                                   $"\nKeys Q + E: Resynchronize Randoms" +
                                                   $"\nKb Player: {PlayerTank.KbPlayer}", new Vector2(10, 500));

        DrawDebugString(spriteBatch, $"Current Mission: {CampaignGlobals.LoadedCampaign.CurrentMission.Name}\nCurrent Campaign: {CampaignGlobals.LoadedCampaign.MetaData.Name}", WindowUtils.WindowBottomLeft - new Vector2(-4, 60), 3, centered: false);

        DrawDebugString(spriteBatch, $"HighestTier: {TankID.Collection.GetKey(AIManager.GetHighestTierActive())}", new(10, WindowUtils.WindowHeight * 0.26f), 1);
        // DebugUtils.DrawDebugString(spriteBatch, $"CurSong: {(Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f) != null ? Music.AllMusic.FirstOrDefault(music => music.Volume == 0.5f).Name : "N/A")}", new(10, WindowUtils.WindowHeight - 100), 1);

        for (int i = 0; i < TankID.Collection.Count; i++)
            DrawDebugString(spriteBatch, $"{TankID.Collection.GetKey(i)}: {AIManager.GetTankCountOfType(i)}", new(10, WindowUtils.WindowHeight * 0.3f + (i * 20)), 1);

        spriteBatch.DrawString(FontGlobals.RebirthFont,
                "Debug Level: " + CurDebugLabel,
                WindowUtils.WindowBottom - new Vector2(0, 30),
                Color.White,
                new Vector2(0.6f),
                origin: FontGlobals.RebirthFont.MeasureString("Debug Level: " + CurDebugLabel) / 2);
        DrawDebugString(spriteBatch,
            $"Garbage Collection: {MemoryParser.To(MemoryParser.Size.Bytes, MemoryParser.Size.Megabytes, RuntimeData.GCMemory):0} MB" +
            $"\nPhysical Memory: {RuntimeData.CompSpecs.RAM}" +
            $"\nGPU: {RuntimeData.CompSpecs.GPU}" +
            $"\nCPU: {RuntimeData.CompSpecs.CPU}" +
            $"\nProcess Memory: {MemoryParser.To(MemoryParser.Size.Bytes, MemoryParser.Size.Megabytes, RuntimeData.MemoryUsageInBytes):0} MB / " +
            $"Total Memory: {MemoryParser.To(MemoryParser.Size.Bytes, MemoryParser.Size.Megabytes, RuntimeData.CompSpecs.RAM.TotalPhysical):0}MB",
            new Vector2(8, WindowUtils.WindowHeight * 0.15f));

        DrawDebugString(spriteBatch, $"Tank Kill Counts:", new(8, WindowUtils.WindowHeight * 0.05f), 2);

        for (int i = 0; i < PlayerTank.TankKills.Count; i++) {
            var tier = TankID.Collection.GetKey(PlayerTank.TankKills.ElementAt(i).Key);
            var count = PlayerTank.TankKills.ElementAt(i).Value;

            DrawDebugString(spriteBatch, $"{tier}: {count}", new(8, WindowUtils.WindowHeight * 0.05f + (14f * (i + 1))), 2);
        }
        DrawDebugString(spriteBatch,
            $"Lives / StartingLives: {PlayerTank.Lives[Client.IsConnected() ? NetPlay.GetMyClientId() : 0]} / {PlayerTank.StartingLives}" +
            $"\nKillCount: {PlayerTank.KillCounts}" +
            $"\n\nSavable Game Data:" +
            $"\nTotal / Bullet / Mine / Bounce Kills: {TankGame.SaveFile.TotalKills} / {TankGame.SaveFile.BulletKills} / {TankGame.SaveFile.MineKills} / {TankGame.SaveFile.BounceKills}" +
            $"\nTotal Deaths: {TankGame.SaveFile.Deaths}" +
            $"\nTotal Suicides: {TankGame.SaveFile.Suicides}" +
            $"\nMissions Completed: {TankGame.SaveFile.MissionsCompleted}" +
            $"\nExp Level / Multiplier: {TankGame.SaveFile.ExpLevel} / {TanksSaveFile.UniversalExpMultiplier}",
            new(8, WindowUtils.WindowHeight * 0.4f),
            2);
        for (int i = 0; i < PlayerTank.TankKills.Count; i++) {
            //var tier = GameData.KillCountsTiers[i];
            //var count = GameData.KillCountsCount[i];
            var tier = PlayerTank.TankKills.ElementAt(i).Key;
            var count = PlayerTank.TankKills.ElementAt(i).Value;

            DrawDebugString(spriteBatch, $"{tier}: {count}", new(WindowUtils.WindowWidth * 0.9f, 8 + (14f * (i + 1))), 2);
        }

        if (DebugLevel == Id.GENERAL || _collPersist) {
            if (InputUtils.AreKeysJustPressed(Keys.R, Keys.B)) GameScene.BoundsRenderer.LoadBounds();
            if (InputUtils.AreKeysJustPressed(Keys.Z, Keys.X)) _collPersist = !_collPersist;
            foreach (var body in Tank.CollisionsWorld.BodyList) {
                Color drawColor = Color.Black;

                if (body.Tag is Color c)
                    drawColor = c;

                foreach (var fixture in body.FixtureList) {
                    switch (fixture.Shape) {
                        case CircleShape circle: {
                                var center = body.GetWorldPoint(circle.Position) * Tank.UNITS_PER_METER;
                                var sphere = new BoundingSphere(new Vector3(center.X, 0, center.Y), circle.Radius * Tank.UNITS_PER_METER);
                                DrawBoundingSphere(sphere, drawColor, CameraGlobals.GameView, CameraGlobals.GameProjection, segments: 12);
                                break;
                            }
                        case PolygonShape polygon: {
                                var min = new Vector2(float.MaxValue);
                                var max = new Vector2(float.MinValue);
                                foreach (var vertex in polygon.Vertices) {
                                    var point = body.GetWorldPoint(vertex) * Tank.UNITS_PER_METER;
                                    min = Vector2.Min(min, point);
                                    max = Vector2.Max(max, point);
                                }
                                var box = new BoundingBox(new Vector3(min.X, 0, min.Y), new Vector3(max.X, Block.SIDE_LENGTH, max.Y));
                                DrawBoundingBox(box, drawColor, CameraGlobals.GameView, CameraGlobals.GameProjection);
                                break;
                            }
                    }
                }
            }
        }
        if (DebuggingEnabled && !_hideUi && DebugLevel == Id.LIGHTS)
            DrawLights(spriteBatch);

        for (int i = 0; i < VanillaAchievements.Repository.GetAchievements().Count; i++) {
            var achievement = VanillaAchievements.Repository.GetAchievements()[i];

            DrawDebugString(spriteBatch,
                $"{achievement.Name}: {(achievement.IsComplete ? "Complete" : "Incomplete")}",
                new Vector2(8, 24 + (i * 20)),
                level: Id.ACH_DAT,
                centered: false);
        }

        if (DebugLevel == Id.FREECAM) {
            spriteBatch.DrawString(FontGlobals.RebirthFont,
               $"Position: {CameraGlobals.RebirthFreecam.Position}" +
            $"\nRotation: {CameraGlobals.RebirthFreecam.Rotation}" +
            $"\nFOV: {CameraGlobals.RebirthFreecam.FieldOfView}�" +
            $"\nForward: {CameraGlobals.GameView.Forward}" +
            $"\nBackward: {CameraGlobals.GameView.Backward}" +
            $"\nLeft: {CameraGlobals.GameView.Left}" +
            $"\nRight: {CameraGlobals.GameView.Right}" +
            $"\n\nToggle Persist Freecam: Z + X (Currently {(persistFreecam ? "enabled" : "disabled")})" +
            $"\n\nCTRL + C: Copy Position and Rotation Vector3s" +
            $"\n\nCTRL + X: Copy Camera Look Vector3" +
            $"\nPress + & - to play camera animation with keyframes" +
            $"\nIncrease/Decrease keyframe timespan: T & -/+" +
            $"\nChange easing type: K & -/+" +
            $"\nDelete latest: DELETE" +
            "\nAdd bezier point: LeftAlt" +
            "\nSnap to axis origin: LeftShift + AxisKey" +
            "\nSnap to rotational quarter-circle: RightShift + AxisKey (Y+Z only)" + 
            $"\n\ncurrent easing type: {_povAnimatorFunction}" +
            $"\ncurrent frame timespan: {_povAnimatorDuration}" +
            $"\nCamera keyframes:" +
            $"\n{string.Join("\n", _povAnimatorKeyframeBuilder)}" +
            $"\n\nAnim: {_povAnimatorTest?.ToString() ?? "N/A"}",
                new Vector2(0, 50),
                Color.White,
                Vector2.One * 0.75f);

            AnimDebug.DrawPath(TankGame.Instance.GraphicsDevice, _povAnimatorKeyframeBuilder, CameraGlobals.GameView, CameraGlobals.GameProjection);
        }

        if (DebugLevel == Id.METRICS) {
            RuntimeData.RenderFpsGraph.Draw(spriteBatch, new Vector2(100, 400), 2);
            RuntimeData.LogicFpsGraph.Draw(spriteBatch, new Vector2(100, 800), 2);
            RuntimeData.RenderTimeGraph.Draw(spriteBatch, new Vector2(500, 400), 2);
            RuntimeData.LogicTimeGraph.Draw(spriteBatch, new Vector2(500, 800), 2);
        }

        if (InputUtils.AreKeysJustPressed(Keys.LeftControl, Keys.C)) {
            TextCopy.ClipboardService.SetText($"{CameraGlobals.RebirthFreecam.Position.ToCtor()}, {CameraGlobals.RebirthFreecam.Rotation.ToCtor()}");
        }
        else if (InputUtils.AreKeysJustPressed(Keys.LeftControl, Keys.X)) {
            TextCopy.ClipboardService.SetText($"{CameraGlobals.RebirthFreecam.World.Forward.ToCtor()}");
        }

        var information = new string[] {
            $"PrimitiveCount: {TankGame.Instance.Graphics.GraphicsDevice.Metrics.PrimitiveCount}",
            $"Vx: {TankGame.Instance.Graphics.GraphicsDevice.Metrics.VertexShaderCount}",
            $"Px: {TankGame.Instance.Graphics.GraphicsDevice.Metrics.PixelShaderCount}",
            $"Draw calls: {TankGame.Instance.Graphics.GraphicsDevice.Metrics.DrawCount}",
            $"Sprites: {TankGame.Instance.Graphics.GraphicsDevice.Metrics.SpriteCount}",
            $"Textures: {TankGame.Instance.Graphics.GraphicsDevice.Metrics.TextureCount}",
            $"Targets: {TankGame.Instance.Graphics.GraphicsDevice.Metrics.TargetCount}"
        };
        DrawDebugString(FontGlobals.RebirthFont, spriteBatch, string.Join('\n', information), Vector2.Zero, -1);
    }
    public static void SpawnCrateAtMouse() {
        var pos = MatrixUtils.GetWorldPosition(MouseUtils.MousePosition);

        var drop = Crate.SpawnCrate(new(pos.X, 200, pos.Z), 2f);
        drop.Scale = 1.25f;
        drop.TankToSpawn = new TankTemplate() {
            AIType = TankID.ClientRandomTier(),
            Team = TeamID.NoTeam
        };
    }
    public static AITank SpawnTankAt(Vector3 position, int tier, int team) {
        var rot = 0f;

        var x = new AITank(tier) {
            DesiredChassisRotation = rot,
            ChassisRotation = rot,
            TurretRotation = rot,

            Team = team,
            IsDestroyed = false
        };
        x.Physics.Position = position.FlattenZ() / Tank.UNITS_PER_METER;
        x.Position = position.FlattenZ();
        return x;
    }
    public static PlayerTank SpawnMe(int playerType, int team, Vector3 posOverride = default) {
        var pos = LevelEditorUI.IsActive ? EditorTile.CurrentlyHovered.Position : MatrixUtils.GetWorldPosition(MouseUtils.MousePosition);

        if (posOverride != default)
            pos = posOverride;
        var myTank = new PlayerTank(playerType) {
            Team = team,
            IsDestroyed = false
        };
        myTank.Physics.Position = pos.FlattenZ() / Tank.UNITS_PER_METER;
        myTank.Position = pos.FlattenZ();

        if (Client.IsConnected())
            Client.RequestPlayerTankSpawn(myTank);

        return myTank;
    }
    
    public static void DrawBoundingBox(
         BoundingBox box,
         Color color,
         Matrix view,
         Matrix projection,
         Matrix? world = null) {

        if (!DebuggingEnabled || _hideUi) return;

        DrawUtils.DrawBoundingBox(box, color, view, projection, world);
    }

    // eensy weensy bit of Owner help cuz i was programming this at 3am
    /// <summary>Draws a wireframe 3D bounding sphere.</summary>
    public static void DrawBoundingSphere(BoundingSphere sphere, Color color, Matrix view,
        Matrix projection, Matrix? world = null, int segments = 32) {

        if (!DebuggingEnabled || _hideUi) return;

        DrawUtils.DrawBoundingSphere(sphere, color, view, projection, world, segments);
    }

    const float LIGHT_MARKER_SIZE = 4f;
    const float SPOT_PREVIEW_LENGTH = 40f;
    const float LIGHT_HOVER_DISTANCE = 60f;

    static void DrawLights(SpriteBatch spriteBatch) {
        var view = CameraGlobals.GameView;
        var projection = CameraGlobals.GameProjection;
        var sun = LightingSystem.Sun;
        var ambient = LightingSystem.Ambient;
        var stats = LightingSystem.Stats;

        var panelText = $"Lighting: {(LightingSystem.IsActive ? "on" : "off")} | Preset: {LightingPresets.Current}" +
            $"\nLights: {LightingSystem.Lights.Count} permanent, {LightingSystem.FrameLights.Count} this frame, {LightingSystem.ActiveLights.Count} drawn (max {LightingSystem.Quality.MaxLocalLights})" +
            $"\nPoint / Spot: {stats.PointLights} / {stats.SpotLights} | Shadowed: {stats.ShadowedPointLights} / {stats.ShadowedSpotLights}" +
            $"\nDraws: {stats.ShadowDraws} shadow, {stats.LightBufferDraws} light, {stats.ShaftDraws} shaft" +
            $"\n\nSun: {(sun.Enabled ? "on" : "off")} | Dir {StringUtils.Format(Vector3.Normalize(sun.Direction))} | Color {StringUtils.Format(sun.Color)} x{sun.Intensity:0.##} | Wrap {sun.Wrap:0.##}" +
            $"\nSun shadows: {(sun.CastsShadows ? "on" : "off")} | Bias {sun.ShadowBias:0.##} | Room map {(sun.RoomShadows ? "on" : "off")} | Shafts {(sun.Shafts.Enabled ? "on" : "off")}" +
            $"\nAmbient: Sky {StringUtils.Format(ambient.Sky)} | Ground {StringUtils.Format(ambient.Ground)} x{ambient.Intensity:0.##}" +
            $"\n\nHover a light for its range and settings";
        var panelPos = new Vector2(8, WindowUtils.WindowHeight * 0.05f);
        var panelScale = 0.6f * WindowUtils.WindowWidth / 1920f;
        var panelSize = FontGlobals.RebirthFont.MeasureString(panelText) * panelScale;
        var panel = new Rectangle(0, 0, (int)(panelPos.X + panelSize.X), (int)(panelPos.Y + panelSize.Y));
        DrawDebugString(spriteBatch, panelText, panelPos, Id.LIGHTS);

        if (sun.Enabled) {
            var direction = Vector3.Normalize(sun.Direction);
            DrawUtils.DrawArrow(sun.ShadowCenter - direction * 300f, sun.ShadowCenter, sun.Color, view, projection);
        }
        if (sun.ShadowBounds is BoundingBox shadowBounds)
            DrawBoundingBox(shadowBounds, Color.Olive, view, projection);
        foreach (var blocker in LightingSystem.SunBlockers)
            DrawBoundingBox(blocker, Color.DimGray, view, projection);

        _debugLights.Clear();
        for (int i = 0; i < LightingSystem.Lights.Count; i++)
            _debugLights.Add((LightingSystem.Lights[i], $"#{i}"));
        for (int i = 0; i < LightingSystem.FrameLights.Count; i++)
            _debugLights.Add((LightingSystem.FrameLights[i], $"frame #{i}"));

        // only lights close to mouse are fully visible
        Light? hovered = null;
        var hoverDistance = LIGHT_HOVER_DISTANCE * WindowUtils.WindowWidth / 1920f;
        foreach (var (light, _) in _debugLights) {
            if (!TryGetScreenPosition(light.Position, view, projection, out var screenPos))
                continue;
            var distance = Vector2.Distance(screenPos, MouseUtils.MousePosition);
            if (distance < hoverDistance) {
                hoverDistance = distance;
                hovered = light;
            }
        }

        foreach (var (light, label) in _debugLights)
            DrawLight(spriteBatch, light, label, light == hovered, panel, view, projection);
    }
    static readonly List<(Light Light, string Label)> _debugLights = [];

    static void DrawLight(SpriteBatch spriteBatch, Light light, string label, bool hovered, Rectangle panel, Matrix view, Matrix projection) {
        var color = !light.Enabled ? Color.Gray : hovered ? Color.White : light.Color;
        var dimColor = Color.Lerp(color, Color.Black, 0.5f);
        var spot = light as SpotLight;
        var direction = spot is null ? Vector3.Zero : Vector3.Normalize(spot.Direction);

        DrawBoundingSphere(new BoundingSphere(light.Position, LIGHT_MARKER_SIZE), color, view, projection, segments: 6);

        if (spot is not null) {
            var length = hovered ? spot.Range : SPOT_PREVIEW_LENGTH;
            DrawUtils.DrawLine(spot.Position, spot.Position + direction * length, color, view, projection);
            DrawUtils.DrawCone(spot.Position, direction, length, spot.OuterAngle, color, view, projection);
            if (hovered)
                DrawUtils.DrawCone(spot.Position, direction, length, spot.InnerAngle, dimColor, view, projection);
        }
        if (hovered)
            DrawUtils.DrawCircle(light.Position, Vector3.Up, light.Range, dimColor, view, projection, 48);

        if (!TryGetScreenPosition(light.Position, view, projection, out var screenPos))
            return;

        var shadows = !light.CastsShadows ? "off" : LightingSystem.HasShadowMap(light) ? "on" : "no slot";
        var state = !light.Enabled ? " (disabled)" : !LightingSystem.ActiveLights.Contains(light) ? " (culled)" : "";
        var name = $"{(spot is null ? "Point" : "Spot")} {label}{state}";

        string text;
        if (hovered) {
            text = $"{name}" +
                $"\nPos {StringUtils.Format(light.Position)}" +
                $"\nColor {StringUtils.Format(light.Color)} x{light.Intensity:0.##} | Range {light.Range:0} | Wrap {light.Wrap:0.##}" +
                $"\nShadows {shadows} | Bias {light.ShadowBias:0.##} | Priority {light.Priority}";
            if (spot is not null)
                text += $"\nDir {StringUtils.Format(direction)} | Cone {MathHelper.ToDegrees(spot.InnerAngle):0}/{MathHelper.ToDegrees(spot.OuterAngle):0} deg";
        }
        else {
            // labels under the stats panel would be unreadable
            if (panel.Contains(screenPos))
                return;
            text = shadows == "on" ? $"{name} | shadows" : name;
        }

        var scale = new Vector2(hovered ? 0.45f : 0.3f).ToResolution();
        DrawUtils.DrawStringWithBorder(spriteBatch, FontGlobals.RebirthFont, text, screenPos + new Vector2(0, 10).ToResolution(), Color.White, Color.Black,
            scale, 0f, Anchor.TopCenter, 0.5f);
    }
    static bool TryGetScreenPosition(Vector3 position, Matrix view, Matrix projection, out Vector2 screenPos) {
        screenPos = default;
        if (CameraGlobals.ViewFrustum.Contains(position) == ContainmentType.Disjoint)
            return false;
        screenPos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(position), view, projection);
        return true;
    }
}