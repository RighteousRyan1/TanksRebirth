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
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.TankSystem;
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

namespace TanksRebirth.GameContent;

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
    #region The Rest
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
    public int PlayerId { get; }
    public int PlayerType { get; }

    public static Keybind MoveUp = new("Up", Keys.W);
    public static Keybind MoveDown = new("Down", Keys.S);
    public static Keybind MoveLeft = new("Left", Keys.A);
    public static Keybind MoveRight = new("Right", Keys.D);
    public static Keybind PlaceMine = new("Place Mine", Keys.Space);
    public static Keybind ShowShotPath = new("Draw Shot Path", Keys.Q);
    public static GamepadBind GamePadShoot = new("Fire Bullet", Buttons.RightTrigger);
    public static GamepadBind GamePadPlaceMine = new("Place Mine", Buttons.A);

    bool playerControl;
    bool _isPlayerModel;

    public Vector2 oldPosition;

    // 46 if using keyboard, 10 if using a controller
    //private float _maxTurnInputBased;
    #endregion

    public static Vector2[] AimTargets = new Vector2[4];

    // -1 if all are controllers!
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
    public PlayerTank(int playerType, bool isPlayerModel = true, int copyTier = -1) {
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
        PlayerId = playerType;

        GameHandler.AllPlayerTanks[PlayerId] = this;

        // initialize drawing parameters
        DrawParams.UsePhong = _isPlayerModel;
        DrawParams.LightPower = 1f;
        DrawParams.AmbientPower = _isPlayerModel ? TankDrawParams.PLR_AMB_MUL : TankDrawParams.AI_AMB_MUL;

        if (copyTier == -1) ApplyDefaults(ref Properties);

        WorldId = Array.IndexOf(GameHandler.AllTanks, null);
        GameHandler.AllTanks[WorldId] = this;

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

        Properties.ShootPitch = 0.1f * PlayerType;

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
        
        base.Update();

        if (LevelEditorUI.IsActive || IsDestroyed) return;

        if (IsTurning) {
            if (DesiredChassisRotation - ChassisRotation >= MathHelper.PiOver2) {
                ChassisRotation += MathHelper.Pi;
                Flip = !Flip;
            }
            else if (DesiredChassisRotation - ChassisRotation <= -MathHelper.PiOver2) {
                ChassisRotation -= MathHelper.Pi;
                Flip = !Flip;
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

            if (InputUtils.CanDetectClick() && UsesKeyboard) {
                if (!ChatSystem.ChatBoxHover && !ChatSystem.ActiveHandle && !GameUI.Paused) {
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

        if (TankGame.PlayerMice.Count <= PlayerId) return;

        if (UsesKeyboard)
            AimTargets[PlayerId] = MouseUtils.MousePosition;

        var shouldAimingHappen = !Modifiers.Map[Modifiers.POV] || LevelEditorUI.IsActive || MainMenuUI.IsActive;
        if (shouldAimingHappen) {
            //int padIndex = GamepadIndex;
            //if (padIndex < 0)
            //    return; // KBM player will use actual mouse

            var cursorToAimAt = TankGame.PlayerMice[PlayerId];

            var mouseWorldPos = MatrixUtils.GetWorldPosition(cursorToAimAt.Position, -11f);
            if (!LevelEditorUI.IsActive)
                TurretRotation = -(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position).ToRotation() + MathHelper.PiOver2;
            else
                TurretRotation = ChassisRotation;
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

                TurretRotation += -deltaX / (312f.ToResolutionX());

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
            var unprojectedPosition = MatrixUtils.ConvertWorldToScreen(
                new Vector3(0, 11, 0), DrawParams.World, DrawParams.View, DrawParams.Projection);

            var newMousePos = new Vector2(
                (int)(unprojectedPosition.X + rightStick.X * 400),
                (int)(unprojectedPosition.Y - rightStick.Y * 400));

            TankGame.PlayerMice[padIndex + 1].Position = newMousePos;
        }
    }
    void ControlHandle_Keybinding() {
        if (PlayerId != PlayerControlledByKeyboard)
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
        if (Client.IsConnected()) {
            // maybe make a camera transition to said tank.

            //if (context.Source is not null)
            //CameraGlobals.SpectatorId = CameraGlobals.SpectateValidTank(context.Source.WorldId, true);

            // only decements the lives on the destroyed player's system, where lives are synced across everyone at all times
            if (NetPlay.IsClientMatched(PlayerId)) {
                Lives[PlayerId]--;
            }
        }
        else {
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
        }
    }
    void DrawShootPath() {
        const int MAX_PATH_UNITS = 10000;

        var whitePixel = TextureGlobals.Pixels[Color.White];
        var pathPos = Position + new Vector2(0, 18).RotatedBy(-TurretRotation);
        var pathDir = Vector2.UnitY.RotatedBy(TurretRotation - MathHelper.Pi);
        pathDir.Y *= -1;
        pathDir *= Properties.ShellSpeed;

        var pathRicochetCount = 0;


        for (int i = 0; i < MAX_PATH_UNITS; i++) {
            var dummyPos = Vector2.Zero;

            if (pathPos.X < GameScene.MIN_X || pathPos.X > GameScene.MAX_X) {
                pathRicochetCount++;
                pathDir.X *= -1;
            }
            if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                pathRicochetCount++;
                pathDir.Y *= -1;
            }

            var pathHitbox = new Rectangle((int)pathPos.X - 3, (int)pathPos.Y - 3, 6, 6);

            // Why is velocity passed by reference here lol
            Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, (c) => c.Properties.IsSolid);

            if (corner)
                return;
            if (block != null) {
                if (block.Properties.AllowShotPathBounce) {
                    switch (dir) {
                        case CollisionDirection.Up:
                        case CollisionDirection.Down:
                            pathDir.Y *= -1;
                            pathRicochetCount += block.Properties.PathBounceCount;
                            break;
                        case CollisionDirection.Left:
                        case CollisionDirection.Right:
                            pathDir.X *= -1;
                            pathRicochetCount += block.Properties.PathBounceCount;
                            break;
                    }
                }
            }

            var cannotBounce = pathRicochetCount > Properties.RicochetCount;
            if (cannotBounce) return;
            var tankInPath = GameHandler.AllTanks.FirstOrDefault(
                tnk => tnk is not null && !tnk.IsDestroyed && tnk.CollisionCircle.Intersects(new Circle { Center = pathPos, Radius = 4 }));
            if (Array.IndexOf(GameHandler.AllTanks, tankInPath) > -1 && tankInPath is not null) {
                TanksSpotted = [tankInPath!];
                return;
            }
            TanksSpotted = [];

            pathPos += pathDir;

            var pathPosScreen = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), CameraGlobals.GameView, CameraGlobals.GameProjection);
            var off = MathF.Abs(MathF.Sin(i * MathF.PI / 5 - RuntimeData.RunTime * 0.3f));
            var rgbColor = ColorUtils.HsvToRgb(RuntimeData.UpdateCount + i % 255 / 255f * 360, 1, 1);
            var scale = Vector2.One * 4 * off;
            DrawUtils.DrawTextureWithBorder(TankGame.SpriteRenderer, whitePixel, pathPosScreen, Color.Black,
                rgbColor, scale, 0f, Anchor.Center, 1f);
            //TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, ColorUtils.HsvToRgb(RuntimeData.UpdateCount + i % 255 / 255f * 360, 1, 1), 0, whitePixel.Size() / 2, new Vector2(3 + off).ToResolution(), default, default);
        }
    }
    public override void Render() {
        base.Render();
        if (IsDestroyed)
            return;
        DrawExtras();
        if (Properties.Invisible && CampaignGlobals.InMission)
            return;
        for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            foreach (ModelMesh mesh in DrawParamsTank.Model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = i == 0 ? boneTransforms[mesh.ParentBone.Index] : 
                        boneTransforms[mesh.ParentBone.Index] 
                        * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                    effect.View = DrawParams.View;
                    effect.Projection = DrawParams.Projection;
                    effect.TextureEnabled = true;

                    if (!Properties.HasTurret)
                        if (mesh.Name == "Cannon")
                            return;

                    if (mesh.Name == "Shadow") {
                        if (!Lighting.AccurateShadows) {
                            effect.Alpha = DrawParamsTank.ShadowAlpha;
                            effect.Texture = DrawParamsTank.ShadowTexture;
                            mesh.Draw();
                        }
                        continue;
                    }

                    effect.Alpha = DrawParamsTank.TankAlpha;
                    effect.Texture = DrawParamsTank.TankTexture;

                    effect.SetDefaultGameLighting_IngameEntities(DrawParams.LightPower, DrawParams.AmbientPower, DrawParams.UsePhong, DrawParams.LightDirection);
                    mesh.Draw();
                }
            }
        }
    }
    private void DrawExtras() {
        if (IsDestroyed)
            return;

        if (!MainMenuUI.IsActive) {
            if (NetPlay.IsClientMatched(PlayerId)) {
                var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/bullet_ui");
                var scale = 0.5f; // the graphic gets smaller for each availiable shell.
                for (int i = 0; i < Properties.ShellLimit; i++) {
                    var scalar = 0.95f + (i * 0.001f); //changetankproperty ShellLimit 
                    scalar = MathHelper.Clamp(scalar, 0f, 0.99f);
                    scale *= scalar;
                }
                var realSize = (tex.Size() * scale).ToResolution();
                for (int i = 1; i <= Properties.ShellLimit; i++) {
                    var colorToUse = i > OwnedShellCount ? Color.White : Color.DimGray;
                    var position = new Vector2(WindowUtils.WindowWidth - realSize.X * scale, 0);
                    TankGame.SpriteRenderer.Draw(tex, position + new Vector2(0, i * (realSize.Y + (scale * 10).ToResolutionY())), null, colorToUse, 0f, new Vector2(tex.Size().X, 0), new Vector2(scale).ToResolution(), default, default);
                }
            }
        }

        // a bit hardcoded but whatever
        bool needClarification = (!MainMenuUI.IsActive && !LevelEditorUI.IsActive && IntermissionHandler.TankFunctionWait > 0) || MainMenuUI.MenuState == MainMenuUI.UIState.Mulitplayer;

        if (needClarification && PlayerId < Server.CurrentClientCount) {
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