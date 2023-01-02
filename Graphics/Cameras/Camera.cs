using Microsoft.Xna.Framework;

namespace TanksRebirth.Graphics.Cameras
{
    public abstract class Camera
    {
        private Matrix _projection;
        private Matrix _world, _view, _viewProjection;
        private Vector3 _position;
        private float _nearZ;
        private float _farZ;

        public Matrix Projection
        {
            get { return _projection; }
            set
            {
                _projection = value;
                _viewProjection = _view * _projection;
            }
        }

        public Matrix ViewProjection
        {
            get { return _viewProjection; }
        }

        public float NearZ
        {
            get { return _nearZ; }
            set
            {
                _nearZ = value;
                CreateProjection();
            }
        }

        public float FarZ
        {
            get { return _farZ; }
            set
            {
                _farZ = value;
                CreateProjection();
            }
        }

        public Matrix View
        {
            get { return _view; }
        }

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                _world.Translation = value;
                OnWorldMatrixChanged();
            }
        }

        public Vector3 Up
        {
            get { return _world.Up; }
        }

        public Vector3 Down
        {
            get { return _world.Down; }
        }

        public Vector3 Left
        {
            get { return _world.Left; }
        }

        public Vector3 Right
        {
            get { return _world.Right; }
        }

        public Vector3 Forward
        {
            get { return _world.Forward; }
        }

        public Vector3 Backward
        {
            get { return _world.Backward; }
        }

        protected Camera(float nearZ, float farZ)
        {
            _nearZ = nearZ;
            _farZ = farZ;

            _world = Matrix.Identity;
            _view = Matrix.Identity;
        }

        public void SetLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 up)
        {
            _view = Matrix.CreateLookAt(cameraPosition, cameraTarget, up);
            _world = Matrix.Invert(_view);
            _position = cameraPosition;
            OnWorldMatrixChanged();
        }

        public void SetOrientation(Quaternion orientation)
        {
            _world = Matrix.CreateFromQuaternion(orientation);
            _world.Translation = _position;
            OnWorldMatrixChanged();
        }

        private void OnWorldMatrixChanged()
        {
            _view = Matrix.Invert(_world);
            _viewProjection = _view * _projection;
        }

        protected abstract void CreateProjection();
    }
}