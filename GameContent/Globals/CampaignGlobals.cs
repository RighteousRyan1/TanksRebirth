using TanksRebirth.Enums;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Globals;

public static class CampaignGlobals {
    public static DeltaStats DeltaMissionStats = new();
    public static DeltaStats DeltaCampaignStats = new();
    public static Campaign LoadedCampaign { get; set; } = new();
    public static bool InMission { get; set; } = false;
    public static bool ShouldMissionsProgress = true;

    public delegate void MissionStartEvent();

    public static event MissionStartEvent? OnMissionStart;

    public delegate void MissionEndEvent(int delay, MissionEndContext context);

    public static event MissionEndEvent? OnMissionEnd;

    // TODO: make mission stats end screen
    internal static void DoMissionStartInvoke() {
        Server.SyncSeeds();
        //DeltaMissionStats.SetOldData(PlayerTank.PlayerStatistics, TankGame.GameData);
        OnMissionStart?.Invoke();
    }
    public static void MissionEndEvent_Invoke(int delay, MissionEndContext context) {
        //DeltaMissionStats.CalculateDelta(PlayerTank.PlayerStatistics, TankGame.GameData);
        OnMissionEnd?.Invoke(delay, context);
    }
}