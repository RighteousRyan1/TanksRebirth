using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using tainicom.Aether.Physics2D.Collision;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.Tanks.AI;
using TanksRebirth.Graphics;
using TanksRebirth.Net;

namespace TanksRebirth.Internals.Common.Utilities;

public enum Anchor {
    TopLeft, TopRight,
    BottomLeft, BottomRight,
    TopCenter, BottomCenter,
    Center,
    LeftCenter, RightCenter,
}
public static class GameUtils {
    public static float NaturalPitchVariance = 0.1f;

    public static float RandomSemitoneShift(int semitones) => Client.ClientRandom.Next(-semitones, semitones + 1) / 12f;
    public static float RandomBendShift(int bend) => Client.ClientRandom.Next(-bend, bend + 1) / 127f * 2f / 12f;
    public static float NaturalPitchShift => Client.ClientRandom.NextFloat(-NaturalPitchVariance, NaturalPitchVariance);
    public static IAITankDanger? Closest(this IList<IAITankDanger> positions, Vector2 source) {
        if (positions == null || !positions.Any())
            return null;

        float closestDistanceSquared = float.MaxValue;
        IAITankDanger closest = positions[0];

        foreach (var danger in positions) {
            float distanceSquared = Vector2.DistanceSquared(source, danger.Position);
            if (distanceSquared < closestDistanceSquared) {
                closestDistanceSquared = distanceSquared;
                closest = danger;
            }
        }

        return closest;
    }
    public static float ToGameTicks(this TimeSpan t) => (float)(t.TotalMilliseconds / 1 / 60f);
    public static Vector2 GetAnchor(this Anchor a, Vector2 vector) => a switch {
            Anchor.TopLeft => Vector2.Zero,
            Anchor.TopRight => new(vector.X, 0),
            Anchor.BottomLeft => new(0, vector.Y),
            Anchor.BottomRight => new(vector.X, vector.Y),
            Anchor.LeftCenter => new(0, vector.Y / 2),
            Anchor.RightCenter => new(vector.X, vector.Y / 2),
            Anchor.Center => new(vector.X / 2, vector.Y / 2),
            Anchor.TopCenter => new(vector.X / 2, 0),
            Anchor.BottomCenter => new(vector.X / 2, vector.Y),
            _ => default,
        };

    // i should probably just scale up literally everything by 1f / WII_TANKS_UNIT_CONVERSION instead of doing this stupid ahh math
    // divide since the distance is bigger from regular distance calculations
    public const float WII_TANKS_UNIT_CONVERSION = 0.71428571428f;
    public static float TanksDistance(Vector2 position, Vector2 endPoint) => Vector2.Distance(position, endPoint) / WII_TANKS_UNIT_CONVERSION;
    public static float TanksUnits(float value) => value * WII_TANKS_UNIT_CONVERSION;

    /// <summary>The result of a <see cref="SweepForWalls"/>.</summary>
    public struct WallSweep {
        // if there was a hit
        public bool Hit;
        // frame of first contact
        public int HitFrame;
        // how far before first contact
        public float Distance;
        // summed normal of all contacts
        public Vector2 Normal;
    }
    // the original game's circle radius for wall checks. for now, constant
    public const float WALL_SWEEP_DEF_RADIUS = 16f;

    /// <summary>Whether a wall-sweep circle at <paramref name="position"/> touches a solid block or the arena bounds.</summary>
    public static bool TouchesWall(Vector2 position, out Vector2 normal, float scanRadius = WALL_SWEEP_DEF_RADIUS) {
        normal = Vector2.Zero;
        var hit = false;
        var radius = TanksUnits(scanRadius);
        var half = new Vector2(Block.SIDE_LENGTH / 2f);

        // two loops here lowkey disgusts me
        for (int i = 0; i < Block.AllBlocks.Length; i++) {
            var block = Block.AllBlocks[i];
            if (block is null || !block.Properties.IsCollidable) continue;

            if (!GeometryUtils.CircleIntersectsRect(position, radius, block.Position - half, block.Position + half, out var n)) continue;
            normal += n;
            hit = true;
        }
        foreach (var bound in GameScene.BoundsRenderer.Boundaries) {
            if (bound is null) continue;
            foreach (var fixture in bound.FixtureList) {
                fixture.GetAABB(out AABB aabb, 0);

                if (!GeometryUtils.CircleIntersectsRect(position, radius, aabb.LowerBound * Tank.UNITS_PER_METER, aabb.UpperBound * Tank.UNITS_PER_METER, out var n)) continue;
                normal += n;
                hit = true;
            }
        }
        return hit;
    }
    /// <summary>Moves a circle along <paramref name="direction"/> and checks for walls on frames 1, 3, 5... below <paramref name="numFrames"/>.</summary>
    /// <param name="speedPerFrame">How far the circle moves each frame.</param>
    /// <param name="stop">Whether to stop at the first contact instead of summing normals all the way.</param>
    public static WallSweep SweepForWalls(Vector2 start, Vector2 direction, float speedPerFrame, int numFrames, float scanRadius = WALL_SWEEP_DEF_RADIUS, bool stop = true) {
        var sweep = new WallSweep();
        for (int frame = 1; frame < numFrames; frame += 2) {
            if (!TouchesWall(start + direction * speedPerFrame * frame, out var normal, scanRadius)) continue;

            if (!sweep.Hit) {
                sweep.Hit = true;
                sweep.HitFrame = frame;
                sweep.Distance = speedPerFrame * frame;
            }
            sweep.Normal += normal;
            if (stop) break;
        }
        return sweep;
    }
}
