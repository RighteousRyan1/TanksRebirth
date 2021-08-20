using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using WiiPlayTanksRemake.Enums;

namespace WiiPlayTanksRemake.GameContent
{
    public class Tank
    {
        public static List<Tank> AllTanks { get; } = new();

        public Vector3 position;
        public float speed;
        public float bulletShootSpeed;
        public float barrelRotation; // do remember this is in radians
        public int maxLayableMines;

        private Matrix viewMatrix;

        public bool IsAI { get; }

        public TankType type;

        public int Tier => (int)type;

        public Action<Tank> behavior;

        public Tank(Vector3 beginPos, bool ai = false)
        {
            position = beginPos;
            IsAI = ai;
            AllTanks.Add(this);
        }

        internal void Update()
        {
            if (!IsAI)
                if (behavior != null)
                    throw new Exception($"Player tanks cannot have ai behavior!");
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