using Microsoft.Xna.Framework;
using System;
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
    public KeyFrame(Vector2 position, Vector2 scale, float rotation, TimeSpan duration, EasingFunction easing = EasingFunction.Linear) {
        Easing = easing;
        Position = position;
        Duration = duration;
        Scale = scale;
        Rotation = rotation;
    }
}
