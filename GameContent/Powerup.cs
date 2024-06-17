using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
{
    public class Powerup
    {
        public delegate void PickupDelegate(ref Tank recipient);
        public static event PickupDelegate OnPickup;
        public delegate void PostUpdateDelegate(Powerup powerup);
        public static event PostUpdateDelegate OnPostUpdate;
        public delegate void PostRenderDelegate(Powerup powerup);
        public static event PostRenderDelegate OnPostRender;

        public const int MAX_POWERUPS = 50;
        public static Powerup[] Powerups = new Powerup[MAX_POWERUPS];

        /// <summary>The <see cref="Tank"/> this <see cref="Powerup"/> is currently affecting, if any.</summary>
        public Tank AffectedTank { get; private set; }

        /// <summary>Whether or not this <see cref="Powerup"/> is affecting a <see cref="Tank"/>.</summary>
        public bool HasOwner => AffectedTank is not null;

        /// <summary>The effect of this <see cref="Powerup"/> on a <see cref="Tank"/>.</summary>
        public Action<Tank> PowerupEffects { get; }

        /// <summary>The place to reset the effects of this <see cref="Powerup"/> on the <see cref="Tank"/> it was applied to.
        /// <para></para>
        /// It can also be used to do some cool ending effects on a powerup.
        /// </summary>
        public Action<Tank> PowerupReset { get; }

        /// <summary>The duration of this <see cref="Powerup"/> on a <see cref="Tank"/></summary>
        public int Duration;

        public Vector3 Position;

        /// <summary>The maximum distance from which a <see cref="Tank"/> can pick up this <see cref="Powerup"/>.</summary>
        public float PickupRadius;

        public int Id;

        /// <summary>The name of this <see cref="Powerup"/>.</summary>
        public string Name { get; set; }

        /// <summary>Whether or not this <see cref="Powerup"/> has been already picked up.</summary>
        public bool InWorld { get; private set; }

        private Model _model;

        public float Alpha = 1f;

        private readonly string TextureName;

        public Vector3 Rotation = new(0, -MathHelper.PiOver2, 0);

        public const float DEF_PICKUP_RANGE = 20f;

        public static PowerupTemplate Speed { get; } = new("Speed", "Assets/textures/medal/medal_speed", 1000, DEF_PICKUP_RANGE, tnk =>
            tnk.Properties.MaxSpeed *= 1.5f, 
        tnk =>
            tnk.Properties.MaxSpeed /= 1.5f);   
        public static PowerupTemplate Invisibility { get; } = new("Invisibility", "Assets/textures/medal/medal_invis", 1000, DEF_PICKUP_RANGE, tnk => 
            tnk.Properties.Invisible = !tnk.Properties.Invisible, 
        tnk => 
            tnk.Properties.Invisible = !tnk.Properties.Invisible);
        public static PowerupTemplate ShellHome { get; } = new("Homing", "Assets/textures/medal/medal_homshell", 1000, DEF_PICKUP_RANGE, tnk => 
        { 
            tnk.Properties.ShellHoming.Radius = 150f; tnk.Properties.ShellHoming.Speed = tnk.Properties.ShellSpeed; tnk.Properties.ShellHoming.Power = 1f; 
        }, 
        tnk => 
            tnk.Properties.ShellHoming = new());

        public Powerup(PowerupTemplate template)
        {
            _model = GameResources.GetGameResource<Model>("Assets/medal");
            PickupRadius = template.pickupRadius;
            Duration = template.duration;
            PowerupEffects = template.PowerupEffects;
            PowerupReset = template.PowerupReset;
            TextureName = template.TextureName;

            int index = Array.IndexOf(Powerups, Powerups.First(pw => pw is null));

            Id = index;

            Powerups[index] = this;
        }

        /// <summary>Spawns this <see cref="Powerup"/> in the world.</summary>
        public void Spawn(Vector3 position)
        {
            InWorld = true;

            Position = position;
        }

        public void Remove()
        {
            Powerups[Id] = null;
        }

        public void Update()
        {
            if (!MapRenderer.ShouldRender)
                return;
            if (HasOwner)
            {
               //  AffectedTank.ApplyDefaults();
                // PowerupEffects?.Invoke(AffectedTank);
                Duration--;
                if (Duration <= 0)
                {
                    PowerupReset?.Invoke(AffectedTank);
                    Powerups[Id] = null;
                }
            }
            else
            {
                Rotation.X += 0.05f * TankGame.DeltaTime;
                if (GameHandler.AllTanks.TryGetFirst(tnk => tnk is not null && Vector3.Distance(Position, tnk.Position3D) <= PickupRadius, out Tank tank))
                    Pickup(tank);
            }
            OnPostUpdate?.Invoke(this);
        }

        public void Render()
        {
            if (!MapRenderer.ShouldRender)
                return;
            if (!HasOwner)
            {
                foreach (ModelMesh mesh in _model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = Matrix.CreateScale(10) * Matrix.CreateFromYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z) * Matrix.CreateTranslation(Position);
                        effect.View = TankGame.GameView;
                        effect.Projection = TankGame.GameProjection;

                        effect.SetDefaultGameLighting_IngameEntities();

                        effect.TextureEnabled = true;

                        effect.Texture = GameResources.GetGameResource<Texture2D>(TextureName);
                    }

                    mesh.Draw();
                }
                var pos = MatrixUtils.ConvertWorldToScreen(default, Matrix.CreateTranslation(Position), TankGame.GameView, TankGame.GameProjection);

                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, this, pos, 4, centered: true);

                // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), GeometryUtils.CreateRectangleFromCenter((int)pos.X, (int)pos.Y, 25, 25), Color.White * 0.9f);
            }
            else
            {
                var pos = MatrixUtils.ConvertWorldToScreen(default, AffectedTank.World, TankGame.GameView, TankGame.GameProjection);

                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, this, pos, 4, centered: true);
            }
            OnPostRender?.Invoke(this);
        }
        /// <summary>
        /// Make a <see cref="Tank"/> pick this <see cref="Powerup"/> up.
        /// </summary>
        /// <param name="recipient">The recipient of this <see cref="Powerup"/>.</param>
        public void Pickup(Tank recipient)
        {
            AffectedTank = recipient;
            InWorld = false;

            PowerupEffects?.Invoke(AffectedTank);
            OnPickup?.Invoke(ref recipient);
        }
        public override string ToString()
        {
            if (AffectedTank is PlayerTank)
                return $"duration: {Duration} | HasOwner: {HasOwner}" + (HasOwner ? $" | OwnerTier: {(AffectedTank as PlayerTank).PlayerType}" : "");
            else
                return $"duration: {Duration} | HasOwner: {HasOwner}" + (HasOwner ? $" | OwnerTier: {(AffectedTank as AITank).AiTankType}" : "");
        }
    }
    /// <summary>A template for creating a <see cref="Powerup"/>. The fields in this class are identical to the ones in <see cref="Powerup"/>.</summary>
    public readonly struct PowerupTemplate
    {
        public readonly float pickupRadius;
        public readonly int duration;

        public readonly string Name;

        public readonly Action<Tank> PowerupEffects;

        public readonly Action<Tank> PowerupReset;

        public readonly string TextureName;

        public PowerupTemplate(string name, string textureName, int duration, float pickupRadius, Action<Tank> fx, Action<Tank> end)
        {
            TextureName = textureName;
            Name = name;
            PowerupEffects = fx;
            PowerupReset = end;

            this.pickupRadius = pickupRadius;
            this.duration = duration;
        }
    }
}
