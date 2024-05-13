using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Animation;

/// <summary>Used in the construction of an <see cref="Animator"/>.
public readonly struct KeyFrame
{
    public EasingFunction Easing { get; }
    public Vector2 Position { get; }
    public TimeSpan Duration { get; }
    public Vector2 Scale { get; }
    public float Rotation { get; }
    public List<Vector2> BezierPoints { get; }
    public KeyFrame(Vector2 position, Vector2 scale, float rotation, TimeSpan duration, EasingFunction easing = EasingFunction.Linear) {
        Easing = easing;
        Position = position;
        Duration = duration;
        Scale = scale;
        Rotation = rotation;
        BezierPoints = new();
    }
    /// <summary>BezierPoints automatically prepends <see cref="Position"/> and appends the next <see cref="KeyFrame"/>'s <see cref="Position"/> when implemented into an <see cref="Animator"/>.</summary>
    public KeyFrame(Vector2 position, List<Vector2> bezierPoints, Vector2 scale, float rotation, TimeSpan duration, EasingFunction easing = EasingFunction.Linear) {
        Easing = easing;
        Duration = duration;
        Scale = scale;
        Rotation = rotation;
        Position = position;
        BezierPoints = bezierPoints;
    }
}
