using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems;

public record struct Mission
{
    /// <summary>The name of this <see cref="Mission"/>.</summary>
    public string Name { get; set; } = "No Name";

    /// <summary>The popup text that will display at the beginning of this <see cref="Mission"/>.</summary>
    public string Note { get; set; } = string.Empty;
    /// <summary>The <see cref="Tank"/>s that will be spawned upon mission load.</summary>
    public TankTemplate[] Tanks { get; set; }

    /// <summary>The obstacles in the <see cref="Mission"/>.</summary>
    public BlockTemplate[] Blocks { get; set; }

    public bool GrantsExtraLife { get; set; }

    /// <summary>
    /// Construct a mission. Should generally never be called by the user unless you want to use a lot of time figuring things out.
    /// </summary>
    /// <param name="tanks"></param>
    /// <param name="obstacles"></param>
    public Mission(TankTemplate[] tanks, BlockTemplate[] obstacles) {
        Tanks = tanks;
        Blocks = obstacles;
    }

    /// <summary>
    /// Creates a <see cref="Mission"/> instance from the current placement of everything.
    /// </summary>
    /// <returns>The mission that is currently active, or created.</returns>
    public static Mission GetCurrent(string? name = null, bool grantsLife = false) {
        const int roundingFactor = 5;
        List<TankTemplate> tanks = [];
        List<BlockTemplate> blocks = [];

        foreach (var tank in GameHandler.AllTanks) {
            if (tank is not null) {
                var tmp = new TankTemplate {
                    IsPlayer = tank is PlayerTank,
                    Position = tank.Position,
                    Rotation = MathF.Round(tank.ChassisRotation, roundingFactor),
                    Team = tank.Team,
                };

                if (tmp.IsPlayer)
                    tmp.PlayerType = (tank as PlayerTank).PlayerType;
                else
                    tmp.AiTier = (tank as AITank).AiTankType;

                tanks.Add(tmp);
            }
        }
        foreach (var block in Block.AllBlocks) {
            if (block is not null) {
                blocks.Add(new() {
                    Position = block.Position,
                    Stack = block.Stack,
                    TpLink = block.TpLink,
                    Type = block.Type,
                });
            }
        }

        return new([.. tanks], [.. blocks]) { Name = name, GrantsExtraLife = grantsLife };
    }

    /// <summary>
    /// Loads a <see cref="Mission"/> and instantly applies it to the game field.
    /// </summary>
    /// <param name="mission">The mission instance to load.</param>
    public static void LoadDirectly(Mission mission) {
        SceneManager.CleanupEntities();
        PlacementSquare.ResetSquares();
        SceneManager.CleanupScene();
        for (int i = 0; i < mission.Tanks.Length; i++) {
            var tnk = mission.Tanks[i];
            tnk.Rotation = tnk.Rotation; // Use setter for magical purposes.

            var tank = tnk.GetTank();

            var placement = PlacementSquare.GetFromClosest(tank.Position3D);

            if (placement is not null) {
                placement.TankId = tank.WorldId;
                placement.HasBlock = false;
                tank.Position = placement.Position.FlattenZ(); // BlockMapPosition.Convert2D(placement.RelativePosition);
            }

            // TODO: Find the root cause that causes us to manually have to add Math.PI to 1.57.
            // Explanation for hack fix: When rotating in the editor, we add Math.Pi / 2 to the rotation.
            // However for some unknown reason, when returning from test play, this value gets automatically increased to Math.Pi. With no reason.
            // This bug does not appear to manifest should Math.Pi + 1.57 be used. Potentially caused by some old, left over code somewhere in the past.
            // However trying to trail it down in this codebase would take much longer than this hack fix.
            // Ignore the warning posted by the code, it should probably work fine (tested it).
            // - Dottik

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            //if (float.Round(tnk.Rotation) == float.Round(1.57f))
            //tnk.Rotation += MathF.PI * 2;

            tank.ChassisRotation = MathF.Round(tnk.Rotation, 5);
            tank.DesiredChassisRotation = tank.ChassisRotation;
            tank.TurretRotation = -tank.ChassisRotation;
            if (tank is AITank aiTank)
                aiTank.TargetTurretRotation = tank.TurretRotation;
        }
        for (int i = 0; i < mission.Blocks.Length; i++) {
            var blockr = mission.Blocks[i];

            var block = blockr.GetBlock();

            var placement = PlacementSquare.Placements.FindIndex(place => Vector3.Distance(place.Position, block.Position3D) < Block.SIDE_LENGTH / 2);
            if (placement > -1) {
                PlacementSquare.Placements[placement].BlockId = block.Id;
                PlacementSquare.Placements[placement].HasBlock = true;
            }
        }
    }

