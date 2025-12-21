using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.Graphics.Cameras;

public class Freecam(GraphicsDevice device) {
    public readonly GraphicsDevice Device = device;
    float _minPitch;
    public float MinPitch {
        get => _minPitch;
        set {
            //if (_rotation.Y < MathHelper.ToRadians(value)) {
            //    _rotation.Y = value;
            //}
            _minPitch = value;
        }
    }
    float _maxPitch;
    public float MaxPitch {
        get => _maxPitch;
        set {
            //if (_rotation.Y > MathHelper.ToRadians(value)) {
            //    _rotation.Y = value;
            //}
        }
    }
    // this is a precarious property
    public Vector3 Velocity { get; private set; }
    Vector3 _position;
    public Vector3 Position {
        get => _position;
        set {
            Velocity = value - _position;
            _position = value;
            ChangeViewWorld();
        }
    }
    Vector3 _rotation;
    public Vector3 Rotation {
        get => _rotation;
        set {
            _rotation = value;
            ChangeViewWorld();
        }
    }
    float _fov = 90;
    /// <summary>Degrees -> Radians</summary>
    public float FieldOfView {
        get => _fov;
        set {
            if (value < 5) value = 5;
            if (value > 175) value = 175;
            _fov = value;
            ChangeProjection();
        }
    }
    Vector3 _lookAt;
    public Vector3 LookAt {
        get => _lookAt;
        set {
            _lookAt = value;
            if (!HasLookAt) return;
            ChangeViewWorld();
        }
    }

    public bool HasLookAt { get; set; }

    float _near = 1f;
    public float NearViewDistance {
        get => _near;
        set {
            _near = value;
            ChangeProjection();
        }
    }
    float _far = 75000f;
    public float FarViewDistance {
        get => _far;
        set {
            _far = value;
            ChangeProjection();
        }
    }

    public Matrix World { get; private set; }
    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }

    public void Move(Vector3 moveAmount) {
        Position += moveAmount;
    }

    void ChangeViewWorld() {
        var lookAt = HasLookAt ? Matrix.CreateLookAt(Position, LookAt, Vector3.Up) : Matrix.Identity;
        World = Matrix.CreateFromYawPitchRoll(_rotation.Z, _rotation.Y, _rotation.X) * Matrix.CreateWorld(_position, Vector3.Forward, Vector3.Up);
        View = Matrix.Invert(World) * lookAt;
    }
    void ChangeProjection() {
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_fov), Device.Viewport.AspectRatio, _near, _far);
    }
}
