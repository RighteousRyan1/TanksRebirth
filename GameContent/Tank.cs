using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using WiiPlayTanksRemake.Enums;
using System.Linq;

namespace WiiPlayTanksRemake.GameContent
{
    public class Tank
    {
        public static List<Tank> AllTanks { get; } = new();

        public Vector2 position;
        public float speed;
        public float bulletShootSpeed;
        public float barrelRotation; // do remember this is in radians
        public int maxLayableMines;

        public bool IsAI { get; }

        public TankTier tier;

        public int Tier => (int)tier;

        public Action<Tank> behavior;

        public static TankTier GetHighestTierActive()
        {
            TankTier highest = TankTier.None;

            foreach (Tank tank in AllTanks)
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            // var x = AllTanks.OrderBy(t => t.tier)[AllTanks.OrderBy(t => t.tier).Length - 1];
            return highest;
        }

        public Tank(Vector2 beginPos, TankTier tier, bool ai = false)
        {
            position = beginPos;
            IsAI = ai;
            this.tier = tier;
            AllTanks.Add(this);
        }

        internal void Update()
        {
            if (IsAI)
            {
                behavior?.Invoke(this);
            }
            else
            {
                if (behavior != null)
                    throw new Exception($"Player tanks cannot have ai behavior!");


            }
        }

        internal void Draw()
        {
            if (IsAI)
            {
                
            }
            else
            {

            }
        }
    }
}