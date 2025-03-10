using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.TimeZoneInfo;
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

namespace TanksRebirth.GameContent.Globals;

public static class CameraGlobals {
    private static bool _oView;
    private static int _transitionTimer;
    public static bool OverheadView {
        get => _oView;
        set {
            _transitionTimer = 100;
            _oView = value;
        }
    }

    public static Freecam RebirthFreecam;

    public static int SpectatorId;

    public const float DEFAULT_ORTHOGRAPHIC_ANGLE = 0.75f;
    public const float DEFAULT_ZOOM = 3.3f;
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
            _transitionTimer--;
            if (OverheadView) {
                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, MathHelper.PiOver2, 0.08f * TankGame.DeltaTime);
                AddativeZoom = MathUtils.SoftStep(AddativeZoom, 0.6f, 0.08f * TankGame.DeltaTime);
                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 82f, 2f * TankGame.DeltaTime);
            }
            else {
                OrthoRotationVector.Y = MathUtils.SoftStep(OrthoRotationVector.Y, DEFAULT_ORTHOGRAPHIC_ANGLE, 0.08f * TankGame.DeltaTime);
                if (!LevelEditorUI.Active)
                    AddativeZoom = MathUtils.SoftStep(AddativeZoom, 1f, 0.08f * TankGame.DeltaTime);
                CameraFocusOffset.Y = MathUtils.RoughStep(CameraFocusOffset.Y, 0f, 2f * TankGame.DeltaTime);
            }
        }
    }
    public static void Update() {
        if (DebugManager.DebugLevel != DebugManager.Id.FreeCamTest && !DebugManager.persistFreecam) {
            if (!MainMenuUI.Active) {
                if (!Difficulties.Types["POV"] || LevelEditorUI.Active) {
                    UpdateOverhead();

                    GameView = Matrix.CreateScale(DEFAULT_ZOOM * AddativeZoom) *
                        // TODO: the Z component is 350 because for some reason values have been offset by that amount. i'll have to dig into my code
                        // to see where tf that happens but alright
                        Matrix.CreateLookAt(new(0f, 0, 350f), Vector3.Zero, Vector3.Up) *
                        Matrix.CreateTranslation(CameraFocusOffset.X, -CameraFocusOffset.Y + 40, 0) *
                        Matrix.CreateRotationY(OrthoRotationVector.X) *
                        Matrix.CreateRotationX(OrthoRotationVector.Y);
                    GameProjection = Matrix.CreateOrthographic(1920, 1080, -2000, 5000);
                }
            }
            else {
                // main menu animation semantics
                if (MainMenuUI.CameraPositionAnimator.CurrentPosition3D != Vector3.Zero) {
                    RebirthFreecam.Position = MainMenuUI.CameraPositionAnimator.CurrentPosition3D;
                    RebirthFreecam.Rotation = MainMenuUI.CameraRotationAnimator.CurrentPosition3D;
                }
                //RebirthFreecam.HasLookAt = true;
                //RebirthFreecam.LookAt = new Vector3(0, 0, 50);
                RebirthFreecam.FieldOfView = 100f;
                RebirthFreecam.NearViewDistance = 0.1f;
                RebirthFreecam.FarViewDistance = 100000f;

                GameView = RebirthFreecam.View;
                GameProjection = RebirthFreecam.Projection;
            }
            if (Difficulties.Types["POV"]) {
                if (GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()] is not null && !GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].Dead) {
                    SpectatorId = NetPlay.GetMyClientId();
                    POVCameraPosition = GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].Position.ExpandZ();
                    POVCameraRotation = -GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()].TurretRotation;
                }
                else if (GameHandler.AllPlayerTanks[SpectatorId] is not null) {

                    if (InputUtils.KeyJustPressed(Keys.Left))
                        SpectatorId = SpectateValidTank(SpectatorId, false);
                    else if (InputUtils.KeyJustPressed(Keys.Right))
                        SpectatorId = SpectateValidTank(SpectatorId, true);

                    POVCameraPosition = GameHandler.AllPlayerTanks[SpectatorId].Position.ExpandZ();
                    POVCameraRotation = -GameHandler.AllPlayerTanks[SpectatorId].TurretRotation;
                }


                // pov...

                if (IntermissionHandler.ThirdPersonTransitionAnimation != null && PlayerTank.ClientTank is not null) {
                    IntermissionHandler.ThirdPersonTransitionAnimation.KeyFrames[1]
                        = new(position2d: new Vector2(-PlayerTank.ClientTank.TurretRotation), position3d: PlayerTank.ClientTank.Position3D);
                }
                // TODO: this shit is ass.
                var povCameraRotationCurrent = IntermissionHandler.TankFunctionWait > 0 && IntermissionHandler.ThirdPersonTransitionAnimation != null ?
                    IntermissionHandler.ThirdPersonTransitionAnimation.CurrentPosition2D.X : POVCameraRotation;
                var povCameraPosCurrent = IntermissionHandler.TankFunctionWait > 0 && IntermissionHandler.ThirdPersonTransitionAnimation != null ?
                    IntermissionHandler.ThirdPersonTransitionAnimation.CurrentPosition3D : POVCameraPosition;

                GameView = Matrix.CreateLookAt(povCameraPosCurrent,
                        POVCameraPosition + new Vector2(0, 20).Rotate(povCameraRotationCurrent).ExpandZ(),
                        Vector3.Up) * Matrix.CreateScale(AddativeZoom) *
                    Matrix.CreateTranslation(0, -20, 0);

                /*GameView = Matrix.CreateLookAt(POVCameraPosition,
                        POVCameraPosition + new Vector3(0, 0, 20).FlattenZ().RotatedByRadians(POVCameraRotation).ExpandZ(),
                        Vector3.Up) * Matrix.CreateScale(AddativeZoom) *
                    Matrix.CreateRotationX(POVRotationVector.Y - MathHelper.PiOver4) *
                    Matrix.CreateRotationY(POVRotationVector.X) *
                    Matrix.CreateTranslation(0, -20, 0);*/

                GameProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90), TankGame.Instance.GraphicsDevice.Viewport.AspectRatio, 0.1f, 10000);
            }
        }
        else if (!GameUI.Paused && !MainMenuUI.Active && DebugManager.DebuggingEnabled) {
            if (DebugManager.DebugLevel == DebugManager.Id.FreeCamTest || DebugManager.persistFreecam) {

                if (InputUtils.AreKeysJustPressed(Keys.Z, Keys.X)) {
                    DebugManager.persistFreecam = !DebugManager.persistFreecam;
                }
                // free camera movement test

                var moveSpeed = 10f * TankGame.DeltaTime;

                var rotationSpeed = 0.01f;

                RebirthFreecam.NearViewDistance = 0.1f;
                RebirthFreecam.FarViewDistance = 1000000f;
                RebirthFreecam.MinPitch = -180;
                RebirthFreecam.MaxPitch = 180;
                RebirthFreecam.HasLookAt = false;

                var isPlayerActive = PlayerTank.ClientTank is not null;

                var keysprint = LevelEditorUI.Active || !isPlayerActive ? Keys.LeftShift : Keys.RightShift;
                var keyslow = LevelEditorUI.Active || !isPlayerActive ? Keys.LeftControl : Keys.RightControl;

                if (InputUtils.CurrentKeySnapshot.IsKeyDown(keysprint))
                    moveSpeed *= 2;
                if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyslow))
                    moveSpeed /= 4;

                var keyf = LevelEditorUI.Active || !isPlayerActive ? Keys.W : Keys.Up;
                var keyb = LevelEditorUI.Active || !isPlayerActive ? Keys.S : Keys.Down;
                var keyl = LevelEditorUI.Active || !isPlayerActive ? Keys.A : Keys.Left;
                var keyr = LevelEditorUI.Active || !isPlayerActive ? Keys.D : Keys.Right;

                if (InputUtils.MouseRight)
                    RebirthFreecam.Rotation -= new Vector3(0, MouseUtils.MouseVelocity.Y * rotationSpeed, MouseUtils.MouseVelocity.X * rotationSpeed);
                if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Add))
                    RebirthFreecam.FieldOfView += 0.5f * TankGame.DeltaTime;
                if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.Subtract))
                    RebirthFreecam.FieldOfView -= 0.5f * TankGame.DeltaTime;
                if (InputUtils.MouseMiddle)
                    RebirthFreecam.FieldOfView = 90;
                if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyf))
                    RebirthFreecam.Move(RebirthFreecam.World.Forward * moveSpeed);
                if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyb))
                    RebirthFreecam.Move(RebirthFreecam.World.Backward * moveSpeed);
                if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyl))
                    RebirthFreecam.Move(RebirthFreecam.World.Left * moveSpeed);
                if (InputUtils.CurrentKeySnapshot.IsKeyDown(keyr))
                    RebirthFreecam.Move(RebirthFreecam.World.Right * moveSpeed);

                GameView = RebirthFreecam.View;
                GameProjection = RebirthFreecam.Projection;
            }
        }
    }
    public static int SpectateValidTank(int id, bool increase) {
        var arr = GameHandler.AllPlayerTanks;

        var newId = id + (increase ? 1 : -1);

        if (newId < 0)
            return arr.Length - 1;
        else if (newId >= arr.Length)
            return 0;

        if (arr[newId] is null || arr[newId].Dead)
            return SpectateValidTank(newId, increase);
        else return newId;
    }
}
