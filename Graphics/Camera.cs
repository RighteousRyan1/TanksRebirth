using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using WiiPlayTanksRemake.GameContent;

namespace WiiPlayTanksRemake.Graphics
{
    public class Camera
    {
        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;

        private Vector3 _position;

        private bool IsOmnicient { get; set; }

        public float _fov = MathHelper.ToRadians(90);

        public static GraphicsDevice GraphicsDevice { get; set; }

        public Camera()
        {
            if (GraphicsDevice is null)
                throw new Exception("Please assign a proper graphics device for the camera to use.");

            _viewMatrix = Matrix.CreatePerspectiveFieldOfView(_fov, GraphicsDevice.Viewport.AspectRatio, 1f, 3000f);
            _projectionMatrix = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2000f, 5000f);
        }

        public Matrix GetView() => _viewMatrix;
        public Matrix GetProjection() => _projectionMatrix;

        public Camera SetToYawPitchRoll(float yaw, float pitch, float roll)
        {
            _viewMatrix *= Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
            return this;
        }

        public Camera SetOmni(bool omni)
        {
            IsOmnicient = omni;

            return this;
        }

        public Vector3 GetPosition() => _position;

        public Camera SetPosition(Vector3 pos)
        {
            _position = pos;
            return this;
        }

        public Camera SetFov(float degrees)
        {
            _fov = MathHelper.ToRadians(degrees);
            _viewMatrix = Matrix.CreatePerspectiveFieldOfView(_fov, GraphicsDevice.Viewport.AspectRatio, 0.01f, 3000f);
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
    }
}