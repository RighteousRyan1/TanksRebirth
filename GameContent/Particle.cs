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
        private Model _model;

        public Texture2D Texture;

        public Vector3 position;

        public Quaternion rotation;

        public float Scale = 1f;

        public float Opacity = 1f;

        public readonly int id;

        public bool FaceTowardsMe;

        public Action<Particle> UniqueBehavior;

        internal Particle(Model modelOverride = null)
        {
            int index = Array.IndexOf(ParticleSystem.CurrentParticles, ParticleSystem.CurrentParticles.First(particle => particle == null));

            id = index;

            if (modelOverride is null)
                _model = GameResources.GetGameResource<Model>("Assets/check");
            else 
                _model = modelOverride;

            ParticleSystem.CurrentParticles[index] = this;
        }

        public void Update()
        {
            UniqueBehavior?.Invoke(this);
        }

        internal void Render()
        {
            DebugUtils.DrawDebugString(TankGame.spriteBatch, "0", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(position), TankGame.GameView, TankGame.GameProjection), colorOverride: Color.White * Opacity, centerIt: true);
            foreach (var mesh in _model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    var quat = Quaternion.CreateFromYawPitchRoll(0.5f, 0f, 0f);

                    effect.World = Matrix.CreateScale(Scale) * /*(FaceTowardsMe ? Matrix.CreateFromQuaternion(quat) : Matrix.CreateFromQuaternion(rotation)) **/ Matrix.CreateTranslation(position);
                    effect.View = TankGame.GameView;
                    effect.Projection = TankGame.GameProjection;

                    // effect.Alpha = Opacity;

                    effect.SetDefaultGameLighting_IngameEntities();
                }
            }
        }

        public void Destroy()
        {
            ParticleSystem.CurrentParticles[id] = null;
        }
    }
}