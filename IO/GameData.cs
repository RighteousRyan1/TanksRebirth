using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Achievements;
using TanksRebirth.Enums;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.IO
{
    public class GameData : IFileSerializable
    {
        public string Directory { get; } = TankGame.SaveDirectory;
        public string Name { get; } = "GameData.dat";

        public const float DecayPerLevel = 1.01f;
        public static float UniversalExpMultiplier { get; internal set; }

        private static byte[] _message = IOUtils.ToAsciiBytes("If you ever choose to modify this file manually, just know you are making the game unfun for yourself."); // hmm...

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

        public Dictionary<TankTier, uint> TankKills = new(Enum.GetValues<TankTier>().Length);

        public float ExpLevel; // every whole number is an XP level | 1.000 = XP level 1

        public void Setup()
        {
            for (int i = 0; i < Enum.GetValues<TankTier>().Length; i++)
            {
                TankKills.Add((TankTier)i, 0);
            }
        }
        public void Serialize()
        {


            using var writer = new BinaryWriter(File.Open(Path.Combine(Directory, Name), FileMode.OpenOrCreate));
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

            writer.Write(TotalKills);
            writer.Write(BounceKills);
            writer.Write(MineKills);
            writer.Write(MissionsCompleted);
            writer.Write(CampaignsCompleted);
            writer.Write(Deaths);
            writer.Write(Suicides);

            writer.Write(TimePlayed.TotalMilliseconds);

            for (int i = 0; i < TankKills.Count; i++)
                writer.Write(TankKills[(TankTier)i]);

            //VanillaAchievements.Repository.Save(writer);

            writer.Write(ExpLevel);

        }
        public void Deserialize()
        {
            using var reader = new BinaryReader(File.Open(Path.Combine(Directory, Name), FileMode.OpenOrCreate));

            // reader.ReadString();

            TotalKills = reader.ReadUInt32();
            BounceKills = reader.ReadUInt32();
            MineKills = reader.ReadUInt32();
            MissionsCompleted = reader.ReadUInt32();
            CampaignsCompleted = reader.ReadUInt32();
            Deaths = reader.ReadUInt32();
            Suicides = reader.ReadUInt32();

            TimePlayed = TimeSpan.FromMilliseconds(reader.ReadDouble());

            for (int i = 0; i < TankKills.Count; i++)
                TankKills[(TankTier)i] = reader.ReadUInt32();

            //VanillaAchievements.Repository.Load(reader);

            ExpLevel = reader.ReadSingle();
        }
    }
}
