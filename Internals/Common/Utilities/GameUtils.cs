using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Systems.AI;

namespace TanksRebirth.Internals.Common.Utilities;

public enum Anchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    TopCenter,
    BottomCenter,
    Center,
    LeftCenter,
    RightCenter,
}
public static class GameUtils
{
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
    public static Vector2 GetAnchor(this Anchor a, Vector2 vector) {
        return a switch {
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
    }

    // divide since the distance is bigger from regular distance calculations
    const float WII_TANKS_UNIT_CONVERSION = 0.71428571428f;
    public static float Distance_WiiTanksUnits(Vector2 position, Vector2 endPoint) => Vector2.Distance(position, endPoint) / WII_TANKS_UNIT_CONVERSION;
    public static float Value_WiiTanksUnits(float value) => value * WII_TANKS_UNIT_CONVERSION;
}
