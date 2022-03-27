using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiPlayTanksRemake.Enums;
using WiiPlayTanksRemake.GameContent.Systems.Coordinates;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    /// <summary>A campaign for players to play on with <see cref="AITank"/>s, or even <see cref="PlayerTank"/>s if supported.</summary>
    public class Campaign
    {
        public static string[] GetCampaignNames()
            => IOUtils.GetSubFolders(Path.Combine(TankGame.SaveDirectory, "Campaigns"), true);
        public Mission[] CachedMissions { get; set; } = new Mission[100];
        public Mission CurrentMission { get; private set; }

        public int CurrentMissionId { get; private set; }

        public void LoadMission(Mission mission)
        {
            if (string.IsNullOrEmpty(mission.Name))
                return;
            CurrentMission = mission;
        }
        public void LoadMission(int id)
            => CurrentMission = CachedMissions[id];

        /// <summary>Loads an array of <see cref="Mission"/>s into the cache.</summary>
        public void LoadMissionsToCache(params Mission[] missions)
        {
            var list = CachedMissions.ToList();

            list.AddRange(missions);

            CachedMissions = list.ToArray();
        }

        /// <summary>Loads the next mission in the <see cref="Campaign"/>.</summary>
        public void LoadNextMission()
        {
            if (CachedMissions[++CurrentMissionId].Tanks is null || ++CurrentMissionId >= 100)
            {
                GameHandler.ClientLog.Write($"CachedMissions[{++CurrentMissionId}] is not existent.", LogType.Warn);
                return;
            }

            CurrentMission = CachedMissions[++CurrentMissionId];
        }

        /// <summary>Sets up the <see cref="Mission"/> that is loaded.</summary>
        /// <param name="spawnNewSet">If true, will spawn all tanks as if it's the first time the player(s) has/have entered this mission.</param>
        public void SetupLoadedMission(bool spawnNewSet)
        {

            if (spawnNewSet)
            {
                for (int a = 0; a < GameHandler.AllTanks.Length; a++)
                    GameHandler.AllTanks[a]?.Remove();
                for (int i = 0; i < CurrentMission.Tanks.Length; i++)
                {
                    var template = CurrentMission.Tanks[i];

                    if (!template.IsPlayer)
                    {
                        var tank = template.GetAiTank();

                        tank.Position = template.Position;
                        tank.TankRotation = template.Rotation;
                        tank.TargetTankRotation = template.Rotation + MathHelper.Pi;
                        tank.TurretRotation = -template.Rotation;
                        tank.Dead = false;
                    }
                    else
                    {
                        var tank = template.GetPlayerTank();

                        tank.Position = template.Position;
                        tank.TankRotation = template.Rotation;
                        tank.Dead = false;
                    }
                }

                for (int a = 0; a < Block.AllBlocks.Length; a++)
                    Block.AllBlocks[a]?.Remove();

                for (int p = 0; p < PlacementSquare.Placements.Count; p++)
                    PlacementSquare.Placements[p].CurrentBlockId = -1;

                for (int b = 0; b < CurrentMission.Blocks.Length; b++)
                {
                    var cube = CurrentMission.Blocks[b];

                    var c = cube.GetBlock();

                    var foundPlacement = PlacementSquare.Placements.First(placement => placement.Position == c.Position3D);
                    foundPlacement.CurrentBlockId = c.Id;
                }
            }

            GameHandler.ClientLog.Write($"Loaded mission '{CurrentMission.Name}' with {CurrentMission.Tanks.Length} tanks and {CurrentMission.Blocks.Length} obstacles.", LogType.Info);
        }

        public static void LoadFromFolder(string campaignName)
        {
            var root = Path.Combine(TankGame.SaveDirectory, "Campaigns");
            var path = Path.Combine(root, campaignName);
            Directory.CreateDirectory(root);
            if (!Directory.Exists(path))
            {
                GameHandler.ClientLog.Write($"Could not find a campaign folder with name {campaignName}. Aborting folder load...", LogType.Warn);
                return;
            }
        }
    }
}
