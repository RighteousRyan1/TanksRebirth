using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using WiiPlayTanksRemake.Enums;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    public abstract class Tank
    {
        /// <summary>This <see cref="Tank"/>'s model.</summary>
        public Model Model { get; set; }
        /// <summary>This <see cref="Tank"/>'s world position. Used to change the actual location of the model relative to the <see cref="View"/> and <see cref="Projection"/>.</summary>
        public Matrix World { get; set; }
        /// <summary>How the <see cref="Model"/> is viewed through the <see cref="Projection"/>.</summary>
        public Matrix View { get; set; }
        /// <summary>The projection from the screen to the <see cref="Model"/>.</summary>
        public Matrix Projection { get; set; }
        /// <summary>Whether the tank has been destroyed or not.</summary>
        public bool Dead { get; set; }
        /// <summary>Whether or not the tank should become invisible at mission start.</summary>
        public bool Invisible { get; set; }
        /// <summary>How fast the tank should accelerate towards it's <see cref="MaxSpeed"/>.</summary>
        public float Acceleration { get; set; } = 0.3f;
        /// <summary>The current speed of this tank.</summary>
        public float Speed { get; set; } = 1f;
        /// <summary>The maximum speed this tank can achieve.</summary>
        public float MaxSpeed { get; set; } = 1f;
        /// <summary>How fast the bullets this <see cref="Tank"/> shoot are.</summary>
        public float ShellSpeed { get; set; } = 1f;
        /// <summary>The rotation of this <see cref="Tank"/>'s barrel. Generally should not be modified in a player context.</summary>
        public float TurretRotation { get; set; }
        /// <summary>The rotation of this <see cref="Tank"/>.</summary>
        public float TankRotation { get; set; }
        /// <summary>The pitch of the footprint placement sounds.</summary>
        public float TreadPitch { get; set; }
        /// <summary>The pitch of the shoot sound.</summary>
        public float ShootPitch { get; set; }
        /// <summary>The type of bullet this <see cref="Tank"/> shoots.</summary>
        public ShellTier ShellType { get; set; } = ShellTier.Regular;
        /// <summary>The maximum amount of mines this <see cref="Tank"/> can place.</summary>
        public int MineLimit { get; set; }
        /// <summary>The hitbox of this <see cref="Tank"/>.</summary>
        public BoundingBox CollisionBox { get; set; }
        /// <summary>How long this <see cref="Tank"/> will be immobile upon firing a bullet.</summary>
        public int ShootStun { get; set; }
        /// <summary>How long this <see cref="Tank"/> will be immobile upon laying a mine.</summary>
        public int MineStun { get; set; }
        /// <summary>How long this <see cref="Tank"/> has to wait until it can fire another bullet..</summary>
        public int ShellCooldown { get; set; }
        public int MineCooldown { get; set; }
        public int RicochetCount { get; set; }
        public int ShellLimit { get; set; }
        public float TurningSpeed { get; set; } = 1f;
        public float MaximalTurn { get; set; }
        public Team Team { get; set; }

        public int OwnedBulletCount { get; set; }
        public int OwnedMineCount { get; set; }
        public Vector2 Position2D => position.FlattenZ();
        public Vector2 Velocity2D => velocity.FlattenZ();

        public Vector3 position, oldPosition, velocity;

        public string GetGeneralStats()
            => $"Pos2D: {Position2D} | Vel: {Velocity2D} | Dead: {Dead}";

        public virtual void Destroy() { }

        public virtual void LayFootprint() { }

        public virtual void Shoot() { }

        public virtual void LayMine() { }

        // everything under this comment is added outside of the faithful remake. homing shells, etc

        public Shell.HomingProperties ShellHoming = new();
    }

    public class TankFootprint
    {
        public const int MAX_FOOTPRINTS = 100000;

        public static TankFootprint[] footprints = new TankFootprint[MAX_FOOTPRINTS];

        public Vector3 location;
        public float rotation;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Model Model;

        public Texture2D texture;

        private static int total_treads_placed;

        public TankFootprint()
        {
            if (total_treads_placed + 1 > MAX_FOOTPRINTS)
                return;
            total_treads_placed++;

            Model = GameResources.GetGameResource<Model>("Assets/footprint"); // use this :smiley:

            texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/tank_footprint");

            footprints[total_treads_placed] = this;
            total_treads_placed++;
        }
        public void Render()
        {
            World = Matrix.CreateScale(0.5f, 1f, 0.1f) * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(location);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;
                    effect.TextureEnabled = true;

                    effect.Texture = texture;

                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }
    }

    public class TankDeathMark
    {
        public const int MAX_DEATH_MARKS = 1000;

        public static TankDeathMark[] deathMarks = new TankDeathMark[MAX_DEATH_MARKS];

        public Vector3 location;
        public float rotation;

        private static int total_death_marks;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Model Model;

        public Texture2D texture;

        public enum CheckColor
        {
            Blue,
            Red,
            White
        }

        public TankDeathMark(CheckColor color)
        {
            if (total_death_marks + 1 > MAX_DEATH_MARKS)
                return;
            total_death_marks++;

            Model = GameResources.GetGameResource<Model>("Assets/check");

            texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/check/check_{color.ToString().ToLower()}");

            deathMarks[total_death_marks] = this;
        }
        public void Render()
        {
            World = Matrix.CreateTranslation(location);
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;
                    effect.TextureEnabled = true;

                    effect.Texture = texture;

                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }
    }

    public enum Team
    {
        Red    = 1, 
        Blue   = 2,
        Green  = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6,
        Cyan   = 7,

    }
}