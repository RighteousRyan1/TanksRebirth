using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.CommandsSystem;
using TanksRebirth.GameContent.Systems.TankSystem.AI;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Collisions;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.TankSystem;

// pretty sure literally everything breaks if you try local multiplayer input in a multiplayer server. get to that later!
public enum PlayerInput {
    KBM,
    Gamepad,
    Wiimote
}
public ref struct PlayerBinds {
    public Keybind ControlUp;
    public Keybind ControlDown;
    public Keybind ControlLeft;
    public Keybind ControlRight;

    public Keybind ControlMine;
    public Keybind ToggleShootPath;
}
public class PlayerTank : Tank {
    static bool _justCenteredMouse = false;

    public static int NumLocalPlayers => PlayerControlledByKeyboard == -1 ? InputUtils.NumGamepadsConnected : InputUtils.NumConnectedInputs;
    #region The Rest

    // "My" denotes that it's for the client's tank team/tank type
    public static int MyTeam;
    public static int MyTankType;
    public static int StartingLives = 3;
    public static Dictionary<int, int> TankKills { get; set; } = []; // this campaign only!
    // questioning the validity of this struct but whatever
    public struct CampaignStats {
        public int ShellsShot;
        public int ShellHits;
        public int MinesLaid;
        public int MineHits;
        public int Suicides; // self-damage this campaign?
    }
    public static CampaignStats PlayerStatistics;
    public static bool _drawShotPath;
    public static int[] KillCounts { get; set; } = [0, 0, 0, 0];
    public int PlayerId { get; } = -1;
    public int PlayerType { get; }

    public static Keybind MoveUp = new("Up", Keys.W);
    public static Keybind MoveDown = new("Down", Keys.S);
    public static Keybind MoveLeft = new("Left", Keys.A);
    public static Keybind MoveRight = new("Right", Keys.D);
    public static Keybind PlaceMine = new("Place Mine", Keys.Space);
    public static Keybind ShowShotPath = new("Draw Shot Path", Keys.Q);
    public static GamepadBind GamePadShoot = new("Fire Bullet", Buttons.RightShoulder);
    public static GamepadBind GamePadPlaceMine = new("Place Mine", Buttons.A);

    bool playerControl;
    bool _isPlayerModel;

    public Vector2 oldPosition;

    // 46 if using keyboard, 10 if using a controller
    //private float _maxTurnInputBased;
    #endregion

    public static Vector2[] AimTargets = new Vector2[4];

    // -1 if all are controllers!
    // -2 is only the client tank
    public static int PlayerControlledByKeyboard = PlayerID.Blue;

    /// <summary>
    /// True if this player is the one controlled by keyboard+mouse.
    /// </summary>
    public bool UsesKeyboard => PlayerId == PlayerControlledByKeyboard;

    /// <summary>
    /// Returns the gamepad index for this player, or -1 if this player uses keyboard/mouse.
    /// The mapping "packs" gamepads so that when one player is KBM, the controllers shift down.
    /// </summary>
    public int GamepadIndex {
        get {
            if (PlayerControlledByKeyboard == -1)
                return PlayerId; // everyone is controller: 0->0, 1->1...

            if (PlayerId == PlayerControlledByKeyboard)
                return -1; // THIS player is keyboard+mouse, no gamepad index.

            if (PlayerId < PlayerControlledByKeyboard)
                return PlayerId; // before the KBM slot, indices are unchanged.

            // after the KBM slot, shift left by one
            return PlayerId - 1;
        }
    }

    public PlayerInput InputMethod = PlayerInput.KBM;

