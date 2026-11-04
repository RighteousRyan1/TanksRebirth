using Microsoft.Xna.Framework;
using System;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.GameMechanics;

// i debated using an ACTUAL Shell instance as the path, but this is alloc free and is just overall
// better so i don't have to use hardcoded branching in the Shell constructor
public enum ShellSimResult {
    None,
    Ricochet,
    Destroyed
}

/// <summary>
/// A <see cref="Shell"/> stripped down to its movement and ricochet rules.
/// </summary>
public struct ShellSim(Vector2 position, Vector2 velocity, int ricochets) {
    public Vector2 Position = position;
    public Vector2 Velocity = velocity;
    public int RicochetsRemaining = ricochets;
    /// <summary>Frames this sim has been stepped. Same meaning as <see cref="Shell.LifeTime"/>.</summary>
    public int LifeTime;
    /// <summary>Distance travelled since the last ricochet (or the start).</summary>
    public float Distance;

    /// <summary>Creates a sim of a shell fired by <paramref name="owner"/> along <paramref name="direction"/>.</summary>
    public static ShellSim From(Tank owner, Vector2 position, Vector2 direction)
        => new(position, direction.NormalizeOrDefault() * owner.Properties.ShellSpeed, owner.Properties.RicochetCount);

    // reuse collision code or actually use collision code? :think:
    // this code is only "close enough" and doesn't actually fully work. i need to see what's going on
    /// <summary>Moves one frame, checking collisions every <see cref="Shell.SUBSTEP_LENGTH"/>.</summary>
    /// <param name="collides">Picks which blocks the shell reacts to.</param>
    /// <param name="block">The block the sim collided with, if applicable.</param>
    public ShellSimResult Step(Func<Block, bool> collides, out Block? block) {
        var result = ShellSimResult.None;
        block = null;

        int substeps = Shell.NumSubsteps((Velocity * Shell.MOVE_SCALE).Length());
        for (int i = 0; i < substeps; i++) {
            // velocity can change between substeps (ricochets), so move by the current one
            var sub = Velocity * Shell.MOVE_SCALE / substeps;
            Position += sub;
            Distance += sub.Length();

            var subResult = Collide(collides, out var subBlock);
            if (subBlock is not null) block = subBlock;

            if (subResult == ShellSimResult.Destroyed) {
                result = subResult;
                break;
            }
            if (subResult == ShellSimResult.Ricochet) result = subResult;
        }

        LifeTime++;
        return result;
    }

    /// <summary>The bounds and block checks of one substep.</summary>
    ShellSimResult Collide(Func<Block, bool> collides, out Block? block) {
        var result = ShellSimResult.None;
        block = null;

        if (Position.X < GameScene.MIN_X) {
            Position.X = GameScene.MIN_X;
            result = Bounce(Vector2.UnitX, 1);
        }
        else if (Position.X > GameScene.MAX_X) {
            Position.X = GameScene.MAX_X;
            result = Bounce(Vector2.UnitX, 1);
        }
        if (result == ShellSimResult.Destroyed) return result;

        if (Position.Y < GameScene.MIN_Z) {
            Position.Y = GameScene.MIN_Z;
            result = Bounce(Vector2.UnitY, 1);
        }
        else if (Position.Y > GameScene.MAX_Z) {
            Position.Y = GameScene.MAX_Z;
            result = Bounce(Vector2.UnitY, 1);
        }
        if (result == ShellSimResult.Destroyed) return result;

        var hitbox = new Rectangle((int)Position.X - Shell.COLL_RECT_DIM / 2, (int)Position.Y - Shell.COLL_RECT_DIM / 2, Shell.COLL_RECT_DIM, Shell.COLL_RECT_DIM);
        var dummy = Vector2.Zero;
        Collision.HandleCollisionSimple_ForBlocks(hitbox, Velocity, ref dummy, out var dir, out block, out bool corner, false, collides);

        if (corner) return ShellSimResult.Destroyed;
        if (block is null || block.Type == BlockID.Teleporter || !block.Properties.AllowShotPathBounce) return result;

        var bounce = dir switch {
            CollisionDirection.Up or CollisionDirection.Down => Bounce(Vector2.UnitY, block.Properties.PathBounceCount),
            CollisionDirection.Left or CollisionDirection.Right => Bounce(Vector2.UnitX, block.Properties.PathBounceCount),
            _ => ShellSimResult.None
        };
        return bounce == ShellSimResult.None ? result : bounce;
    }

    ShellSimResult Bounce(Vector2 normal, int cost) {
        // same rules as Shell.Ricochet
        if (RicochetsRemaining < cost || LifeTime < Shell.TOO_SHORT_LIFETIME) return ShellSimResult.Destroyed;
        Velocity = Vector2.Reflect(Velocity, normal);
        RicochetsRemaining -= cost;
        return ShellSimResult.Ricochet;
    }
}
