using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.GameContent.Speedrunning
{
    public class Speedrun
    {
        public Stopwatch Timer;
        public string CampaignName { get; set; }

        /// <summary>Stores the name of the mission completed and the time it took to complete it both in TOTAL and BY-MISSION.</summary>
        public Dictionary<string, (TimeSpan, TimeSpan)> MissionTimes { get; set; } // total, mission only

        public Speedrun(string campaignName)
        {
            CampaignName = campaignName;
            MissionTimes = new();
            Timer = new();
        }
    }
}