    public Vector2 DesiredDirection;
    public static float StickDeadzone { get; set; } = 0.12f;
    public static float StickAntiDeadzone { get; set; } = 0.85f;
    /// <summary>A <see cref="PlayerTank"/> instance which represents the current client's tank they *primarily* control. Will return null in cases where
    /// the tank simply is inexistent (i.e: in the main menu). In a single-player context, this will always be the first player tank.</summary>
    public static PlayerTank ClientTank => GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()];
    /// <summary>The amount of lives for every existing player. Access a certain player's life count via indexing this array with a PlayerID entry.
    /// <para>Note that lives are always synced on multiplayer.</para>
    /// </summary>
    public static int[] Lives { get; set; } = new int[Server.MaxClients];

    /// <summary>In multiplayer, gets the lives of the client that this code is currently being called on.</summary>
    public static int GetMyLives() => Lives[NetPlay.GetMyClientId()];
    /// <summary>Adds lives to the player in Single-Player, adds to the lives of all players in Multiplayer.</summary>
    /// <param name="num">How many lives to add.</param>
    public static void AddLives(int num) {
        if (Client.IsConnected())
            Lives[NetPlay.GetMyClientId()] += num;
        else
            for (int i = 0; i < Lives.Length; i++)
                Lives[i] += num;
    }
    /// <summary>
    /// Sets the lives of the player in Single-Player, sets the lives of all players in Multiplayer.
    /// </summary>
    /// <param name="num">How many lives to set the player(s) to.</param>
    public static void SetLives(int num) {
        if (Client.IsConnected())
            Lives[NetPlay.GetMyClientId()] = num;
        else
            for (int i = 0; i < Lives.Length; i++)
                Lives[i] = num;
    }
    public PlayerTank(int playerType, bool isPlayerModel = true, int copyTier = -1, bool ignoreRegister = false) : base(ignoreRegister) {
        DrawParamsTank.Model = isPlayerModel ? ModelGlobals.TankPlayer.Asset : ModelGlobals.TankEnemy.Asset;

        Texture2D texAsset;

        if (copyTier == -1) texAsset = Assets[$"plrtank_" + PlayerID.Collection.GetKey(playerType)!.ToLower()]!;
        else {
            texAsset = Assets[$"tank_" + TankID.Collection.GetKey(copyTier)!.ToLower()]!;
            Properties = AIManager.GetAITankProperties(copyTier);
        }

        DrawParamsTank.TankTexture = texAsset!.Duplicate(TankGame.Instance.GraphicsDevice);
        DrawParamsTank.ShadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

        _isPlayerModel = isPlayerModel;
        PlayerType = playerType;

        // initialize drawing parameters
        DrawParams.UsePhong = _isPlayerModel;
        DrawParams.LightPower = 1f;
        DrawParams.AmbientPower = _isPlayerModel ? TankDrawParams.PLR_AMB_MUL : TankDrawParams.AI_AMB_MUL;

        if (!ignoreRegister) {
            PlayerId = playerType;
            GameHandler.AllPlayerTanks[PlayerId] = this;
            WorldId = Array.IndexOf(GameHandler.AllTanks, null);
            GameHandler.AllTanks[WorldId] = this;
        }

        if (copyTier == -1) ApplyDefaults(ref Properties);

        base.Initialize();
    }
    public sealed override void ApplyDefaults(ref TankProperties properties) {
        properties.TreadVolume = 0.2f;
        properties.ShellCooldown = 5; // 5
        properties.ShootStun = 5; // 5
        properties.ShellSpeed = 3f; // 3f
        properties.MaxSpeed = 1.8f; // 1.8
        properties.RicochetCount = 1; // 1
        properties.ShellLimit = 5; // 5
        properties.MineCooldown = 6;
        properties.MineLimit = 2; // 2
        properties.MineStun = 1; // 8
        properties.Invisible = false;
        properties.Acceleration = 0.3f;
        properties.Deceleration = 0.6f;
        properties.TurningSpeed = 0.1f;

        // this changes depending on input (or should it?)
        // normally it's 10 degrees, but we want to make it easier for keyboard players
        int padIndex = GamepadIndex;
        bool hasGamepad =
            padIndex >= 0 &&
            padIndex < InputUtils.GamePads.Length &&
            InputUtils.GamePads[padIndex].Current.IsConnected;
        properties.MaximalTurn = MathHelper.ToRadians(hasGamepad ? 10 : 46);

        properties.ShootPitch = 0.1f * PlayerType;
        properties.ShellType = ShellID.Player;
        properties.ShellHoming = new();

        properties.DestructionColor = PlayerType switch {
            PlayerID.Blue => Color.Blue,
            PlayerID.Red => Color.Crimson,
            PlayerID.Green => Color.Lime,
            PlayerID.Yellow => Color.Yellow,
            _ => throw new Exception($"The player type with number \"{PlayerType}\" is not mapped to a color!"),
        };

        base.ApplyDefaults(ref properties);
    }
    public override void Update() {
        base.Update();
        if (IgnoreRegister) return;
        /*if (Input.KeyJustPressed(Keys.P))
            foreach (var m in TankDeathMark.deathMarks)
                m?.ResurrectTank();*/
        // FIXME: reference?

        // pi/2 = up
        // 0 = down
        // pi/4 = right
        // 3/4pi = left

        // moved to tank spawn/load only since yes
        if (WiimoteSystem.IsConnected) {
            InputMethod = PlayerInput.Wiimote;
        }
        else {
            // If this is the configured keyboard player, force KBM.
            if (UsesKeyboard) {
                InputMethod = PlayerInput.KBM;
            }
            else {
                int padIndex = GamepadIndex;
                if (padIndex >= 0 && InputUtils.IsGamepadBeingUsed(padIndex))
                    InputMethod = PlayerInput.Gamepad;
                else
                    InputMethod = PlayerInput.KBM; // fallback if pad not actually in use
            }
        }

        DesiredDirection = Vector2.Zero;
       
        // base.Update used to be here

        if (LevelEditorUI.IsActive || IsDestroyed) return;

        if (IsTurning) {
            if (DesiredChassisRotation - ChassisRotation >= MathHelper.PiOver2) {
                ChassisRotation += MathHelper.Pi;
                DrawParamsTank.GraphicalFlip = !DrawParamsTank.GraphicalFlip;
            }
            else if (DesiredChassisRotation - ChassisRotation <= -MathHelper.PiOver2) {
                ChassisRotation -= MathHelper.Pi;
                DrawParamsTank.GraphicalFlip = !DrawParamsTank.GraphicalFlip;
            }
        }

        if (NetPlay.IsClientMatched(PlayerId))
            Client.SyncPlayerTank(this);

        // TODO: optimize?
        ProcessPlayerMouse();

        if (!CampaignGlobals.InMission || LevelEditorUI.IsActive || ChatSystem.ActiveHandle) {
            playerControl = false;
            return;
        }

        if (!TankGame.MouseUIHover) {
            if (NetPlay.IsClientMatched(PlayerId)) {
                if (!Properties.Stationary && CurShootStun <= 0 && CurMineStun <= 0) {
                    switch (InputMethod) {
                        case PlayerInput.Gamepad:
                            ControlHandle_Gamepad();
                            break;
                        case PlayerInput.KBM:
                            ControlHandle_Keybinding();
                            break;
                        case PlayerInput.Wiimote:
                            ControlHandle_Wiimote(WiimoteSystem.State);
                            break;
                    }
                }

                // THIS MUST HAVE A BETTER WAY
                if (InputUtils.CanDetectClick() && (UsesKeyboard || PlayerControlledByKeyboard == -2)) {
                    Shoot(false);
                }
            }
        }

        if (playerControl) {
            var norm = Vector2.Normalize(DesiredDirection);

            DesiredChassisRotation = norm.ToRotation() - MathHelper.PiOver2;
            ChassisRotation = MathUtils.RoughStep(ChassisRotation, DesiredChassisRotation, Properties.TurningSpeed * RuntimeData.DeltaTime);
            Velocity = Vector2.UnitY.RotatedBy(ChassisRotation) * Speed;

            oldPosition = Position;
        }
    }
    void ProcessPlayerMouse() {
        if (!NetPlay.IsClientMatched(PlayerId)) return;

        if (UsesKeyboard) {
            AimTargets[PlayerId] = MouseUtils.MousePosition;
            var numMice = InputUtils.NumConnectedInputs;

            // ensures the mouse is not used if invalid
            if (numMice - 1 > PlayerControlledByKeyboard) {
                TankGame.PlayerMice[PlayerId].Position = MouseUtils.MousePosition;
            }
        }

        var denyFpsAiming = !Modifiers.Map[Modifiers.POV] || LevelEditorUI.IsActive || MainMenuUI.IsActive;
        if (denyFpsAiming) {
            var mouseIndex = PlayerControlledByKeyboard == -2 ? 0 : PlayerId;
            //int padIndex = GamepadIndex;
            //if (padIndex < 0)
            //    return; // KBM player will use actual mouse

            var cursorToAimAt = TankGame.PlayerMice[mouseIndex];

            if (cursorToAimAt != null) {
                var mouseWorldPos = MatrixUtils.GetWorldPosition(cursorToAimAt.Position, -11f);

                // hacky ass fix
                if (float.IsNaN(mouseWorldPos.X)) return;
                // if (float.IsNaN(mouseWorldPos.X)) mouseWorldPos = Vector3.Zero;
                if (!LevelEditorUI.IsActive)
                    TurretRotation = -(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position).ToRotation() + MathHelper.PiOver2;
                else
                    TurretRotation = ChassisRotation;
            }
        }
        // handle POV mode aiming
        // also pov mode should not be used in local games for now (i do not want to make splitscreen pls)
        else if (!GameUI.Paused) {
            if (!DebugManager.IsFreecamEnabled && !InputUtils.CanDetectClick() && !PlaceMine.JustPressed) {
                var mouseState = Mouse.GetState();
                var screenCenter = new Point(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2);

                if (_justCenteredMouse) {
                    // skip to avoid jumps
                    _justCenteredMouse = false;
                    return;
                }

                // subtract mouse delta eventually
                int deltaX = mouseState.X - screenCenter.X;

                TurretRotation += -deltaX / 312f.ToResolutionX();

                // recenter
                Mouse.SetPosition(screenCenter.X, screenCenter.Y);
                _justCenteredMouse = true;
            }
        }
    }
    public override void Remove(bool nullifyMe) {
        if (nullifyMe) {
            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
            DrawParamsTank.TankTexture?.Dispose();
        }
        base.Remove(nullifyMe);
    }
    public override void Shoot(bool fxOnly, bool netSend = true) {
        PlayerStatistics.ShellsShot++;
        base.Shoot(false);
    }
    public override void LayMine() {
        PlayerStatistics.MinesLaid++;
        base.LayMine();
    }
    // B is already mapped to left click forcibly
    void ControlHandle_Wiimote(WiimoteLib.WiimoteState state) {
        if (state.ExtensionType != WiimoteLib.ExtensionType.Nunchuk) {
            if (state.ButtonState.Up) {
                playerControl = true;
                DesiredDirection.Y = -1;
            }
            else if (state.ButtonState.Down) {
                playerControl = true;
                DesiredDirection.Y = 1;
            }

            if (state.ButtonState.Right) {
                playerControl = true;
                DesiredDirection.X = 1;
            }
            else if (state.ButtonState.Left) {
                playerControl = true;
                DesiredDirection.X = -1;
            }
        }
        else {
            DesiredDirection = Vector2.Normalize(WiimoteSystem.NunchukAxis);

            if (DesiredDirection.Length() > 0.5f)
                playerControl = true;
        }

        if (state.ButtonState.A && !_prev.A)
            LayMine();

        _prev = state.ButtonState;
    }
    static WiimoteLib.ButtonState _prev;

    // TODO: this is where we are going to handle the heavenly local multiplayer :)
    void ControlHandle_Gamepad() {
        int padIndex = GamepadIndex;
        if (padIndex < 0)
            return; // should never happen if InputMethod == Gamepad, but guard anyway

        var leftStick = InputUtils.GamePads[padIndex].Current.ThumbSticks.Left;
        var rightStick = InputUtils.GamePads[padIndex].Current.ThumbSticks.Right;
        var dPad = InputUtils.GamePads[padIndex].Current.DPad;

        // inverse y because stick down is positive y (which is up z)
        DesiredDirection = new Vector2(leftStick.X, -leftStick.Y);

        if (leftStick.Length() > 0) {
            playerControl = true;
        }

        if (dPad.Down == ButtonState.Pressed) {
            playerControl = true;
            DesiredDirection.Y = 1;
        }
        if (dPad.Up == ButtonState.Pressed) {
            playerControl = true;
            DesiredDirection.Y = -1;
        }
        if (dPad.Left == ButtonState.Pressed) {
            playerControl = true;
            DesiredDirection.X = -1;
        }
        if (dPad.Right == ButtonState.Pressed) {
            playerControl = true;
            DesiredDirection.X = 1;
        }

        if (GamePadShoot.JustPressed(GamepadIndex))
            Shoot(false);
        if (GamePadPlaceMine.JustPressed(GamepadIndex))
            LayMine();

        if (rightStick.Length() > 0) {
            // ChatSystem.SendMessage(rightStick);
            var unprojectedPosition = MatrixUtils.ConvertWorldToScreen(
                new Vector3(0, 11, 0), DrawParams.World, DrawParams.View, DrawParams.Projection);

            var newMousePos = new Vector2(
                (int)(unprojectedPosition.X + rightStick.X * 400),
                (int)(unprojectedPosition.Y - rightStick.Y * 400));

            TankGame.PlayerMice[PlayerId].Position = newMousePos;
        }
    }
    void ControlHandle_Keybinding() {
        if (PlayerId != PlayerControlledByKeyboard && PlayerControlledByKeyboard > -2)
            return;

        if (ShowShotPath.JustPressed)
            _drawShotPath = !_drawShotPath;
        if (PlaceMine.JustPressed)
            LayMine();

        IsTurning = false;

        //var rotationMet = TankRotation > TargetTankRotation - Properties.MaximalTurn && TankRotation < TargetTankRotation + Properties.MaximalTurn;

        ChassisRotation %= MathHelper.Tau;

        if (MoveDown.IsPressed) {
            playerControl = true;
            DesiredDirection.Y = 1;
        }
        if (MoveUp.IsPressed) {
            playerControl = true;
            DesiredDirection.Y = -1;
        }
        if (MoveLeft.IsPressed) {
            playerControl = true;
            DesiredDirection.X = -1;
        }
        if (MoveRight.IsPressed) {
            playerControl = true;
            DesiredDirection.X = 1;
        }

        if (Modifiers.Map[Modifiers.POV])
            DesiredDirection = DesiredDirection.RotatedBy(-TurretRotation + MathHelper.Pi);
    }
    public override void Destroy(ITankHurtContext context, bool netSend) {
        /*if (Client.IsConnected()) {
            // maybe make a camera transition to said tank.

            //if (context.Source is not null)
            //CameraGlobals.SpectatorId = CameraGlobals.SpectateValidTank(context.Source.WorldId, true);

            // only decements the lives on the destroyed player's system, where lives are synced across everyone at all times
            if (NetPlay.IsClientMatched(PlayerId)) {
                Lives[PlayerId]--;
            }
        }
        else {*/
            Lives[PlayerId]--;

            Remove(false);

            var c = PlayerType switch {
                PlayerID.Blue => TankDeathMark.CheckColor.Blue,
                PlayerID.Red => TankDeathMark.CheckColor.Red,
                PlayerID.Green => TankDeathMark.CheckColor.Green,
                PlayerID.Yellow => TankDeathMark.CheckColor.Yellow, // TODO: change these colors.
                _ => throw new Exception($"Player Death Mark for colour {PlayerType} is not supported."),
            };

            var playerDeathMark = new TankDeathMark(c) {
                Position = Position3D + new Vector3(0, 0.1f, 0),
            };

            playerDeathMark.StoredTank = new TankTemplate {
                IsPlayer = true,
                Position = playerDeathMark.Position.FlattenZ(),
                Rotation = ChassisRotation,
                Team = Team,
                PlayerType = PlayerType,
            };

            base.Destroy(context, netSend);

            if (context.Source is not PlayerTank player) return;

            // only increment these data values on the destroyed player's system
            // ensure the source tank is 
            if (NetPlay.IsClientMatched(player.PlayerId)) {
                TankGame.SaveFile.Suicides++;
                PlayerStatistics.Suicides++;
            }
            TankGame.SaveFile.Deaths++;
        // }
    }

    // this should probably be voided lol
    void DrawShootPath() {
        const int MAX_PATH_UNITS = 10000;

        // data
        var whitePixel = TextureGlobals.Pixels[Color.White];
        var pathPos = Position + new Vector2(0, 18).RotatedBy(-TurretRotation);
        var pathDir = Vector2.UnitY.RotatedBy(TurretRotation - MathHelper.Pi);
        pathDir.Y *= -1;
        pathDir *= Properties.ShellSpeed;

        var pathRicochetCount = 0;
        TanksSpotted = [];

        // maybe no linq...
        var activeTanks = GameHandler.AllTanks
            .Where(t => t is not null && !t.IsDestroyed)
            .ToArray();

        // stackalloc is more optimal...? maybe
        Span<Vector2> corners = stackalloc Vector2[Properties.RicochetCount + 2];
        Span<int> cornerSteps = stackalloc int[Properties.RicochetCount + 2];
        int cornersCount = 0;

        corners[cornersCount] = pathPos;
        cornerSteps[cornersCount] = 0;
        cornersCount++;

        int currentStep = 0;

        const float tankHitRadiusSq = TNK_WIDTH * 4;

        var dummyPos = Vector2.Zero;

        for (int i = 0; i < MAX_PATH_UNITS; i++) {
            currentStep++;

            // 2a. BOUNDS CHECK
            bool hitBound = false;
            if (pathPos.X < GameScene.MIN_X || pathPos.X > GameScene.MAX_X) {
                pathDir.X *= -1;
                hitBound = true;
            }
            if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                pathDir.Y *= -1;
                hitBound = true;
            }

            if (hitBound) {
                pathRicochetCount++;
                // Add bounce point
                if (cornersCount < corners.Length) {
                    corners[cornersCount] = pathPos;
                    cornerSteps[cornersCount] = i;
                    cornersCount++;
                }
            }

            // block coll
            if (pathRicochetCount <= Properties.RicochetCount) {
                var pathHitbox = new Rectangle((int)pathPos.X - Shell.COLL_RECT_DIM / 2, (int)pathPos.Y - Shell.COLL_RECT_DIM / 2, Shell.COLL_RECT_DIM, Shell.COLL_RECT_DIM);

                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, (c) => c.Properties.IsSolid);

                if (corner) break;

                if (block != null && block.Properties.AllowShotPathBounce) {
                    bool bounced = false;
                    switch (dir) {
                        case CollisionDirection.Up:
                        case CollisionDirection.Down:
                            pathDir.Y *= -1;
                            bounced = true;
                            break;
                        case CollisionDirection.Left:
                        case CollisionDirection.Right:
                            pathDir.X *= -1;
                            bounced = true;
                            break;
                    }

                    if (bounced) {
                        pathRicochetCount += block.Properties.PathBounceCount;

                        if (cornersCount < corners.Length) {
                            corners[cornersCount] = pathPos;
                            cornerSteps[cornersCount] = i;
                            cornersCount++;
                        }
                    }
                }
            }

            if (pathRicochetCount > Properties.RicochetCount) break;

            // tank coll
            Tank targetTank = null;
            for (int t = 0; t < activeTanks.Length; t++) {
                var tnk = activeTanks[t];
                // Use DistanceSquared to avoid Sqrt calls
                if (Vector2.DistanceSquared(tnk.Position, pathPos) <= tankHitRadiusSq) {
                    targetTank = tnk;
                    break;
                }
            }

            if (targetTank != null) {
                TanksSpotted = [targetTank];

                if (cornersCount < corners.Length) {
                    corners[cornersCount] = pathPos;
                    cornerSteps[cornersCount] = i;
                    cornersCount++;
                }
                break;
            }

            pathPos += pathDir;
        }

        // Add the very last position if we didn't crash into a tank
        if (TanksSpotted.Length == 0 && cornersCount < corners.Length) {
            corners[cornersCount] = pathPos;
            cornerSteps[cornersCount] = currentStep;
            cornersCount++;
        }

        // drawing stuffs
        var viewProj = CameraGlobals.GameView * CameraGlobals.GameProjection;
        var viewport = TankGame.Instance.GraphicsDevice.Viewport;

        Vector2 ProjectAtTankHeight(Vector2 pos) {
            var pos3 = new Vector3(pos.X, TNK_DMG_COLL_Y, pos.Y);
            var screenPos = viewport.Project(pos3, CameraGlobals.GameProjection, CameraGlobals.GameView, Matrix.Identity);
            return new Vector2(screenPos.X, screenPos.Y);
        }

        for (int i = 0; i < cornersCount - 1; i++) {
            var startWorld = corners[i];
            var endWorld = corners[i + 1];

            var startScreen = ProjectAtTankHeight(startWorld);
            var endScreen = ProjectAtTankHeight(endWorld);

            // We need to know how many "simulation steps" this segment took to match your animation style
            int stepsInSegment = cornerSteps[i + 1] - cornerSteps[i];
            if (stepsInSegment <= 0) continue;

            for (int step = 0; step < stepsInSegment; step++) {
                float t = step / (float)stepsInSegment;
                var currentScreenPos = Vector2.Lerp(startScreen, endScreen, t);

                int globalI = cornerSteps[i] + step;

                var off = MathF.Abs(MathF.Sin(globalI * MathF.PI / 5 - RuntimeData.RunTime * 0.3f));
                var rgbColor = ColorUtils.HsvToRgb(RuntimeData.UpdateCount + globalI % 255 / 255f * 360, 1, 1);
                var scale = Vector2.One * 4 * off;

                DrawUtils.DrawTextureWithBorder(TankGame.SpriteRenderer, whitePixel, currentScreenPos, Color.Black,
                    rgbColor, scale, 0f, Anchor.Center, 1f);
            }
        }
    }
    public override void Render() {
        base.Render();

        if (IsDestroyed) return;

        // doing this for now. will eventually pass it in via Render(SpriteBatch)
        DrawExtras(TankGame.SpriteRenderer);
        if (Properties.Invisible && CampaignGlobals.InMission) return;
        foreach (ModelMesh mesh in DrawParamsTank.Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = boneTransforms[mesh.ParentBone.Index];
                effect.View = DrawParams.View;
                effect.Projection = DrawParams.Projection;
                effect.TextureEnabled = true;

                if (!Properties.HasTurret)
                    if (mesh.Name == "Cannon")
                        return;

                if (mesh.Name == "Shadow") {
                    if (!CommandGlobals.DrawMeshShadows)
                        continue;
                    effect.Alpha = DrawParamsTank.ShadowAlpha;
                    effect.Texture = DrawParamsTank.ShadowTexture;
                    mesh.Draw();
                    continue;
                }

                effect.Alpha = DrawParamsTank.TankAlpha;
                effect.Texture = DrawParamsTank.TankTexture;

                effect.SetDefaultGameLighting_IngameEntities(DrawParams.LightPower, DrawParams.AmbientPower, DrawParams.UsePhong, DrawParams.LightDirection);
                mesh.Draw();
            }
        }
    }
    static Dictionary<int, float[]> _bulletAnimationStates = [];
    void DrawExtras(SpriteBatch spriteBatch) {
        if (IsDestroyed || IgnoreRegister) return;

        // todo: a good way of making the displays not overlap

        // draw every player's bullet count, even in MP
        if (!MainMenuUI.IsActive) {
            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/bullet_ui");
            var baseScale = 0.5f;
            var offX = 25f.ToResolutionX();
            var offY = (tex.Height * baseScale).ToResolutionY();
            var spacing = 5f;
            var font = FontGlobals.RebirthFont;

            // player data
            var baseColor = PlayerID.PlayerTankColors[PlayerId];
            var brightColor = PlayerID.PlayerTankColorsBright[PlayerId];
            string displayName;

            if (Server.ConnectedClients is not null && Server.ConnectedClients[PlayerId] != null)
                displayName = Server.ConnectedClients[PlayerId].Name;
            else
                displayName = $"P{PlayerId + 1}";

            // anim state failsafe
            if (!_bulletAnimationStates.TryGetValue(PlayerId, out float[]? animStates) || animStates.Length != Properties.ShellLimit) {
                animStates = (new float[Properties.ShellLimit]);
                _bulletAnimationStates[PlayerId] = animStates;
                // Initialize all to 1.0f (full) so they don't pop in on game start
                Array.Fill(_bulletAnimationStates[PlayerId], 1f);
            }

            // offsets
            float panelHeight = 160f;
            float startX = WindowUtils.WindowWidth - 25f;
            float startY = 25f + (PlayerId * panelHeight).ToResolutionY();

            // name/player index
            var nameSize = font.MeasureString(displayName);
            var namePos = new Vector2(startX - nameSize.X, startY);
            DrawUtils.DrawStringWithBorder(spriteBatch, font, displayName, namePos, baseColor, Color.White, Vector2.One, 0f);

            float bulletStartY = startY + nameSize.Y + 8f;
            for (int i = 0; i < Properties.ShellLimit; i++) {
                // fade in or fade out?
                float targetState = (i < OwnedShellCount) ? 0f : 1f;

                // speed of fade in/out
                float speed = 0.08f * RuntimeData.DeltaTime;
                animStates[i] = MathHelper.Lerp(animStates[i], targetState, speed);

                // easings
                float smoothedValue;
                if (targetState > 0.5f)
                    smoothedValue = Easings.OutBack(animStates[i]);
                else // Shooting
                    smoothedValue = Easings.OutQuart(animStates[i]);

                // interpolation
                var colorToUse = Color.Lerp(Color.DimGray * 0.5f, brightColor, smoothedValue);
                float currentScale = MathHelper.Lerp(baseScale * 0.7f, baseScale, smoothedValue);

                // draw
                float xPos = startX - offX;
                float yPos = bulletStartY + (i * (offY + spacing));
                var position = new Vector2(xPos, yPos);

                DrawUtils.DrawTextureWithBorder(spriteBatch, tex, position, colorToUse, 
                    Color.White * smoothedValue, new Vector2(currentScale).ToResolution(), 0f, Anchor.Center);
            }
        }

        // a bit hardcoded but whatever
        bool needClarification = 
            !MainMenuUI.IsActive && !LevelEditorUI.IsActive && IntermissionHandler.TankFunctionWait > 0 
            || MainMenuUI.MenuState == MainMenuUI.UIState.Mulitplayer;

        //  && PlayerId < Server.CurrentClientCount
        if (needClarification) {
            var playerColor = PlayerID.PlayerTankColors[PlayerType];
            var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, DrawParams.World, DrawParams.View, DrawParams.Projection) - new Vector2(0, 50).ToResolution();

            bool flip = false;

            float rotation = 0f;

            // flip the graphic so it doesn't appear offscreen if it would normally appear too high
            if (pos.Y <= 150) {
                flip = true;
                pos.Y += 75;
                rotation = MathHelper.Pi;
            }
            var tex1 = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/chevron_border");
            var tex2 = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/chevron_inside");

            // var p = GameHandler.AllPlayerTanks;
            //string pText = "nerd";
            var scale = 0.3f;

            string pText = Client.IsConnected() ? Server.ConnectedClients[PlayerId].Name : $"P{PlayerId + 1}"; // heeheeheeha

            TankGame.SpriteRenderer.Draw(tex1, pos, null, Color.White, rotation, Anchor.BottomCenter.GetAnchor(tex1.Size()), scale.ToResolution(), default, default);
            TankGame.SpriteRenderer.Draw(tex2, pos, null, playerColor, rotation, Anchor.BottomCenter.GetAnchor(tex2.Size()), scale.ToResolution(), default, default);

            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, pText, new(pos.X, pos.Y + (flip ? 90 : -110).ToResolutionY()), playerColor, Color.White, new Vector2(scale * 2).ToResolution(), 0f, Anchor.Center, 2f);
        }

        if (DebugManager.DebugLevel == 1 || _drawShotPath)
            DrawShootPath();

        if (Properties.Invisible && CampaignGlobals.InMission)
            return;

        Properties.Armor?.Render();
    }
    public override string ToString()
        => $"pos: {Position} | vel: {Velocity} | dead: {IsDestroyed} | rotation: {ChassisRotation} | OwnedBullets: {OwnedShellCount}";
}