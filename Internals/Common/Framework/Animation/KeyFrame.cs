using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Animation;

/// <summary>Used in the construction of an <see cref="Animator"/>.
/// BezierPoints automatically prepends <see cref="Position"/> and appends the next <see cref="KeyFrame"/>'s <see cref="Position"/> when implemented into an <see cref="Animator"/>.</summary>
public readonly struct KeyFrame(Vector3 position = default, Vector3 scale = default, float[]? floats = null, List<Vector3>? bezierPoints = null, TimeSpan duration = default, EasingFunction easing = EasingFunction.Linear) {
    /// <summary>The easing function to use when the animation goes into this frame.</summary>
    public EasingFunction Easing { get; } = easing;
    public Vector3 Position { get; } = position;
    public Vector3 Scale { get; } = scale;
    public TimeSpan Duration { get; } = duration;
    public float[]? Floats { get; } = floats;
    public List<Vector3>? BezierPoints { get; } = bezierPoints;
    public override readonly string ToString() => $"ease: {Easing} | pos: {Position} | scl: {Scale:0.00} | dur: {Duration:c} | flts: {(Floats is null ? 0 : Floats.Length)}";
}