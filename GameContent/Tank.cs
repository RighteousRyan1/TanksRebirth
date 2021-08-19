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
        public Mine[] minesLaid;
        public Bullet[] bulletsFired;
        public float barrelRotation;
        public int maxLayableMines;

        public bool IsAI { get; }

        public TankType TankType;

        public Tank(Vector3 beginPos)
        {
            position = beginPos;
            AllTanks.Add(this);
        }

        internal void Update()
        {
            //instead of updating & drawing mines and bullets
            //in WiiPlayTanksRemake.Update()/Draw(), we could
            //do it here/Draw() instead. there shouldn't be any
            //difference other than the readability as the
            //call positions should be the same
        }

        internal void Draw()
        {
            if (IsAI)
            {
                TankGame.Models.TankModelEnemy.Draw(new Matrix(), new Matrix(), new Matrix());
            }
            else
            {
                TankGame.Models.TankModelPlayer.Draw(new Matrix(), new Matrix(), new Matrix());
            }
        }
    }
}