using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TanksRebirth.Enums;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Systems
{
    public record struct Mission
    {
        /// <summary>The name of this <see cref="Mission"/>.</summary>
        public string Name { get; set; } = "No Name";

        /// <summary>The popup text that will display at the beginning of this <see cref="Mission"/>.</summary>
        public string Note { get; set; } = string.Empty;
        /// <summary>The <see cref="Tank"/>s that will be spawned upon mission load.</summary>
        public TankTemplate[] Tanks { get; }

        /// <summary>The obstacles in the <see cref="Mission"/>.</summary>
        public BlockTemplate[] Blocks { get; }

        /// <summary>
        /// Construct a mission. Should generally never be called by the user unless you want to use a lot of time figuring things out.
        /// </summary>
        /// <param name="tanks"></param>
        /// <param name="obstacles"></param>
        public Mission(TankTemplate[] tanks, BlockTemplate[] obstacles)
        {
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
        /// <summary>
        /// Creates a <see cref="Mission"/> instance from the current placement of everything.
        /// </summary>
        /// <returns>The mission that is currently active, or created.</returns>
        public static Mission GetCurrent()
        {
            List<TankTemplate> tanks = new();
            List<BlockTemplate> blocks = new();

            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null)
                {
                    var tmp = new TankTemplate
                    {
                        IsPlayer = tank is PlayerTank,
                        Position = tank.Position,
                        Rotation = tank.TankRotation,
                        Team = tank.Team,
                    };

                    if (tmp.IsPlayer)
                        tmp.PlayerType = (tank as PlayerTank).PlayerType;
                    else
                        tmp.AiTier = (tank as AITank).Tier;

                    tanks.Add(tmp);
                }
            }
            foreach (var block in Block.AllBlocks)
            {
                if (block is not null)
                {
                    blocks.Add(new()
                    {
                        Position = block.Position,
                        Stack = block.Stack,
                        TpLink = block.TpLink,
                        Type = block.Type,
                    });
                }
            }

            return new(tanks.ToArray(), blocks.ToArray());
        }
        /// <summary>
        /// Loads a <see cref="Mission"/> and instantly applies it to the game field.
        /// </summary>
        /// <param name="mission">The mission instance to load.</param>
        public static void LoadDirectly(Mission mission)
        {
            foreach (var block in Block.AllBlocks)
                block?.Remove();
            foreach (var mine in Mine.AllMines)
                mine?.Remove();
            foreach (var shell in Shell.AllShells)
                shell?.Remove();
            foreach (var tank in GameHandler.AllTanks)
                tank?.Remove();
            for (int i = 0; i < mission.Tanks.Length; i++) {
                var tnk = mission.Tanks[i];
                var tank = tnk.GetTank();
                tank.TargetTankRotation = -tank.TankRotation - MathHelper.Pi;
            }
            for (int i = 0; i < mission.Blocks.Length; i++) {
                var block = mission.Blocks[i];

                block.GetBlock();
            }
        }
        /// <summary>
        /// Saves a mission as a <c>.mission</c> file for reading later.
        /// </summary>
        /// <param name="name">The name of the mission to save.</param>
        /// <param name="campaignName">The mission folder to save this to. If the directory does not exist, one will be created.<para></para>If null, saves to the <c>Missions/</c> directory by default.</param>
        /// <param name="useDefaultPaths">If true, the path for the location of the mission file will be located in <paramref name="campaignName"/></param>
        /// <param name="fileName">If not null, will use this instead of <paramref name="name"/> for the file name.</param>
        public static void Save(string name, string campaignName, bool useDefaultPaths = true, string fileName = null)
        {
            string root;
            if (useDefaultPaths)
            {
                if (campaignName is not null)
                    root = Path.Combine(TankGame.SaveDirectory, "Campaigns", campaignName);
                else
                    root = Path.Combine(TankGame.SaveDirectory, "Missions");
            }
            else
                root = campaignName;

            var path = Path.Combine(root, (fileName == null ? name : fileName) + ".mission");
            if (useDefaultPaths)
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
             *  - TpLink (sbyte) (VERSION 2 or GREATER)
             *  
             *  8) Extras
             *   - Note (string) (VERSION 3 OR GREATER) (NOT IMPLEMENTED YET)
             */

            writer.Write(TankGame.LevelFileHeader);
            writer.Write(TankGame.LevelEditorVersion);
            writer.Write(name);

            int totalTanks = GameHandler.AllTanks.Count(tnk => tnk is not null && !tnk.Dead);
            writer.Write(totalTanks);
            for (int i = 0; i < GameHandler.AllTanks.Length; i++)
            {
                var tank = GameHandler.AllTanks[i];
                if (tank is not null && !tank.Dead)
                {
                    var temp = new TankTemplate();
                    if (tank is AITank ai)
                        temp.AiTier = ai.Tier;
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
                        TpLink = block.TpLink
                    };

                    writer.Write((byte)temp.Type);
                    writer.Write(temp.Stack);
                    writer.Write(temp.Position.X);
                    writer.Write(temp.Position.Y);
                    writer.Write(temp.TpLink);
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

        /// <summary>
        /// Loads a mission from a <c>.mission</c> file and returns it for code use.
        /// </summary>
        /// <param name="missionName">The name of the mission.</param>
        /// <param name="campaignName">The campaign to search for the mission in. If null, searches the <c>Missions/</c> directory.</param>
        /// <returns>The successfully loaded mission.</returns>
        /// <exception cref="FileLoadException"></exception>
        public static Mission Load(string missionName, string campaignName)
        {
            string root;
            if (campaignName is not null)
                root = Path.Combine(TankGame.SaveDirectory, "Campaigns", campaignName);
            else
                root = Path.Combine(TankGame.SaveDirectory, "Missions");
            var path = missionName.EndsWith(".mission") ? Path.Combine(root, missionName) : Path.Combine(root, missionName + ".mission");

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

            if (!header.SequenceEqual(TankGame.LevelFileHeader))
                throw new FileLoadException($"The byte header of this file does not match what this game expects! File name = \"{path}\"");

            var version = reader.ReadInt32();

           // if (version != TankGame.LevelEditorVersion)
                //ChatSystem.SendMessage($"Warning: This level was saved with a different version of the level editor. It may not work correctly.", Color.Yellow);

            Mission mission = new();

            if (version == 1)
            {
                var name = reader.ReadString();

                var totalTanks = reader.ReadInt32();

                for (int i = 0; i < totalTanks; i++)
                {
                    var isPlayer = reader.ReadBoolean();
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    var rotation = -reader.ReadSingle(); // i genuinely hate having to make this negative :(
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
                        Team = (TankTeam)team
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

                mission = new Mission(tanks.ToArray(), blocks.ToArray())
                {
                    Name = name
                };
            }
            else if (version == 2)
            {
                var name = reader.ReadString();

                var totalTanks = reader.ReadInt32();

                for (int i = 0; i < totalTanks; i++)
                {
                    var isPlayer = reader.ReadBoolean();
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    var rotation = -reader.ReadSingle(); // i genuinely hate having to make this negative :(
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
                        Team = (TankTeam)team
                    });
                }

                var totalBlocks = reader.ReadInt32();

                for (int i = 0; i < totalBlocks; i++)
                {
                    var type = reader.ReadByte();
                    var stack = reader.ReadSByte();
                    var x = reader.ReadSingle();
                    var y = reader.ReadSingle();
                    var link = reader.ReadSByte();

                    blocks.Add(new()
                    {
                        Type = (Block.BlockType)type,
                        Stack = stack,
                        Position = new(x, y),
                        TpLink = link
                    });
                }

                mission = new Mission(tanks.ToArray(), blocks.ToArray())
                {
                    Name = name
                };
            }

            // ChatSystem.SendMessage($"Loaded mission with {tanks.Count} tank(s) and {blocks.Count} block(s).", Color.Lime);

            return mission;
        }
    }
}