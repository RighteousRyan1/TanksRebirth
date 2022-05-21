using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.IO
{
    public class GameData : IFileSerializable
    {
        private static byte[] _message = IOUtils.ToAsciiBytes("If you ever choose to modify this file manually, just know you are making the game unfun for yourself.");

        public string Directory { get; } = TankGame.SaveDirectory;
        public string Name { get; } = "GameData.dat";

        // only kills from a player will count in single player to increment these!
        public uint TotalKills;
        public uint BulletKills;
        public uint BounceKills;
        public uint MineKills;

        public uint MissionsCompleted;
        public uint CampaignsCompleted;
        public uint Deaths;
        public uint Suicides;

        // Multiplayer wins, etc (for when they are made)

        // same with these
        public TankTier[] KillCountsTiers = new TankTier[Enum.GetValues<TankTier>().Length];
        public uint[] KillCountsCount = new uint[Enum.GetValues<TankTier>().Length];

        public float ExpLevel; // every whole number is an XP level | 1.000 = XP level 1
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
             */

            writer.Write(_message);

            writer.Write(TotalKills);
            writer.Write(BounceKills);
            writer.Write(MineKills);
            writer.Write(MissionsCompleted);
            writer.Write(CampaignsCompleted);
            writer.Write(Deaths);
            writer.Write(Suicides);

            for (int i = 0; i < KillCountsTiers.Length; i++) {

                writer.Write((byte)KillCountsTiers[i]);
                writer.Write(KillCountsCount[i]);
            }

        }
        public void Deserialize()
        {

        }
    }
}
