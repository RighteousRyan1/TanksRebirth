﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems;

public record Difficulties
{
    public static Dictionary<string, bool> Types = new()
    {
        ["TanksAreCalculators"] = false,
        ["PieFactory"] = false,
        ["UltraMines"] = false,
        ["BulletHell"] = false,
        ["AllInvisible"] = false,
        ["AllStationary"] = false,
        ["AllHoming"] = false,
        ["Armored"] = false,
        ["BumpUp"] = false,
        ["Monochrome"] = false,
        ["InfiniteLives"] = false,
        ["MasterModBuff"] = false,
        ["MarbleModBuff"] = false,
        ["MachineGuns"] = false,
        ["RandomizedTanks"] = false,
        ["ThunderMode"] = false,
        ["ThirdPerson"] = false,
        ["AiCompanion"] = false,
        ["Shotguns"] = false,
        ["Predictions"] = false,
        ["RandomPlayer"] = false,
        ["BulletBlocking"] = false,
        ["FFA"] = false,
        ["LanternMode"] = false
    };
    public static TankTemplate[] HijackTanks(TankTemplate[] tanks) {
        for (int i = 0; i < tanks.Length; i++) {
            var t = tanks[i];
            if (t.IsPlayer)
                continue;

            var newTemplate = t;

            newTemplate.AiTier = GameHandler.GameRand.Next(MainMenu.RandomTanksLower, MainMenu.RandomTanksUpper + 1);
            tanks[i] = newTemplate;
        }
        return tanks;
    }
    public static Mission Flip(Mission mission, bool x = false, bool y = false) {
        if (!(x && y))
            return mission;

        var newMission = mission;

        var tanks = newMission.Tanks;
        var blocks = newMission.Blocks;

        var tanksWithPlacements = new Dictionary<TankTemplate, PlacementSquare>();
        var blocksWithPlacements = new Dictionary<BlockTemplate, PlacementSquare>();

        PlacementSquare.InitializeLevelEditorSquares();

        for (int i = 0; i < tanks.Length; i++) {
            // bro :sob:
            tanksWithPlacements[tanks[i]] = PlacementSquare.Placements.First(x => Vector2.Distance(x.Position.FlattenZ(), tanks[i].Position) < 5);
        }
        for (int i = 0; i < blocks.Length; i++) {
            blocksWithPlacements[blocks[i]] = PlacementSquare.Placements.First(x => Vector2.Distance(x.Position.FlattenZ(), blocks[i].Position) < 5);
        }

        // TODO: this
        // MaxX - PosX = FlipX
        // MaxY - PosY = FlipY
        if (x) {
            for (int i = 0; i < tanks.Length; i++) {

            }
            for (int i = 0; i < blocks.Length; i++) {

            }
        }
        if (y) {
            for (int i = 0; i < tanks.Length; i++) {

            }
            for (int i = 0; i < blocks.Length; i++) {

            }
        }
        return newMission;
    }
}
