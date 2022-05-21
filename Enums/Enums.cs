namespace TanksRebirth.Enums
{
    /// <summary>Serves the purpose of parsing tiers into strings.</summary>
    public enum TankTier : byte
    {
        None,
        Random,
        Brown,
        Ash,
        Marine,
        Yellow,
        Pink,
        Purple,
        Green,
        White,
        Black,

        // here separates the vanilla tanks from the master mod tanks

        Bronze,
        Silver,
        Sapphire,
        Citrine,
        Ruby,
        Amethyst,
        Emerald,
        Gold,
        Obsidian,

        // here separates the master mod tanks from the advanced mod tanks

        Granite,
        Bubblegum,
        Water,
        Tiger,
        Crimson,
        Fade,
        Creeper,
        Gamma,
        Marble,

        Explosive,
        Electro,
        RocketDefender,
        Assassin,
        Commando
    }

    public enum PlayerType : byte
    {
        Blue,
        Red
    }

    public enum ShellType : byte
    {
        Player,
        Standard,
        Rocket,
        TrailedRocket,
        Supressed,
        Explosive
    }

    public enum MenuMode : byte
    {
        MainMenu,
        PauseMenu,
        IngameMenu,
        LevelEditorMenu
    }

    public enum TankTeam
    {
        NoTeam = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6,
        Cyan = 7,
        Magenta = 8
    }

    public enum MissionEndContext
    {
        Lose,
        Win,
        CampaignCompleteMajor,
        CampaignCompleteMinor
    }
}