using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.GameMechanics;

public readonly record struct ShotSegment(Vector2 Start, Vector2 End, Vector2 Direction, int Frames) {
    public float Length => Vector2.Distance(Start, End);
}

/// <summary>Splits a shot into straight segments.</summary>
public static class ShotPath {
    // public const int MAX_SEGMENTS = 3;
    public const int TOO_SHORT = 6;

    /// <summary>Traces a shot of <paramref name="owner"/> from <paramref name="position"/> into <paramref name="segments"/>.
    /// <br></br>Returns false if the first segment is too short or never ends, which means the tank can't shoot.</summary>
    public static bool Trace(Tank owner, Vector2 position, Vector2 direction, List<ShotSegment> segments, Func<Block, bool>? pattern = null) {
        segments.Clear();
        pattern ??= c => c.Properties.IsSolid || c.Type == BlockID.Teleporter;

        var sim = ShellSim.From(owner, position, direction);

        var start = sim.Position;
        var dir = sim.Velocity.NormalizeOrDefault();
        var startFrame = 0;

        for (int frame = 0; ; frame++) {
            var result = sim.Step(pattern, out _);
            if (result == ShellSimResult.None) continue;

            var frames = frame + 1 - startFrame;
            segments.Add(new(start, sim.Position, dir, frames));

            if (segments.Count == 1 && frames <= TOO_SHORT) return false;
            // the shell is destroyed
            if (result == ShellSimResult.Destroyed) break;

            start = sim.Position;
            dir = sim.Velocity.NormalizeOrDefault();
            startFrame = frame + 1;
        }
        return segments.Count > 0;
    }
}
