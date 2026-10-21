using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals;

namespace TanksRebirth.IO;

public class TanksSaveFile : IFileSerializable {
    // 0 and 1 were the old binary format. 2 and up are gzipped json
    private const byte CURRENT_GAME_DATA_VERSION = 2;

    public bool ReadingOutdatedFile;
    public string Directory { get; } = TankGame.SaveDirectory;
    public string Name { get; } = "GameData.dat";

    public const float DECAY_PER_LEVEL = 1.01f;
    public static float UniversalExpMultiplier { get; internal set; } = 1f;


    // If you ever choose to modify this file manually, just know you are making the game unfun for yourself.
    /*static byte[] _message = 
        IOUtils.ToAsciiBytes(
            "If you ever choose to modify this file manually, just know you are making the game unfun for yourself.");*/ // hmm...

    public byte CollectedKeys;
    public List<int> UnlockedCosmetics = [];

    // only kills from a player will count in single player to increment these!
    public uint TotalKills;
    public uint BulletKills;
    public uint BounceKills;
    public uint MineKills;

    public uint MissionsCompleted;
    public uint CampaignsCompleted;
    public uint Deaths;
    public uint Suicides;

    public TimeSpan TimePlayed;

    // Multiplayer wins, etc (for when they are made)

    // under is a bunch of encounter booleans

    public Dictionary<int, uint> TankKills = new(TankID.Collection.Count);

    public float ExpLevel; // every whole number is an XP level | 1.000 = XP level 1

    // kills of tanks from mods that aren't loaded, kept so saving doesn't lose them
    readonly Dictionary<string, uint> _unknownTankKills = [];

    static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    string FilePath => Path.Combine(Directory, Name);

    public TanksSaveFile() => Reset();

    // a fresh save
    void Reset() {
        TankKills.Clear();
        UnlockedCosmetics.Clear();
        _unknownTankKills.Clear();
        for (int i = 0; i < TankID.Collection.Count; i++)
            TankKills.Add(i, 0);
    }

    public void Serialize() {
        // tanks are saved by name for mod support
        // ...however, not exactly sure if i actually want to save modded tank kill counts
        // if names are ever changed internally there is just going to be a permanent filesize stain
        var tankKills = new Dictionary<string, uint>(_unknownTankKills);
        foreach (var (id, kills) in TankKills)
            tankKills[TankID.Collection.GetKey(id) ?? id.ToString(CultureInfo.InvariantCulture)] = kills;

        var file = new SaveData {
            Version = CURRENT_GAME_DATA_VERSION,
            TotalKills = TotalKills,
            BulletKills = BulletKills,
            BounceKills = BounceKills,
            MineKills = MineKills,
            MissionsCompleted = MissionsCompleted,
            CampaignsCompleted = CampaignsCompleted,
            Deaths = Deaths,
            Suicides = Suicides,
            TimePlayed = TimePlayed,
            TankKills = tankKills,
            ExpLevel = ExpLevel,
            CollectedKeys = CollectedKeys,
            UnlockedCosmetics = UnlockedCosmetics,
        };
        GzipJson.Write(FilePath, file, _jsonOptions);
    }
    public void Deserialize() {
        Reset();

        try {
            using var stream = File.OpenRead(FilePath);

            // the binary format started with its version, which was never above 1
            var first = stream.ReadByte();
            stream.Position = 0;
            if (first is 0 or 1)
                ReadBinary(stream);
            else
                ReadJson(stream);
        } catch (Exception e) when (e is IOException or JsonException) {
            TankGame.ReportError(e);
            Backup("unreadable");
            TankGame.ClientLog.Write(
                "Your save file could not be read, so a fresh one is used. The old one was copied to the Backup folder. Sorry!",
                RebirthLogLevel.Info);
            Reset();
        }

        GameHandler.ExpBar = new() { MaxValue = 1f, Value = ExpLevel - MathF.Floor(ExpLevel) };
    }

