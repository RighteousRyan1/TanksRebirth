using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TanksRebirth.Graphics.Cameras;

public struct Freecam(GraphicsDevice device) : IPerspectiveCamera {
    public readonly GraphicsDevice Device = device;

    float _minPitch;
    float _maxPitch;
    float _fov = 90;
    float _near = 1f;
    float _far = 75000f;

    Vector3 _position;
    Vector3 _rotation;
    Vector3 _focus;
    public float MinPitch {
        readonly get => _minPitch;
        set {
            //if (_rotation.Y < MathHelper.ToRadians(value)) {
            //    _rotation.Y = value;
            //}
            _minPitch = value;
        }
    }
    public float MaxPitch {
        readonly get => _maxPitch;
        set {
            //if (_rotation.Y > MathHelper.ToRadians(value)) {
            //    _rotation.Y = value;
            //}
            _maxPitch = value;
        }
    }
    /// <summary>Degrees -> Radians</summary>
    public float FieldOfView {
        readonly get => _fov;
        set {
            if (value < 5) value = 5;
            if (value > 175) value = 175;
            _fov = value;
            ChangeProjection();
        }
    }
    public Vector3 Position {
        readonly get => _position;
        set {
            _position = value;
            ChangeViewWorld();
        }
    }
    public Vector3 Rotation {
        readonly get => _rotation;
        set {
            _rotation = value;
            ChangeViewWorld();
        }
    }
    public Vector3 Focus {
        readonly get => _focus;
        set {
            _focus = value;
            if (!UseFocus) return;

            ChangeViewWorld();
        }
    }
    public bool UseFocus { get; set; }

    public float Near {
        readonly get => _near;
        set {
            _near = value;
            ChangeProjection();
        }
    }
    public float Far {
        readonly get => _far;
        set {
            _far = value;
            ChangeProjection();
        }
    }

    public Matrix World { get; set; }
    public Matrix View { get; set; }
    public Matrix Projection { get; set; }
    public void Move(Vector3 moveAmount) => Position += moveAmount;
    void ChangeViewWorld() {
        var lookAt = UseFocus ? Matrix.CreateLookAt(Position, Focus, Vector3.Up) : Matrix.Identity;
        World = Matrix.CreateFromYawPitchRoll(_rotation.Z, _rotation.Y, _rotation.X) * Matrix.CreateWorld(_position, Vector3.Forward, Vector3.Up);
        View = Matrix.Invert(World) * lookAt;
    }
    void ChangeProjection() {
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_fov), Device.Viewport.AspectRatio, _near, _far);
    }
}
