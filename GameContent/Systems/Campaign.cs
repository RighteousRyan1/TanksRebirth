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
using WiiPlayTanksRemake.Internals.Common.Framework;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    /// <summary>A campaign for players to play on with <see cref="AITank"/>s, or even <see cref="PlayerTank"/>s if supported.</summary>
    public class Campaign
    {
        /// <summary>The maximum allowed missions in a campaign.</summary>
        public const int MAX_MISSIONS = 100;
        /// <summary>Returns the names of campaigns in the user's <c>Campaigns/</c> directory.</summary>
        public static string[] GetCampaignNames()
            => IOUtils.GetSubFolders(Path.Combine(TankGame.SaveDirectory, "Campaigns"), true);
        public Mission[] CachedMissions { get; set; } = new Mission[MAX_MISSIONS];
        public Mission CurrentMission { get; private set; }
        public Mission LoadedMission { get; private set; }
        public int CurrentMissionId { get; private set; }

        public void LoadMission(Mission mission)
        {
            if (string.IsNullOrEmpty(mission.Name))
                return;
            LoadedMission = mission;
        }
        public void LoadMission(int id)
            => LoadedMission = CachedMissions[id];

        /// <summary>Loads an array of <see cref="Mission"/>s into memory.</summary>
        public void LoadMissionsToCache(params Mission[] missions)
        {
            var list = CachedMissions.ToList();

            list.AddRange(missions);

            CachedMissions = list.ToArray();
        }

        /// <summary>Loads the next mission in the <see cref="Campaign"/>.</summary>
        public void LoadNextMission()
        {
            if (CurrentMissionId + 1 >= MAX_MISSIONS || CurrentMissionId + 1 >= CachedMissions.Length)
            {
                GameHandler.ClientLog.Write($"CachedMissions[{CurrentMissionId + 1}] is not existent.", LogType.Warn);
                return;
            }
            CurrentMissionId++;

            LoadedMission = CachedMissions[CurrentMissionId];
        }

        /// <summary>Sets up the <see cref="Mission"/> that is loaded.</summary>
        /// <param name="spawnNewSet">If true, will spawn all tanks as if it's the first time the player(s) has/have entered this mission.</param>
        public void SetupLoadedMission(bool spawnNewSet)
        {
            if (spawnNewSet)
            {
                for (int a = 0; a < GameHandler.AllTanks.Length; a++)
                    GameHandler.AllTanks[a]?.Remove();
                for (int i = 0; i < LoadedMission.Tanks.Length; i++)
                {
                    var template = LoadedMission.Tanks[i];

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

                for (int b = 0; b < LoadedMission.Blocks.Length; b++)
                {
                    var cube = LoadedMission.Blocks[b];

                    var c = cube.GetBlock();

                    var foundPlacement = PlacementSquare.Placements.First(placement => placement.Position == c.Position3D);
                    foundPlacement.CurrentBlockId = c.Id;
                }
            }

            CurrentMission = LoadedMission;
            GameHandler.ClientLog.Write($"Loaded mission '{LoadedMission.Name}' with {LoadedMission.Tanks.Length} tanks and {LoadedMission.Blocks.Length} obstacles.", LogType.Info);
        }
        /// <summary>
        /// Loads missions from inside the <paramref name="campaignName"/> folder to memory.
        /// </summary>
        /// <param name="campaignName">The name of the campaign folder to load files from.</param>
        /// <param name="autoSetLoadedMission">Sets the currently loaded mission to the first mission loaded from this folder.</param>
        /// <exception cref="FileLoadException"></exception>
        public Mission[] LoadFromFolder(string campaignName, bool autoSetLoadedMission)
        {
            var root = Path.Combine(TankGame.SaveDirectory, "Campaigns");
            var path = Path.Combine(root, campaignName);
            Directory.CreateDirectory(root);
            if (!Directory.Exists(path))
            {
                GameHandler.ClientLog.Write($"Could not find a campaign folder with name {campaignName}. Aborting folder load...", LogType.Warn);
                return default;
            }

            List<Mission> missions = new();

            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                missions.Add(Mission.Load(file, ""));
                // campaignName argument is null since we are loading from the campaign folder anyway. 

                CachedMissions = missions.ToArray();
            }

            if (autoSetLoadedMission)
                LoadedMission = CachedMissions[0];

            return missions.ToArray();
        }

        /*public int LoadRandomizedMission(Range<int> missionRange, TankTier highestTier = TankTier.None, int highestCount = 0)
        {
            if (missionRange.Max >= CachedMissions.Length)
                missionRange.Max = CachedMissions.Length - 1;

            int num = GameHandler.GameRand.Next(missionRange.Min, missionRange.Max);

            var mission = CachedMissions[num];

            for (int i = 0; i < mission.Tanks.Length; i++)
            {
                var tnk = mission.Tanks[i];

                if (!tnk.IsPlayer)
                {
                    tnk.AiTier = TankTier.Random;
                    tnk.RandomizeRange = new();
                }
            }

            return num;
        }*/
        // Considering making all 100 campaigns unique...
    }
}
