using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;

namespace TanksRebirth.GameContent.Speedrunning;

public class Speedrun
{
    public static bool SpeedrunMode;
    public static bool AreSpeedrunsFetched { get; internal set; }
    public static SpeedrunData[] LoadedSpeedruns { get; private set; } = [];

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SpeedrunData
    {
        public readonly TimeSpan TimeTaken = TimeSpan.Zero;
        public readonly string? Runner = null;
        public readonly DateTime Date = DateTime.UnixEpoch;

        public SpeedrunData(string? runner, TimeSpan timeTaken, DateTime date) {
            Runner = runner;
            Date = date;
            TimeTaken = timeTaken;
        }

        public override string ToString() => $"{Runner} in {TimeUtils.StringFormatCustom(TimeTaken, ":")} on {Date:d}";
    }
    internal static void GetSpeedruns() {
        try {
            var bytes = WebUtils.DownloadWebFile("https://raw.githubusercontent.com/RighteousRyan1/tanks_rebirth_motds/master/topspeedruns_0-20", out var name);
            var str = System.Text.Encoding.Default.GetString(bytes);

            var strSplit = str.Split('\n').Where(x => x != string.Empty).ToArray();

            var data = new SpeedrunData[strSplit.Length];

            for (int i = 0; i < strSplit.Length; i++) {
                var spl = strSplit[i].Split('|');
                data[i] = new(spl[0], TimeSpan.Parse(spl[1]), DateTime.Parse(spl[2], CultureInfo.InvariantCulture, styles: DateTimeStyles.None));
            }
            LoadedSpeedruns = data;
        }
        catch {
            LoadedSpeedruns = new SpeedrunData[1];
            LoadedSpeedruns[0] = new("Unable to fetch speedrun data.", TimeSpan.Zero, DateTime.UnixEpoch);
        }
    }
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
        if (CampaignGlobals.ShouldMissionsProgress) {
            if (CampaignGlobals.LoadedCampaign.CurrentMissionId <= 0) {
                CurrentSpeedrun = new(CampaignGlobals.LoadedCampaign.MetaData.Name);
                foreach (var mission in CampaignGlobals.LoadedCampaign.CachedMissions)
                    CurrentSpeedrun.MissionTimes.Add(mission.Name, (TimeSpan.Zero, TimeSpan.Zero));
                CurrentSpeedrun.Timer.Start();
            }
        }
    }

    public static void DrawSpeedrunHUD(SpriteBatch spriteBatch) {
        if (SpeedrunMode && CurrentSpeedrun is not null) {
            var num = CampaignGlobals.LoadedCampaign.CurrentMissionId switch {
                > 2 => CampaignGlobals.LoadedCampaign.CurrentMissionId - 2,
                1 => CampaignGlobals.LoadedCampaign.CurrentMissionId - 1,
                _ => 0,
            };

            var len = CampaignGlobals.LoadedCampaign.CurrentMissionId + 2 > CampaignGlobals.LoadedCampaign.CachedMissions.Length ? CampaignGlobals.LoadedCampaign.CachedMissions.Length - 1 : CampaignGlobals.LoadedCampaign.CurrentMissionId + 2;

            spriteBatch.DrawString(FontGlobals.RebirthFontLarge, $"Time: {CurrentSpeedrun.Timer.Elapsed}", new Vector2(10, 5), Color.White, new Vector2(0.15f), 0f, Vector2.Zero);
            for (int i = num; i <= len; i++) { // current.times.count originally

                var time = CurrentSpeedrun.MissionTimes.ElementAt(i);
                // display mission name and time taken
                spriteBatch.DrawString(FontGlobals.RebirthFontLarge, $"{time.Key}: {time.Value.Item2}", new Vector2(10, 20 + ((i - num) * 15)), Color.White, new Vector2(0.15f), 0f, Vector2.Zero);
            }
        }
    }
}
