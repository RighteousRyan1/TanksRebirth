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
        public const int LevelEditorVersion = 1;

        public static readonly byte[] FileHeader = { 84, 65, 78, 75 };

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
        public static void SaveMissionFile(string name, string campaignName)
        {
            string root; 
            if (campaignName is not null)
                root = Path.Combine(TankGame.SaveDirectory, "Campaigns", campaignName);
            else
                root = Path.Combine(TankGame.SaveDirectory, "Missions");
            var path = Path.Combine(root, name + ".mission");
            Directory.CreateDirectory(root);

            using var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite));

            /* File Order / Format
             * 1) File Header (TANK in ASCII) (byte[])
             * 2) Level Editor version (to check if older levels might cause anomalies!)
             * 3) Name (string)
             * 
             * 4) Total Tanks Used (int)
             * 
             * 5) Storing of Tanks (their respective templates)
             *  - IsPlayer (bool)
             *  - X (float)
             *  - Y (float)
             *  - Rotation (float)
             *  - AiType (byte) - should be as default if it's a player.
             *  - PlayerType (byte) - should be as default if it's an AI.
             *  - Team (byte)
             *  
             * 6) Total Blocks Used (int)
             *  
             * 7) Storing of Blocks (their respective templates)
             *  - Type (byte)
             *  - Stack (sbyte)
             *  - X (float)
             *  - Y (float)
             */

            writer.Write(FileHeader);
            writer.Write(LevelEditorVersion);
            writer.Write(name);

            int totalTanks = GameHandler.AllTanks.Count(x => x is not null && !x.Dead);
            writer.Write(totalTanks);
            for (int i = 0; i < GameHandler.AllTanks.Length; i++)
            {
                var tank = GameHandler.AllTanks[i];
                if (tank is not null && !tank.Dead)
                {
                    var temp = new TankTemplate();
                    if (tank is AITank ai)
                        temp.AiTier = ai.tier;
                    else if (tank is PlayerTank player)
                        temp.PlayerType = player.PlayerType;

                    temp.IsPlayer = tank is PlayerTank;
                    temp.Position = tank.Position;
                    temp.Rotation = tank.TankRotation;
                    temp.Team = tank.Team;

                    writer.Write(temp.IsPlayer);
                    writer.Write(temp.Position.X);
                    writer.Write(temp.Position.Y);
                    writer.Write(temp.Rotation);
                    writer.Write((byte)temp.AiTier);
                    writer.Write((byte)temp.PlayerType);
                    writer.Write((byte)temp.Team);
                }
            }

            int totalBlocks = Block.AllBlocks.Count(x => x is not null);
            writer.Write(totalBlocks);
            for (int i = 0; i < Block.AllBlocks.Length; i++)
            {
                var block = Block.AllBlocks[i];
                if (block is not null)
                {
                    var temp = new BlockTemplate
                    {
                        Type = block.Type,
                        Stack = block.Stack,
                        Position = block.Position,
                    };

                    writer.Write((byte)temp.Type);
                    writer.Write(temp.Stack);
                    writer.Write(temp.Position.X);
                    writer.Write(temp.Position.Y);
                }
            }


            ChatSystem.SendMessage($"Saved mission with {totalTanks} tank(s) and {totalBlocks} block(s).", Color.Lime);

            if (File.Exists(path))
            {
                GameHandler.ClientLog.Write($"Overwrote \"{name}.mission\" in map save path.", LogType.Info);
                return;
            }
            GameHandler.ClientLog.Write($"Saved stage file \"{name}.mission\" in map save path.", LogType.Info);
        }
        // TODO: maybe ref https://github.com/RighteousRyan1/BaselessJumping/blob/master/GameContent/Stage.cs#L34 (cuno abandoned the project)

        public static Mission LoadMissionFile(string missionName, string campaignName)
        {
            string root;
            if (campaignName is not null)
                root = Path.Combine(TankGame.SaveDirectory, "Campaigns", campaignName);
            else
                root = Path.Combine(TankGame.SaveDirectory, "Missions");
            var path = Path.Combine(root, missionName + ".mission");

            Directory.CreateDirectory(root);

            if (!File.Exists(path))
            {
                ChatSystem.SendMessage($"Mission not found in file system. Aborting.", Color.Red);
                return default;
            }

            List<TankTemplate> tanks = new();
            List<BlockTemplate> blocks = new();

            using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));

            var header = reader.ReadBytes(4);

            if (!header.SequenceEqual(FileHeader))
                throw new FileLoadException($"The byte header of this file does not match what this game expects! File name = \"{path}\"");

            var version = reader.ReadInt32();
            var name = reader.ReadString();

            var totalTanks = reader.ReadInt32();

            for (int i = 0; i < totalTanks; i++)
            {
                var isPlayer = reader.ReadBoolean();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var rotation = reader.ReadSingle();
                var tier = reader.ReadByte();
                var pType = reader.ReadByte();
                var team = reader.ReadByte();

                tanks.Add(new()
                {
                    IsPlayer = isPlayer,
                    Position = new(x, y),
                    Rotation = rotation,
                    AiTier = (TankTier)tier,
                    PlayerType = (PlayerType)pType,
                    Team = (Team)team
                });
            }

            var totalBlocks = reader.ReadInt32();

            for (int i = 0; i < totalBlocks; i++)
            {
                var type = reader.ReadByte();
                var stack = reader.ReadSByte();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();

                blocks.Add(new()
                {
                    Type = (Block.BlockType)type,
                    Stack = stack,
                    Position = new(x, y),
                });
            }

            var mission = new Mission(tanks.ToArray(), blocks.ToArray());

            ChatSystem.SendMessage($"Loaded mission with {tanks.Count} tank(s) and {blocks.Count} block(s).", Color.Lime);

            return mission;
        }
    }

    public record struct Mission
    {
        public string Name { get; set; }
        /// <summary>The <see cref="Tank"/>s that will be spawned.</summary>
        public TankTemplate[] Tanks { get; }

        /// <summary>The obstacles in the <see cref="Mission"/>.</summary>
        public BlockTemplate[] Blocks { get; }

        public Mission(TankTemplate[] tanks, BlockTemplate[] obstacles)
        {
            Name = "N/A";

            sbyte cBlue = 0;
            sbyte cRed = 0;

            /*if (tanks.Any(tnk => tnk is PlayerTank && (tnk as PlayerTank).PlayerType == Enums.PlayerType.Blue))
                cBlue++;

            if (tanks.Any(tnk => tnk is PlayerTank && (tnk as PlayerTank).PlayerType == Enums.PlayerType.Red))
                cRed++;*/

            if (cBlue > 1 || cRed > 1)
                GameHandler.ClientLog.Write("Only one color allowed per-player.", LogType.Error, true);

            if (cBlue + cRed > 2)
                GameHandler.ClientLog.Write("As of now, only 2 local players are supported.", LogType.Error, true);

            Tanks = tanks;
            Blocks = obstacles;
        }
    }
}
