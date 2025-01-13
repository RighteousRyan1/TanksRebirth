using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.GameContent.Systems
{
    public static class DifficultyAlgorithm
    {

        private static Dictionary<int, float> _tankDiffs = new() {
            [TankID.Brown] = 0.01f,
            [TankID.Ash] = 0.025f,
            [TankID.Marine] = 0.12f,
            [TankID.Yellow] = 0.08f,
            [TankID.Pink] = 0.15f,
            [TankID.Green] = 0.3f,
            [TankID.Violet] = 0.25f,
            [TankID.White] = 0.275f,
            [TankID.Black] = 0.36f,
            [TankID.Bronze] = 0.035f,
            [TankID.Silver] = 0.09f,
            [TankID.Sapphire] = 0.24f,
            [TankID.Ruby] = 0.31f,
            [TankID.Citrine] = 0.26f,
            [TankID.Amethyst] = 0.35f,
            [TankID.Emerald] = 0.42f,
            [TankID.Gold] = 0.61f,
            [TankID.Obsidian] = 0.69f,
            [TankID.Granite] = 0.025f,
            [TankID.Bubblegum] = 0.065f,
            [TankID.Water] = 0.15f,
            [TankID.Crimson] = 0.23f,
            [TankID.Tiger] = 0.27f,
            [TankID.Fade] = 0.29f,
            [TankID.Creeper] = 0.45f,
            [TankID.Gamma] = 0.35f,
            [TankID.Marble] = 0.85f,
            [TankID.Assassin] = 0.775f,
            [TankID.Explosive] = 0.48f,
            [TankID.Commando] = 0.65f,
            [TankID.Cherry] = 0.6f,
            [TankID.Electro] = 0.35f
        };

        public static float GetDifficulty(Mission mission)
        {
            float difficulty = 0;
            Dictionary<int, int> tankCounts = new();
            foreach (var tank in mission.Tanks) {
                if (tank.IsPlayer) continue;

                if (!tankCounts.ContainsKey(tank.AiTier))
                    tankCounts.Add(tank.AiTier, 1);
                else
                    tankCounts[tank.AiTier]++;

                difficulty += _tankDiffs[tank.AiTier] / tankCounts[tank.AiTier] * 1.5f;
            }

            foreach (var block in mission.Blocks) {
                // TODO: based on this, have a list of defaults per-block type.
                if (block.Type != BlockID.Hole || block.Type == BlockID.Teleporter)
                    difficulty *= 0.985f;
            }

            return difficulty;
        }
    }
}
