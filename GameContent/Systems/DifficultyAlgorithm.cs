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
            [TankTier.Black] = 0.36f,
            [TankTier.Bronze] = 0.035f,
            [TankTier.Silver] = 0.09f,
            [TankTier.Sapphire] = 0.24f,
            [TankTier.Ruby] = 0.31f,
            [TankTier.Citrine] = 0.26f,
            [TankTier.Amethyst] = 0.35f,
            [TankTier.Emerald] = 0.42f,
            [TankTier.Gold] = 0.61f,
            [TankTier.Obsidian] = 0.69f,
            [TankTier.Granite] = 0.025f,
            [TankTier.Bubblegum] = 0.065f,
            [TankTier.Water] = 0.15f,
            [TankTier.Crimson] = 0.23f,
            [TankTier.Tiger] = 0.27f,
            [TankTier.Fade] = 0.29f,
            [TankTier.Creeper] = 0.45f,
            [TankTier.Gamma] = 0.35f,
            [TankTier.Marble] = 0.85f,
        };

        public static float GetDifficulty(Mission mission)
        {
            float difficulty = 0;
            Dictionary<TankTier, int> tankCounts = new();
            foreach (var tank in mission.Tanks)
            {
                if (!tank.IsPlayer)
                {
                    if (!tankCounts.ContainsKey(tank.AiTier))
                        tankCounts.Add(tank.AiTier, 1);
                    else
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
