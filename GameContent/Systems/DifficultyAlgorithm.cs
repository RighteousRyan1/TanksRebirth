using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;

namespace TanksRebirth.GameContent.Systems
{
    public static class DifficultyAlgorithm
    {

        private static Dictionary<TankTier, float> _tankDiffs = new()
        {
            [TankTier.Brown] = 0.01f,
            [TankTier.Ash] = 0.025f,
            [TankTier.Marine] = 0.12f,
            [TankTier.Yellow] = 0.08f,
            [TankTier.Pink] = 0.15f,
            [TankTier.Green] = 0.3f,
            [TankTier.Violet] = 0.25f,
            [TankTier.White] = 0.275f,
            [TankTier.Black] = 0.36f
        };

        public static float GetDifficulty(Mission mission)
        {
            float difficulty = 0;
            Dictionary<TankTier, int> tankCounts = new()
            {
                [TankTier.Brown] = 0,
                [TankTier.Ash] = 0,
                [TankTier.Marine] = 0,
                [TankTier.Yellow] = 0,
                [TankTier.Pink] = 0,
                [TankTier.Green] = 0,
                [TankTier.Violet] = 0,
                [TankTier.White] = 0,
                [TankTier.Black] = 0
            };
            foreach (var tank in mission.Tanks)
            {
                if (!tank.IsPlayer)
                {
                    tankCounts[tank.AiTier]++;
                    difficulty += _tankDiffs[tank.AiTier] / tankCounts[tank.AiTier] * 1.5f;
                }
            }

            foreach (var block in mission.Blocks)
            {
                difficulty *= 0.985f;
            }

            return difficulty;
        }
    }
}
