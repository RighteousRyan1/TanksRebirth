using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Achievements;
using TanksRebirth.Enums;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.IO;

public class GameData : IFileSerializable {
    private const byte CURRENT_GAME_DATA_VERSION = 1;

    public bool ReadingOutdatedFile;
    public string Directory { get; } = TankGame.SaveDirectory;
    public string Name { get; } = "GameData.dat";

    public const float DECAY_PER_LEVEL = 1.01f;
    public static float UniversalExpMultiplier { get; internal set; } = 1f;


    // If you ever choose to modify this file manually, just know you are making the game unfun for yourself.
    /*static byte[] _message = 
        IOUtils.ToAsciiBytes(
            "If you ever choose to modify this file manually, just know you are making the game unfun for yourself.");*/ // hmm...

    public uint CollectedKeys;
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

    public void Serialize() {
        using var writer = new BinaryWriter(File.Open(Path.Combine(Directory, Name), FileMode.Create));
        /* File Serialization Order:
         * Do note: If you edit the game's data, you are scum
         * 
         * 1) Total Tanks Killed (uint)
         * 2) Total Tanks Killed Per-type (Dictionary<TankTier, uint>)
         * 3) Missions Completed Total (uint)
         * 
         * I've given up on this lol.
         * 
         */

        // writer.Write(_message);
        writer.Write(CURRENT_GAME_DATA_VERSION);

        writer.Write(TotalKills);
        writer.Write(BulletKills);
        writer.Write(BounceKills);
        writer.Write(MineKills);
        writer.Write(MissionsCompleted);
        writer.Write(CampaignsCompleted);
        writer.Write(Deaths);
        writer.Write(Suicides);

        writer.Write(TimePlayed.TotalMilliseconds);

        for (int i = 0; i < TankKills.Count; i++)
            writer.Write(TankKills[i]);

        //VanillaAchievements.Repository.Save(writer);

        writer.Write(ExpLevel);

        // version 1 and up

        writer.Write(CollectedKeys);
        int cosLen = UnlockedCosmetics.Count;
        writer.Write(cosLen);
        for (int i = 0; i < cosLen; i++) {
            writer.Write(UnlockedCosmetics[i]);
        }
    }
    public void Deserialize() {
        // prepare dictionaries + lists
        TankKills.Clear();
        UnlockedCosmetics.Clear();
        for (int i = 0; i < TankID.Collection.Count; i++) 
            TankKills.Add(i, 0);

        using var reader = new BinaryReader(File.Open(Path.Combine(Directory, Name), FileMode.OpenOrCreate));

        // reader.ReadString();

        var saveVersion = reader.ReadByte();

        if (saveVersion != CURRENT_GAME_DATA_VERSION) {
            TankGame.ClientLog.Write($"Loading an outdated {Name}! (Save File: {saveVersion}. Game Data Version: {CURRENT_GAME_DATA_VERSION})", LogType.Warn);
            ReadingOutdatedFile = true;
        }

        try {

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

            //VanillaAchievements.Repository.Load(reader);

            ExpLevel = reader.ReadSingle();

            GameHandler.ExperienceBar = new() { MaxValue = 1f, Value = ExpLevel - MathF.Floor(ExpLevel) };

            // version 1 and up

            if (saveVersion >= 1) {
                CollectedKeys = reader.ReadUInt32();
                int cosLen = reader.ReadInt32();
                for (int i = 0; i < cosLen; i++) {

                }
            } else {
                CollectedKeys = 0;
                UnlockedCosmetics = [];
            }
        } catch (Exception e) when (ReadingOutdatedFile) {
            TankGame.ReportError(e);
            TankGame.ClientLog.Write(
                "An error occurred, possibly due to your save file being out-of-date. For now, delete it and restart the game. Sorry!",
                LogType.Info);
        } catch (IOException e) { // IO Error.
            TankGame.ReportError(e);
            TankGame.ClientLog.Write(
                "An error was thrown while attempting to read your save file. Please try to restart the game. If this error persists please delete your save file and try again. Sorry!",
                LogType.Info);
        }
    }
}
