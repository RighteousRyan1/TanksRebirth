using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Properties
{
    public class GameProperties
    {
        public static Campaign LoadedCampaign { get; set; } = new();
        public static bool InMission { get; set; } = false;
        public static bool ShouldMissionsProgress = true;

        public delegate void MissionStartEvent();

        public static event MissionStartEvent OnMissionStart;

        public delegate void MissionEndEvent(int delay, MissionEndContext context, bool result1up);

        public static event MissionEndEvent OnMissionEnd;

        internal static void DoMissionStartInvoke()
        {
            Server.SyncSeeds();
            OnMissionStart?.Invoke();
        }
        public static void MissionEndEvent_Invoke(int delay, MissionEndContext context, bool result1up)
        {
            OnMissionEnd?.Invoke(delay, context, result1up);
        }
    }
}
