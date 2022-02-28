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

        public float rotationX;
        public float rotationY;
        public float rotationZ;

        public float Scale = 1f;

        public float Opacity = 1f;

        public readonly int id;

        public bool FaceTowardsMe;

        public bool is2d;

        public Action<Particle> UniqueBehavior;

        public bool isAddative = true;

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
        }

        public BasicEffect effect = new(TankGame.Instance.GraphicsDevice);

        internal void Render()
        {
            if (!is2d)
            {
                effect.World = Matrix.CreateScale(Scale) * Matrix.CreateRotationX(rotationX) * Matrix.CreateRotationY(rotationY) * Matrix.CreateRotationZ(rotationZ) * Matrix.CreateTranslation(position);
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

                TankGame.spriteBatch.End();
                TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.DepthRead, RasterizerState.CullNone, effect);
                TankGame.spriteBatch.Draw(Texture, Vector2.Zero, null, color * Opacity, 0f, Texture.Size() / 2, Scale, default, default);
            }
            else
            {
                TankGame.spriteBatch.End();
                TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                TankGame.spriteBatch.Draw(Texture, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(position), TankGame.GameView, TankGame.GameProjection), null, color * Opacity, 0f, Texture.Size() / 2, Scale, default, default);
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