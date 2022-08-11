using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using FontStashSharp;
using TanksRebirth.GameContent.Systems.Coordinates;
using NativeFileDialogSharp;

namespace TanksRebirth.GameContent.UI
{
    public static class CampaignCompleteUI
    {
        public static Rectangle ParseGradeRect(Grade grade)
        {
            // 128 being the width of each section.
            int col = (int)grade / 3 * 128;
            int row = (int)grade % 3 * 128;

            return new(row, col, 128, 128);
        }

        private static TimeSpan _delayPerTank = TimeSpan.FromMilliseconds(500);

        public static Dictionary<TankTier, int> KillsPerType;

        public static Grade Grade;
        /// <summary>Perform a multithreaded operation that will display tanks killed and their kill counts for the player.</summary>
        public static void PerformSequence(MissionEndContext context)
        {
            KillsPerType?.Clear();
            // then reinitialize with the proper tank-to-kill values.
            // *insert code here*

            // finish this tomorrow i am very tired murder me.
            SetStats(GameProperties.LoadedCampaign, PlayerTank.PlayerStatistics);
            var grade = FormulateGradeLevel();

            Task.Run(() => {
                // sleep the thread for the duration of the fanfare.
                for (int i = 0; i < KillsPerType.Count; i++)
                {
                    var killData = KillsPerType.ElementAt(i);

                    Thread.Sleep(_delayPerTank);
                }
            });
        }
        // TODO: probably support multiple players L + ratio me
        public static void SetStats(Campaign campaign, PlayerTank.DeterministicPlayerStats stats)
        {
            // set everything properly...
            ShellsFired = stats.ShellsShotThisCampaign;
            ShellHits = stats.ShellHitsThisCampaign;

            MinesLaid = stats.MinesLaidThisCampaign;
            MineHits = stats.MineHitsThisCampaign;

            SuicideCount = stats.SuicidesThisCampaign;

            LivesRemaining = PlayerTank.Lives;

            // then, we determine the number of possible lives by checking how many extra life missions
            // there were this campaign, along with adding the starting life count.
            LivesRemaining = PlayerTank.StartingLives + campaign.Properties.ExtraLivesMissions.Length;
        }

        public static Grade FormulateGradeLevel()
        {
            var grade = Grade.APlus;

            float shellRatio = ShellHits / ShellsFired;
            float mineRatio = MineHits / MinesLaid;
            float lifeRatio = LivesRemaining / TotalPossibleLives;

            // (min, max]
            bool isBetween(float input, float min, float max) => input >= min && input < max;
            // reduntant code but whatever i love men
            if (shellRatio > 0.8f)
                grade += 0;
            else if (isBetween(shellRatio, 0.6f, 0.8f))
                grade += 1;
            else if (isBetween(shellRatio, 0.4f, 0.6f))
                grade += 2;
            else if (isBetween(shellRatio, 0.2f, 0.4f))
                grade += 3;
            else if (shellRatio < 0.2f)
                grade += 4;

            if (mineRatio >= 0.25f)
                grade += 0;
            else if (shellRatio < 0.25f)
                grade += 1;

            // check >= just incase something goofy happens.
            if (lifeRatio >= 1f)
                grade += 0;
            else if (isBetween(lifeRatio, 0.75f, 1f))
                grade += 1;
            else if (isBetween(lifeRatio, 0.5f, 0.75f))
                grade += 2;
            else if (isBetween(lifeRatio, 0.25f, 0.5f))
                grade += 3;
            else if (isBetween(lifeRatio, 0f, 0.25f))
                grade += 4;

            grade += SuicideCount;

            if ((int)grade > 14)
                grade = (Grade)14;

            return grade;
        }

        // overall stats throughout the campaign
        // these will factor into the grade level

        public static int ShellsFired;
        public static int ShellHits;

        // considering not using this...
        public static int MinesLaid;
        public static int MineHits;

        public static int TotalPossibleLives;
        public static int LivesRemaining;

        public static int SuicideCount;
    }
}
