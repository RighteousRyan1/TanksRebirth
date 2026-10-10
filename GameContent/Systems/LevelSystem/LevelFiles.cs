using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Collections;
using TanksRebirth.Internals.Common.Framework.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.LevelSystem;

/// <summary>Reads and writes mission and campaign files as gzipped JSON. Older binary files can still be read.</summary>
public static class LevelFiles {
    /// <summary>Writes plain indented JSON instead of gzipped JSON. Useful for debugging and modding.</summary>
    public static bool WriteReadable;

    // idk. probably not gonna use a jsonhandler?
    public static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new BlockEntryConverter() },
    };

    static readonly JsonSerializerOptions _readableOptions = new(JsonOptions) { WriteIndented = true };

    public enum FileKind { 
        // the OG file save kind, what it was for years. kept for backwards compat
        Binary, 
        // the new kid on the block. makes my job 2000000000% easier
        GzipJson, 
        Json 
    }

    /// <summary>Finds the format of a level file from its first byte. The stream position is left unchanged.</summary>
    public static FileKind DetectFormat(Stream stream) {
        var start = stream.Position;
        var first = stream.ReadByte();
        stream.Position = start;

        // the original first header was TANKS in binary.
        return first switch {
            'T' => FileKind.Binary,
            // according to the internet, gzip always starts with 1F 8B (which is the header or something, then the compression method)
            0x1F => FileKind.GzipJson,
            // fallback, in case it doesn't get gzipped for whatever reason
            _ => FileKind.Json,
        };
    }

    // mission reading

    public static Mission ReadMission(Stream stream) {
        stream = StreamUtils.MakeSeekable(stream);
        var kind = DetectFormat(stream);
        if (kind == FileKind.Binary)
            return Mission.Read(new BinaryReader(stream));

        var file = Deserialize<MissionFile>(stream, kind);
        return ToMission(file);
    }

    public static void WriteMission(string path, Mission mission) {
        var file = FromMission(mission);
        file.Format = LevelEditorUI.EDITOR_VERSION;
        Serialize(path, file);
    }

    // campaign reading

    /// <summary>Reads a JSON campaign. Returns null for a binary file so the caller can use the legacy loader.</summary>
    public static Campaign? ReadCampaign(Stream stream) {
        var kind = DetectFormat(stream);
        if (kind == FileKind.Binary)
            return null;

        var file = Deserialize<CampaignFile>(stream, kind);
        var campaign = new Campaign {
            MetaData = new Campaign.CampaignMetaData {
                Name = file.Name ?? string.Empty,
                Description = file.Description ?? string.Empty,
                Author = file.Author ?? string.Empty,
                Version = file.Version ?? string.Empty,
                Tags = file.Tags ?? [],
                ExtraLivesMissions = [],
                StartingLives = file.StartingLives,
                HasMajorVictory = file.HasMajorVictory,
                BackgroundColor = ParseColor(file.BackgroundColor, IntermissionSystem.DefaultBackgroundColor),
                MissionStripColor = ParseColor(file.MissionStripColor, IntermissionSystem.DefaultStripColor),
            }
        };

        var missions = file.Missions ?? [];
        campaign.CachedMissions = new Mission[missions.Count];
        for (int i = 0; i < missions.Count; i++)
            campaign.CachedMissions[i] = ToMission(missions[i]);
        return campaign;
    }

    public static void WriteCampaign(string path, Campaign campaign) {
        var meta = campaign.MetaData;
        var file = new CampaignFile {
            Format = LevelEditorUI.EDITOR_VERSION,
            Name = meta.Name,
            Description = meta.Description,
            Author = meta.Author,
            Version = meta.Version,
            Tags = meta.Tags,
            StartingLives = meta.StartingLives,
            HasMajorVictory = meta.HasMajorVictory,
            BackgroundColor = FormatColor(meta.BackgroundColor),
            MissionStripColor = FormatColor(meta.MissionStripColor),
            Missions = [],
        };
        foreach (var mission in campaign.CachedMissions) {
            if (mission != default)
                file.Missions.Add(FromMission(mission));
        }
        Serialize(path, file);
    }

    // conversions

    static MissionFile FromMission(Mission mission) {
        var grid = MapGrid.Current;
        var file = new MissionFile {
            Name = mission.Name ?? string.Empty,
            Note = string.IsNullOrEmpty(mission.Note) ? null : mission.Note,
            GrantsExtraLife = mission.GrantsExtraLife ? true : null,
            Width = grid.Width,
            Height = grid.Height,
            Tanks = [],
            Blocks = [],
        };

        foreach (var tank in mission.Tanks ?? []) {
            var cell = grid.WorldToCell(tank.Position);
            file.Tanks.Add(new TankEntry {
                X = cell.X,
                Y = cell.Y,
                Facing = tank.Facing,
                Tier = tank.IsPlayer ? null : NameOf(TankID.Collection, tank.AIType),
                Player = tank.IsPlayer ? NameOf(PlayerID.Collection, tank.PlayerType) : null,
                Team = NameOf(TeamID.Collection, tank.Team),
            });
        }

        foreach (var block in mission.Blocks ?? []) {
            var cell = grid.WorldToCell(block.Position);
            file.Blocks.Add(new BlockEntry {
                Type = NameOf(BlockID.Collection, block.Type),
                Stack = block.Stack,
                X = cell.X,
                Y = cell.Y,
                TpLink = block.TpLink,
            });
        }
        return file;
    }

    static Mission ToMission(MissionFile file) {
        var grid = MapGrid.Current;
        if (file.Width != grid.Width || file.Height != grid.Height)
            TankGame.ClientLog.Write($"Mission '{file.Name}' was made for a {file.Width}x{file.Height} board but the board is {grid.Width}x{grid.Height}.", LogType.Warn);

        var tanks = new List<TankTemplate>();
        foreach (var entry in file.Tanks ?? []) {
            var isPlayer = entry.Player is not null;
            tanks.Add(new TankTemplate {
                IsPlayer = isPlayer,
                Position = grid.CellToWorld(entry.X, entry.Y),
                Facing = entry.Facing,
                AIType = isPlayer ? 0 : IdOf(TankID.Collection, entry.Tier, TankID.Brown, "tank"),
                PlayerType = isPlayer ? IdOf(PlayerID.Collection, entry.Player, PlayerID.Blue, "player") : 0,
                Team = IdOf(TeamID.Collection, entry.Team, TeamID.NoTeam, "team"),
            });
        }

        var blocks = new List<BlockTemplate>();
        foreach (var entry in file.Blocks ?? []) {
            blocks.Add(new BlockTemplate {
                Type = IdOf(BlockID.Collection, entry.Type, BlockID.Wood, "block"),
                Stack = (byte)entry.Stack,
                Position = grid.CellToWorld(entry.X, entry.Y),
                TpLink = (byte)entry.TpLink,
            });
        }

        return new Mission([.. tanks], [.. blocks]) {
            Name = file.Name ?? string.Empty,
            Note = file.Note ?? string.Empty,
            GrantsExtraLife = file.GrantsExtraLife ?? false,
        };
    }

    /// <summary>The name of an ID, or the number as text if it has no name.</summary>
    static string NameOf<T>(ReflectionDictionary<T> collection, int id) where T : class, new()
        => collection.GetKey(id) ?? id.ToString(CultureInfo.InvariantCulture);

    /// <summary>The ID for a name or a number. Unknown names log a warning and use the fallback.</summary>
    static int IdOf<T>(ReflectionDictionary<T> collection, string? name, int fallback, string kind) where T : class, new() {
        if (string.IsNullOrEmpty(name))
            return fallback;
        if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return number;
        if (collection.ContainsKey(name))
            return collection.GetValue(name)!.Value;
        TankGame.ClientLog.Write($"Unknown {kind} '{name}' in a level file (is a mod missing?). Using {collection.GetKey(fallback)} instead.", LogType.Warn);
        return fallback;
    }

    static string FormatColor(UnpackedColor color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    // turns a hex color string into an UnpackedColor. credit to ai for suggesting bit math
    static UnpackedColor ParseColor(string? text, UnpackedColor fallback) {
        if (text is null || text.Length != 7 || text[0] != '#'
            || !int.TryParse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return fallback;
        return new UnpackedColor((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, 255);
    }

    // all the IO stuff

    static T Deserialize<T>(Stream stream, FileKind kind) {
        using var source = kind == FileKind.GzipJson ? new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true) : null;
        return JsonSerializer.Deserialize<T>(source ?? stream, JsonOptions)!; // exclamation mark because it's loud!!!
    }
    // no JsonHandler because i'm serializing a gzip stream, which JsonHandler doesn't do
    static void Serialize<T>(string path, T value) {
        using var file = File.Create(path);
        if (WriteReadable) {
            JsonSerializer.Serialize(file, value, _readableOptions);
            return;
        }
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        JsonSerializer.Serialize(gzip, value, JsonOptions);
    }

    // file layouts. pretty nice and readable. i really like refactoring old code!
    // i kind of hate storing the tanks as string identifiers but it will be necessary for allowing modded tanks to be saved

    // describes the campaign file. changed to class cuz it contains strings + arrays (which are managed types, which makes the struct pointless)
    class CampaignFile {
        public int Format { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public string[]? Tags { get; set; }
        public int StartingLives { get; set; } = 3;
        public bool HasMajorVictory { get; set; }
        public string? BackgroundColor { get; set; }
        public string? MissionStripColor { get; set; }
        public List<MissionFile>? Missions { get; set; }
    }

    // describes a mission file. same reasoning for the campaign file
    class MissionFile {
        /// <summary>Only written for standalone mission files.</summary>
        public int? Format { get; set; }
        public string? Name { get; set; }
        public string? Note { get; set; }
        public bool? GrantsExtraLife { get; set; }
        public int Width { get; set; } = MapGrid.STD_WIDTH;
        public int Height { get; set; } = MapGrid.STD_HEIGHT;
        public List<TankEntry>? Tanks { get; set; }
        public List<BlockEntry>? Blocks { get; set; }
    }

    class TankEntry {
        public int X { get; set; }
        public int Y { get; set; }
        public Facing Facing { get; set; }
        /// <summary>Set for AI tanks.</summary>
        public string? Tier { get; set; }
        /// <summary>Set for player tanks.</summary>
        public string? Player { get; set; }
        public string? Team { get; set; }
    }

    /// <summary>Written as 
    /// [type, stack, x, y] 
    /// with the teleporter link added at the end when it is set.</summary>
    class BlockEntry {
        public string? Type { get; set; }
        public int Stack { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int TpLink { get; set; }
    }

    // apparently i have to write this stupid wizardry for json converters. you learn something new about json every day
    class BlockEntryConverter : JsonConverter<BlockEntry> {
        public override BlockEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("A block must be an array of [type, stack, x, y].");

            var block = new BlockEntry();
            var index = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                switch (index++) {
                    case 0:
                        block.Type = reader.TokenType == JsonTokenType.String
                            ? reader.GetString()
                            : reader.GetInt32().ToString(CultureInfo.InvariantCulture);
                        break;
                    case 1: block.Stack = reader.GetInt32(); break;
                    case 2: block.X = reader.GetInt32(); break;
                    case 3: block.Y = reader.GetInt32(); break;
                    case 4: block.TpLink = reader.GetInt32(); break;
                    default: reader.Skip(); break;
                }
            }
            return block;
        }

        public override void Write(Utf8JsonWriter writer, BlockEntry value, JsonSerializerOptions options) {
            writer.WriteStartArray();
            writer.WriteStringValue(value.Type);
            writer.WriteNumberValue(value.Stack);
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            if (value.TpLink != 0)
                writer.WriteNumberValue(value.TpLink);
            writer.WriteEndArray();
        }
    }
}
