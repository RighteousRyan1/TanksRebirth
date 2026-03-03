using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Tanks;


namespace TanksRebirth.GameContent.Systems.LevelSystem; 
public static class CampaignProgression {
    /// <summary>
    /// Returns whether or not there was a victory on vanilla conditions.<br></br>
    /// This means that either: One team remains or one tank remains.
    /// </summary>
    /// <param name="mission">The mission to check.</param>
    /// <param name="predicate">Functional checking for each tank to include into the check.</param>
    /// <param name="finalTeam">The final team, with respect to the predicate.</param>
    /// <returns>Whether or not one team or one player dominates the map.</returns>
    public static bool VanillaCheckCompletion(Mission mission, out int finalTeam, Func<Tank, bool>? predicate = null) {
        finalTeam = -1;

        if (mission.Tanks is null)
            return true;

        var teamSet = new HashSet<int>();
        int aliveTankCount = 0;

        foreach (var tank in GameHandler.AllTanks) {
            if (tank is null || tank.IsDestroyed)
                continue;

            if (predicate is not null && !predicate(tank))
                continue;

            aliveTankCount++;
            teamSet.Add(tank.Team);
        }

        if (teamSet.Count == 0)
            return true; // no teams alive

        if (teamSet.Count == 1) {
            finalTeam = teamSet.First();
            if (finalTeam == TeamID.NoTeam)
                return aliveTankCount <= 1;
            return true;
        }

        return false; // multiple teams still active
    }
    /// <summary>
    /// Checks if the current state of the level satisfies all mission completion conditions, including modded ones.
    /// </summary>
    public static void CheckMissionCompletion() {
        // if (Client.IsConnected() && !Client.IsHost()) return;

        if (CampaignGlobals.LoadedCampaign.CachedMissions[0].Name is null)
            return;

        // check modded first because i'd assume they're the least intensive?


        var nothingAnymore = VanillaCheckCompletion(CampaignGlobals.LoadedCampaign.CurrentMission, out var finalTeam);
        var myTank = PlayerTank.ClientTank;

        // i think this would cause issues if players have PvP.
        bool victory = myTank is null || myTank.IsOnSameTeamAs(finalTeam); // myTank.Team != TeamID.NoTeam && myTank.Team == finalTeam;

        if (nothingAnymore) {
            IntermissionHandler.PrepareIntermission(victory);
        }
    }
}
