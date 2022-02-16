using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WiiPlayTanksRemake.GameContent
{
    public class ParticleSystem
    {
        public static int MAX_PARTICLES = 300;
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
        /// <param name="texture">The texture applied to the model.</param>
        /// <param name="modelOverride">Overrides the model used. If not set, will use a flat model for 2D particles in a 3D space</param>
        /// <returns>The particle created.</returns>
        public static Particle MakeParticle(Vector3 position, Texture2D texture, Model modelOverride = null)
        {
            return new(modelOverride)
            {
                position = position,
                Texture = texture,
            };
        }
    }
}