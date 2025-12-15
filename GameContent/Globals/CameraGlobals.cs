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
using TanksRebirth.Internals.Common.Framework.Animation;
using System.Collections.Generic;

namespace TanksRebirth.GameContent.Globals;

#pragma warning disable
public static class CameraGlobals {
    enum CameraMode {
        Overhead,
        POV,
        MainMenu,
        Freecam
    }
    public static bool IsUsingFirstPersonCamera => MatrixUtils.AreMatricesEqual(GameProjection, RebirthFreecam.Projection, 0.1f);

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

    public const float POV_CAM_OFFSET_Y = 20f;

    static Animator _povAnimatorTest; // used in debug cam mode to create custom transitions
    static EasingFunction _povAnimatorFunction;
    static List<KeyFrame> _povAnimatorKeyframeBuilder;

    public static void Initialize(GraphicsDevice device) {
        RebirthFreecam = new(device) {
            Position = MainMenuUI.MenuCameraManipulations[MainMenuUI.UIState.LoadingMods].Position
        };
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
                // Console.WriteLine(RuntimeData.DeltaTime);
            }
        }
    }
    static CameraMode GetCameraMode() {
        bool isMainMenu = MainMenuUI.IsActive;
        bool isFreecamFlag =
            DebugManager.DebugLevel == DebugManager.Id.FreeCamTest ||
            DebugManager.persistFreecam;

        bool isPOV = Modifiers.Map[Modifiers.POV] && !isMainMenu;

        if (isMainMenu)
            return CameraMode.MainMenu;

        if (isFreecamFlag && !GameUI.Paused && DebugManager.DebuggingEnabled)
            return CameraMode.Freecam;

        if (isPOV)
            return CameraMode.POV;

        return CameraMode.Overhead;
    }
    public static void UpdateCamera() {
        var mode = GetCameraMode();

        switch (mode) {
            case CameraMode.MainMenu:
                UpdateMainMenuCamera();
                break;

            case CameraMode.Freecam:
                UpdateFreecamCamera();
                break;

            case CameraMode.POV:
                UpdateOverheadCamera(); 
                ManagePOV();
                break;

            case CameraMode.Overhead:
            default:
                UpdateOverheadCamera();
                break;
        }
    }
    static void UpdateOverheadCamera() {
        UpdateOverhead();

        GameView =
            Matrix.CreateScale(DEFAULT_ZOOM * AddativeZoom) *
            Matrix.CreateLookAt(new(0f, 0f, 100f), Vector3.Zero, Vector3.Up) *
            Matrix.CreateTranslation(
                CameraFocusOffset.X,
                -CameraFocusOffset.Y - 110f,
                0f) *
            Matrix.CreateRotationY(OrthoRotationVector.X) *
            Matrix.CreateRotationX(OrthoRotationVector.Y);

        GameProjection = Matrix.CreateOrthographic(1920, 1080, -2000f, 5000f);
    }

    static void UpdateMainMenuCamera() {
        if (MainMenuUI.CameraPositionAnimator.CurrentPosition != Vector3.Zero) {
            RebirthFreecam.Position = MainMenuUI.CameraPositionAnimator.CurrentPosition;
            RebirthFreecam.Rotation = MainMenuUI.CameraRotationAnimator.CurrentPosition;
        }

        RebirthFreecam.HasLookAt = false;
        RebirthFreecam.FieldOfView = 100f;
        RebirthFreecam.NearViewDistance = 0.1f;
        RebirthFreecam.FarViewDistance = 100000f;

        GameView = RebirthFreecam.View;
        GameProjection = RebirthFreecam.Projection;
    }

    static void UpdateFreecamCamera() {
        if (InputUtils.AreKeysJustPressed(Keys.Z, Keys.X))
            DebugManager.persistFreecam = !DebugManager.persistFreecam;

        float realMoveSpeed = 10f * RuntimeData.DeltaTime;
        const float rotationSpeed = 0.01f;

        RebirthFreecam.HasLookAt = false;
        RebirthFreecam.NearViewDistance = 0.1f;
        RebirthFreecam.FarViewDistance = 1_000_000f;
        RebirthFreecam.MinPitch = -180f;
        RebirthFreecam.MaxPitch = 180f;

        bool isPlayerActive = PlayerTank.ClientTank is not null;
        bool editorOrNoPlayer = LevelEditorUI.IsActive || !isPlayerActive;

        Keys keySprint = editorOrNoPlayer ? Keys.LeftShift : Keys.RightShift;
        Keys keySlow = editorOrNoPlayer ? Keys.LeftControl : Keys.RightControl;

        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keySprint))
            realMoveSpeed *= 2f;
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keySlow))
            realMoveSpeed /= 4f;

        Keys keyF = editorOrNoPlayer ? Keys.W : Keys.Up;
        Keys keyB = editorOrNoPlayer ? Keys.S : Keys.Down;
        Keys keyL = editorOrNoPlayer ? Keys.A : Keys.Left;
        Keys keyR = editorOrNoPlayer ? Keys.D : Keys.Right;

        if (InputUtils.MouseRight) {
            RebirthFreecam.Rotation -= new Vector3(
                0f,
                MouseUtils.MouseVelocity.Y * rotationSpeed,
                MouseUtils.MouseVelocity.X * rotationSpeed);

            RebirthFreecam.Rotation = new Vector3(
                0f,
                MathHelper.Clamp(
                    RebirthFreecam.Rotation.Y,
                    -MathHelper.PiOver2,
                    MathHelper.PiOver2),
                RebirthFreecam.Rotation.Z);
        }

        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.Subtract))
            RebirthFreecam.FieldOfView += 0.5f * RuntimeData.DeltaTime;
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.Add))
            RebirthFreecam.FieldOfView -= 0.5f * RuntimeData.DeltaTime;
        if (InputUtils.MouseMiddle)
            RebirthFreecam.FieldOfView = 90f;

        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyF))
            RebirthFreecam.Move(RebirthFreecam.World.Forward * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyB))
            RebirthFreecam.Move(RebirthFreecam.World.Backward * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyL))
            RebirthFreecam.Move(RebirthFreecam.World.Left * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyR))
            RebirthFreecam.Move(RebirthFreecam.World.Right * realMoveSpeed);

        GameView = RebirthFreecam.View;
        GameProjection = RebirthFreecam.Projection;
    }

    public static void ManagePOV() {
        var clientTank = PlayerTank.ClientTank;

        if (clientTank is null) return;

        Vector3 offsetVector = new(0, POV_CAM_OFFSET_Y, 0);
        var plOffset = clientTank.Position3D + offsetVector;
        if (clientTank is { IsDestroyed: true }) {
            SpectatorId = NetPlay.GetMyClientId();
            POVCameraPosition = plOffset;
            POVCameraRotation = -clientTank.TurretRotation;
        }
        else if (GameHandler.AllPlayerTanks[SpectatorId] is not null) {
            if (InputUtils.KeyJustPressed(Keys.Left))
                SpectatorId = SpectateValidTank(SpectatorId, false);
            else if (InputUtils.KeyJustPressed(Keys.Right))
                SpectatorId = SpectateValidTank(SpectatorId, true);

            POVCameraPosition = GameHandler.AllPlayerTanks[SpectatorId].Position.ExpandZ() + offsetVector;
            POVCameraRotation = -GameHandler.AllPlayerTanks[SpectatorId].TurretRotation;
        }

        if (IntermissionHandler.ThirdPersonTransition is not null && PlayerTank.ClientTank is not null) {
            IntermissionHandler.ThirdPersonTransition.KeyFrames[1] = new(
                position: plOffset
            );
            IntermissionHandler.ThirdPersonTransition.ModifyFloat(0, -clientTank.TurretRotation);
        }

        var anim = IntermissionHandler.ThirdPersonTransition;
        var povCameraRotationCurrent = IntermissionHandler.TankFunctionWait > 0 && anim != null ?
            // the current anim rotation to meet the tank turret rotation
            -clientTank.TurretRotation : POVCameraRotation;

        var povCameraPosCurrent = IntermissionHandler.TankFunctionWait > 0 && anim != null ?
            anim.CurrentPosition : POVCameraPosition;

        GameView = Matrix.CreateLookAt(
            povCameraPosCurrent,
            POVCameraPosition + new Vector2(0, 20).RotatedBy(povCameraRotationCurrent).ExpandZ(),
            Vector3.Up
        ) * Matrix.CreateScale(AddativeZoom);

        RebirthFreecam.FieldOfView = 90f;
        GameProjection = RebirthFreecam.Projection;

        RebirthFreecam.Position = povCameraPosCurrent;
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
