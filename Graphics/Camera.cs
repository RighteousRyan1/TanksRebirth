using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.Graphics
{
    public enum CameraType
    {
        Orthographic,
        FieldOfView
    }
    public class Camera
    {
        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;

        private Vector3 _position;
        private Vector3 _lookAt;
        private Vector3 _upVector = Vector3.Up;

        private float _zoom = 1f;

        private Vector3 _rotation;

        private Vector3 _translation;

        public CameraType CameraType { get; private set; }

        public float FieldOfView { get; private set; } = MathHelper.ToRadians(90);

        public float ViewingRangeMax { get; private set; }
        public float ViewingRangeMin { get; private set; }

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

        public Camera RotateX(float rotation)
        {
            _rotation.X = rotation;
            Recalculate();
            return this;
        }
        public Camera RotateY(float rotation)
        {
            _rotation.Y = rotation;
            Recalculate();
            return this;
        }
        public Camera RotateZ(float rotation)
        {
            _rotation.Z = rotation;
            Recalculate();
            return this;
        }

        public Camera SetCameraType(CameraType type)
        {
            CameraType = type;

            Recalculate();

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

        public Camera Zoom(float zoom)
        {
            _zoom = zoom;
            Recalculate();
            return this;
        }

        public Camera Translate(Vector3 translation)
        {
            _translation = translation;
            Recalculate();
            return this;
        }

        public Camera SetViewingDistances(float minView, float maxView)
        {
            ViewingRangeMin = minView;
            ViewingRangeMax = maxView;
            Recalculate();
            return this;
        }

        private void Recalculate()
        {
            _viewMatrix = Matrix.CreateScale(_zoom) * Matrix.CreateLookAt(_position, _lookAt, _upVector) * Matrix.CreateTranslation(_translation) * Matrix.CreateRotationX(_rotation.X) * Matrix.CreateRotationY(_rotation.Y) * Matrix.CreateRotationZ(_rotation.Z);
            switch (CameraType)
            {
                case CameraType.Orthographic:
                    _projectionMatrix = Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, ViewingRangeMin, ViewingRangeMax);
                    break;
                case CameraType.FieldOfView:
                    _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FieldOfView), GraphicsDevice.Viewport.AspectRatio, ViewingRangeMin, ViewingRangeMax);
                    break;
            }
        }
    }
}