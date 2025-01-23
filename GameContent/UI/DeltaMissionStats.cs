using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.IO;

namespace TanksRebirth.GameContent.UI; 

/// <summary>Track information only at the start of a mission to show changes in values by the end of the mission.</summary>
public class DeltaMissionStats(PlayerTank.CampaignStats playerStats, GameData data) {
    public PlayerTank.CampaignStats OldStats = playerStats;
    public GameData OldData = data;

    public void CalculateDelta(PlayerTank.CampaignStats stats, GameData data, out PlayerTank.CampaignStats deltaStats, out GameData deltaData) {
        deltaStats = new() {
            MineHitsThisCampaign = stats.MineHitsThisCampaign - OldStats.MineHitsThisCampaign,
            MinesLaidThisCampaign = stats.MinesLaidThisCampaign - OldStats.MineHitsThisCampaign,
            ShellHitsThisCampaign = stats.ShellHitsThisCampaign - OldStats.ShellHitsThisCampaign,
            ShellsShotThisCampaign = stats.ShellsShotThisCampaign - OldStats.ShellsShotThisCampaign,
            SuicidesThisCampaign = stats.SuicidesThisCampaign - OldStats.SuicidesThisCampaign
        };
        deltaData = new() {
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
            deltaData.TankKills[i] = data.TankKills[i] - OldData.TankKills[i];
        }
    }
}
