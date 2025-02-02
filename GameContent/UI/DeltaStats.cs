using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.IO;

namespace TanksRebirth.GameContent.UI; 

/// <summary>Track information only at the start of an event to show changes in values by the end of said event.</summary>
public class DeltaStats {
    public PlayerTank.CampaignStats OldStats { get; private set; }
    public GameData OldData { get; private set; } = new();

    public GameData DeltaData;
    public PlayerTank.CampaignStats DeltaPlayerStats;
    public int NumStatsWithDelta { get; private set; }

    public object[] OldValues { get; private set; }
    public object[] NewValues { get; private set; }

    // zero clue why OldData is the same as 'data' within CalculateDelta, especially given this code below.
    public void SetOldData(PlayerTank.CampaignStats stats, GameData data) {
        OldData.TotalKills = data.TotalKills;
        OldData.BounceKills = data.BounceKills;
        OldData.BulletKills = data.BulletKills;
        OldData.MineKills = data.MineKills;
        OldData.CampaignsCompleted = data.CampaignsCompleted;
        OldData.MissionsCompleted = data.MissionsCompleted;
        OldData.Deaths = data.Deaths;
        OldData.ExpLevel = data.ExpLevel;
        OldData.Suicides = data.Suicides;
        OldData.TimePlayed = data.TimePlayed;
        for (int i = 0; i < OldData.TankKills.Count; i++) {
            OldData.TankKills[i] = data.TankKills[i];
        }
    }
    /// <summary>Sends the delta of the statistics to <see cref="DeltaData"/> and <see cref="DeltaPlayerStats"/></summary>
    public void CalculateDelta(PlayerTank.CampaignStats stats, GameData data) {
        DeltaPlayerStats = new() {
            MineHits = stats.MineHits - OldStats.MineHits,
            MinesLaid = stats.MinesLaid - OldStats.MineHits,
            ShellHits = stats.ShellHits - OldStats.ShellHits,
            ShellsShot = stats.ShellsShot - OldStats.ShellsShot,
            Suicides = stats.Suicides - OldStats.Suicides
        };
        DeltaData = new() {
            TotalKills = data.TotalKills - OldData.TotalKills,
            BounceKills = data.BounceKills - OldData.BounceKills,
            BulletKills = data.BulletKills - OldData.BulletKills,
            MineKills = data.MineKills - OldData.MineKills,
            CampaignsCompleted = data.CampaignsCompleted - OldData.CampaignsCompleted,
            MissionsCompleted = data.CampaignsCompleted - OldData.CampaignsCompleted,
            Deaths = data.CampaignsCompleted - OldData.CampaignsCompleted,
            ExpLevel = data.ExpLevel - OldData.ExpLevel,
            Suicides = data.Suicides - OldData.Suicides,
            TimePlayed = data.TimePlayed - OldData.TimePlayed
        };
        for (int i = 0; i < OldData.TankKills.Count; i++) {
            DeltaData.TankKills[i] = data.TankKills[i] - OldData.TankKills[i];
        }
        var members = typeof(GameData).GetFields();
        foreach (var member in members) {
            var type = member.FieldType;
            switch (type) {
                case var _ when type.Equals(typeof(uint)):
                    if ((uint)member.GetValue(DeltaData) != 0) {
                        NumStatsWithDelta++;
                    }
                    break;
                //case var _ when type.Equals(typeof(float)):
                    //break;
                case var _ when type.Equals(typeof(TimeSpan)):
                    if ((TimeSpan)member.GetValue(DeltaData) != TimeSpan.Zero) {
                        NumStatsWithDelta++;
                    }
                    break;
            }
        }
    }
}
