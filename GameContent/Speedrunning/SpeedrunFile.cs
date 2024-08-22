using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.IO;

namespace TanksRebirth.GameContent.Speedrunning;

public class SpeedrunFile : IFileSerializable {
    public static List<Speedrun> Speedruns = new();
    public string Directory { get; set; } = TankGame.SaveDirectory;
    public string Name { get; set; } = "SpeedrunData.dat";

    public void Serialize() {
        using var writer = new BinaryWriter(File.Open(Path.Combine(Directory, Name), FileMode.OpenOrCreate));

        writer.Write(Speedruns.Count);

        foreach (var speedrun in Speedruns) {
            writer.Write(speedrun.CampaignName);

            writer.Write(speedrun.MissionTimes.Count);

            foreach (var pair in speedrun.MissionTimes) {
                writer.Write(pair.Key);
                writer.Write(pair.Value.Item2.TotalMilliseconds);
                writer.Write(pair.Value.Item2.TotalMilliseconds);
            }
        }
    }
    public void Deserialize() {
        using var reader = new BinaryReader(File.Open(Path.Combine(Directory, Name), FileMode.OpenOrCreate));

        // num speedruns
        for (int i = 0; i < reader.ReadInt32(); i++) {
            Speedruns[i].CampaignName = reader.ReadString();

            // total missions and their times
            for (int j = 0; j < reader.ReadInt32(); j++) {
                var name = reader.ReadString();
                var timeTotal = TimeSpan.FromMilliseconds(reader.ReadDouble());
                var timeMissionWise = TimeSpan.FromMilliseconds(reader.ReadDouble());

                Speedruns[i].MissionTimes.Add(name, (timeTotal, timeMissionWise));
            }
            // i just threw this together, hopefully it works
        }
    }
}
