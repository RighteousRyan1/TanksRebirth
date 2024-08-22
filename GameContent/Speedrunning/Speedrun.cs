using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Properties;

namespace TanksRebirth.GameContent.Speedrunning;

public class Speedrun
{
    public Stopwatch Timer;
    public string CampaignName { get; set; }

    /// <summary>Stores the name of the mission completed and the time it took to complete it both in TOTAL and BY-MISSION.</summary>
    public Dictionary<string, (TimeSpan, TimeSpan)> MissionTimes { get; set; } // total, mission only
    internal Speedrun(string campaignName)
    {
        CampaignName = campaignName;
        MissionTimes = new();
        Timer = new();
    }

    public static Speedrun? CurrentSpeedrun;
    public static void StartSpeedrun() {
        if (GameProperties.ShouldMissionsProgress) {
            if (GameProperties.LoadedCampaign.CurrentMissionId <= 0) {
                CurrentSpeedrun = new(GameProperties.LoadedCampaign.MetaData.Name);
                foreach (var mission in GameProperties.LoadedCampaign.CachedMissions)
                    CurrentSpeedrun.MissionTimes.Add(mission.Name, (TimeSpan.Zero, TimeSpan.Zero));
                CurrentSpeedrun.Timer.Start();
            }
        }
    }
}
