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
        public float barrelRotation;
        public int maxLayableMines;

        public bool IsAI { get; }

        public TankType TankType;

        public Tank(Vector3 beginPos, bool ai = false)
        {
            position = beginPos;
            IsAI = ai;
            AllTanks.Add(this);
        }

        internal void Update()
        {

        }

        internal void Draw()
        {
            if (IsAI)
            {
                
            }
            else
            {
                TankGame.Models.TankModelPlayer.Draw(new Matrix(), new Matrix(), new Matrix());
            }
        }
    }
}