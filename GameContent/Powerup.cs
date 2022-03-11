using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public class Powerup
    {
        public const int MAX_POWERUPS = 50;
        public static Powerup[] activePowerups = new Powerup[MAX_POWERUPS];

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
        public int duration;

        public Vector3 position;

        /// <summary>The maximum distance from which a <see cref="Tank"/> can pick up this <see cref="Powerup"/>.</summary>
        public float pickupRadius;

        public int id;

        /// <summary>The name of this <see cref="Powerup"/>.</summary>
        public string Name { get; set; }

        /// <summary>Whether or not this <see cref="Powerup"/> has been already picked up.</summary>
        public bool InWorld { get; private set; }

        public Powerup(int duration, float pickupRadius, Action<Tank> effects, Action<Tank> end)
        {
            this.pickupRadius = pickupRadius;
            this.duration = duration;

            PowerupEffects = effects;
            PowerupReset = end;

            int index = Array.IndexOf(activePowerups, activePowerups.First(tank => tank is null));

            id = index;

            activePowerups[index] = this;
        }

        public Powerup(PowerupTemplate template)
        {
            pickupRadius = template.pickupRadius;
            duration = template.duration;
            PowerupEffects = template.PowerupEffects;
            PowerupReset = template.PowerupReset;

            int index = Array.IndexOf(activePowerups, activePowerups.First(tank => tank is null));

            id = index;

            activePowerups[index] = this;
        }

        /// <summary>Spawns this <see cref="Powerup"/> in the world.</summary>
        public void Spawn(Vector3 position)
        {
            InWorld = true;

            this.position = position;
        }

        public void Update()
        {
            if (HasOwner)
            {
               //  AffectedTank.ApplyDefaults();
                // PowerupEffects?.Invoke(AffectedTank);
                duration--;
                if (duration <= 0)
                {
                    PowerupReset?.Invoke(AffectedTank);
                    activePowerups[id] = null;
                }
            }
            else
            {
                if (GameHandler.AllTanks.TryGetFirst(tnk => tnk is not null && Vector3.Distance(position, tnk.position3d) <= pickupRadius, out Tank tank))
                {
                    Pickup(tank);
                }
            }
        }

        public void Render()
        {
            if (!HasOwner)
            {
                var pos = GeometryUtils.ConvertWorldToScreen(default, Matrix.CreateTranslation(position), TankGame.GameView, TankGame.GameProjection);

                DebugUtils.DrawDebugString(TankGame.spriteBatch, this, pos, 3, centered: true);

                TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), GeometryUtils.CreateRectangleFromCenter((int)pos.X, (int)pos.Y, 25, 25), Color.White * 0.9f);
            }
            else
            {
                var pos = GeometryUtils.ConvertWorldToScreen(default, AffectedTank.World, TankGame.GameView, TankGame.GameProjection);

                DebugUtils.DrawDebugString(TankGame.spriteBatch, this, pos, 3, centered: true);
            }
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
        }
        public override string ToString()
        {
            if (AffectedTank is PlayerTank)
                return $"duration: {duration} | HasOwner: {HasOwner}" + (HasOwner ? $" | OwnerTier: {(AffectedTank as PlayerTank).PlayerType}" : "");
            else
                return $"duration: {duration} | HasOwner: {HasOwner}" + (HasOwner ? $" | OwnerTier: {(AffectedTank as AITank).tier}" : "");
        }
    }
    /// <summary>A template for creating a <see cref="Powerup"/>. The fields in this class are identical to the ones in <see cref="Powerup"/>.</summary>
    public struct PowerupTemplate
    {
        public float pickupRadius;
        public int duration;

        public string Name { get; set; }

        public Action<Tank> PowerupEffects { get; }

        public Action<Tank> PowerupReset { get; }

        public PowerupTemplate(int duration, float pickupRadius, Action<Tank> fx, Action<Tank> end)
        {
            Name = string.Empty;
            PowerupEffects = fx;
            PowerupReset = end;

            this.pickupRadius = pickupRadius;
            this.duration = duration;
        }
    }
}
