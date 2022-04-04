using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
{
    public class ParticleSystem
    {
        public static int MAX_PARTICLES = 150000;
        public static Particle[] CurrentParticles = new Particle[MAX_PARTICLES];

        public static void RenderParticles()
        {

            foreach (var particle in CurrentParticles)
            {
                if (particle != null)
                    particle.Render();
            }
        }
        public static void UpdateParticles()
        {
            foreach (var particle in CurrentParticles)
            {
                if (particle != null)
                    particle.Update();
            }
        }
        /// <summary>Creates a particle.</summary>
        /// <param name="position">The initial position of this particle.</param>
        /// <param name="texture">The texture used for the particle.</param>
        /// <returns>The particle created.</returns>
        public static Particle MakeParticle(Vector3 position, Texture2D texture)
        {
            return new(position)
            {
                Texture = texture,
            };
        }

        public static void MakeSmallExplosion(Vector3 position, int numClouds, int numSparks, float shineScale, int movementFactor)
        {
            MakeSmokeCloud(position, movementFactor, numClouds);
            MakeSparkEmission(position, numSparks);
            MakeShineSpot(position, Color.Orange, shineScale);
        }
        public static void MakeSparkEmission(Vector3 position, int numSparks)
                                                                                                        {
            for (int i = 0; i < numSparks; i++)
            {
                var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/particle_line");

                var spark = MakeParticle(position, texture);

                var vel = new Vector3(GameHandler.GameRand.NextFloat(-0.25f, 0.25f), GameHandler.GameRand.NextFloat(0, 0.75f), GameHandler.GameRand.NextFloat(-0.25f, 0.25f));

                spark.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                var angles = GeometryUtils.AsEulerAngles(new Quaternion(new Vector3(spark.Roll, spark.Pitch, spark.Yaw), 0f));

                spark.Roll = angles.Roll;
                spark.Pitch = angles.Pitch;
                spark.Yaw = angles.Yaw;
                spark.Opacity = 1f;
                spark.Scale = new(GameHandler.GameRand.NextFloat(0.4f, 0.6f));

                spark.color = Color.Yellow;

                spark.UniqueBehavior = (part) =>
                {
                    part.position += vel;
                    part.Opacity -= 0.025f;
                    part.position += vel;

                    if (part.Opacity <= 0f)
                        part.Destroy();
                };
            }
        }
        public static void MakeSmokeCloud(Vector3 position, int timeMovingSideways, int numClouds)
        {
            for (int i = 0; i < numClouds; i++)
            {
                var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke");

                var smoke = MakeParticle(position, texture);

                smoke.isAddative = true;

                smoke.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                smoke.Scale = new(0.8f);

                var velocity = Vector2.UnitY.RotatedByRadians(MathHelper.ToRadians(360f / numClouds * i)).ExpandZ() / 2;

                smoke.position.Y += 5f + GameHandler.GameRand.NextFloat(0f, 8f);

                smoke.color = Color.DarkOrange;

                smoke.UniqueBehavior = (p) =>
                {
                    smoke.position += velocity;
                    GeometryUtils.Add(ref smoke.Scale, -0.01f);

                    if (smoke.Scale.X <= 0f)
                        smoke.Destroy();

                    if (smoke.lifeTime > timeMovingSideways)
                    {
                        smoke.Opacity -= 0.02f;
                        smoke.position.Y += GameHandler.GameRand.NextFloat(0.1f, 0.25f);
                        velocity.X *= 0.9f;
                        velocity.Z *= 0.9f;
                    }
                };
            }
        }

        public static void MakeShineSpot(Vector3 position, Color color, float scale)
        {
            var p = MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_star"));
            p.Scale = new(scale);
            p.color = color;
            p.UniqueBehavior = (part) =>
            {
                GeometryUtils.Add(ref p.Scale, -0.0175f);

                p.Opacity -= 0.025f;

                if (p.Opacity <= 0f || p.Scale.X <= 0f)
                    p.Destroy();
            };
        }
    }
}