    void ReadJson(Stream stream) {
        var file = GzipJson.Read<SaveData>(stream, _jsonOptions) ?? new SaveData();

        if (file.Version > CURRENT_GAME_DATA_VERSION) {
            TankGame.ClientLog.Write($"Loading a {Name} from a newer version of the game! (Save File: {file.Version}. Game Data Version: {CURRENT_GAME_DATA_VERSION})", RebirthLogLevel.Warn);
            ReadingOutdatedFile = true;
        }

        TotalKills = file.TotalKills;
        BulletKills = file.BulletKills;
        BounceKills = file.BounceKills;
        MineKills = file.MineKills;
        MissionsCompleted = file.MissionsCompleted;
        CampaignsCompleted = file.CampaignsCompleted;
        Deaths = file.Deaths;
        Suicides = file.Suicides;
        TimePlayed = file.TimePlayed;
        ExpLevel = file.ExpLevel;
        CollectedKeys = file.CollectedKeys;
        UnlockedCosmetics = file.UnlockedCosmetics ?? [];

        foreach (var (name, kills) in file.TankKills ?? []) {
            var id = TankID.Collection.ContainsKey(name) ? TankID.Collection.GetValue(name) : null;
            if (id.HasValue)
                TankKills[id.Value] = kills;
            else
                _unknownTankKills[name] = kills;
        }
    }

    void ReadBinary(Stream stream) {
        using var reader = new BinaryReader(stream);

        var saveVersion = reader.ReadByte();
        TankGame.ClientLog.Write($"Converting {Name} from the old format. (current={saveVersion}. expected={CURRENT_GAME_DATA_VERSION})", RebirthLogLevel.Info);
        ReadingOutdatedFile = true;

        // the conversion only goes one way
        Backup($"binary_v{saveVersion}");

        TotalKills = reader.ReadUInt32();
        BulletKills = reader.ReadUInt32();
        BounceKills = reader.ReadUInt32();
        MineKills = reader.ReadUInt32();
        MissionsCompleted = reader.ReadUInt32();
        CampaignsCompleted = reader.ReadUInt32();
        Deaths = reader.ReadUInt32();
        Suicides = reader.ReadUInt32();

        TimePlayed = TimeSpan.FromMilliseconds(reader.ReadDouble());

        for (int i = 0; i < TankKills.Count; i++)
            TankKills[i] = reader.ReadUInt32();

        ExpLevel = reader.ReadSingle();

        // version 1 and up
        if (saveVersion >= 1) {
            CollectedKeys = reader.ReadByte();
            int cosLen = reader.ReadInt32();
            for (int i = 0; i < cosLen; i++)
                UnlockedCosmetics.Add(reader.ReadInt32());
        }
    }

    void Backup(string reason) {
        try {
            var backup = Path.Combine(TankGame.SaveDirectory, "Backup", $"{Path.GetFileNameWithoutExtension(Name)}_{reason}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(Name)}");
            File.Copy(FilePath, backup, overwrite: true);
        } catch (IOException e) {
            TankGame.ClientLog.Write($"Could not back up {Name}: {e.Message}", RebirthLogLevel.Warn);
        }
    }

    // the layout of the file
    class SaveData {
        public int Version { get; set; }
        public uint TotalKills { get; set; }
        public uint BulletKills { get; set; }
        public uint BounceKills { get; set; }
        public uint MineKills { get; set; }
        public uint MissionsCompleted { get; set; }
        public uint CampaignsCompleted { get; set; }
        public uint Deaths { get; set; }
        public uint Suicides { get; set; }
        public TimeSpan TimePlayed { get; set; }
        /// <summary>Kills per tank, by the tank's name.</summary>
        public Dictionary<string, uint>? TankKills { get; set; }
        public float ExpLevel { get; set; }
        public byte CollectedKeys { get; set; }
        public List<int>? UnlockedCosmetics { get; set; }
    }
}
