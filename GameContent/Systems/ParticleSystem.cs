using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
{
    public class ParticleSystem
    {
        public int MaxParticles = 150000;
        public Particle[] CurrentParticles;

        public ParticleSystem(int maxParticles)
        {
            MaxParticles = maxParticles;
            CurrentParticles = new Particle[MaxParticles];
        }

        public void RenderParticles()
        {
            foreach (var particle in CurrentParticles)
                particle?.Render();
        }
        public void UpdateParticles()
        {
            foreach (var particle in CurrentParticles)
                particle?.Update();
        }
        /// <summary>Creates a particle.</summary>
        /// <param name="position">The initial position of this particle.</param>
        /// <param name="texture">The texture used for the particle.</param>
        /// <returns>The particle created.</returns>
        public Particle MakeParticle(Vector3 position, Texture2D texture)
        {
            return new(position, this)
            {
                //IsText = true,
                //Text = "fuck",
                //Roll = MathHelper.Pi,
                Texture = texture
            };
        }
        public Particle MakeParticle(Vector3 position, string text)
        {
            return new(position, this)
            {
                IsText = true,
                Text = text
            };
        }

        public void MakeSmallExplosion(Vector3 position, int numClouds, int numSparks, float shineScale, int movementFactor)
        {
            MakeSmokeCloud(position, movementFactor, numClouds);
            MakeSparkEmission(position, numSparks);
            MakeShineSpot(position, Color.Orange, shineScale);
        }
        public void MakeSparkEmission(Vector3 position, int numSparks)
                                                                                                        {
            for (int i = 0; i < numSparks; i++)
            {
                var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/particle_line");

                var spark = MakeParticle(position, texture);

                var vel = new Vector3(GameHandler.GameRand.NextFloat(-0.25f, 0.25f), GameHandler.GameRand.NextFloat(0, 0.75f), GameHandler.GameRand.NextFloat(-0.25f, 0.25f)) * 2;

                spark.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                var angles = GeometryUtils.ToEulerAngles(new Quaternion(new Vector3(spark.Roll, spark.Pitch, spark.Yaw), 0f));

                spark.Roll = angles.Roll;
                spark.Pitch = angles.Pitch;
                spark.Yaw = angles.Yaw;
                spark.Alpha = 1f;
                spark.Scale = new(GameHandler.GameRand.NextFloat(0.4f, 0.6f));

                spark.Color = Color.Yellow;

                spark.UniqueBehavior = (part) =>
                {
                    part.Position += vel * TankGame.DeltaTime;
                    part.Alpha -= 0.025f * TankGame.DeltaTime;

                    if (part.Alpha <= 0f)
                        part.Destroy();
                };
            }
        }
        public void MakeSmokeCloud(Vector3 position, int timeMovingSideways, int numClouds)
        {
            for (int i = 0; i < numClouds; i++)
            {
                var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smoke");

                var smoke = MakeParticle(position, texture);

                smoke.HasAddativeBlending = true;

                smoke.Roll = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

                smoke.Scale = new(0.8f);

                var velocity = Vector2.UnitY.RotatedByRadians(MathHelper.ToRadians(360f / numClouds * i)).ExpandZ() / 2;

                smoke.Position.Y += 5f + GameHandler.GameRand.NextFloat(0f, 8f);

                smoke.Color = Color.DarkOrange;

                smoke.UniqueBehavior = (p) =>
                {
                    smoke.Position += velocity;
                    GeometryUtils.Add(ref smoke.Scale, -0.01f * TankGame.DeltaTime);

                    if (smoke.Scale.X <= 0f)
                        smoke.Destroy();

                    if (smoke.LifeTime > timeMovingSideways)
                    {
                        smoke.Alpha -= 0.02f * TankGame.DeltaTime;
                        smoke.Position.Y += GameHandler.GameRand.NextFloat(0.1f, 0.25f) * TankGame.DeltaTime;
                        velocity.X *= 0.9f * TankGame.DeltaTime;
                        velocity.Z *= 0.9f * TankGame.DeltaTime;
                    }
                };
            }
        }

        public void MakeShineSpot(Vector3 position, Color color, float scale)
        {
            var p = MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_star"));
            p.Scale = new(scale);
            p.Color = color;
            p.UniqueBehavior = (part) =>
            {
                GeometryUtils.Add(ref p.Scale, -0.0175f * TankGame.DeltaTime);

                p.Alpha -= 0.025f * TankGame.DeltaTime;

                if (p.Alpha <= 0f || p.Scale.X <= 0f)
                    p.Destroy();
            };
        }
    }
}