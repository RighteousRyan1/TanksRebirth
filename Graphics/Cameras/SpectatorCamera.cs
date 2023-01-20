using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics.Cameras;

public class SpectatorCamera : PerspectiveCamera
{
    public SpectatorCamera(float fieldOfView, float aspectRatio, float nearZ, float farZ) : base(fieldOfView, aspectRatio, nearZ, farZ) { }

    private Vector3 _cameraTarget;
    private Vector3 _cameraUp;

    // Previous mouse position
    private Vector2 _prevMousePos;

    // Camera rotation speed
    public float RotationSpeed = 0.01f;

    public void Update()
    {
        SetLookAt(Position, _cameraTarget, _cameraUp);
        Projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearZ, FarZ);

        // Update camera orientation
        if (InputUtils.MouseLeft)
        {
            // Calculate mouse delta
            Vector2 mouseDelta = MouseUtils.MousePosition - _prevMousePos;

            // Calculate camera orientation quaternion
            Quaternion cameraOrientation = Quaternion.CreateFromYawPitchRoll(
                -mouseDelta.X * RotationSpeed,
                -mouseDelta.Y * RotationSpeed,
                0);

            // Rotate camera up vector
            _cameraUp = Vector3.Transform(_cameraUp, cameraOrientation);

            // Update previous mouse position
            _prevMousePos = MouseUtils.MousePosition;
        }
    }
}
