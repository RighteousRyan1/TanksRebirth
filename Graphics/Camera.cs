using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.Graphics
{
    public class Camera
    {
        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;

        private Vector3 _position;
        private Vector3 _lookAt;
        private Vector3 _upVector = Vector3.Up;

        private float _rotation;

        private bool IsOrthographic { get; set; }

        public float FieldOfView { get; private set; } = MathHelper.ToRadians(90);

        public static GraphicsDevice GraphicsDevice { get; set; }

        public Camera(GraphicsDevice device)
        {
            GraphicsDevice = device;
            if (GraphicsDevice is null)
                throw new Exception("Please assign a proper graphics device for the camera to use.");
        }

        public Matrix GetView() => _viewMatrix;
        public Matrix GetProjection() => _projectionMatrix;

        public Camera SetToYawPitchRoll(float yaw, float pitch, float roll)
        {
            Recalculate();
            _viewMatrix *= Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
            return this;
        }

        public Camera SetOmni(bool omni)
        {
            IsOrthographic = omni;

            return this;
        }

        public Vector3 GetPosition() => _position;

        public Camera SetPosition(Vector3 pos)
        {
            _position = pos;
            Recalculate();
            return this;
        }
        public Camera SetLookAt(Vector3 pos)
        {
            _lookAt = pos;
            Recalculate();
            return this;
        }

        public Camera SetFov(float degrees)
        {
            FieldOfView = MathHelper.ToRadians(degrees);
            Recalculate();
            return this;
        }

        public Camera Crunch()
        {

            return this;
        }

        public Camera Stretch()
        {

            return this;
        }

        private void Recalculate()
        {
            _viewMatrix = Matrix.CreateLookAt(_position, _lookAt, _upVector);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FieldOfView), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000f);
        }
    }
}