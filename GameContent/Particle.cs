using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;

namespace TanksRebirth.GameContent
{
    public enum ParticleIntensity
    {
        None, // no particles at all
        Low, // Particles emit at 25% the frequency
        Medium, // ... 50%
        High, // ... 100%
    }
    public class Particle
    {
        public object Tag;

        public Texture2D Texture;

        public Vector3 Position;

        public Color Color = Color.White;

        public float Roll;
        public float Pitch;
        public float Yaw;

        public float TextureScale = int.MinValue;
        public float Rotation2D;

        public Vector2 Origin2D;

        public float Opacity = 1f;

        public readonly int Id;

        public bool FaceTowardsMe;

        public bool Is2d;

        public Action<Particle> UniqueBehavior;

        public bool isAddative = true;

        public Rectangle? TextureCrop; // for framing a particle's texture

        public int LifeTime;

        public bool IsText;
        public string Text;

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

                TankGame.SpriteRenderer.End();
                TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, isAddative ? BlendState.Additive : BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.DepthRead, TankGame.DefaultRasterizer, effect);
                if (!IsText)
                    TankGame.SpriteRenderer.Draw(Texture, Vector2.Zero, TextureCrop, Color * Opacity, Rotation2D, Origin2D != default ? Origin2D : Texture.Size() / 2, TextureScale == int.MinValue ? Scale.X : TextureScale, default, default);
                else
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, Text, Vector2.Zero, Color * Opacity, new Vector2(Scale.X, Scale.Y), Rotation2D, Origin2D);
            }
            else
            {
                TankGame.SpriteRenderer.End();
                TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, isAddative ? BlendState.Additive : BlendState.NonPremultiplied, rasterizerState: TankGame.DefaultRasterizer);
                if (!IsText)
                    TankGame.SpriteRenderer.Draw(Texture, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(Position), TankGame.GameView, TankGame.GameProjection), TextureCrop, Color * Opacity, Rotation2D, Origin2D != default ? Origin2D : Texture.Size() / 2, TextureScale == int.MinValue ? Scale.X : TextureScale, default, default);
                else
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, Text, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(Position), TankGame.GameView, TankGame.GameProjection), Color * Opacity, new Vector2(Scale.X, Scale.Y), Rotation2D, Origin2D);
            }
            TankGame.SpriteRenderer.End();
            TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public void Destroy()
        {
            UniqueBehavior = null;
            ParticleSystem.CurrentParticles[Id] = null;
        }
    }
}