    /// <summary>
    /// Saves a mission as a <c>.mission</c> file for reading later.
    /// </summary>
    /// <param name="path">The path to where the mission will be stored.</param>
    public readonly void Save(string path) {
        if (Path.GetExtension(path) == string.Empty)
            path += ".mission";

        using var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite));

        WriteToStream(writer);

        if (File.Exists(path)) {
            TankGame.ClientLog.Write($"Overwrote \"{Name}.mission\" in map save path.", LogType.Info);
            return;
        }
        TankGame.ClientLog.Write($"Saved mission file \"{Name}.mission\" in map save path.", LogType.Info);
    }

    // TODO: write is bonus mission to stream
    public readonly void WriteToStream(BinaryWriter writer) {
        /* File Order / Format
         * 1) File Header (TANK in ASCII) (byte[])
         * 2) Level Editor version (to check if older levels might cause anomalies!)
         * 3) Name (string)
         * 4) GrantsBonusLife (bool)
         *
         * 5) Total Tanks Used (int)
         *
         * 6) Storing of Tanks (their respective templates)
         *  - IsPlayer (bool)
         *  - X (float)
         *  - Y (float)
         *  - Rotation (float)
         *  - AiType (byte) - should be as default if it's a player.
         *  - PlayerType (byte) - should be as default if it's an AI.
         *  - Team (byte)
         *
         * 7) Total Blocks Used (int)
         *
         * 8) Storing of Blocks (their respective templates)
         *  - Type (byte)
         *  - Stack (sbyte)
         *  - X (float)
         *  - Y (float)
         *  - TpLink (sbyte) (VERSION 2 or GREATER)
         *
         *  9) Extras
         *   - Note (string) (NOT IMPLEMENTED YET)
         */

        writer.Write(LevelEditorUI.LevelFileHeader);
        writer.Write(LevelEditorUI.EDITOR_VERSION);
        writer.Write(Name);
        writer.Write(GrantsExtraLife);

        int totalTanks = Tanks.Length;
        writer.Write(totalTanks);

        for (int i = 0; i < totalTanks; i++) {
            var template = Tanks[i];

            writer.Write(template.IsPlayer);
            writer.Write(template.Position.X);
            writer.Write(template.Position.Y);
            writer.Write(template.Rotation);

            // THEORETICALLY if mods add 255 tank types then this is cooked
            writer.Write((byte)template.AiTier);
            writer.Write((byte)template.PlayerType);
            writer.Write((byte)template.Team);
        }

        int totalBlocks = Blocks.Length;
        writer.Write(totalBlocks);
        for (int i = 0; i < totalBlocks; i++) {
            var temp = Blocks[i];
            writer.Write((byte)temp.Type);
            writer.Write(temp.Stack);
            writer.Write(temp.Position.X);
            writer.Write(temp.Position.Y);
            writer.Write(temp.TpLink);
        }
        ChatSystem.SendMessage($"Saved mission with {totalTanks} tank(s) and {totalBlocks} block(s).", Color.Lime);
    }

    /// <summary>
    /// Loads a mission from a <c>.mission</c> file and returns it for code use.
    /// </summary>
    /// <param name="missionName">The name of the mission.</param>
    /// <param name="campaignName">The campaign to search for the mission in. If null, searches the <c>Missions/</c> directory.</param>
    /// <returns>The successfully loaded mission.</returns>
    /// <exception cref="FileLoadException"></exception>
    public static Mission Load(string missionName, string? campaignName) {
        string root;
        if (campaignName is not null)
            root = Path.Combine(TankGame.SaveDirectory, "Campaigns", campaignName);
        else
            root = Path.Combine(TankGame.SaveDirectory, "Missions");
        var path = missionName.EndsWith(".mission") ? Path.Combine(root, missionName) : Path.Combine(root, missionName + ".mission");

        Directory.CreateDirectory(root);

        if (!File.Exists(path)) {
            ChatSystem.SendMessage($"Mission not found in file system. Aborting.", Color.Red);
            return default;
        }

        using var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));

        // ChatSystem.SendMessage($"Loaded mission with {tanks.Count} tank(s) and {blocks.Count} block(s).", Color.Lime);

        return Read(reader);
    }

    /// <summary>
    /// Reads from the current position in the <paramref name="reader"/>'s stream and returns the mission.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/> that is accessed.</param>
    /// <returns>The read mission data.</returns>
    /// <exception cref="FileLoadException"></exception>
    public static Mission Read(BinaryReader reader) {
        var header = reader.ReadBytes(4);

        if (!header.SequenceEqual(LevelEditorUI.LevelFileHeader))
            throw new FileLoadException($"The byte header of this file does not match what this game expects!");

        var version = reader.ReadInt32();

        //if (version != LevelEditorUI.EDITOR_VERSION)
        //ChatSystem.SendMessage($"Warning: This level was saved with a different version of the level editor. It may not work correctly.", Color.Yellow);
        return version switch {
            2 => LoadMissionV2(reader),
            3 => LoadMissionV3(reader),
            4 => LoadMissionV4(reader),
            5 => LoadMissionV5(reader),
            _ => throw new Exception("This is not supposed to happen."),
        };
    }

    // methods of loading mission data
    // preceding numbers represent the version of the editor the level was saved with

    // this exists solely to port from older versions (< 5) to the version where the center is actually at (0, 0)
    const float ADJUST_FOR_CENTER = 131f;

    public static Mission LoadMissionV2(BinaryReader reader) {
        List<TankTemplate> tanks = [];
        List<BlockTemplate> blocks = [];
        var name = reader.ReadString();

        var totalTanks = reader.ReadInt32();

        for (int i = 0; i < totalTanks; i++) {
            var isPlayer = reader.ReadBoolean();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle() - ADJUST_FOR_CENTER;
            var rotation = reader.ReadSingle();
            var tier = reader.ReadByte();

            tier = (byte)MathHelper.Clamp(tier, TankID.Brown, TankID.Collection.Count);

            // this is due to the failures of removing 2 constants in TankID.
            // if the player doesn't update their campaign for a minute then oh well
            if (!isPlayer) tier -= 1;
            var pType = reader.ReadByte();
            var team = reader.ReadByte();

            tanks.Add(new() {
                IsPlayer = isPlayer,
                Position = new(x, y),
                Rotation = rotation,
                AiTier = tier,
                PlayerType = pType,
                Team = team
            });
        }

        var totalBlocks = reader.ReadInt32();

        for (int i = 0; i < totalBlocks; i++) {
            var type = reader.ReadByte();
            var stack = reader.ReadByte();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle() - ADJUST_FOR_CENTER;
            var link = reader.ReadSByte();

            blocks.Add(new() {
                Type = type,
                Stack = stack,
                Position = new(x, y),
                TpLink = link
            });
        }

        return new Mission([.. tanks], [.. blocks]) {
            Name = name
        };
    }
    public static Mission LoadMissionV3(BinaryReader reader) {
        List<TankTemplate> tanks = [];
        List<BlockTemplate> blocks = [];
        var name = reader.ReadString();

        var totalTanks = reader.ReadInt32();

        for (int i = 0; i < totalTanks; i++) {
            var isPlayer = reader.ReadBoolean();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle() - ADJUST_FOR_CENTER;
            var rotation = reader.ReadSingle();
            var tier = reader.ReadByte();
            var pType = reader.ReadByte();
            var team = reader.ReadByte();

            tier = (byte)MathHelper.Clamp(tier, TankID.Brown, TankID.Collection.Count);

            tanks.Add(new() {
                IsPlayer = isPlayer,
                Position = new(x, y),
                Rotation = rotation,
                AiTier = tier,
                PlayerType = pType,
                Team = team
            });
        }

        var totalBlocks = reader.ReadInt32();

        for (int i = 0; i < totalBlocks; i++) {
            var type = reader.ReadByte();
            var stack = reader.ReadByte();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle() - ADJUST_FOR_CENTER;
            var link = reader.ReadSByte();

            blocks.Add(new() {
                Type = type,
                Stack = stack,
                Position = new(x, y),
                TpLink = link
            });
        }

        return new Mission([.. tanks], [.. blocks]) {
            Name = name
        };
    }
    public static Mission LoadMissionV4(BinaryReader reader) {
        List<TankTemplate> tanks = [];
        List<BlockTemplate> blocks = [];
        var name = reader.ReadString();
        var grantsLife = reader.ReadBoolean();

        var totalTanks = reader.ReadInt32();

        for (int i = 0; i < totalTanks; i++) {
            var isPlayer = reader.ReadBoolean();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle() - ADJUST_FOR_CENTER;
            var rotation = reader.ReadSingle();
            var tier = reader.ReadByte();
            var pType = reader.ReadByte();
            var team = reader.ReadByte();

            tanks.Add(new() {
                IsPlayer = isPlayer,
                Position = new(x, y),
                Rotation = rotation,
                AiTier = tier,
                PlayerType = pType,
                Team = team
            });
        }

        var totalBlocks = reader.ReadInt32();

        for (int i = 0; i < totalBlocks; i++) {
            var type = reader.ReadByte();
            var stack = reader.ReadByte();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle() - ADJUST_FOR_CENTER;
            var link = reader.ReadSByte();

            blocks.Add(new() {
                Type = type,
                Stack = stack,
                Position = new(x, y),
                TpLink = link
            });
        }

        return new Mission([.. tanks], [.. blocks]) {
            Name = name,
            GrantsExtraLife = grantsLife
        };
    }
    // could possibly be different.
    public static Mission LoadMissionV5(BinaryReader reader) {
        List<TankTemplate> tanks = [];
        List<BlockTemplate> blocks = [];
        var name = reader.ReadString();
        var grantsLife = reader.ReadBoolean();

        var totalTanks = reader.ReadInt32();

        for (int i = 0; i < totalTanks; i++) {
            var isPlayer = reader.ReadBoolean();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var rotation = reader.ReadSingle();
            var tier = reader.ReadByte();
            var pType = reader.ReadByte();
            var team = reader.ReadByte();

            tanks.Add(new() {
                IsPlayer = isPlayer,
                Position = new(x, y),
                Rotation = rotation,
                AiTier = tier,
                PlayerType = pType,
                Team = team
            });
        }

        var totalBlocks = reader.ReadInt32();

        for (int i = 0; i < totalBlocks; i++) {
            var type = reader.ReadByte();
            var stack = reader.ReadByte();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var link = reader.ReadSByte();

            blocks.Add(new() {
                Type = type,
                Stack = stack,
                Position = new(x, y),
                TpLink = link
            });
        }

        return new Mission([.. tanks], [.. blocks]) {
            Name = name,
            GrantsExtraLife = grantsLife
        };
    }
}