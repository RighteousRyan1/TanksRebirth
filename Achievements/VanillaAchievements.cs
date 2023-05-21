using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.Achievements
{
    public static class VanillaAchievements
    {
        public static AchievementRepository Repository { get; } = new();

        private static Achievement[] _achievements = {
            #region Tank Genocide
            new("Simply a start", "Destroy your first 100 tanks.", () => TankGame.GameData.TotalKills >= 100),
            new("A true warrior", "Destroy 1000 tanks.", () => TankGame.GameData.TotalKills >= 1000),
            new("Genocide", "Destroy 10,000 tanks total.", () => TankGame.GameData.TotalKills >= 10000),
            new("There are no equals", "Destroy 100,000 tanks total!", () => TankGame.GameData.TotalKills >= 100000),

            new("Child Abuse", "Destroy 100 Brown Tanks.", () => TankGame.GameData.TankKills[TankID.Brown] >= 100),
            new("Catch them all", "Destroy 100 Ash tanks.", () => TankGame.GameData.TankKills[TankID.Ash] >= 100),
            new("Enlisted", "Destroy 100 Marine tanks.", () => TankGame.GameData.TankKills[TankID.Marine] >= 100),
            new("Those weren't submarines...", "Destroy 100 Yellow Tanks.", () => TankGame.GameData.TankKills[TankID.Yellow] >= 100),
            new("Bratatatat", "Destroy 100 Pink Tanks.", () => TankGame.GameData.TankKills[TankID.Pink] >= 100),
            new("Bratatatat 2", "Destroy 100 Purple Tanks.", () => TankGame.GameData.TankKills[TankID.Violet] >= 100),
            new("Outsmarting the Calculator", "Destroy 100 Green Tanks.", () => TankGame.GameData.TankKills[TankID.Green] >= 100),
            new("Now you see me...", "Destroy 100 White Tanks.", () => TankGame.GameData.TankKills[TankID.White] >= 100),
            new("Conquering the World", "Destroy 100 Black Tanks.", () => TankGame.GameData.TankKills[TankID.Black] >= 100),

            new("Lesser Child Abuse", "Destroy 100 Bronze Tanks", () => TankGame.GameData.TankKills[TankID.Bronze] >= 100),
            new("It's no Use!", "Destroy 100 Silver Tanks", () => TankGame.GameData.TankKills[TankID.Silver] >= 100),
            new("Gemmin' it up", "Destroy 100 Sapphire Tanks", () => TankGame.GameData.TankKills[TankID.Sapphire] >= 100),
            new("The Glint in your Eye", "Destroy 100 Ruby Tanks", () => TankGame.GameData.TankKills[TankID.Ruby] >= 100),
            new("Fast and Furious", "Destroy 100 Citrine Tanks", () => TankGame.GameData.TankKills[TankID.Citrine] >= 100),
            new("Purple Pain", "Destroy 100 Amethyst Tanks", () => TankGame.GameData.TankKills[TankID.Amethyst] >= 100),
            new("Perfect for Trading", "Destroy 100 Emerald Tanks", () => TankGame.GameData.TankKills[TankID.Emerald] >= 100),
            new("Found You!", "Destroy 100 Gold Tanks", () => TankGame.GameData.TankKills[TankID.Gold] >= 100),
            new("Unbreakable Will", "Destroy 100 Obsidian Tanks", () => TankGame.GameData.TankKills[TankID.Obsidian] >= 100),

            new("In the eyes of the maker", "Kill a Green Tank with a mine."),
            new("Close and Personal", "Kill a black tank within 50 units of it."),

            new("Will you be mine?", "Kill 100 Tanks with mines.", () => TankGame.GameData.MineKills >= 100),
            new("Simple Geometry", "Destroy 100 tanks with bullets that have ricocheted at least once.", () => TankGame.GameData.BounceKills >= 100),
            #endregion
            #region Meetups
            new("Playing with fire", "Encounter a Marine tank, a tank with fast, flame-trailed bullets."),
            new("Double Whammy", "Encounter a Green tank, a tank that can precisely calculate double bounces."),
            new("Camouflage", "Encounter a White tank, a tank which can go invisible and sneak up on you."),
            new("Black Attack!", "Encounter a Black tank, a tank which goes fast, shoots fast, and dodges bullets well."),
            #endregion
            #region Self-Deprecation
            new("Doomed to failure", "Destroy yourself with your own mine."),
            new("Just an accident", "Destroy yourself with your own bullet."),
            new("Beyond Saving", "Destroy yourself with your own bullet... 10 times."),
            new("I teleported bullets... For 3 days!", "Destroy a tank with a bullet that traveled through a teleporter."),
            #endregion
            #region Misc
            new("See through the dragon's eyes", "Complete a campaign in third-person mode."),
            new("TANKS a lot!", "Be destroyed by one of your teammates."),
            new("I cannon believe you've done this.", "Destroy one of your teammates."),
            #endregion
            #region Secrets
            new("Nice tunes, bro", "This is a hidden achievement!"), // click main menu logo
            #endregion
            #region Time Played
            new("A Good Start", "Play for your first 10 minutes.", 
                () => TankGame.CurrentSessionTimer.Elapsed + TankGame.GameData.TimePlayed >= TimeSpan.FromMinutes(10)),
            new("Addicted yet?", "Play for an hour of your life.",
                () => TankGame.CurrentSessionTimer.Elapsed + TankGame.GameData.TimePlayed >= TimeSpan.FromHours(1)),
            new("Enjoyment Supreme", "Play for 3 hours.",
                () => TankGame.CurrentSessionTimer.Elapsed + TankGame.GameData.TimePlayed >= TimeSpan.FromHours(3)),
            new("Entertainment Value", "Play for 5 hours.",
                () => TankGame.CurrentSessionTimer.Elapsed + TankGame.GameData.TimePlayed >= TimeSpan.FromHours(5)),
            new("Please go outside", "Play for 10 hours!",
                () => TankGame.CurrentSessionTimer.Elapsed + TankGame.GameData.TimePlayed >= TimeSpan.FromHours(10)),
            new("Grass exists, bro", "Play for 100 hours!",
                () => TankGame.CurrentSessionTimer.Elapsed + TankGame.GameData.TimePlayed >= TimeSpan.FromHours(100)),
            #endregion
            #region Creation!
            new("The Power of Creation", "Create a campaign."),
            new("What are these?", "Create a mission."),
            #endregion
            #region Blood, Sweat, and Tears
            new("A winner is you", "Complete the Vanilla campaign."),
            new("Is winning this easy?", "Complete 5 custom-made campaigns."),
            new("All your base are belong to us", "Complete 10 custom-made campaigns."),
            #endregion
            // get some new ideas later normie
        };

        public static void InitializeToRepository()
        {
            // If an achievement from _achievemnts already exists in the repository, don't add it again.
            if (Repository.GetAchievements().Intersect(_achievements).Any())
                return;
            
            foreach (var achievement in _achievements)
                Repository.AddAchievement(achievement);
        }
    }
}
