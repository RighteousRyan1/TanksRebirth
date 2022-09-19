using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Graphics;
using TanksRebirth.Internals.Common.IO;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems
{
    /// <summary>A campaign for players to play on with <see cref="AITank"/>s, or even <see cref="PlayerTank"/>s if supported.</summary>
    public class Campaign
    {
        public delegate void MissionLoadDelegate(ref Tank[] tanks, ref Block[] blocks);
        public static event MissionLoadDelegate OnMissionLoad;

        /// <summary>The maximum allowed missions in a campaign.</summary>
        public const int MAX_MISSIONS = 100;
        /// <summary>Returns the names of campaigns in the user's <c>Campaigns/</c> directory.</summary>
        public static string[] GetCampaignNames()
            => IOUtils.GetSubFolders(Path.Combine(TankGame.SaveDirectory, "Campaigns"), true);
        public Mission[] CachedMissions = new Mission[MAX_MISSIONS];
        public Mission CurrentMission { get; private set; }
        public Mission LoadedMission { get; private set; }
        public int CurrentMissionId { get; private set; }

        public CampaignMetaData MetaData;

        public Campaign()
        {
            MetaData = CampaignMetaData.GetDefault();
        }

        public void LoadMission(Mission mission)
        {
            if (string.IsNullOrEmpty(mission.Name))
                return;
            LoadedMission = mission;
        }
        public void LoadMission(int id)
        {
            LoadedMission = CachedMissions[id];

            CurrentMissionId = id;
            if (LoadedMission.Tanks != null)
            {
                TrackedSpawnPoints = new (Vector2, bool)[LoadedMission.Tanks.Length];
                for (int i = 0; i < LoadedMission.Tanks.Length; i++)
                {
                    TrackedSpawnPoints[i].Item1 = LoadedMission.Tanks[i].Position;
                    TrackedSpawnPoints[i].Item2 = true;
                }
            }
        }

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

            TrackedSpawnPoints = new (Vector2, bool)[LoadedMission.Tanks.Length];
            for (int i = 0; i < LoadedMission.Tanks.Length; i++)
            {
                TrackedSpawnPoints[i].Item1 = LoadedMission.Tanks[i].Position;
                TrackedSpawnPoints[i].Item2 = true;
            }
            // run line 120 and 121 in each when i get back
        }

        public (Vector2, bool)[] TrackedSpawnPoints; // position of spawn, alive

        /// <summary>Sets up the <see cref="Mission"/> that is loaded.</summary>
        /// <param name="spawnNewSet">If true, will spawn all tanks as if it's the first time the player(s) has/have entered this mission.</param>
        public void SetupLoadedMission(bool spawnNewSet)
        {
            PlacementSquare.ResetSquares();
            //foreach (var body in Tank.CollisionsWorld.BodyList)
            //Tank.CollisionsWorld.Remove(body);
            for (int a = 0; a < GameHandler.AllTanks.Length; a++)
                GameHandler.AllTanks[a]?.Remove();
            for (int i = 0; i < LoadedMission.Tanks.Length; i++)
            {
                var template = LoadedMission.Tanks[i];

                if (spawnNewSet) {
                    if (GameProperties.ShouldMissionsProgress) {
                        TrackedSpawnPoints[i].Item1 = LoadedMission.Tanks[i].Position;
                        TrackedSpawnPoints[i].Item2 = true;
                    }
                }
                if (!template.IsPlayer)
                {
                    if (TrackedSpawnPoints[i].Item2)
                    {
                        var tank = template.GetAiTank();

                        tank.Position = template.Position;
                        tank.TankRotation = template.Rotation;
                        tank.TargetTankRotation = -template.Rotation + MathHelper.Pi;
                        tank.TurretRotation = template.Rotation;
                        tank.Dead = false;
                        tank.Team = template.Team;
                        if (GameProperties.ShouldMissionsProgress)
                        {
                            // TODO: fix the leak, dumbass
                            tank.OnDestroy += (sender, e) =>
                            {
                                TrackedSpawnPoints[Array.IndexOf(TrackedSpawnPoints, TrackedSpawnPoints.First(pos => pos.Item1 == template.Position))].Item2 = false; // make sure the tank is not spawned again
                            };
                        }
                        var placement = PlacementSquare.Placements.FindIndex(place => place.Position == tank.Position3D);

                        if (placement > -1)
                        {
                            PlacementSquare.Placements[placement].TankId = tank.WorldId;
                            PlacementSquare.Placements[placement].HasBlock = false;
                        }
                    }
                }
                else
                {
                    var tank = template.GetPlayerTank();

                    tank.Position = template.Position;
                    tank.TankRotation = template.Rotation;
                    tank.Dead = false;
                    tank.Team = template.Team;

                    if (Difficulties.Types["AiCompanion"])
                    {
                        tank.Team = TeamID.Magenta;
                        var tnk = new AITank(TankID.Black)
                        {
                            Position = template.Position,
                            Team = tank.Team,
                            TankRotation = template.Rotation,
                            TargetTankRotation = template.Rotation - MathHelper.Pi,
                            TurretRotation = -template.Rotation,
                            Dead = false
                        };
                        tnk.Body.Position = template.Position;

                        tnk.Swap(AITank.PickRandomTier());
                    }
                    var placement = PlacementSquare.Placements.FindIndex(place => place.Position == tank.Position3D);

                    if (placement > -1)
                    {
                        PlacementSquare.Placements[placement].TankId = tank.WorldId;
                        PlacementSquare.Placements[placement].HasBlock = false;
                    }

                    if (NetPlay.IsClientMatched(tank.PlayerId))
                        PlayerTank.MyTeam = tank.Team;
                }
            }

            for (int a = 0; a < Block.AllBlocks.Length; a++)
                Block.AllBlocks[a]?.Remove();

            for (int p = 0; p < PlacementSquare.Placements.Count; p++)
                PlacementSquare.Placements[p].BlockId = -1;

            for (int b = 0; b < LoadedMission.Blocks.Length; b++)
            {
                var template = LoadedMission.Blocks[b];

                var block = template.GetBlock();

                var placement = PlacementSquare.Placements.FindIndex(place => place.Position == block.Position3D);
                if (placement > -1)
                {
                    PlacementSquare.Placements[placement].BlockId = block.Id;
                    PlacementSquare.Placements[placement].HasBlock = true;
                }
            }

            CurrentMission = LoadedMission;
            GameHandler.ClientLog.Write($"Loaded mission '{LoadedMission.Name}' with {LoadedMission.Tanks.Length} tanks and {LoadedMission.Blocks.Length} obstacles.", LogType.Info);

            OnMissionLoad?.Invoke(ref GameHandler.AllTanks, ref Block.AllBlocks);
        }
        /// <summary>
        /// Loads missions from inside the <paramref name="campaignName"/> folder to memory.
        /// </summary>
        /// <param name="campaignName">The name of the campaign folder to load files from.</param>
        /// <param name="autoSetLoadedMission">Sets the currently loaded mission to the first mission loaded from this folder.</param>
        /// <exception cref="FileLoadException"></exception>
        public static Campaign LoadFromFolder(string campaignName, bool autoSetLoadedMission)
        {
            Campaign campaign = new();

            var root = Path.Combine(TankGame.SaveDirectory, "Campaigns");
            var path = Path.Combine(root, campaignName);
            Directory.CreateDirectory(root);
            if (!Directory.Exists(path))
            {
                GameHandler.ClientLog.Write($"Could not find a campaign folder with name {campaignName}. Aborting folder load...", LogType.Warn);
                return default;
            }

            CampaignMetaData properties = CampaignMetaData.Get(path, "_properties.json");

            List<Mission> missions = new();

            var files = Directory.GetFiles(path).Where(file => file.EndsWith(".mission")).ToArray();

            foreach (var file in files)
            {
                missions.Add(Mission.Load(file, ""));
                // campaignName argument is empty since we are loading from the campaign folder anyway. 

                campaign.CachedMissions = missions.ToArray();
            }

            if (autoSetLoadedMission)
            {
                campaign.LoadMission(0); // first mission in campaign
                campaign.TrackedSpawnPoints = new (Vector2, bool)[campaign.LoadedMission.Tanks.Length];
                PlayerTank.StartingLives = properties.StartingLives;
            }

            campaign.MetaData = properties;


            return campaign;
        }

        /// <summary>Does not work yet.</summary>
        public static void Save(string fileName, Campaign campaign)
        {
            using var writer = new BinaryWriter(File.Open(fileName.Contains(".campaign") ? fileName : fileName + ".campaign", FileMode.OpenOrCreate));

            writer.Write(TankGame.LevelFileHeader);
            writer.Write(TankGame.LevelEditorVersion);

            int totalMissions = campaign.CachedMissions.Count(m => m != default);
            writer.Write(totalMissions);

            writer.Write(campaign.MetaData.Name);
            writer.Write(campaign.MetaData.Description);
            writer.Write(campaign.MetaData.Author);
            writer.Write(campaign.MetaData.Tags.Length);
            Array.ForEach(campaign.MetaData.Tags, tag => writer.Write(tag));
            writer.Write(campaign.MetaData.StartingLives);
            writer.Write(campaign.MetaData.ExtraLivesMissions.Length);
            Array.ForEach(campaign.MetaData.ExtraLivesMissions, id => writer.Write(id));
            writer.Write(campaign.MetaData.Version);
            writer.Write(campaign.MetaData.HasMajorVictory);
            writer.Write(campaign.MetaData.MissionStripColor);
            writer.Write(campaign.MetaData.BackgroundColor);

            for (int i = 0; i < totalMissions; i++)
                Mission.WriteContentsOf(writer, campaign.CachedMissions[i]);

            ChatSystem.SendMessage($"Saved campaign with {totalMissions} missions.", Color.Lime);
        }

        public static Campaign Load(string fileName)
        {
            Campaign campaign = new();

            using var reader = new BinaryReader(File.Open(Path.Combine(TankGame.SaveDirectory, fileName), FileMode.Open, FileAccess.Read));

            var header = reader.ReadBytes(4);
            if (!header.SequenceEqual(TankGame.LevelFileHeader))
                throw new FileLoadException($"The byte header of this file does not match what this game expects! File name = \"{fileName}\"");
            var editorVersion = reader.ReadInt32();
            if (editorVersion < 2)
                throw new FileLoadException($"Cannot load a campaign at this level editor version! File name = \"{fileName}\"");
            // first available version.
            if (editorVersion == 2) {
                var totalMissions = reader.ReadInt32();

                campaign.CachedMissions = new Mission[totalMissions];

                campaign.MetaData.Name = reader.ReadString();
                campaign.MetaData.Description = reader.ReadString();
                campaign.MetaData.Author = reader.ReadString();
                campaign.MetaData.Tags = new string[reader.ReadInt32()];
                for (int j = 0; j < campaign.MetaData.Tags.Length; j++)
                    campaign.MetaData.Tags[j] = reader.ReadString();
                campaign.MetaData.StartingLives = reader.ReadInt32();
                campaign.MetaData.ExtraLivesMissions = new int[reader.ReadInt32()];
                for (int j = 0; j < campaign.MetaData.ExtraLivesMissions.Length; j++)
                    campaign.MetaData.ExtraLivesMissions[j] = reader.ReadInt32();
                campaign.MetaData.Version = reader.ReadString();
                campaign.MetaData.HasMajorVictory = reader.ReadBoolean();
                campaign.MetaData.MissionStripColor = reader.ReadColor();
                campaign.MetaData.BackgroundColor = reader.ReadColor();

                for (int i = 0; i < totalMissions; i++)
                {
                    // TODO: do for loop, load each.

                    campaign.CachedMissions[i] = Mission.Read(reader);
                }
            }
            return campaign;
        }
        public struct CampaignMetaData {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Author { get; set; }
            public string Version { get; set; }
            public string[] Tags { get; set; }
            public bool HasMajorVictory { get; set; }

            public int[] ExtraLivesMissions { get; set; }
            public int StartingLives { get; set; }

            public UnpackedColor BackgroundColor { get; set; }
            public UnpackedColor MissionStripColor { get; set; }

            // TODO: support for custom mission results panel color?

            public static CampaignMetaData Get(string path, string fileName)
            {
                var properties = CampaignMetaData.GetDefault();

                var file = Path.Combine(path, fileName);

                JsonHandler<CampaignMetaData> handler = new(properties, file);

                if (!File.Exists(file))
                {
                    handler.Serialize(new() { WriteIndented = true }, true);
                    return properties;
                }

                properties = handler.Deserialize();

                return properties;
            }

            public static CampaignMetaData GetDefault()
            {
                return new()
                {
                    Name = "Unnamed",
                    Description = "No description",
                    Author = "Unknown",
                    Version = "0.0.0.0",
                    Tags = new string[] { "N/A" },
                    ExtraLivesMissions = Array.Empty<int>(),
                    StartingLives = 3,
                    BackgroundColor = IntermissionSystem.DefaultBackgroundColor,
                    MissionStripColor = IntermissionSystem.DefaultStripColor,
                    HasMajorVictory = false
                };
            }
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
