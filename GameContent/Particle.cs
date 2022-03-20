using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public class Particle
    {
        public Texture2D Texture;

        public Vector3 position;

        public Color color = Color.White;

        public float roll;
        public float pitch;
        public float yaw;

        public float Opacity = 1f;

        public readonly int id;

        public bool FaceTowardsMe;

        public bool is2d;

        public Action<Particle> UniqueBehavior;

        public bool isAddative = true;

        public int lifeTime;

        // NOTE: scale.X is used for 2d scaling.
        public Vector3 Scale;

        public float addativeLightPower;

        /* TODO:
         * Model alpha must be set!
         * 
         * billboard from 'position' to the camera.
         */

        internal Particle(Vector3 position)
        {
            this.position = position;
            int index = Array.IndexOf(ParticleSystem.CurrentParticles, ParticleSystem.CurrentParticles.First(particle => particle == null));

            id = index;

            ParticleSystem.CurrentParticles[index] = this;
        }

        public void Update()
        {
            UniqueBehavior?.Invoke(this);
            lifeTime++;
        }

        public static BasicEffect effect = new(TankGame.Instance.GraphicsDevice);

        internal void Render()
        {
            if (!is2d)
            {
                effect.World = Matrix.CreateScale(Scale) * Matrix.CreateRotationX(roll) * Matrix.CreateRotationY(pitch) * Matrix.CreateRotationZ(yaw) * Matrix.CreateTranslation(position);
                effect.View = TankGame.GameView;
                effect.Projection = TankGame.GameProjection;
                effect.TextureEnabled = true;
                effect.Texture = Texture;
                effect.AmbientLightColor = color.ToVector3();
                effect.DiffuseColor = color.ToVector3();
                effect.FogColor = color.ToVector3();
                effect.EmissiveColor = color.ToVector3();
                effect.SpecularColor = color.ToVector3();

                effect.Alpha = Opacity;

                effect.SetDefaultGameLighting_IngameEntities(addativeLightPower);

                effect.FogEnabled = false;

                TankGame.spriteBatch.End();
                TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, isAddative ? BlendState.Additive : BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.DepthRead, RasterizerState.CullNone, effect);
                TankGame.spriteBatch.Draw(Texture, Vector2.Zero, null, color * Opacity, 0f, Texture.Size() / 2, Scale.X, default, default);
            }
            else
            {
                TankGame.spriteBatch.End();
                TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, isAddative ? BlendState.Additive : BlendState.NonPremultiplied);
                TankGame.spriteBatch.Draw(Texture, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(position), TankGame.GameView, TankGame.GameProjection), null, color * Opacity, 0f, Texture.Size() / 2, Scale.X, default, default);
            }
            TankGame.spriteBatch.End();
            TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public void Destroy()
        {
            ParticleSystem.CurrentParticles[id] = null;
        }
    }
}