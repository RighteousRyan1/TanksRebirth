using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Tanks;

public class TankDeathMark {
    public const int MAX_DEATH_MARKS = 1000;
    internal static int totalDeathMarks;
    readonly Particle? _particle;
    readonly Texture2D? _texture;
    readonly int id;

    public static TankDeathMark[] DeathMarks = new TankDeathMark[MAX_DEATH_MARKS];
    public float Rotation;
    public Vector3 Position;
    public Matrix World, View, Proj;

    /// <summary>The tank that died here.</summary>
    public Tank StoredTank;

    public enum CheckColor {
        Blue,
        Red,
        Green,
        Yellow,
        White
    }

    /// <summary>Resurrects <see cref="StoredTank"/>.</summary>
    public void ResurrectTank() {
        // there's probably more to this i'm forgetting (im the dev btw)
        StoredTank.IsDestroyed = false;
    }

    TankDeathMark(Tank tank, CheckColor color) {
        totalDeathMarks++;

        StoredTank = tank;
        _texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/deathmark/deathmark_{color.ToString().ToLower()}");

        _particle = GameHandler.Particles.MakeParticle(Position + new Vector3(0, 0.1f, 0), _texture);
        _particle.Additive = false;
        _particle.Pitch = -MathHelper.PiOver2;
        _particle.Layer = 0;

        id = totalDeathMarks;
        DeathMarks[id] = this;
    }

    public void Remove() {
        DeathMarks[id]?._particle?.Destroy();
        DeathMarks[id] = null!;
    }

    public static TankDeathMark Create(Tank tank, CheckColor color) {
        if (totalDeathMarks + 1 > MAX_DEATH_MARKS) return null!;

        return new(tank, color);
    }

    public void Render() {
        if (_particle is null) return;

        _particle.Position = Position;
        _particle.Scale = new(0.6f);
    }
}