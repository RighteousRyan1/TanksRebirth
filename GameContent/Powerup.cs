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

        public Tank AffectedTank { get; private set; }

        public bool HasOwner => AffectedTank is not null;

        public Action<Tank> PowerupEffects { get; }

        public int duration;

        public Vector3 position;

        public float pickupRadius;

        public int id;

        public bool InWorld { get; set; }

        public Powerup(int duration, float pickupRadius, Action<Tank> effects)
        {
            this.pickupRadius = pickupRadius;
            this.duration = duration;

            PowerupEffects = effects;

            int index = Array.IndexOf(activePowerups, activePowerups.First(tank => tank is null));

            id = index;

            activePowerups[index] = this;
        }

        public Powerup(PowerupTemplate template)
        {
            pickupRadius = template.pickupRadius;
            duration = template.duration;
            PowerupEffects = template.PowerupEffects;

            int index = Array.IndexOf(activePowerups, activePowerups.First(tank => tank is null));

            id = index;

            activePowerups[index] = this;
        }

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
                    AffectedTank.ApplyDefaults();
                    activePowerups[id] = null;
                }
            }
            else
            {
                if (WPTR.AllTanks.TryGetFirst(tnk => tnk is not null && Vector3.Distance(position, tnk.position) <= pickupRadius, out Tank tank))
                {
                    Pickup(tank);
                }
            }
        }

        public void Render()
        {
            if (InWorld)
            {
                var pos = GeometryUtils.ConvertWorldToScreen(default, Matrix.CreateTranslation(position), TankGame.GameView, TankGame.GameProjection);

                DebugUtils.DrawDebugString(TankGame.spriteBatch, this, pos, 3, centerIt: true);

                TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), GeometryUtils.CreateRectangleFromCenter((int)pos.X, (int)pos.Y, 25, 25), Color.White * 0.9f);
            }
            else
            {
                var pos = GeometryUtils.ConvertWorldToScreen(default, AffectedTank.World, TankGame.GameView, TankGame.GameProjection);

                DebugUtils.DrawDebugString(TankGame.spriteBatch, this, pos, 3, centerIt: true);
            }
        }

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

    public class PowerupTemplate
    {
        public float pickupRadius;
        public int duration;

        public Action<Tank> PowerupEffects { get; }

        public PowerupTemplate(int duration, float pickupRadius, Action<Tank> fx)
        {
            PowerupEffects = fx;

            this.pickupRadius = pickupRadius;
            this.duration = duration;
        }
    }
}
