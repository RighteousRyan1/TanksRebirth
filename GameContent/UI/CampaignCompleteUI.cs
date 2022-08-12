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
using Microsoft.Xna.Framework.Input;

namespace TanksRebirth.GameContent.UI
{
    public static class CampaignCompleteUI
    {
        public static bool IsViewingResults;
        public static Rectangle ParseGradeRect(Grade grade)
        {
            // 128 being the width of each section.
            int col = (int)grade / 3;
            int row = (int)grade % 3;

            return new(col * 128, row * 128, 128, 128);
        }

        private static TimeSpan _delayPerTank = TimeSpan.FromMilliseconds(700);

        public static Dictionary<TankTier, int> KillsPerType = new();

        public static Grade Grade;

        private static float _gradeRotation;
        private static float _panelAlpha = 1f;

        private static bool _doingAnimation;
        private static bool _skip;

        // TODO: tomorrow do this stuff
        // music loop, depending on end context
        // tank grafiks

        public static Dictionary<TankTier, (Vector2, float, float)> TierDisplays; // position, alpha, scale
        /// <summary>Perform a multithreaded operation that will display tanks killed and their kill counts for the player.</summary>
        public static void PerformSequence(MissionEndContext context)
        {
            IsViewingResults = true;

            KillsPerType?.Clear();
            // then reinitialize with the proper tank-to-kill values.
            // *insert code here*

            // finish this tomorrow i am very tired murder me.
            SetStats(GameProperties.LoadedCampaign, PlayerTank.PlayerStatistics, PlayerTank.TankKills);
            Grade = FormulateGradeLevel();

            Task.Run(() => {
                // sleep the thread for the duration of the fanfare.
                _doingAnimation = true;
                Vector2 basePos = new(GameUtils.WindowWidth / 2, GameUtils.WindowHeight * 0.2f);
                float offY = 0;
                for (int i = 0; i < KillsPerType.Count; i++)
                {
                    var killData = KillsPerType.ElementAt(i);

                    TierDisplays.Add(killData.Key, (basePos + new Vector2(0, offY), 0f, 0f));

                    offY += 30f;

                    if (!_skip)
                        Thread.Sleep(_delayPerTank);
                }
                _skip = false;
                _doingAnimation = false;

                while (!Input.KeyJustPressed(Keys.Enter))
                    Thread.Sleep(TankGame.LastGameTime.ElapsedGameTime);

                IsViewingResults = false;
                MainMenu.Open();
            });
            // changing bools in other threads = safe
            // - ryan
            Task.Run(() => {
                while (!Input.KeyJustPressed(Keys.Enter) && IsViewingResults)
                    Thread.Sleep(TankGame.LastGameTime.ElapsedGameTime);

                if (Input.KeyJustPressed(Keys.Enter) && IsViewingResults)
                    _skip = true;
            });
        }

        public static void Render()
        {
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Vector2(GameUtils.WindowWidth / 3, 0), null, Color.Beige * _panelAlpha, 0f, Vector2.Zero, new Vector2(GameUtils.WindowWidth / 3, GameUtils.WindowHeight), default, 0f);

            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/grades");
            TankGame.SpriteRenderer.Draw(tex, new Vector2(GameUtils.WindowWidth / 3 * 2, 200), ParseGradeRect(Grade), Color.White, _gradeRotation, new Vector2(64, 64), 1f, default, 0f);

            /*for (int i = 0; i < TierDisplays.Count; i++)
            {
                var display = TierDisplays.ElementAt(i);


            }*/
            var txt = "Press 'Enter' to exit.";
            var measure = TankGame.TextFont.MeasureString(txt);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, txt, new Vector2(GameUtils.WindowWidth / 2, GameUtils.WindowHeight - 8), Color.Black, new Vector2(1f), 0f, new Vector2(measure.X / 2, measure.Y));
        }
        // TODO: probably support multiple players L + ratio me
        public static void SetStats(Campaign campaign, PlayerTank.DeterministicPlayerStats stats, Dictionary<TankTier, int> killCounts)
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
            TotalPossibleLives = PlayerTank.StartingLives + campaign.Properties.ExtraLivesMissions.Length;
        }

        public static Grade FormulateGradeLevel()
        {
            var grade = Grade.APlus;

            float shellRatio = (float)ShellHits / ShellsFired;
            float mineRatio = (float)MineHits / MinesLaid;
            float lifeRatio = (float)LivesRemaining / TotalPossibleLives;

            if (float.IsNaN(shellRatio))
                shellRatio = 0f;
            if (float.IsNaN(mineRatio))
                mineRatio = 0f;

            // (min, max]
            bool isBetween(float input, float min, float max) => input >= min && input < max;
            // redundant code but whatever i love men
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
            else if (mineRatio < 0.25f)
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

            if (grade > Grade.FMinus)
                grade = Grade.FMinus;

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
