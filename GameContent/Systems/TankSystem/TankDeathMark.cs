using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent;

public class TankDeathMark {
    private const int MAX_DEATH_MARKS = 1000;

    public static TankDeathMark[] deathMarks = new TankDeathMark[MAX_DEATH_MARKS];

    public Vector3 Position;
    public float rotation;

    internal static int total_death_marks;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;

    public Particle check;

    public Texture2D texture;

    public TankTemplate StoredTank;

    public enum CheckColor {
        Blue,
        Red,
        Green,
        Yellow,
        White
    }

    /// <summary>Resurrects <see cref="StoredTank"/>.</summary>
    public void ResurrectTank() {
        StoredTank.GetTank();
    }

    public TankDeathMark(CheckColor color) {
        if (total_death_marks + 1 > MAX_DEATH_MARKS)
            return;
        total_death_marks++;

        texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/check/check_{color.ToString().ToLower()}");

        check = GameHandler.ParticleSystem.MakeParticle(Position + new Vector3(0, 0.1f, 0), texture);
        check.HasAddativeBlending = false;
        check.Roll = -MathHelper.PiOver2;
        check.Layer = 0;

        deathMarks[total_death_marks] = this;
    }

    public void Render() {
        check.Position = Position;
        check.Scale = new(0.6f);
    }
}