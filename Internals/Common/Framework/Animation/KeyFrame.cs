using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Animation;

/// <summary>Used in the construction of an <see cref="Animator"/>.</summary>
public readonly struct KeyFrame(Vector2 position2d = default, Vector3 position3d = default, Vector2 scale = default, float[] floats = null, TimeSpan duration = default, EasingFunction easing = EasingFunction.Linear) {
    public EasingFunction Easing { get; } = easing;
    public Vector2 Position2D { get; } = position2d;
    public Vector3 Position3D { get; } = position3d;
    public TimeSpan Duration { get; } = duration;
    public Vector2 Scale { get; } = scale;
    public float[] Floats { get; } = floats;
    public List<Vector2> BezierPoints { get; } = [];
    // /// <summary>BezierPoints automatically prepends <see cref="Position2D"/> and appends the next <see cref="KeyFrame"/>'s <see cref="Position2D"/> when implemented into an <see cref="Animator"/>.</summary>
    /*public KeyFrame(Vector2 position, List<Vector2> bezierPoints, Vector2 scale, float[] floats, TimeSpan duration = default, EasingFunction easing = EasingFunction.Linear) {
        Easing = easing;
        Duration = duration;
        Scale = scale;
        Floats = floats;
        Position2D = position;
        BezierPoints = bezierPoints;
    }*/
    /*public KeyFrame(Vector2 scale, float[] floats, TimeSpan duration = default, EasingFunction easing = EasingFunction.Linear) {
        Position2D = Vector2.Zero;
        BezierPoints = [];
        Scale = scale;
        Floats = floats;
        Duration = duration;
        Easing = easing;
    }
    public KeyFrame(float[] floats, TimeSpan duration = default, EasingFunction easing = EasingFunction.Linear) {
        Position2D = Vector2.Zero;
        BezierPoints = [];
        Scale = Vector2.One;
        Floats = floats;
        Duration = duration;
        Easing = easing;
    }*/
}