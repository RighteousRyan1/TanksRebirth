using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;

namespace TanksRebirth.Achievements
{
    public static class VanillaAchievements
    {
        public static AchievementRepository Repository { get; } = new();

        private static Achievement[] _achievements = {
            new("Simply a start", "Destroy your first 100 tanks.", () => TankGame.GameData.TotalKills >= 100),
            new("A true warrior", "Destroy 1000 tanks.", () => TankGame.GameData.TotalKills >= 1000),
            new("Genocide", "Destroy 10,000 tanks total.", () => TankGame.GameData.TotalKills >= 10000),
            new("There are no equals", "Destroy 100,000 tanks total!", () => TankGame.GameData.TotalKills >= 100000),

            new("Child Abuse", "Destroy 100 Brown Tanks.", () => TankGame.GameData.TankKills[TankTier.Brown] >= 100),
            new("Catch them all", "Destroy 100 Ash tanks.", () => TankGame.GameData.TankKills[TankTier.Ash] >= 100),
            new("Enlisted", "Destroy 100 Marine tanks.", () => TankGame.GameData.TankKills[TankTier.Marine] >= 100),
            new("Those weren't submarines...", "Destroy 100 Yellow Tanks.", () => TankGame.GameData.TankKills[TankTier.Yellow] >= 100),
            new("Bratatatat", "Destroy 100 Pink Tanks.", () => TankGame.GameData.TankKills[TankTier.Pink] >= 100),
            new("Bratatatat 2", "Destroy 100 Purple Tanks.", () => TankGame.GameData.TankKills[TankTier.Purple] >= 100),
            new("Outsmarting the Calculator", "Destroy 100 Green Tanks.", () => TankGame.GameData.TankKills[TankTier.Green] >= 100),
            new("Now you see me...", "Destroy 100 White Tanks.", () => TankGame.GameData.TankKills[TankTier.White] >= 100),
            new("Conquering the World", "Destroy 100 Black Tanks.", () => TankGame.GameData.TankKills[TankTier.Black] >= 100),

            new("Lesser Child Abuse", "Destroy 100 Bronze Tanks", () => TankGame.GameData.TankKills[TankTier.Bronze] >= 100),
            new("It's no Use!", "Destroy 100 Silver Tanks", () => TankGame.GameData.TankKills[TankTier.Silver] >= 100),
            new("Gemmin' it up", "Destroy 100 Sapphire Tanks", () => TankGame.GameData.TankKills[TankTier.Sapphire] >= 100),
            new("The Glint in your Eye", "Destroy 100 Ruby Tanks", () => TankGame.GameData.TankKills[TankTier.Ruby] >= 100),
            new("Fast and Furious", "Destroy 100 Citrine Tanks", () => TankGame.GameData.TankKills[TankTier.Citrine] >= 100),
            new("Purple Pain", "Destroy 100 Amethyst Tanks", () => TankGame.GameData.TankKills[TankTier.Amethyst] >= 100),
            new("Perfect for Trading", "Destroy 100 Emerald Tanks", () => TankGame.GameData.TankKills[TankTier.Emerald] >= 100),
            new("Found You!", "Destroy 100 Gold Tanks", () => TankGame.GameData.TankKills[TankTier.Gold] >= 100),
            new("Unbreakable Will", "Destroy 100 Obsidian Tanks", () => TankGame.GameData.TankKills[TankTier.Obsidian] >= 100),

            new("Will you be mine?", "Kill 100 Tanks with mines.", () => TankGame.GameData.MineKills >= 100),
            new("Simple Geometry", "Destroy 100 tanks with bullets that have ricocheted at least once.", () => TankGame.GameData.BounceKills >= 100),

            new("Playing with fire", "Encounter a Marine tank, a tank with fast, flame-trailed bullets."),
            new("Double Whammy", "Encounter a Green tank, a tank that can precisely calculate double bounces."),
            new("Camouflage", "Encounter a White tank, a tank which can go invisible and sneak up on you."),
            new("Black Attack!", "Encounter a Black tank, a tank which goes fast, shoots fast, and dodges bullets well."),

            new("Doomed to failure", "Destroy yourself with your own mine."),
            new("Just an accident", "Destroy yourself with your own bullet."),
            new("Beyond Saving", "Destroy yourself with your own bullet... 10 times."),
            new("Soldier in disguise", "Destroy a tank with a bullet that traveled through a teleporter."),

            new("See through the dragon's eyes", "Complete a campaign in third-person mode."),

            new("You found me!", "It's a secret... Shhh...."), // uh, click title logo

            // get some new ideas later normie
        };

        public static void InitializeToRepository()
        {
            // If an achievement from _achievemnts already exists in the repository, don't add it again.
            if (Repository.GetAchievements().Intersect(_achievements).Any())
                return;
            
            foreach (var ach in _achievements)
                Repository.AddAchievement(ach);
        }
    }
}
