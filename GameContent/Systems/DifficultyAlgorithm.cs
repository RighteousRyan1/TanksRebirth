using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.GameContent.Systems;

public static class DifficultyAlgorithm
{

    public static Dictionary<int, float> TankDiffs = new() {
        [TankID.Brown] = 0.01f,
        [TankID.Ash] = 0.025f,
        [TankID.Marine] = 0.9f,
        [TankID.Yellow] = 0.065f,
        [TankID.Pink] = 0.12f,
        [TankID.Green] = 0.28f,
        [TankID.Violet] = 0.22f,
        [TankID.White] = 0.25f,
        [TankID.Black] = 0.33f,

        [TankID.Bronze] = 0.035f,
        [TankID.Silver] = 0.09f,
        [TankID.Sapphire] = 0.24f,
        [TankID.Ruby] = 0.31f,
        [TankID.Citrine] = 0.26f,
        [TankID.Amethyst] = 0.35f,
        [TankID.Emerald] = 0.42f,
        [TankID.Gold] = 0.55f,
        [TankID.Obsidian] = 0.69f,
    };
    public static Dictionary<float, string> DiffNames = new() {
        //[0] = TankGame.GameLanguage.Trivial,
        //[0] = TankGame.GameLanguage.Casual,
        //[0] = TankGame.GameLanguage.Trivial,
    };

    public static float GetDifficulty(Mission mission)
    {
        float difficulty = 0;
        // Dictionary<int, int> tankCounts = [];

        foreach (var tank in mission.Tanks) {
            if (tank.IsPlayer) continue;

            //if (!tankCounts.TryGetValue(tank.AiTier, out int value)) tankCounts.Add(tank.AiTier, 1);
            //else tankCounts[tank.AiTier] = ++value;

            difficulty += TankDiffs[tank.AiTier];
        }

        foreach (var block in mission.Blocks) {
            // TODO: based on this, have a list of defaults per-block type.
            if (block.Type != BlockID.Hole || block.Type == BlockID.Teleporter)
                difficulty *= 0.985f;
        }

        return difficulty;
    }
}
