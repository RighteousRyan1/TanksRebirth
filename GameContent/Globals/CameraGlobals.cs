using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Steamworks;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics.Cameras;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Globals;

#pragma warning disable
public static class CameraGlobals {
    enum CameraMode {
        Overhead,
        POV,
        MainMenu,
        Freecam
    }
    public static bool IsUsingFirstPersonCamera => MatrixUtils.AreMatricesEqual(GameProjection, PerspectiveCamera.Projection, 0.1f);

    // screen camera stuff

    public static Matrix ScreenView;
    public static Matrix ScreenProjOrthographic;
    public static Matrix ScreenProjPerspective;

    public static void SetMatrices() {
        ScreenProjOrthographic = Matrix.CreateOrthographic(TankGame.Instance.GraphicsDevice.Viewport.Width, TankGame.Instance.GraphicsDevice.Viewport.Height, -1000f, 1000f);
        ScreenProjPerspective = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), TankGame.Instance.GraphicsDevice.Viewport.AspectRatio, 1f, 10000f);

        // still dont know why i offset z by -500
        ScreenView = Matrix.CreateLookAt(Vector3.Backward, Vector3.Zero, Vector3.Up) * Matrix.CreateTranslation(-Vector3.UnitZ);
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

    public static Freecam PerspectiveCamera;

    public static int SpectatorId;

    // 1/8 of a circle, as opposed to 0.75, maybe?
    public const float DEFAULT_ORTHOGRAPHIC_ANGLE = MathHelper.PiOver4;
    public const float DEFAULT_ZOOM = 3.25f;

    public const float LVL_EDIT_ZOOM = 0.6f;
    public const float LVL_EDIT_Y_OFF = 82f;
    public const float LVL_EDIT_ANGLE = MathHelper.PiOver2;

    public static float AddativeZoom = 1f;
    public static float POVCameraRotation;

    public const float CAMERA_DEF_OFF_Y = -110;
    public static Vector3 CameraFocusOffset;
    public static Vector2 OrthoRotationVector = new(0, DEFAULT_ORTHOGRAPHIC_ANGLE);
    public static Vector3 POVCameraPosition = new(0, 100, 0);

    public static Matrix GameView;
    public static Matrix GameProjection;

    public const float POV_CAM_OFFSET_Y = 20f;

    public static BoundingFrustum ViewFrustum = new(Matrix.Identity);

    // freecam options

    // 1.0 = locked, 0.0 = unlocked
    static Vector3 _locks;

    public static void Initialize(GraphicsDevice device) {
        PerspectiveCamera = new(device) {
            Position = MainMenuUI.MenuGraphicsStates[MainMenuUI.UIState.LoadingMods].Position
        };
    }
    public static void UpdateOverhead() {
        if (_transitionTimer > 0) {
            _transitionTimer -= RuntimeData.DeltaFrames;
            if (OverheadView) {
                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, LVL_EDIT_ANGLE, 0.08f * RuntimeData.DeltaFrames);
                AddativeZoom = MathUtils.SoftStep(AddativeZoom, LVL_EDIT_ZOOM, 0.08f * RuntimeData.DeltaFrames);
                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, LVL_EDIT_Y_OFF, 2f * RuntimeData.DeltaFrames);
            }
            else {
                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, DEFAULT_ORTHOGRAPHIC_ANGLE, 0.08f * RuntimeData.DeltaFrames);
                if (!LevelEditorUI.IsActive)
                    AddativeZoom = MathUtils.SoftStep(AddativeZoom, 1f, 0.08f * RuntimeData.DeltaFrames);
                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 0f, 2f * RuntimeData.DeltaFrames);
                // Console.WriteLine(RuntimeData.DeltaFrames);
            }
        }
    }
    static CameraMode GetCameraMode() {
        bool isMainMenu = MainMenuUI.IsActive;
        bool isFreecamFlag =
            DebugManager.DebugLevel == DebugManager.Id.FREECAM ||
            DebugManager.persistFreecam;

        bool isPOV = Modifiers.IsOn(Modifiers.POV) && !isMainMenu;

        if (isMainMenu)
            return CameraMode.MainMenu;

        if (isFreecamFlag && DebugManager.DebuggingEnabled)
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

        // if (InputUtils.KeyJustPressed(Keys.N))
        ViewFrustum.Matrix = GameView * GameProjection;
    }
    static void UpdateOverheadCamera() {
        UpdateOverhead();

        // default orthographic view. maybe i should split into cameras, like ICamera?

        GameView =
            Matrix.CreateScale(DEFAULT_ZOOM * AddativeZoom) *
            Matrix.CreateLookAt(new(0f, 0f, 100f), Vector3.Zero, Vector3.Up) *
            Matrix.CreateTranslation(
                CameraFocusOffset.X,
                -CameraFocusOffset.Y + CAMERA_DEF_OFF_Y,
                -CameraFocusOffset.Z) *
            Matrix.CreateRotationY(OrthoRotationVector.X) *
            Matrix.CreateRotationX(OrthoRotationVector.Y);

        var defRes = WindowUtils.RenderResolution;
        GameProjection = Matrix.CreateOrthographic(defRes.X, defRes.Y, -3500, 75000f);
    }
    static void UpdateMainMenuCamera() {
        if (MainMenuUI.CameraPositionAnimator.CurrentPosition != Vector3.Zero) {
            PerspectiveCamera.Position = MainMenuUI.CameraPositionAnimator.CurrentPosition;
            PerspectiveCamera.Rotation = MainMenuUI.CameraRotationAnimator.CurrentPosition;
        }

        PerspectiveCamera.UseFocus = false;
        PerspectiveCamera.FieldOfView = 100f;

        GameView = PerspectiveCamera.View;
        GameProjection = PerspectiveCamera.Projection;
    }
    static void UpdateFreecamCamera() {
        if (InputUtils.AreKeysJustPressed(Keys.Z, Keys.X))
            DebugManager.persistFreecam = !DebugManager.persistFreecam;

        float realMoveSpeed = 5f * RuntimeData.DeltaFrames;
        const float rotationSpeed = 0.01f;

        PerspectiveCamera.UseFocus = false;
        PerspectiveCamera.Far = 75000;
        PerspectiveCamera.MinPitch = -180f;
        PerspectiveCamera.MaxPitch = 180f;

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
        Keys keyU = Keys.E;
        Keys keyD = Keys.Q;

        // smooths movement based on zoom/low fov
        var mul = CameraGlobals.PerspectiveCamera.FieldOfView / 90;
        if (InputUtils.MouseRight) {
            var subPos = new Vector3(
                0f,
                MouseUtils.MouseVelocity.Y * rotationSpeed,
                MouseUtils.MouseVelocity.X * rotationSpeed);
            PerspectiveCamera.Rotation -= subPos * mul;

            var clamped = new Vector3(
                0f,
                MathHelper.Clamp(
                    PerspectiveCamera.Rotation.Y,
                    -MathHelper.PiOver2,
                    MathHelper.PiOver2),
                PerspectiveCamera.Rotation.Z);
            PerspectiveCamera.Rotation = clamped;
        }

        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.Subtract))
            PerspectiveCamera.FieldOfView += 0.5f * RuntimeData.DeltaFrames;
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.Add))
            PerspectiveCamera.FieldOfView -= 0.5f * RuntimeData.DeltaFrames;
        if (InputUtils.MouseMiddle)
            PerspectiveCamera.FieldOfView = 90f;

        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyF))
            PerspectiveCamera.Move(PerspectiveCamera.World.Forward * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyB))
            PerspectiveCamera.Move(PerspectiveCamera.World.Backward * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyL))
            PerspectiveCamera.Move(PerspectiveCamera.World.Left * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyR))
            PerspectiveCamera.Move(PerspectiveCamera.World.Right * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyU))
            PerspectiveCamera.Move(Vector3.UnitY * realMoveSpeed);
        if (InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(keyD))
            PerspectiveCamera.Move(-Vector3.UnitY * realMoveSpeed);

        GameView = PerspectiveCamera.View;
        GameProjection = PerspectiveCamera.Projection;

        // ensures rotation doesn't just... go haywire.
        PerspectiveCamera.Rotation = new(PerspectiveCamera.Rotation.X, PerspectiveCamera.Rotation.Y, PerspectiveCamera.Rotation.Z % MathHelper.Tau);
    }

    public static void ManagePOV() {
        var clientTank = PlayerTank.ClientTank;

        if (clientTank is null) return;

        Vector3 offsetVector = new(0, POV_CAM_OFFSET_Y, 0);
        var plOffset = clientTank.Position3D + offsetVector;
        if (!clientTank.IsDestroyed) {
            SpectatorId = NetPlay.GetMyClientId();
            POVCameraPosition = plOffset;
            POVCameraRotation = clientTank.TurretRotation;
        }
        else if (GameHandler.AllPlayerTanks[SpectatorId] is not null) {
            if (InputUtils.KeyJustPressed(Keys.Left))
                SpectatorId = SpectateValidTank(SpectatorId, false);
            else if (InputUtils.KeyJustPressed(Keys.Right))
                SpectatorId = SpectateValidTank(SpectatorId, true);

            var spectated = GameHandler.AllPlayerTanks[SpectatorId];
            if (spectated is null || spectated.IsDestroyed) {
                SpectatorId = SpectateValidTank(SpectatorId, true);
                spectated = GameHandler.AllPlayerTanks[SpectatorId];
            }
            if (spectated is not null) {
                POVCameraPosition = spectated.Position.ExpandZ() + offsetVector;
                POVCameraRotation = spectated.TurretRotation;
            }
        }
        var anim = IntermissionHandler.ThirdPersonTransition;
        if (anim is not null && clientTank is not null) {
            // keeps the animation updated
            anim.ModifyFloat(0, clientTank.TurretRotation);
        }

        var povCameraRotationCurrent = IntermissionHandler.TankFunctionWait > 0 && anim != null ?
            // the current anim rotation to meet the tank turret rotation
            clientTank.TurretRotation : POVCameraRotation;

        var povCameraPosCurrent = IntermissionHandler.TankFunctionWait > 0 && anim != null ?
            anim.CurrentPosition : POVCameraPosition;

        GameView = Matrix.CreateLookAt(
            povCameraPosCurrent,
            POVCameraPosition + new Vector2(0, 20).RotatedBy(povCameraRotationCurrent).ExpandZ(),
            Vector3.Up
        ) * Matrix.CreateScale(AddativeZoom);

        PerspectiveCamera.FieldOfView = 90f;
        GameProjection = PerspectiveCamera.Projection;

        PerspectiveCamera.Position = povCameraPosCurrent;
    }

    public static int SpectateValidTank(int id, bool increase) {
        var tanks = GameHandler.AllPlayerTanks;
        for (int step = 1; step <= tanks.Length; step++) {
            // maintains id in array bounds. looks funny but works
            var next = ((increase ? id + step : id - step) % tanks.Length + tanks.Length) % tanks.Length;
            if (tanks[next] is { IsDestroyed: false })
                return next;
        }
        return id;
    }
}
