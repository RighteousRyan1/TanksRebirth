using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Internals.Common.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common;
using TanksRebirth.Net;
using TanksRebirth.Graphics.Cameras;
using System.Linq;
using System;

namespace TanksRebirth.GameContent.Globals;

public static class CameraGlobals {

    public static bool IsUsingFirstPresonCamera => MatrixUtils.AreMatricesEqual(GameProjection, RebirthFreecam.Projection, 0.1f);

    // screen camera stuff

    public static Matrix ScreenView;
    public static Matrix ScreenProjOrthographic;
    public static Matrix ScreenProjPerspective;

    public static void SetMatrices() {
        // old = Matrix.CreateOrthographicOffCenter(0f, WindowUtils.WindowWidth, WindowUtils.WindowHeight, 0f, 0.1f, 100000f); 
        ScreenProjOrthographic = Matrix.CreateOrthographic(TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, -2000f, 5000f);
        ScreenProjPerspective = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), TankGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.1f, 10000f);
        
        // still dont know why i offset z by -500
        ScreenView = Matrix.CreateLookAt(Vector3.Backward, Vector3.Zero, Vector3.Up) * Matrix.CreateTranslation(0, 0, -500);
    }

    // game camera stuff

    static bool _oView;
    static float _transitionTimer;
    public static bool OverheadView {
        get => _oView;
        set {
            _transitionTimer = 100;
            _oView = value;
        }
    }

    public static Freecam RebirthFreecam;

    public static int SpectatorId;

    // 1/8 of a circle, as opposed to 0.75, maybe?
    public const float DEFAULT_ORTHOGRAPHIC_ANGLE = MathHelper.PiOver4;
    public const float DEFAULT_ZOOM = 3.25f;

    public const float LVL_EDIT_ZOOM = 0.6f;
    public const float LVL_EDIT_Y_OFF = 82f;
    public const float LVL_EDIT_ANGLE = MathHelper.PiOver2;
    public static float AddativeZoom = 1f;
    public static float POVCameraRotation;

    public static Vector2 CameraFocusOffset;
    public static Vector2 OrthoRotationVector = new(0, DEFAULT_ORTHOGRAPHIC_ANGLE);
    public static Vector3 POVCameraPosition = new(0, 100, 0);

    public static Matrix GameView;
    public static Matrix GameProjection;

    public static void Initialize(GraphicsDevice device) {
        RebirthFreecam = new(device);
        RebirthFreecam.Position = MainMenuUI.MenuCameraManipulations[MainMenuUI.UIState.LoadingMods].Position;
    }
    public static void UpdateOverhead() {
        if (_transitionTimer > 0) {
            _transitionTimer -= RuntimeData.DeltaTime;
            if (OverheadView) {
                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, LVL_EDIT_ANGLE, 0.08f * RuntimeData.DeltaTime);
                AddativeZoom = MathUtils.SoftStep(AddativeZoom, LVL_EDIT_ZOOM, 0.08f * RuntimeData.DeltaTime);
                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, LVL_EDIT_Y_OFF, 2f * RuntimeData.DeltaTime);
            }
            else {
                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, DEFAULT_ORTHOGRAPHIC_ANGLE, 0.08f * RuntimeData.DeltaTime);
                if (!LevelEditorUI.IsActive)
                    AddativeZoom = MathUtils.SoftStep(AddativeZoom, 1f, 0.08f * RuntimeData.DeltaTime);
                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 0f, 2f * RuntimeData.DeltaTime);
                Console.WriteLine(RuntimeData.DeltaTime);
            }
        }
    }

    public static void Update() {
        bool isFreecam = DebugManager.DebugLevel == DebugManager.Id.FreeCamTest || DebugManager.persistFreecam;
        bool isMainMenu = MainMenuUI.IsActive;
        bool isPOV = Difficulties.Types["POV"] && !isMainMenu;

        if (!isFreecam) {
            if (!isMainMenu) {
                if (!Difficulties.Types["POV"] || LevelEditorUI.IsActive) {
                    UpdateOverhead();

                    GameView = Matrix.CreateScale(DEFAULT_ZOOM * AddativeZoom) *
                               Matrix.CreateLookAt(new(0f, 0, 100), Vector3.Zero, Vector3.Up) *
                               Matrix.CreateTranslation(CameraFocusOffset.X, -CameraFocusOffset.Y - 110, 0) *
                               Matrix.CreateRotationY(OrthoRotationVector.X) *
                               Matrix.CreateRotationX(OrthoRotationVector.Y);

                    GameProjection = Matrix.CreateOrthographic(1920, 1080, -2000, 5000);
                    // GameProjection = Matrix.CreateOrthographic(WindowUtils.WindowWidth, WindowUtils.WindowHeight, -2000, 5000);
                }
            }
            else {
                if (MainMenuUI.CameraPositionAnimator.CurrentPosition3D != Vector3.Zero) {
                    RebirthFreecam.Position = MainMenuUI.CameraPositionAnimator.CurrentPosition3D;
                    RebirthFreecam.Rotation = MainMenuUI.CameraRotationAnimator.CurrentPosition3D;
                }

                RebirthFreecam.HasLookAt = false;
                RebirthFreecam.FieldOfView = 100f;
                RebirthFreecam.NearViewDistance = 0.1f;
                RebirthFreecam.FarViewDistance = 100000f;

                GameView = RebirthFreecam.View;
                GameProjection = RebirthFreecam.Projection;
            }

            if (isPOV) {
                if (PlayerTank.ClientTank is { IsDestroyed: false }) {
                    SpectatorId = NetPlay.GetMyClientId();
                    POVCameraPosition = PlayerTank.ClientTank.Position.ExpandZ();
                    POVCameraRotation = -PlayerTank.ClientTank.TurretRotation;
                }
                else if (GameHandler.AllPlayerTanks[SpectatorId] is not null) {
                    if (InputUtils.KeyJustPressed(Keys.Left))
                        SpectatorId = SpectateValidTank(SpectatorId, false);
                    else if (InputUtils.KeyJustPressed(Keys.Right))
                        SpectatorId = SpectateValidTank(SpectatorId, true);

                    POVCameraPosition = GameHandler.AllPlayerTanks[SpectatorId].Position.ExpandZ();
                    POVCameraRotation = -GameHandler.AllPlayerTanks[SpectatorId].TurretRotation;
                }

                if (IntermissionHandler.ThirdPersonTransitionAnimation is not null && PlayerTank.ClientTank is not null) {
                    IntermissionHandler.ThirdPersonTransitionAnimation.KeyFrames[1] = new(
                        position2d: new Vector2(-PlayerTank.ClientTank.TurretRotation),
                        position3d: PlayerTank.ClientTank.Position3D
                    );
                }

                var anim = IntermissionHandler.ThirdPersonTransitionAnimation;
                var povCameraRotationCurrent = IntermissionHandler.TankFunctionWait > 0 && anim != null ?
                    anim.CurrentPosition2D.X : POVCameraRotation;

                var povCameraPosCurrent = IntermissionHandler.TankFunctionWait > 0 && anim != null ?
                    anim.CurrentPosition3D : POVCameraPosition;

                GameView = Matrix.CreateLookAt(
                    povCameraPosCurrent,
                    POVCameraPosition + new Vector2(0, 20).Rotate(povCameraRotationCurrent).ExpandZ(),
                    Vector3.Up
                ) * Matrix.CreateScale(AddativeZoom) *
                    Matrix.CreateTranslation(0, -20, 0);

                RebirthFreecam.FieldOfView = 90f;
                GameProjection = RebirthFreecam.Projection;

                RebirthFreecam.Position = POVCameraPosition;
            }
        }
        else if (!GameUI.Paused && !isMainMenu && DebugManager.DebuggingEnabled) {
            if (InputUtils.AreKeysJustPressed(Keys.Z, Keys.X)) {
                DebugManager.persistFreecam = !DebugManager.persistFreecam;
            }

            var realMoveSpeed = 10f * RuntimeData.DeltaTime;
            var rotationSpeed = 0.01f;

            RebirthFreecam.HasLookAt = false;
            RebirthFreecam.NearViewDistance = 0.1f;
            RebirthFreecam.FarViewDistance = 1_000_000f;
            RebirthFreecam.MinPitch = -180;
            RebirthFreecam.MaxPitch = 180;

            bool isPlayerActive = PlayerTank.ClientTank is not null;

            var keysprint = LevelEditorUI.IsActive || !isPlayerActive ? Keys.LeftShift : Keys.RightShift;
            var keyslow = LevelEditorUI.IsActive || !isPlayerActive ? Keys.LeftControl : Keys.RightControl;

            if (InputUtils.CurrentKeySnapshot.IsKeyDown(keysprint))
                realMoveSpeed *= 2;
            if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyslow))
                realMoveSpeed /= 4;

            var keyf = LevelEditorUI.IsActive || !isPlayerActive ? Keys.W : Keys.Up;
            var keyb = LevelEditorUI.IsActive || !isPlayerActive ? Keys.S : Keys.Down;
            var keyl = LevelEditorUI.IsActive || !isPlayerActive ? Keys.A : Keys.Left;
            var keyr = LevelEditorUI.IsActive || !isPlayerActive ? Keys.D : Keys.Right;

            if (InputUtils.MouseRight) {
                RebirthFreecam.Rotation -= new Vector3(0, 
                    MouseUtils.MouseVelocity.Y * rotationSpeed,
                    MouseUtils.MouseVelocity.X * rotationSpeed);

                 RebirthFreecam.Rotation = new Vector3(0,
                     MathHelper.Clamp(RebirthFreecam.Rotation.Y, -MathHelper.PiOver2, MathHelper.PiOver2),
                     RebirthFreecam.Rotation.Z);
            }
            if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Add))
                RebirthFreecam.FieldOfView += 0.5f * RuntimeData.DeltaTime;
            if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Subtract))
                RebirthFreecam.FieldOfView -= 0.5f * RuntimeData.DeltaTime;
            if (InputUtils.MouseMiddle)
                RebirthFreecam.FieldOfView = 90;

            if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyf))
                RebirthFreecam.Move(RebirthFreecam.World.Forward * realMoveSpeed);
            if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyb))
                RebirthFreecam.Move(RebirthFreecam.World.Backward * realMoveSpeed);
            if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyl))
                RebirthFreecam.Move(RebirthFreecam.World.Left * realMoveSpeed);
            if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyr))
                RebirthFreecam.Move(RebirthFreecam.World.Right * realMoveSpeed);

            GameView = RebirthFreecam.View;
            GameProjection = RebirthFreecam.Projection;
        }
    }
    public static int SpectateValidTank(int id, bool increase) {
        var count = GameHandler.AllPlayerTanks.Count(x => x is not null);

        if (count == 0)
            return 0;

        var arr = GameHandler.AllPlayerTanks.Where(x => x is not null).ToArray();

        var newId = id + (increase ? 1 : -1);

        if (newId < 0) newId = arr.Length - 1;
        else if (newId >= arr.Length) newId = 0;

        if (arr[newId].IsDestroyed)
            return SpectateValidTank(newId, increase); // this should just return the only player then...?
        else return newId;
    }
}
