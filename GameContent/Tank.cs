using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.GameContent
{
    public class Tank
    {
        private static readonly List<Tank> totalTanks = new();
        public Vector3 position;
        public float speed;
        public float bulletShootSpeed;
        public Mine[] minesLaid;
        public float barrelRotation;
        public int maxLayableMines;

        public bool IsAi { get; }

        public Tank(Vector3 beginPos)
        {
            position = beginPos;
            totalTanks.Add(this);
        }

        public static void UpdateAllTanks()
        {

        }
        public static void DrawAllTankModels()
        {

        }
    }
}