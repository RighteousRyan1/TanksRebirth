using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
{
    public class Particle
    {
        public Texture2D Texture;

        public Vector3 Position;

        public Color Color = Color.White;

        public float Roll;
        public float Pitch;
        public float Yaw;

        public float TextureScale = int.MinValue;
        public float TextureRotation;

        public Vector2 TextureOrigin;

        public float Opacity = 1f;

        public readonly int Id;

        public bool FaceTowardsMe;

        public bool Is2d;

        public Action<Particle> UniqueBehavior;

        public bool isAddative = true;

        public int LifeTime;

        // NOTE: scale.X is used for 2d scaling.
        public Vector3 Scale;

        public float LightPower;

        /* TODO:
         * Model alpha must be set!
         * 
         * billboard from 'position' to the camera.
         */

        internal Particle(Vector3 position)
        {
            Position = position;
            int index = Array.IndexOf(ParticleSystem.CurrentParticles, ParticleSystem.CurrentParticles.First(particle => particle == null));

            Id = index;

            ParticleSystem.CurrentParticles[index] = this;
        }

        public void Update()
        {
            UniqueBehavior?.Invoke(this);
            LifeTime++;
        }

        public static BasicEffect effect = new(TankGame.Instance.GraphicsDevice);

        internal void Render()
        {
            if (!Is2d)
            {
                var world =
                    Matrix.CreateScale(Scale) * Matrix.CreateRotationX(Roll) * Matrix.CreateRotationY(Pitch) * Matrix.CreateRotationZ(Yaw) * Matrix.CreateTranslation(Position);
                effect.World = world;
                effect.View = TankGame.GameView;
                effect.Projection = TankGame.GameProjection;
                effect.TextureEnabled = true;
                effect.Texture = Texture;
                effect.AmbientLightColor = Color.ToVector3() * GameHandler.GameLight.Brightness;
                effect.DiffuseColor = Color.ToVector3() * GameHandler.GameLight.Brightness;
                effect.FogColor = Color.ToVector3() * GameHandler.GameLight.Brightness;
                effect.EmissiveColor = Color.ToVector3() * GameHandler.GameLight.Brightness;
                effect.SpecularColor = Color.ToVector3() * GameHandler.GameLight.Brightness;

                effect.Alpha = Opacity;

                effect.SetDefaultGameLighting_IngameEntities(LightPower);

                effect.FogEnabled = false;

                TankGame.spriteBatch.End();
                TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, isAddative ? BlendState.Additive : BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.DepthRead, TankGame.DefaultRasterizer, effect);
                TankGame.spriteBatch.Draw(Texture, Vector2.Zero, null, Color * Opacity, TextureRotation, TextureOrigin != default ? TextureOrigin : Texture.Size() / 2, TextureScale == int.MinValue ? Scale.X : TextureScale, default, default);
            }
            else
            {
                TankGame.spriteBatch.End();
                TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, isAddative ? BlendState.Additive : BlendState.NonPremultiplied, rasterizerState: TankGame.DefaultRasterizer);
                TankGame.spriteBatch.Draw(Texture, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(Position), TankGame.GameView, TankGame.GameProjection), null, Color * Opacity, TextureRotation, TextureOrigin != default ? TextureOrigin : Texture.Size() / 2, TextureScale == int.MinValue ? Scale.X : TextureScale, default, default);
            }
            TankGame.spriteBatch.End();
            TankGame.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public void Destroy()
        {
            UniqueBehavior = null;
            ParticleSystem.CurrentParticles[Id] = null;
        }
    }
}